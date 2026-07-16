using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using EditorUI.Built;
using EditorUI.Common;
using EditorUI.Memory;
using EditorUI.Text;
using EditorUI.Utility;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Threading;
using Primary.Timing;

namespace EditorUI.Visual.Built
{
    public sealed class Painter : IDisposable
    {
        private readonly GradientManager _gradientManager;
        private readonly TemporaryAllocator _temporaryAllocator;
        private readonly MeshBuilder _meshBuilder;

        private PaintCmd[] _commands;

        private List<MeshBuildCmd> _meshBuildQueue;
        private List<PaintCmd> _commandList;

        private Dictionary<object, ushort> _objectIndices;
        private List<object> _objectList;

        private Dictionary<Rect, ushort> _scissorIndices;
        private List<Rect> _scissorList;

        private Stack<ushort> _scissorStack;
        private Stack<Vector2> _translateStack;

        private Vector2 _translateV2;
        private Vector128<float> _translateV4;

        private int _head;
        private int _itemsActuallyAdded;

        private int _submittedCommands;

        private uint _depth;

        private int _vertexCount;
        private int _indexCount;

        private ushort _currentScissorIndex;
        private Boundaries _currentDrawBounds;
        private bool _localDrawBoundsChanged;

        private bool _skipAllCommands;

        private bool _disposedValue;

