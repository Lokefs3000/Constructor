using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text.Wrappers;
using Microsoft.Extensions.ObjectPool;
using Primary.Common.Memory;
using Primary.Profiling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Text
{
    public sealed class TextManager : IDisposable
    {
        private TextShapeCache _textShapeCache;

        private ObjectPool<OverflowTextWrapper> _overflowPool;
        private ObjectPool<WordTextWrapper> _wordPool;

        private Dictionary<TextShapeCacheKey, DeferredShapingData> _deferredData;
        private ConcurrentBag<ShapedTextData> _keptShapingData;

        private LinearBlockAllocator _stringMemory;
        private StringHandleAllocator _stringAllocator;

        private bool _disposedValue;

        internal TextManager()
        {
            _textShapeCache = new TextShapeCache();

            _overflowPool = ObjectPool.Create(new OverflowTextWrapper.Policy());
            _wordPool = ObjectPool.Create(new WordTextWrapper.Policy());

            _deferredData = new Dictionary<TextShapeCacheKey, DeferredShapingData>();
            _keptShapingData = new ConcurrentBag<ShapedTextData>();

            _stringMemory = new LinearBlockAllocator(4096);
            _stringAllocator = new StringHandleAllocator(_stringMemory);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _stringMemory.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        internal void ReturnUsedData()
        {
            if (!_keptShapingData.IsEmpty)
            {
                while (_keptShapingData.TryTake(out ShapedTextData? result))
                {
                    _textShapeCache.ReturnTextData(result);
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        public ShapedTextData ShapeTextDeferred(in TextWrapInfo wrapInfo, UITextOverflow overflow, ReadOnlySpan<char> text)
        {
            StringHandle handle = _stringAllocator.GetStringHandle(text);
            TextShapeCacheKey key = new TextShapeCacheKey(handle.Hash == StringHandle.InvalidHashCode ? handle.Pointer : handle.Hash, wrapInfo.DefaultVisualInfo.Style, wrapInfo.DefaultVisualInfo.FontSize, wrapInfo.MaxExtents.X, overflow);

            ShapedTextData? textData;
            if (handle.Hash != int.MinValue)
            {
                if (_textShapeCache.TryFindInCache(key, out textData))
                    return textData;
            }

            if (_deferredData.TryGetValue(key, out DeferredShapingData shapingData))
                return shapingData.TextData;

            textData = _textShapeCache.GetBlankTextData();

            _deferredData.Add(key, new DeferredShapingData(textData, wrapInfo, handle));
            if (handle.Hash != StringHandle.InvalidHashCode)
                _textShapeCache.StoreDataInCache(key, textData);

            return textData;
        }

        /// <summary>Not thread-safe</summary>
        public ShapedTextData ShapeText(in TextWrapInfo wrapInfo, UITextOverflow overflow, Span<char> text, int hashCode)
        {
            TextShapeCacheKey key = new TextShapeCacheKey(hashCode == StringHandle.InvalidHashCode ? long.MinValue : hashCode, wrapInfo.DefaultVisualInfo.Style, wrapInfo.DefaultVisualInfo.FontSize, wrapInfo.MaxExtents.X, overflow);

            if (hashCode != int.MinValue)
            {
                if (_textShapeCache.TryFindInCache(key, out ShapedTextData? cached))
                {
                    return cached;
                }
            }

            ShapedTextData textData;
            if (_deferredData.Remove(key, out DeferredShapingData shapingData))
            {
                textData = shapingData.TextData;
                text = shapingData.Text.String;
            }
            else
                textData = _textShapeCache.GetBlankTextData();

            textData.InitializeLetterArray(text.Length);

            Vector2 extentsInEms = wrapInfo.MaxExtents / PixelsPerEM;
            switch (overflow)
            {
                case UITextOverflow.Overflow:
                    {
                        OverflowTextWrapper textWrapper = _overflowPool.Get();
                        textWrapper.WrapText(wrapInfo, textData, text);

                        _overflowPool.Return(textWrapper);
                        break;
                    }
                case UITextOverflow.WrapWords:
                    {
                        WordTextWrapper textWrapper = _wordPool.Get();
                        textWrapper.WrapText(wrapInfo, textData, text);

                        _wordPool.Return(textWrapper);
                        break;
                    }
            }

            textData.SortSections();

            if (hashCode != int.MinValue)
                _textShapeCache.StoreDataInCache(key, textData);
            return textData;
        }

        /// <summary>Not thread-safe</summary>
        internal void ShapeAllDeferredTextData()
        {
            if (_deferredData.Count > 0)
            {
                using (new ProfilingScope("ShapeText"))
                {
                    Action<KeyValuePair<TextShapeCacheKey, DeferredShapingData>> callback = (kvp) =>
                    {
                        using (new ProfilingScope("Wrap"))
                        {
                            TextShapeCacheKey key = kvp.Key;
                            DeferredShapingData value = kvp.Value;

                            value.TextData.InitializeLetterArray(value.Text.String.Length);

                            Vector2 extentsInEms = value.WrapInfo.MaxExtents / PixelsPerEM;
                            switch (key.Overflow)
                            {
                                case UITextOverflow.Overflow:
                                    {
                                        OverflowTextWrapper textWrapper = _overflowPool.Get();
                                        textWrapper.WrapText(value.WrapInfo, value.TextData, value.Text.String);

                                        _overflowPool.Return(textWrapper);
                                        break;
                                    }
                                case UITextOverflow.WrapWords:
                                    {
                                        WordTextWrapper textWrapper = _wordPool.Get();
                                        textWrapper.WrapText(value.WrapInfo, value.TextData, value.Text.String);

                                        _wordPool.Return(textWrapper);
                                        break;
                                    }
                            }

                            value.TextData.SortSections();

                            if (value.Text.Hash == StringHandle.InvalidHashCode)
                                _keptShapingData.Add(value.TextData);
                        }
                    };

                    Parallel.ForEach(_deferredData, callback);

                    _deferredData.Clear();
                }
            }
        }

        //Should be scalable externally
        public const float PixelsPerEM = 16.0f;

        private readonly record struct DeferredShapingData(ShapedTextData TextData, TextWrapInfo WrapInfo, StringHandle Text);
    }
}
