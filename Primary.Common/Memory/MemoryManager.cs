using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Common.Memory
{
    public static class MemoryManager
    {
        private static Dictionary<string, MemoryStatistic> _statistics = new Dictionary<string, MemoryStatistic>();

        public static void IncrementUnique(string key, ulong value)
        {
            ref MemoryStatistic statistic = ref CollectionsMarshal.GetValueRefOrAddDefault(_statistics, key, out bool exists);
            if (!exists)
            {
                statistic.UsedMemory = value;
                statistic.UniqueAllocations = 1;
            }
            else
            {
                statistic.UsedMemory += value;
                statistic.UniqueAllocations++;
            }
        }

        public static void DecrementUnique(string key, ulong value)
        {
            ref MemoryStatistic statistic = ref CollectionsMarshal.GetValueRefOrNullRef(_statistics, key);
            if (!Unsafe.IsNullRef(ref statistic))
            {
                statistic.UsedMemory = (statistic.UsedMemory < value) ? 0 : statistic.UsedMemory - value;
                statistic.UniqueAllocations = Math.Max(statistic.UniqueAllocations - 1, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryStatistic GetStatistic(string key)
        {
            if (_statistics.TryGetValue(key, out MemoryStatistic statistic))
                return statistic;
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<string, MemoryStatistic>.Enumerator GetEnumerator()
        {
            return _statistics.GetEnumerator();
        }
    }

    public struct MemoryStatistic
    {
        public ulong UsedMemory;
        public int UniqueAllocations;
    }
}