        internal Painter(GradientManager gradientManager)
        {
            _gradientManager = gradientManager;
            _temporaryAllocator = new TemporaryAllocator(1024);
            _meshBuilder = new MeshBuilder();

            _commands = new PaintCmd[8];

            _meshBuildQueue = new List<MeshBuildCmd>();
            _commandList = new List<PaintCmd>();

            _objectIndices = new Dictionary<object, ushort>();
            _objectList = new List<object>();

            _scissorIndices = new Dictionary<Rect, ushort>();
            _scissorList = new List<Rect>();

            _scissorStack = new Stack<ushort>();
            _translateStack = new Stack<Vector2>();

            _translateV2 = Vector2.Zero;
            _translateV4 = Vector128<float>.Zero;

            _head = 0;
            _itemsActuallyAdded = 0;

            _submittedCommands = 0;

            _depth = 0;

            _vertexCount = 0;
            _indexCount = 0;

            _currentScissorIndex = 0;
            _currentDrawBounds = Boundaries.Zero;
            _localDrawBoundsChanged = false;

            _skipAllCommands = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _meshBuilder.Dispose();
                    _temporaryAllocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearData()
        {
            _temporaryAllocator.ResetAllocationOffset();
            _meshBuilder.ClearData();

            _meshBuildQueue.Clear();
            _commandList.Clear();

            _objectIndices.Clear();
            _objectList.Clear();

            _scissorIndices.Clear();
            _scissorList.Clear();

            _scissorStack.Clear();
            _translateStack.Clear();

            _translateV2 = Vector2.Zero;
            _translateV4 = Vector128<float>.Zero;

            _head = 0;
            _itemsActuallyAdded = 0;

            _submittedCommands = 0;

            _depth = 0;

            _vertexCount = 0;
            _indexCount = 0;

            _currentScissorIndex = 0;
            _currentDrawBounds = Boundaries.Zero;
            _localDrawBoundsChanged = false;

            _skipAllCommands = false;
        }

        internal JobHandle FinishPaint()
        {
            if (_itemsActuallyAdded < 8)
            {
                for (int i = 0; i < _head; ++i)
                {
                    _commandList.Add(_commands[i]);
                }
            }
            else
            {
                int index = _head;
                IncrementIndex(ref index);

                for (int i = 0; i < _commands.Length; ++i)
                {
                    _commandList.Add(_commands[index]);
                    IncrementIndex(ref index);
                }
            }

            _commandList.Sort(static (x, y) =>
            {
                int r = (x.CommandIndex & ~(0b1111 << 28)).CompareTo(y.CommandIndex & ~(0b1111 << 28));
                if (r != 0)
                    return r;
                r = ((x.CommandIndex >> 28) & 0b1111).CompareTo((y.CommandIndex >> 28) & 0b1111);
                if (r != 0)
                    return r;
                return x.SubmissionIndex.CompareTo(y.SubmissionIndex);
            });

            return _meshBuilder.Build(_vertexCount, _indexCount, _commandList, _meshBuildQueue, _objectList);
        }

        private void AddCommand(PaintCmdType type, int meshIndex, ushort obj0, ushort obj1, ushort obj2, ushort obj3, Boundaries boundaries)
        {
            ++_submittedCommands;

            PaintCmd cmd = new PaintCmd(type, meshIndex, obj0, obj1, obj2, obj3, boundaries, _itemsActuallyAdded, _submittedCommands);

            if (_submittedCommands == 1)
            {
                _commands[_head] = cmd;
                // _commandList.Add(cmd);

                IncrementIndex(ref _head);
                ++_itemsActuallyAdded;
            }
            else
            {
                int compatibleIndex = -1;

                int startIndex = _head;
                DecrementIndex(ref startIndex);

                int index = startIndex;
                int count = 0;

                do
                {
                    ref PaintCmd currentCmd = ref _commands[index];
                    if (IsCmdCompatibleWith(ref currentCmd, ref cmd))
                    {
                        compatibleIndex = index;
                        break;
                    }
                    else if (currentCmd.RenderBounds.IsIntersecting(cmd.RenderBounds))
                    {
                        break;
                    }

                    DecrementIndex(ref index);
                    ++count;

                } while (index != _head && count < _itemsActuallyAdded);

                if (compatibleIndex == -1)
                {
                    if (_itemsActuallyAdded >= _commands.Length)
                        _commandList.Add(_commands[_head]);
                    _commands[_head] = cmd;

                    IncrementIndex(ref _head);
                    // if (_itemsActuallyAdded >= _commands.Length)
                    //     _commandList.Add(_commands[_head]);

                    ++_itemsActuallyAdded;
                }
                else
                {
                    cmd.CommandIndex = _commands[compatibleIndex].CommandIndex | (_commands.Length - GetZeroIndexedValue(compatibleIndex)) << 28;
                    _commands[compatibleIndex].RenderBounds = cmd.RenderBounds;

                    _commandList.Add(cmd);
                }
                // else if (compatibleIndex == startIndex)
                // {
                //     _commandList.Add(cmd);
                // }
                // else
                // {
                //     InsertCommand(cmd, compatibleIndex);
                // }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCmdCompatibleWith(ref PaintCmd a, ref PaintCmd b)
        {
            return a.CmdType == b.CmdType && a.Obj0 == b.Obj0 && a.Obj3 == b.Obj3;
        }

        private void InsertCommand(PaintCmd cmd, int index)
        {
            int lastIndex;
            int currentIndex;

            if (_itemsActuallyAdded >= _commands.Length)
            {
                lastIndex = _head;
                currentIndex = _head;
            }
            else
            {
                lastIndex = 0;
                currentIndex = 0;
            }

            if (index == lastIndex)
            {
                _commands[lastIndex] = cmd;
                _commandList.Add(cmd);
                return;
            }
            else if (_itemsActuallyAdded < _commands.Length)
            {
                for (int i = _itemsActuallyAdded - 1; i >= index; --i)
                {
                    _commands[i + 1] = _commands[i];

                    if (i == index)
                    {
                        _commands[i] = cmd;
                        ++_itemsActuallyAdded;
                        IncrementIndex(ref _head);
                        return;
                    }
                }

                throw new UnreachableException();
            }

            IncrementIndex(ref currentIndex);

            do
            {
                _commands[lastIndex] = _commands[currentIndex];

                if (lastIndex == _head)
                {
                    _commandList.Add(_commands[lastIndex]);
                }

                if (currentIndex == index)
                {
                    _commands[index] = cmd;
                    break;
                }

                lastIndex = currentIndex;
                IncrementIndex(ref currentIndex);
            } while (currentIndex != _head);

            ++_itemsActuallyAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementIndex(ref int index)
        {
            if (++index >= _commands.Length)
                index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementIndex(ref int index)
        {
            if (index == 0)
                index = _commands.Length;
            --index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetZeroIndexedValue(int index)
        {
            return _itemsActuallyAdded < 8 ? index : (index < _head ? (_commands.Length - (_head - index)) : (index - _head));
        }

        private ushort GetIndexForObject(object obj)
        {
            ref ushort index = ref CollectionsMarshal.GetValueRefOrAddDefault(_objectIndices, obj, out bool exists);
            if (!exists)
            {
                index = (ushort)_objectList.Count;
                _objectList.Add(obj);
            }

            return index;
        }

        private ushort GetIndexForScissor(Rect rect)
        {
            ref ushort index = ref CollectionsMarshal.GetValueRefOrAddDefault(_scissorIndices, rect, out bool exists);
            if (!exists)
            {
                index = (ushort)_scissorList.Count;
                _scissorList.Add(rect);
            }

            return index;
        }

        internal void PushClip(Rect clipRect)
        {
            clipRect.X = (int)(clipRect.X + _translateV2.X);
            clipRect.Y = (int)(clipRect.Y + _translateV2.Y);

            if (_scissorStack.Count > 0)
            {
                Rect parentClip = _scissorList[_currentScissorIndex];

                Int2 newPosition = Int2.Max(clipRect.Position, parentClip.Position);
                clipRect = new Rect(newPosition, Int2.Min(clipRect.Size, Int2.Max(parentClip.Size - (parentClip.Position - newPosition), Int2.Zero)));
            }

            _skipAllCommands = Int2.LessThanOrEqualAny(clipRect.Size, Int2.Zero);
            _currentDrawBounds = _skipAllCommands ? Boundaries.Zero : clipRect.AsBoundaries();
            _localDrawBoundsChanged = true;

            _scissorStack.Push(_currentScissorIndex);
            _currentScissorIndex = GetIndexForScissor(clipRect);
        }

        internal void PopClip()
        {
            if (_scissorStack.TryPop(out ushort result))
            {
                Rect clipRect = _scissorList[result];

                _skipAllCommands = Int2.LessThanOrEqualAny(clipRect.Size, Int2.Zero);
                _currentDrawBounds = _skipAllCommands ? Boundaries.Zero : clipRect.AsBoundaries();
                _localDrawBoundsChanged = true;

                _currentScissorIndex = result;
            }
        }

        internal void PushTranslate(Vector2 translation)
        {
            if (_translateStack.TryPeek(out Vector2 result))
            {
                translation += result;
            }

            _translateStack.Push(translation);

            _translateV2 = translation;
            _translateV4 = Vector128.Create(GetVector64FromVector2(translation));
        }

        internal void PopTranslate()
        {
            if (_translateStack.TryPop(out _))
            {
                if (_translateStack.TryPeek(out Vector2 result))
                {
                    _translateV2 = result;
                    _translateV4 = Vector128.Create(GetVector64FromVector2(result));
                }
                else
                {
                    _translateV2 = Vector2.Zero;
                    _translateV4 = Vector128<float>.Zero;
                }

                _localDrawBoundsChanged = true;
            }
        }

        internal void SetGlobalTranslation(Vector2 translation)
        {
            _translateV2 = translation;
            _translateV4 = Vector128.Create(GetVector64FromVector2(translation));

            _localDrawBoundsChanged = true;

            _translateStack.Clear();
            _translateStack.Push(translation);
        }

        internal void AddPoints(ReadOnlySpan<Vector2> points, Paint paint, float radius)
        {
            if (_skipAllCommands)
                return;

            if (points.IsEmpty || radius < 1.0f)
                return;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside: radius -= paint.StrokeWidth; break;
                    case StrokePosition.Middle: radius -= paint.StrokeWidth * 0.5f; break;
                }
            }

            SpanWriter writer = _temporaryAllocator.GetWritableSpan(points.Length * Unsafe.SizeOf<float>() * 6);
            writer.Write(radius);

            Boundaries boundaries = default;
            Vector128<float> radiusVector = Vector128.Create(radius) * Vector128.Create(-0.5f, -0.5f, 0.5f, 0.5f);

            for (int i = 0; i < points.Length; ++i)
            {
                Vector2 p = points[i] + _translateV2;

                Vector128<float> minMax = Vector128.Create(GetVector64FromVector2(p)) + radiusVector;
                if (i == 0)
                    boundaries = GetBoundariesFromVector128(minMax);
                else
                    boundaries = Boundaries.Union(boundaries, GetBoundariesFromVector128(minMax));

                minMax.StoreUnsafe(ref writer.Get<float>(4));
                p.StoreUnsafe(ref writer.Get<float>(2));
            }

            if (!_currentDrawBounds.IsIntersecting(boundaries))
                return;

            int vertexCount = points.Length * 4;
            int indexCount = points.Length * 6;

            _vertexCount += vertexCount;
            _indexCount += indexCount;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Points, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), vertexCount, indexCount));

            AddCommand(PaintCmdType.Points, meshIndex, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, boundaries);
        }

        internal void AddLines(ReadOnlySpan<Vector2> points, Paint paint, LinePaintMode paintMode, float thickness)
        {
            if (_skipAllCommands)
                return;

            if (thickness < 1.0f)
                return;

            if (paintMode == LinePaintMode.List)
            {
                if (points.Length % 2 != 0)
                    points = points[..(points.Length - 1)];
            }

            if (points.Length < 2)
                return;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside: thickness -= paint.StrokeWidth; break;
                    case StrokePosition.Middle: thickness -= paint.StrokeWidth * 0.5f; break;
                }
            }

