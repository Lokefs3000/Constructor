using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Rendering.Assets;

namespace PrimaryEditor.Search.Database
{
    public sealed class StringMatcher
    {
        private readonly Dictionary<StringMatchKey, int> _strings;
        private readonly Dictionary<TriagramIndex, HashSet<StringMatchKey>> _index;

        private readonly Dictionary<StringMatchKey, int> _dictToSearchWith;

        internal StringMatcher()
        {
            _strings = new Dictionary<StringMatchKey, int>();
            _index = new Dictionary<TriagramIndex, HashSet<StringMatchKey>>();

            _dictToSearchWith = new Dictionary<StringMatchKey, int>();
        }

        public void AddString(StringMatchKey str)
        {
            ref int duplicateCount = ref CollectionsMarshal.GetValueRefOrAddDefault(_strings, str, out bool exists);
            if (exists)
            {
                ++duplicateCount;
                return;
            }

            duplicateCount = 1;

            for (int i = -2; i < str.Value.Length + 2; ++i)
            {
                TriagramIndex index = TriagramIndex.CreateAt(str.Value.AsSpan(), i);
                ref HashSet<StringMatchKey>? set = ref CollectionsMarshal.GetValueRefOrAddDefault(_index, index, out exists);

                if (!exists || set == null)
                {
                    set = [str];
                }
                else
                {
                    set.Add(str);
                }
            }
        }

        public void RemoveString(StringMatchKey str)
        {
            ref int duplicateCount = ref CollectionsMarshal.GetValueRefOrNullRef(_strings, str);
            if (!Unsafe.IsNullRef(in duplicateCount))
            {
                if (--duplicateCount == 0)
                {
                    _strings.Remove(str);

                    for (int i = -2; i < str.Value.Length + 2; ++i)
                    {
                        TriagramIndex index = TriagramIndex.CreateAt(str.Value.AsSpan(), i);
                        ref HashSet<StringMatchKey> set = ref CollectionsMarshal.GetValueRefOrNullRef(_index, index);

                        if (!Unsafe.IsNullRef(in set))
                        {
                            if (set.Remove(str) && set.Count == 0)
                            {
                                _index.Remove(index);
                            }
                        }
                    }
                }
            }
        }

        public void QueryForAllStrings(ref RentedList<StringMatchKey> outputList)
        {
            foreach (var (value, _) in _strings)
            {
                outputList.Add(value);
            }
        }

        public void SearchForString(string str, ref RentedList<StringMatchKey> valuesThatMatch, int maxResults = int.MaxValue)
        {
            Guard.IsGreaterThan(maxResults, 0);

            int maxCollisionCount = 0;
            int totalCollisions = 0;
            int numWithMultipleCollisions = 0;

            using RentedList<(int Count, StringMatchKey Value)> collisionCounts = new RentedList<(int Count, StringMatchKey Value)>();
            for (int i = str.Length; i >= -2; i--)
            {
                TriagramIndex triagram = TriagramIndex.CreateAt(str.AsSpan(), i);
                if (_index.TryGetValue(triagram, out HashSet<StringMatchKey>? matches))
                {
                    foreach (StringMatchKey match in matches)
                    {
                        ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictToSearchWith, match, out bool exists);
                        if (!exists)
                        {
                            index = collisionCounts.Count;
                            collisionCounts.Add((1, match));

                            maxCollisionCount = Math.Max(maxCollisionCount, 1);
                        }
                        else
                        {
                            ref (int Count, StringMatchKey Value) tuple = ref collisionCounts[index];

                            if (tuple.Count == 1)
                                ++numWithMultipleCollisions;

                            ++tuple.Count;
                            ++totalCollisions;

                            maxCollisionCount = Math.Max(maxCollisionCount, tuple.Count);
                        }
                    }
                }
            }

            _dictToSearchWith.Clear();
            if (collisionCounts.IsEmpty)
                return;
            
            collisionCounts.AsSpan().Sort(static (x, y) =>
            {
                int r;
                if ((r = x.Count.CompareTo(y.Count)) != 0)
                    return r;
                if ((r = x.Value.Value.CompareTo(y.Value.Value, StringComparison.Ordinal)) != 0)
                    return r;
                return x.Value.Id.CompareTo(y.Value.Id);
            });

            int acceptedMinCount = collisionCounts[^1].Count == 1 ? 1 : (int)Math.Ceiling(totalCollisions / (double)numWithMultipleCollisions) + 1;
            for (int i = collisionCounts.Count - 1; i >= 0; i--)
            {
                (int collisionCount, StringMatchKey matchedValue) = collisionCounts[i];
                if (collisionCount < acceptedMinCount)
                {
                    break;
                }
                else
                {
                    valuesThatMatch.Add(matchedValue);
                }
            }
        }

        private readonly record struct TriagramIndex(char Left, char Middle, char Right)
        {
            public override int GetHashCode() => HashCode.Combine(Left, Middle, Right);

            public static TriagramIndex CreateAt(ReadOnlySpan<char> chars, int index)
            {
                Guard.IsInRange(index, -2, chars.Length + 2);

                if (chars.Length < 3)
                {
                    char left = (++index < 0 || index >= chars.Length) ? ' ' : chars[index];
                    char middle = (++index < 0 || index >= chars.Length) ? ' ' : chars[index];
                    char right = (index < 0 || index >= chars.Length) ? ' ' : chars[index];

                    return new TriagramIndex(char.ToLower(left), char.ToLower(middle), char.ToLower(right));
                }
                else if (index < 0)
                {
                    return index == -2 ? new TriagramIndex(' ', ' ', char.ToLower(chars[0])) : new TriagramIndex(' ', char.ToLower(chars[0]), char.ToLower(chars[1]));
                }
                else if (index >= chars.Length)
                {
                    return index == chars.Length + 1 ? new TriagramIndex(char.ToLower(chars[^1]), ' ', ' ') : new TriagramIndex(char.ToLower(chars[^2]), char.ToLower(chars[^1]), ' ');
                }
                else
                {
                    // Yeah this actually kinda sucks
                    return Unsafe.ReadUnaligned<TriagramIndex>(ref Unsafe.As<char, byte>(ref chars.DangerousGetReferenceAt(index)));
                }
            }
        }
    }

    public readonly record struct StringMatchKey(string Value, AssetId Id) : IEquatable<StringMatchKey>
    {
        public override int GetHashCode() => HashCode.Combine(Value.GetDjb2HashCode(), Id);
    }
}
