using Editor.UI.Datatypes;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public sealed class UIGradientManager
    {
        private Dictionary<UIGradientColor, int> _registeredGradients;
        private UIComputedGradient[] _computedGradients;

        private Vector2 _gradientTextureSize;

        internal UIGradientManager()
        {
            _registeredGradients = new Dictionary<UIGradientColor, int>();
            _computedGradients = Array.Empty<UIComputedGradient>();

            _gradientTextureSize = Vector2.Zero;
        }

        internal void ClearPreviousData()
        {
            if (_registeredGradients.Count > 0)
            {
                _registeredGradients.Clear();
                Array.Clear(_computedGradients);

                _gradientTextureSize = Vector2.Zero;
            }
        }

        internal void ComputeGradientLayouts()
        {
            if (_registeredGradients.Count > 0)
            {
                using RentedArray<PackingRect> rects = RentedArray<PackingRect>.Rent(_registeredGradients.Count, true);
                foreach (var kvp in _registeredGradients)
                {
                    UIGradientColor gradient = kvp.Key;
                    rects[kvp.Value] = new PackingRect(gradient, Vector2.Zero, new Vector2(LinearGradientWidth, LinearGradientHeight), kvp.Value);
                }

                Span<PackingRect> span = rects.Span;
                span.Sort((x, y) => x.Size.Y.CompareTo(y.Size.Y));

                Vector2 pos = Vector2.Zero;
                float maxRowHeight = 0.0f;

                float largestXOffset = 0.0f;

                foreach (ref PackingRect rect in span)
                {
                    if (pos.X + rect.Size.X > MaxTextureRowWidth)
                    {
                        largestXOffset = MathF.Max(largestXOffset, pos.X);

                        pos.X = 0.0f;
                        pos.Y += maxRowHeight;

                        maxRowHeight = 0.0f;
                    }

                    rect.Position = pos;
                    pos.X += rect.Size.X;

                    maxRowHeight = MathF.Max(maxRowHeight, rect.Size.Y);
                }

                if (_computedGradients.Length < span.Length)
                {
                    int size = (int)BitOperations.RoundUpToPowerOf2((uint)span.Length);
                    _computedGradients = new UIComputedGradient[span.Length];
                }

                Vector2 availSize = new Vector2(MathF.Max(largestXOffset, pos.X), pos.Y + maxRowHeight);

                Vector2 mulDiv = Vector2.One / availSize;
                foreach (ref PackingRect rect in span)
                {
                    _computedGradients[rect.Index] = new UIComputedGradient(rect.Gradient, new Boundaries(rect.Position * mulDiv, (rect.Position + rect.Size) * mulDiv), new Boundaries(rect.Position, rect.Position + rect.Size));
                }

                _gradientTextureSize = availSize;
            }
        }

        internal int RegisterGradient(UIGradientColor gradient)
        {
            if (!_registeredGradients.TryGetValue(gradient, out int index))
            {
                _registeredGradients.Add(gradient, _registeredGradients.Count);
            }

            return index;
        }

        internal Boundaries GetGradientUVs(int key)
        {
            return _computedGradients[key].UVs;
        }

        public bool NeedsGradientsGenerated => _registeredGradients.Count > 0;

        public ReadOnlySpan<UIComputedGradient> Gradients => _computedGradients;

        public Vector2 GradientTextureSize => _gradientTextureSize;

        public const int MaxTextureRowWidth = 128;

        public const int LinearGradientWidth = 48;
        public const int LinearGradientHeight = 8;

        private record struct PackingRect(UIGradientColor Gradient, Vector2 Position, Vector2 Size, int Index);
    }

    public readonly record struct UIComputedGradient(UIGradientColor Gradient, Boundaries UVs, Boundaries Region);
}