            SpanWriter writer = _temporaryAllocator.GetWritableSpan((paintMode == LinePaintMode.List ? points.Length / 2 : points.Length - 1) * Unsafe.SizeOf<float>() * 12 + Unsafe.SizeOf<float>());
            writer.Write(thickness);

            Boundaries boundaries = default;
            Vector128<float> thicknessVector = Vector128.Create(thickness) * Vector128.Create(-0.5f, 0.5f, 0.5f, -0.5f);

            int vertexCount;
            int indexCount;

            if (paintMode == LinePaintMode.List)
            {
                for (int i = 0; i < points.Length;)
                {
                    Vector2 p0 = points[i++] + _translateV2;
                    Vector2 p1 = points[i++] + _translateV2;

                    Vector2 m = p1 - p0;

                    Vector128<float> p0v = Vector128.Create(GetVector64FromVector2(p0));
                    Vector128<float> p1v = Vector128.Create(GetVector64FromVector2(p1));

                    Vector128<float> p = Vector128.Shuffle(Vector128.Create(GetVector64FromVector2(m)), Vector128.Create(1, 0, 1, 0));
                    p /= float.Sqrt(Vector64.Dot(p.GetLower(), p.GetLower()));
                    p *= thicknessVector;

                    p0v += p;
                    p1v += p;

                    if (i == 0)
                        boundaries = Boundaries.Union(GetBoundariesFromVector128(p0v), GetBoundariesFromVector128(p1v));
                    else
                        boundaries = Boundaries.Union(boundaries, Boundaries.Union(GetBoundariesFromVector128(p0v), GetBoundariesFromVector128(p1v)));

                    p0v.StoreUnsafe(ref writer.Get<float>(4));
                    p1v.StoreUnsafe(ref writer.Get<float>(4));
                    p0.StoreUnsafe(ref writer.Get<float>(2));
                    p1.StoreUnsafe(ref writer.Get<float>(2));
                }

                if (!_currentDrawBounds.IsIntersecting(boundaries))
                    return;

                vertexCount = points.Length / 2 * 4;
                indexCount = points.Length / 2 * 6;
            }
            else
            {
                Vector2 p0 = points[0] + _translateV2;
                for (int i = 1; i < points.Length; ++i)
                {
                    Vector2 p1 = points[i] + _translateV2;

                    Vector2 m = p1 - p0;

                    Vector128<float> p0v = Vector128.Create(GetVector64FromVector2(p0));
                    Vector128<float> p1v = Vector128.Create(GetVector64FromVector2(p1));

                    Vector128<float> p = Vector128.Shuffle(Vector128.Create(GetVector64FromVector2(m)), Vector128.Create(1, 0, 1, 0));
                    p /= float.Sqrt(Vector64.Dot(p.GetLower(), p.GetLower()));
                    p *= thicknessVector;

                    p0v += p;
                    p1v += p;

                    if (i == 0)
                        boundaries = Boundaries.Union(GetBoundariesFromVector128(p0v), GetBoundariesFromVector128(p1v));
                    else
                        boundaries = Boundaries.Union(boundaries, Boundaries.Union(GetBoundariesFromVector128(p0v), GetBoundariesFromVector128(p1v)));

                    p0v.StoreUnsafe(ref writer.Get<float>(4));
                    p1v.StoreUnsafe(ref writer.Get<float>(4));
                    p0.StoreUnsafe(ref writer.Get<float>(2));
                    p1.StoreUnsafe(ref writer.Get<float>(2));

                    p0 = p1;
                }

                if (!_currentDrawBounds.IsIntersecting(boundaries))
                    return;

                vertexCount = (points.Length - 1) * 4;
                indexCount = (points.Length - 1) * 6;
            }

