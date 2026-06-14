using CommunityToolkit.Diagnostics;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Primary.Pooling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Text
{
    internal sealed class TextShapeCache
    {
        private bool _isEnabled;

        private ObjectPool<ShapedTextData> _textDataPool;

        private List<ShapedTextData> _textDataList;
        private int _textDataIndex;

        private Dictionary<TextShapeCacheKey, ShapedTextData> _cachedData;
        private HashSet<ShapedTextData> _returningData;

        internal TextShapeCache()
        {
            _isEnabled = false;

            _textDataPool = new ObjectPool<ShapedTextData>(new ShapedTextData.Policy());

            _textDataList = new List<ShapedTextData>();
            _textDataIndex = 0;

            _cachedData = new Dictionary<TextShapeCacheKey, ShapedTextData>();
            _returningData = new HashSet<ShapedTextData>();
        }

        private void ChangeEnabledState(bool newState)
        {
            if (_isEnabled == newState)
                return;

            if (_isEnabled)
            {
                foreach (var kvp in _cachedData)
                {
                    ReturnTextData(kvp.Value);
                }

                _cachedData.Clear();
            }

            _isEnabled = newState;
        }

        internal void CullCachedData()
        {
            if (_returningData.Count > 0)
            {
                foreach (ShapedTextData textData in _returningData)
                {
                    ReturnTextData(textData);
                }

                _returningData.Clear();
            }

            //foreach (var (key, value) in _cachedData)
            //{
            //    _returningData.Add(value);
            //}

            _textDataIndex = 0;
        }

        internal ShapedTextData GetBlankTextData()
        {
            //ShapedTextData textData = _textDataPool.Get();
            //_returningData.Add(textData);

            if (_textDataList.Count > _textDataIndex)
            {
                ShapedTextData textData = _textDataList[_textDataIndex++];
                textData.Clear();

                return textData;
            }
            else
            {
                ShapedTextData textData = new ShapedTextData();
                _textDataList.Add(textData);

                ++_textDataIndex;
                return textData;
            }
        }

        internal void ReturnTextData(ShapedTextData textData)
        {
            return;
            Debug.Assert(!_textDataPool.Contains(textData));
            _textDataPool.Return(textData);
        }

        internal void StoreDataInCache(TextShapeCacheKey key, ShapedTextData textData)
        {
            if (!_isEnabled)
                return;

            _cachedData.Add(key, textData);
            _returningData.Remove(textData);
        }

        internal bool TryFindInCache(TextShapeCacheKey key, [NotNullWhen(true)] out ShapedTextData? textData)
        {
            if (_cachedData.TryGetValue(key, out textData))
            {
                _returningData.Remove(textData);
                return true;
            }

            return false;
        }

        public bool IsEnabled { get => _isEnabled; set => ChangeEnabledState(value); }
    }

    internal readonly record struct TextShapeCacheKey(long StringHash, UIFontTypeData TypeData, float BaseTextSize, float MaxWrapWidth, UITextOverflow Overflow)
    {
        public override int GetHashCode() => HashCode.Combine(StringHash, TypeData, BaseTextSize, MaxWrapWidth, Overflow);
    }
}
