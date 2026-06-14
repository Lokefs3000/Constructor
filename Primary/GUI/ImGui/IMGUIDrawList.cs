using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Pooling;
using Primary.RHI;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

using ImGuiIndex = ushort;

namespace Primary.GUI.ImGui
{
    public class ImGuiDrawList : IDisposable
    {
        private bool _disposedValue;

        private ImGuiFont _primaryFont;

        private Vector2 _whiteUVs;

        private List<ImGuiDrawCmd> _commands;
        private UnsafeList<ImGuiVertex> _vertices;
        private UnsafeList<ImGuiIndex> _indices;

        private Stack<Vector4> _clipRects;
        private bool _suspendCommands;

        private RHITexture? _currentTexture;

        internal ImGuiDrawList(ImGuiFont font)
        {
            _primaryFont = font;

            _whiteUVs = (new Vector2(font.AtlasTexture.Width, font.AtlasTexture.Height) - Vector2.One) / new Vector2(font.AtlasTexture.Width, font.AtlasTexture.Height);

            _commands = new List<ImGuiDrawCmd>();
            _vertices = new UnsafeList<ImGuiVertex>(32 * 4);
            _indices = new UnsafeList<ImGuiIndex>(32 * 6);

            _clipRects = new Stack<Vector4>();
            _suspendCommands = false;

            _currentTexture = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~ImGuiDrawList()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear()
        {
            _commands.Clear();
            _vertices.Clear();
            _indices.Clear();

            _clipRects.Clear();
            _suspendCommands = false;

            _currentTexture = null;
        }

        private void PushNewCmd(RHITexture texture)
        {
            _commands.Add(new ImGuiDrawCmd(GetTopClipRect(), _indices.Count, texture));
            _currentTexture = texture;
        }

        private Vector4 GetTopClipRect() => _clipRects.TryPeek(out Vector4 clipRect) ? clipRect : new Vector4(0.0f, 0.0f, float.PositiveInfinity, float.PositiveInfinity);

        public void PushClip(Vector4 clipRect, bool clipToCurrentClip = false)
        {
            if (clipToCurrentClip && _clipRects.TryPeek(out Vector4 top))
            {
                clipRect = Boundaries.Clip(new Boundaries(top), new Boundaries(clipRect)).AsVector4();
            }

            _clipRects.Push(clipRect);

            _suspendCommands = clipRect.Z - clipRect.X <= 0.5f || clipRect.W - clipRect.Y <= 0.5f;
            _currentTexture = null;
        }

        public void PopClip()
        {
            if (_clipRects.TryPop(out _))
            {
                Vector4 clipRect = GetTopClipRect();

                _suspendCommands = clipRect.Z - clipRect.X <= 0.5f || clipRect.W - clipRect.Y <= 0.5f;
                _currentTexture = null;
            }
            else
                _suspendCommands = false;
        }

        public void DrawLine(Vector2 from, Vector2 to, uint color = 0xffffffff, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            Vector2 min, max;
            if (thickness > 1.5f)
            {
                thickness *= 0.5f;

                Vector2 m = to - from;
                Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * thickness;

                min = from + p;
                max = to - p;
            }
            else
            {
                Vector2 m = to - from;
                Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * 0.5f;

                min = from + p;
                max = to - p;
            }

            int baseCount = _vertices.Count;

            _vertices.Add(new ImGuiVertex { Position = min, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(max.X, min.Y), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(min.X, max.Y), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = max, UV = _whiteUVs, Color = color });

            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 2));
        }