            _vertexCount += vertexCount;
            _indexCount += indexCount;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Lines, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), vertexCount, indexCount));

            AddCommand(PaintCmdType.Lines, meshIndex, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, boundaries);
        }

        internal void AddRectangle(Boundaries boundaries, Paint paint, Vector4 cornerRadius)
        {
            if (_skipAllCommands)
                return;

            if (Vector4.LessThanAll(cornerRadius, Vector4.One))
                cornerRadius.X = -1.0f;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside: boundaries = Boundaries.Grow(boundaries, new Vector2(paint.StrokeWidth)); break;
                    case StrokePosition.Middle: boundaries = Boundaries.Grow(boundaries, new Vector2(paint.StrokeWidth * 0.5f)); break;
                }
            }

            boundaries = GetBoundariesFromVector128(boundaries.AsVector128() + _translateV4);

            SpanWriter writer = _temporaryAllocator.GetWritableSpan(Unsafe.SizeOf<float>() * 8);
            boundaries.AsVector128().StoreUnsafe(ref writer.Get<float>(4));
            cornerRadius.StoreUnsafe(ref writer.Get<float>(4));

            if (!_currentDrawBounds.IsIntersecting(boundaries))
                return;

            _vertexCount += 4;
            _indexCount += 6;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Rectangle, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), 4, 6));

            AddCommand(PaintCmdType.Rectangle, meshIndex, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, boundaries);
        }

        internal void AddQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, Paint paint)
        {
            if (_skipAllCommands)
                return;

            Vector128<float> negative = Vector128.Create(0.0f, 0.0f, -0.0f, -0.0f);

            Vector128<float> top = Vector128.Create(GetVector64FromVector2(tl), GetVector64FromVector2(tr)) + _translateV4;
            Vector128<float> bottom = Vector128.Create(GetVector64FromVector2(bl), GetVector64FromVector2(br)) + _translateV4;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside:
                        {
                            top += Vector128.ConvertToSingle(Vector128.Create(-paint.StrokeWidth, -paint.StrokeWidth, paint.StrokeWidth, -paint.StrokeWidth));
                            bottom += Vector128.ConvertToSingle(Vector128.Create(-paint.StrokeWidth, paint.StrokeWidth, paint.StrokeWidth, paint.StrokeWidth));
                            break;
                        }
                    case StrokePosition.Middle:
                        {
                            float halfStrokeWidth = paint.StrokeWidth * 0.5f;

                            top += Vector128.Create(-halfStrokeWidth, -halfStrokeWidth, halfStrokeWidth, -halfStrokeWidth);
                            bottom += Vector128.Create(-halfStrokeWidth, halfStrokeWidth, halfStrokeWidth, halfStrokeWidth);
                            break;
                        }
                }
            }

            Vector128<float> v0 = Vector128.Xor(Vector128.Create(top.GetLower()), negative);
            v0 = Vector128.Min(v0, Vector128.Xor(Vector128.Create(top.GetUpper()), negative));
            v0 = Vector128.Min(v0, Vector128.Xor(Vector128.Create(bottom.GetLower()), negative));
            v0 = Vector128.Min(v0, Vector128.Xor(Vector128.Create(bottom.GetUpper()), negative));

            v0 = Vector128.Xor(v0, negative);

            SpanWriter writer = _temporaryAllocator.GetWritableSpan(Unsafe.SizeOf<float>() * 8 + 1);
            top.StoreUnsafe(ref writer.Get<float>(4));
            bottom.StoreUnsafe(ref writer.Get<float>(4));

            if (!_currentDrawBounds.IsIntersecting(GetBoundariesFromVector128(v0)))
                return;

            _vertexCount += 4;
            _indexCount += 6;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Rectangle, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), 4, 6));

            AddCommand(PaintCmdType.Rectangle, meshIndex, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, GetBoundariesFromVector128(v0));
        }

        internal void AddImage(Boundaries boundaries, Boundaries uvs, Paint paint, object texture)
        {
            if (_skipAllCommands)
                return;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside: boundaries = Boundaries.Grow(boundaries, new Vector2(paint.StrokeWidth)); break;
                    case StrokePosition.Middle: boundaries = Boundaries.Grow(boundaries, new Vector2(paint.StrokeWidth * 0.5f)); break;
                }
            }

            boundaries = GetBoundariesFromVector128(boundaries.AsVector128() + _translateV4);

            SpanWriter writer = _temporaryAllocator.GetWritableSpan(Unsafe.SizeOf<float>() * 8);
            boundaries.AsVector128().StoreUnsafe(ref writer.Get<float>(4));
            uvs.AsVector128().StoreUnsafe(ref writer.Get<float>(4));

            if (!_currentDrawBounds.IsIntersecting(boundaries))
                return;

            _vertexCount += 4;
            _indexCount += 6;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Image, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), 4, 6));

            AddCommand(PaintCmdType.Image, meshIndex, GetIndexForObject(texture), ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, boundaries);
        }

        internal void AddCircle(Vector2 center, float radius, Paint paint)
        {
            if (_skipAllCommands)
                return;

            if (paint.StrokeWidth > 0)
            {
                switch (paint.StrokePosition)
                {
                    case StrokePosition.Outside: radius += paint.StrokeWidth; break;
                    case StrokePosition.Middle: radius += paint.StrokeWidth * 0.5f; break;
                }
            }

            SpanWriter writer = _temporaryAllocator.GetWritableSpan(Unsafe.SizeOf<float>() * 7);

            center += _translateV2;

            Vector128<float> radiusVector = Vector128.Create(radius) * Vector128.Create(-1.0f, -1.0f, 1.0f, 1.0f);
            Vector128<float> minMax = Vector128.Create(GetVector64FromVector2(center)) + radiusVector;

            minMax.StoreUnsafe(ref writer.Get<float>(4));
            center.StoreUnsafe(ref writer.Get<float>(2));
            writer.Write(radius);

            if (!_currentDrawBounds.IsIntersecting(GetBoundariesFromVector128(minMax)))
                return;

            _vertexCount += 4;
            _indexCount += 6;

            int meshIndex = _meshBuildQueue.Count;
            _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Circle, writer.SourceData, writer.Length, _depth++, BuiltPaint.Build(in paint, _gradientManager), 4, 6));

            AddCommand(PaintCmdType.Circle, meshIndex, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, GetBoundariesFromVector128(minMax));
        }

        internal void AddText(Vector2 position, TextShapingData shapingData, Paint paint, Vector2 maxExtents)
        {
            if (_skipAllCommands)
                return;

            ROList<TextShapingSection> sections = shapingData.Sections;
            ROList<TextShapingLine> lines = shapingData.Lines;

            int singleDataSize = Unsafe.SizeOf<Vector2>() * 2 + Unsafe.SizeOf<int>() + Unsafe.SizeOf<ushort>();
            SpanWriter writer = _temporaryAllocator.GetWritableSpan(singleDataSize * sections.Count);

            ushort shapingDataIndex = GetIndexForObject(shapingData);
            BuiltPaint builtPaint = BuiltPaint.Build(in paint, _gradientManager);

            Vector2 sourceExtents = maxExtents;

            position += _translateV2;
            maxExtents += position;

            TextAlignment vertAlign = shapingData.TextBuilder.Alignment & TextAlignment.VerticalAlignment;
            switch (vertAlign)
            {
                case TextAlignment.Top:
                    {
                        position.Y -= sourceExtents.Y - shapingData.TotalSize.Y;
                        break;
                    }
                case TextAlignment.Center:
                    {
                        position.Y -= sourceExtents.Y - shapingData.TotalSize.Y;
                        break;
                    }
            }

            TextAlignment horiAlign = shapingData.TextBuilder.Alignment & TextAlignment.HorizontalAlignment;

            for (int i = 0; i < sections.Count; ++i)
            {
                TextShapingSection section = sections[i];
                TextShapingLine line = lines[section.LinePosition];

                Vector2 sectionPosition = position;
                sectionPosition.Y += line.LineYOffset;

                Vector2 screenSectionPosition = sectionPosition;
                screenSectionPosition.Y -= shapingData.PixelSize;

                Boundaries boundaries = new Boundaries(screenSectionPosition, screenSectionPosition + line.LineSize);
                if (!_currentDrawBounds.IsIntersecting(boundaries))
                    return;

                int vertexCount = section.GlyphsWithActualVisual * 4;
                int indexCount = section.GlyphsWithActualVisual * 6;

                _vertexCount += vertexCount;
                _indexCount += indexCount;

                sectionPosition.StoreUnsafe(ref writer.Get<float>(2));
                maxExtents.StoreUnsafe(ref writer.Get<float>(2));
                writer.Write(shapingDataIndex);
                writer.Write(i);

                int meshIndex = _meshBuildQueue.Count;
                _meshBuildQueue.Add(new MeshBuildCmd(PaintCmdType.Text, writer.SourceData + i * singleDataSize, singleDataSize, _depth++, section.Visual.Paint ?? builtPaint, vertexCount, indexCount));

                AddCommand(PaintCmdType.Text, meshIndex, GetIndexForObject(section.Visual.StyleData), ushort.MaxValue, ushort.MaxValue, _currentScissorIndex, boundaries);
            }
        }

        public int CommandCount => _submittedCommands;

        public MeshBuilder MeshBuilder => _meshBuilder;

        public ROList<PaintCmd> Commands => _commandList;

        public ROList<object> ObjectList => _objectList;
        public ROList<Rect> ScissorList => _scissorList;

        public int VertexCount => _vertexCount;
        public int IndexCount => _indexCount;

        public Vector2 CurrentTranslationV2 => _translateV2;
        public Vector128<float> CurrentTranslationV4 => _translateV4;

        public Boundaries CurrentDrawBoundaries => _currentDrawBounds;
        public bool LocalDrawBoundsChanged { get => _localDrawBoundsChanged; set => _localDrawBoundsChanged = value; }

        public bool IsSkippingCommands => _skipAllCommands;

        public bool IsEmpty => _submittedCommands == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector64<float> GetVector64FromVector2(in Vector2 vector) => Unsafe.BitCast<Vector2, Vector64<float>>(vector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Boundaries GetBoundariesFromVector128(in Vector128<float> vector) => Unsafe.BitCast<Vector128<float>, Boundaries>(vector);
    }

    [StructLayout(LayoutKind.Explicit)]
    public record struct PaintCmd : IEquatable<PaintCmd>
    {
        [FieldOffset(0)] public readonly PaintCmdType CmdType;
        [FieldOffset(1)] public readonly int MeshIndex;
        [FieldOffset(5)] public readonly ushort Obj0;
        [FieldOffset(7)] public readonly ushort Obj1;
        [FieldOffset(9)] public readonly ushort Obj2;
        [FieldOffset(11)] public readonly ushort Obj3;
        [FieldOffset(13)] public Boundaries RenderBounds;
        [FieldOffset(29)] public int CommandIndex;
        [FieldOffset(33)] public int SubmissionIndex;

        public PaintCmd(PaintCmdType cmdType, int meshIndex, ushort obj0, ushort obj1, ushort obj2, ushort obj3, Boundaries renderBounds, int commandIndex, int submissionIndex)
        {
            CmdType = cmdType;
            MeshIndex = meshIndex;
            Obj0 = obj0;
            Obj1 = obj1;
            Obj2 = obj2;
            Obj3 = obj3;
            RenderBounds = renderBounds;
            CommandIndex = commandIndex;
            SubmissionIndex = submissionIndex;
        }

#if DEBUG
        public readonly int DbgCommandIndex => CommandIndex & ~(0b1111 << 28);
        public readonly int DbgCompatibleIndex => (CommandIndex >> 28) & 0b1111;
#endif
    }

    public enum PaintCmdType : byte
    {
        Points = 0,
        Lines,
        Rectangle,
        Circle,
        Triangle,
        Text,
        Image
    }
}