        public void DrawRect(Vector2 min, Vector2 max, uint color = 0xffffffff, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            thickness *= 0.5f;

            int baseCount = _vertices.Count;

            min += Vector2.One;

            Vector128<float> outerTlbr = Vector128.Create(min.X, min.Y, max.X, max.Y);
            Vector128<float> innerTlbr = Vector128.Create(min.X, min.Y, max.X, max.Y);

            Vector128<float> add = Vector128.Create(-thickness, -thickness, thickness, thickness);
            outerTlbr += add;
            innerTlbr -= add;

            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref outerTlbr), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(outerTlbr[2], outerTlbr[1]), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(outerTlbr[0], outerTlbr[3]), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref outerTlbr), 1), UV = _whiteUVs, Color = color });

            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref innerTlbr), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(innerTlbr[2], innerTlbr[1]), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(innerTlbr[0], innerTlbr[3]), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref innerTlbr), 1), UV = _whiteUVs, Color = color });

            /*
                0 ------ 1
                | 4 -- 5 |
                | |    | |
                | 6 -- 7 |
                2 ------ 3
            */

            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));

            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));

            _indices.Add((ushort)(baseCount + 6));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 7));

            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 7));
        }

        public void DrawFilledRect(Vector2 min, Vector2 max, uint color = 0xffffffff) =>
            DrawFilledQuad(min, new Vector2(max.X, min.Y), new Vector2(min.X, max.Y), max, color);

        public void DrawQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, uint color = 0xffffffff, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            thickness *= 0.5f;

            int baseCount = _vertices.Count;

            Vector128<float> tlVec = Vector128.Create(tl.X, tl.Y, tl.X, tl.Y);
            Vector128<float> trVec = Vector128.Create(tr.X, tr.Y, tr.X, tr.Y);
            Vector128<float> blVec = Vector128.Create(bl.X, bl.Y, bl.X, bl.Y);
            Vector128<float> brVec = Vector128.Create(br.X, br.Y, br.X, br.Y);

            Vector64<float> add;
            add = Vector64.Create(-thickness, -thickness);
            tlVec += Vector128.Create(add, -add);

            add = Vector64.Create(thickness, -thickness);
            trVec += Vector128.Create(add, -add);

            add = Vector64.Create(-thickness, thickness);
            trVec += Vector128.Create(add, -add);

            add = Vector64.Create(thickness, thickness);
            trVec += Vector128.Create(add, -add);

            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref tlVec), 1), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref trVec), 1), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref blVec), 1), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector2>(ref brVec), 1), UV = _whiteUVs, Color = color });

            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref tlVec), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref trVec), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref blVec), UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = Unsafe.As<Vector128<float>, Vector2>(ref brVec), UV = _whiteUVs, Color = color });

            /*
                0 ------ 1
                | 4 -- 5 |
                | |    | |
                | 6 -- 7 |
                2 ------ 3
            */

            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));

            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));

            _indices.Add((ushort)(baseCount + 6));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 7));

            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 7));
        }

        public void DrawFilledQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, uint color = 0xffffffff)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            int baseCount = _vertices.Count;

            _vertices.Add(new ImGuiVertex { Position = tl, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = tr, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = bl, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = br, UV = _whiteUVs, Color = color });

            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 2));
        }

        public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, uint color = 0xffffffff, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            thickness *= 0.5f;

            int baseCount = _vertices.Count;

            Vector2 min = Vector2.Min(Vector2.Min(a, b), c);
            Vector2 max = Vector2.Max(Vector2.Max(a, b), c);

            Vector2 center = Vector2.Lerp(min, max, 0.5f);

            Vector2 outerA = a + Vector2.Normalize(center - a);
            Vector2 outerB = b + Vector2.Normalize(center - b);
            Vector2 outerC = c + Vector2.Normalize(center - c);

            _vertices.Add(new ImGuiVertex { Position = outerA, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = outerB, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = outerC, UV = _whiteUVs, Color = color });

            _vertices.Add(new ImGuiVertex { Position = a, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = b, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = c, UV = _whiteUVs, Color = color });

            /*
                     0
                  _/ 4 \_
                _/ _/ \_ \_
               /  5 --- 6  \
              1 ----------- 2
            */

            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));

            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 5));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));

            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 4));
            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)(baseCount + 6));
        }

        public void DrawFilledTriangle(Vector2 a, Vector2 b, Vector2 c, uint color = 0xffffffff)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            int baseCount = _vertices.Count;

            _vertices.Add(new ImGuiVertex { Position = a, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = b, UV = _whiteUVs, Color = color });
            _vertices.Add(new ImGuiVertex { Position = c, UV = _whiteUVs, Color = color });

            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 1));
        }

        public void DrawCircle(Vector2 center, float radius, uint color, int numSegments = 8, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            thickness *= 0.5f;
            numSegments = Math.Max(numSegments, 3);

            float segmentMul = (MathF.PI * 2.0f) / (numSegments++);

            Vector2 p0 = Vector2.Zero;
            for (int i = 0; i < numSegments; i++)
            {
                float rad = i * segmentMul;

                Vector2 p1 = new Vector2(MathF.Sin(rad), MathF.Cos(rad)) * radius;
                if (i == 0)
                {
                    p0 = p1;
                }
                else
                {
                    Vector2 m = p1 - p0;
                    Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * thickness;

                    Vector2 min = p0 + p;
                    Vector2 max = p1 - p;

                    int baseCount = _vertices.Count;

                    _vertices.Add(new ImGuiVertex { Position = min, UV = _whiteUVs, Color = color });
                    _vertices.Add(new ImGuiVertex { Position = new Vector2(max.X, min.Y), UV = _whiteUVs, Color = color });
                    _vertices.Add(new ImGuiVertex { Position = new Vector2(min.X, max.Y), UV = _whiteUVs, Color = color });
                    _vertices.Add(new ImGuiVertex { Position = max, UV = _whiteUVs, Color = color });

                    _indices.Add((ushort)(baseCount + 2));
                    _indices.Add((ushort)baseCount);
                    _indices.Add((ushort)(baseCount + 1));
                    _indices.Add((ushort)(baseCount + 3));
                    _indices.Add((ushort)(baseCount + 1));
                    _indices.Add((ushort)(baseCount + 2));
                }
            }
        }

        public void DrawFilledCircle(Vector2 center, float radius, uint color, int numSegments = 8, float thickness = 1.0f)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            thickness *= 0.5f;
            numSegments = Math.Max(numSegments, 3);

            float segmentMul = (MathF.PI * 2.0f) / (numSegments++);

            int baseCount = _vertices.Count;

            Vector2 p0 = Vector2.Zero;

            for (int i = 0; i < numSegments; i++)
            {
                float rad = i * segmentMul;

                Vector2 p1 = center + new Vector2(MathF.Sin(rad), MathF.Cos(rad)) * radius;
                if (i == 0)
                {
                    p0 = p1;

                    _vertices.Add(new ImGuiVertex { Position = center, UV = _whiteUVs, Color = color });
                    _vertices.Add(new ImGuiVertex { Position = p0, UV = _whiteUVs, Color = color });
                }
                else
                {
                    _vertices.Add(new ImGuiVertex { Position = p1, UV = _whiteUVs, Color = color });

                    _indices.Add((ushort)(baseCount + 1 + i));
                    _indices.Add((ushort)baseCount);
                    _indices.Add((ushort)(baseCount + i));
                }
            }
        }

        public void DrawImage(TextureAsset texture, Vector2 min, Vector2 max, Vector2 uvMin, Vector2 uvMax, uint color = 0xffffffff)
        {
            if (texture.Status != ResourceStatus.Success)
                return;
            DrawImageQuad(texture.RawRHITexture!,
               min, new Vector2(max.X, min.Y), new Vector2(min.X, max.Y), max,
               uvMin, new Vector2(uvMax.X, uvMin.Y), new Vector2(uvMin.X, uvMax.Y), uvMax,
               color);
        }

        public void DrawImage(RHITexture texture, Vector2 min, Vector2 max, Vector2 uvMin, Vector2 uvMax, uint color = 0xffffffff) =>
           DrawImageQuad(texture,
               min, new Vector2(max.X, min.Y), new Vector2(min.X, max.Y), max,
               uvMin, new Vector2(uvMax.X, uvMin.Y), new Vector2(uvMin.X, uvMax.Y), uvMax,
               color);

        public void DrawImageQuad(TextureAsset texture, Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, Vector2 uvTl, Vector2 uvTr, Vector2 uvBl, Vector2 uvBr, uint color = 0xffffffff)
        {
            if (texture.Status != ResourceStatus.Success)
                return;
            DrawImageQuad(texture.RawRHITexture!, tl, tr, bl, br, uvTl, uvTr, uvBl, uvBr, color);
        }

        public void DrawImageQuad(RHITexture texture, Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, Vector2 uvTl, Vector2 uvTr, Vector2 uvBl, Vector2 uvBr, uint color = 0xffffffff)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != texture)
                PushNewCmd(texture);

            int baseCount = _vertices.Count;

            _vertices.Add(new ImGuiVertex { Position = tl, UV = uvTl, Color = color });
            _vertices.Add(new ImGuiVertex { Position = tr, UV = uvTr, Color = color });
            _vertices.Add(new ImGuiVertex { Position = bl, UV = uvBl, Color = color });
            _vertices.Add(new ImGuiVertex { Position = br, UV = uvBr, Color = color });

            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 2));
        }

        public void DrawText(Vector2 position, ReadOnlySpan<char> text, uint color = 0xffffffff)
        {
            if (_suspendCommands)
                return;
            if (_currentTexture != _primaryFont.AtlasTexture.RawRHITexture)
                PushNewCmd(_primaryFont.AtlasTexture.RawRHITexture!);

            Vector2 cursor = position;
            for (int i = 0; i < text.Length; ++i)
            {
                char ch = text.DangerousGetReferenceAt(i);
                if (char.IsControl(ch))
                    continue;

                switch (ch)
                {
                    case ' ':
                        {
                            cursor.X += 4.0f;
                            continue;
                        }
                    case '\n':
                        {
                            cursor = new Vector2(position.X, cursor.Y + 8.0f);
                            continue;
                        }
                }

                ImGuiGlyph glyph = _primaryFont.GetGlyph(ch);

                Vector4 planeBounds = glyph.PlaneBounds + new Vector4(cursor.X, cursor.Y, cursor.X, cursor.Y);
                Vector4 atlasUVs = glyph.AtlasUVs;

                cursor.X += glyph.Advance;

                int baseCount = _vertices.Count;

                _vertices.Add(new ImGuiVertex { Position = new Vector2(planeBounds.X, planeBounds.Y), UV = new Vector2(atlasUVs.X, atlasUVs.Y), Color = color });
                _vertices.Add(new ImGuiVertex { Position = new Vector2(planeBounds.Z, planeBounds.Y), UV = new Vector2(atlasUVs.Z, atlasUVs.Y), Color = color });
                _vertices.Add(new ImGuiVertex { Position = new Vector2(planeBounds.X, planeBounds.W), UV = new Vector2(atlasUVs.X, atlasUVs.W), Color = color });
                _vertices.Add(new ImGuiVertex { Position = new Vector2(planeBounds.Z, planeBounds.W), UV = new Vector2(atlasUVs.Z, atlasUVs.W), Color = color });

                _indices.Add((ushort)(baseCount + 2));
                _indices.Add((ushort)baseCount);
                _indices.Add((ushort)(baseCount + 1));
                _indices.Add((ushort)(baseCount + 3));
                _indices.Add((ushort)(baseCount + 1));
                _indices.Add((ushort)(baseCount + 2));
            }
        }

        public ReadOnlySpan<ImGuiDrawCmd> Cmds => _commands.AsSpan();

        public ReadOnlySpan<ImGuiVertex> Vertices => _vertices.AsSpan();
        public ReadOnlySpan<ImGuiIndex> Indices => _indices.AsSpan();

        internal sealed record class Policy(ImGuiFont Font) : IObjectPoolPolicy<ImGuiDrawList>
        {
            public ImGuiDrawList Create() => new ImGuiDrawList(Font);

            public bool Return(ref ImGuiDrawList obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
