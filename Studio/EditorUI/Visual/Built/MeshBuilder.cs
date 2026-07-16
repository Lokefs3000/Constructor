using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Built;
using EditorUI.Memory;
using EditorUI.Text;
using EditorUI.Utility;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Pooling;
using Primary.Threading;
using SharpGen.Runtime;
using TerraFX.Interop.Windows;
using Vortice.Mathematics.PackedVector;

namespace EditorUI.Visual.Built
{
    public sealed class MeshBuilder : IDisposable
    {
        private readonly TemporaryAllocator _temporaryAllocator;
        private readonly List<BuildCommandsJob> _jobPool;

        private UIVertex[] _vertices;
        private ushort[] _indices;

        private List<MeshDrawSegment> _segments;

        private int _startedJobsCount;

        private bool _disposedValue;

        internal MeshBuilder()
        {
            _temporaryAllocator = new TemporaryAllocator(512);
            _jobPool = new List<BuildCommandsJob>();

            _vertices = [];
            _indices = [];

            _segments = new List<MeshDrawSegment>();

            _startedJobsCount = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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

            _segments.Clear();
        }

        internal JobHandle Build(int vertexCount, int indexCount, ROList<PaintCmd> cmds, ROList<MeshBuildCmd> meshBuildCmds, ROList<object> objectList)
        {
            const int AllowedVerticesForJob = 256;
            const int MaxVerticesForJob = 300;

            if (_vertices.Length < vertexCount)
                _vertices = new UIVertex[(int)(vertexCount * 1.5)];
            if (_indices.Length < indexCount)
                _indices = new ushort[(int)(indexCount * 1.5)];

            MeshDrawSegment lastDrawSegment;
            {
                PaintCmd firstCmd = cmds[0];
                lastDrawSegment = new MeshDrawSegment(firstCmd.CmdType, 0, firstCmd.Obj0, firstCmd.Obj3);
            }

            using RentedList<JobHandle> handles = new RentedList<JobHandle>();

            int requiredDataSize = 0;

            int lastJobVtxOffset = 0;
            int lastJobIdxOffset = 0;
            int lastJobCmdStart = 0;
            int lastJobDataOffset = 0;

            int currentVtxCount = 0;
            int currentIdxCount = 0;

            int lastCmdIndex = cmds.Count - 1;

            for (int i = 0; i < cmds.Count; ++i)
            {
                PaintCmd currentCmd = cmds[i];
                MeshBuildCmd currentMeshBuildCmd = meshBuildCmds[currentCmd.MeshIndex];

                if (!IsSegmentCompatibleWithCmd(ref lastDrawSegment, ref currentCmd))
                {
                    _segments.Add(lastDrawSegment);
                    lastDrawSegment = new MeshDrawSegment(currentCmd.CmdType, lastJobIdxOffset + currentIdxCount, currentCmd.Obj0, currentCmd.Obj3);
                }

                int dataSizeBeforeThisCmd = requiredDataSize;

                switch (currentCmd.CmdType)
                {
                    case PaintCmdType.Points: requiredDataSize += Unsafe.SizeOf<SharedShaderData>(); break;
                    case PaintCmdType.Lines: requiredDataSize += Unsafe.SizeOf<LinesShaderData>(); break;
                    case PaintCmdType.Rectangle: requiredDataSize += Unsafe.SizeOf<RectangleShaderData>(); break;
                    case PaintCmdType.Circle: requiredDataSize += Unsafe.SizeOf<SharedShaderData>(); break;
                    case PaintCmdType.Triangle: break;
                    case PaintCmdType.Text: requiredDataSize += Unsafe.SizeOf<TextShaderData>(); break;
                    case PaintCmdType.Image: requiredDataSize += Unsafe.SizeOf<SharedShaderData>(); break;
                }

                if (i > 0 || i == lastCmdIndex)
                {
                    int nextVtxCount = currentVtxCount + currentMeshBuildCmd.VertexCount;
                    if (nextVtxCount > AllowedVerticesForJob || (nextVtxCount > 0 && i == lastCmdIndex))
                    {
                        if (handles.Count >= _jobPool.Count)
                            _jobPool.Add(new BuildCommandsJob());
                        BuildCommandsJob job = _jobPool[handles.Count];

                        job.Cmds = cmds;
                        job.MeshBuilds = meshBuildCmds;
                        job.ObjectList = objectList;

                        job.CmdsRange = new IndexRange(lastJobCmdStart, -1);

                        if (nextVtxCount > MaxVerticesForJob && i != lastCmdIndex)
                        {
                            job.CmdsRange.End = i;
                            job.Vertices = new ArraySegment<UIVertex>(_vertices, lastJobVtxOffset, currentVtxCount);
                            job.Indices = new ArraySegment<ushort>(_indices, lastJobIdxOffset, currentIdxCount);
                            job.DataPointer = lastJobDataOffset;

                            lastJobVtxOffset += currentVtxCount;
                            lastJobIdxOffset += currentIdxCount;
                            lastJobCmdStart = job.CmdsRange.End;
                            lastJobDataOffset = dataSizeBeforeThisCmd;

                            currentVtxCount = currentMeshBuildCmd.VertexCount;
                            currentIdxCount = currentMeshBuildCmd.IndexCount;
                        }
                        else
                        {
                            currentIdxCount += currentMeshBuildCmd.IndexCount;

                            job.CmdsRange.End = i + 1;
                            job.Vertices = new ArraySegment<UIVertex>(_vertices, lastJobVtxOffset, nextVtxCount);
                            job.Indices = new ArraySegment<ushort>(_indices, lastJobIdxOffset, currentIdxCount);
                            job.DataPointer = lastJobDataOffset;

                            lastJobVtxOffset += nextVtxCount;
                            lastJobIdxOffset += currentIdxCount;
                            lastJobCmdStart = job.CmdsRange.End;
                            lastJobDataOffset = requiredDataSize;

                            currentVtxCount = 0;
                            currentIdxCount = 0;
                        }

                        handles.Add(JobScheduler.Schedule(job));
                    }
                    else
                    {
                        currentVtxCount = nextVtxCount;
                        currentIdxCount += currentMeshBuildCmd.IndexCount;
                    }
                }
                else
                {
                    currentVtxCount = currentMeshBuildCmd.VertexCount;
                    currentIdxCount = currentMeshBuildCmd.IndexCount;
                }
            }

            _segments.Add(lastDrawSegment);

            nint dataPointer = _temporaryAllocator.GetNativePointer(requiredDataSize);

            for (int i = 0; i < handles.Count; ++i)
            {
                BuildCommandsJob job = _jobPool[i];

                job.BaseDataPointer = dataPointer;
                job.DataPointer += dataPointer;
            }

            _startedJobsCount = handles.Count;

            // This is because we are over allocating the array so we may not actually have that many
            JobScheduler.Flush(handles.AsSpan());
            return JobScheduler.CombineAll(handles.AsSpan());
        }

        private static bool IsSegmentCompatibleWithCmd(ref MeshDrawSegment segment, ref PaintCmd cmd)
        {
            return segment.CmdType == cmd.CmdType && segment.Argument == cmd.Obj0 && segment.ClipIndex == cmd.Obj3;
        }

        public ReadOnlySpan<byte> DataBuffer => _temporaryAllocator.AsSpan();

        public ReadOnlySpan<UIVertex> Vertices => _vertices.AsSpan();
        public ReadOnlySpan<ushort> Indices => _indices.AsSpan();

        public ROList<MeshDrawSegment> Segments => _segments;

        public int StartedJobsCount => _startedJobsCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector64<float> GetVector64FromVector2(in Vector2 vector) => Unsafe.BitCast<Vector2, Vector64<float>>(vector);

        private sealed class BuildCommandsJob : IJob
        {
            public ROList<PaintCmd> Cmds;
            public ROList<MeshBuildCmd> MeshBuilds;
            public ROList<object> ObjectList;

            public IndexRange CmdsRange;

            public ArraySegment<UIVertex> Vertices;
            public ArraySegment<ushort> Indices;

            public nint BaseDataPointer;
            public nint DataPointer;

            public void Execute()
            {
                Span<UIVertex> verticesSpan = Vertices.AsSpan();
                Span<ushort> indicesSpan = Indices.AsSpan();

                int vertexOffset = 0;
                int indexOffset = 0;

                for (int i = CmdsRange.Start; i < CmdsRange.End; ++i)
                {
                    PaintCmd cmd = Cmds[i];
                    MeshBuildCmd meshBuildCmd = MeshBuilds[cmd.MeshIndex];

                    Color fillColor = meshBuildCmd.Paint.Fill.ColorType == BuiltColorType.Solid ? meshBuildCmd.Paint.Fill.Solid : new Color(meshBuildCmd.Paint.Fill.GradientIndex, -1.0f);
                    SharedShaderData sharedShaderData = default;

                    if (meshBuildCmd.Paint.StrokeWidth > 0)
                    {
                        sharedShaderData = new SharedShaderData(meshBuildCmd.Paint.StrokeWidth, new Half4(meshBuildCmd.Paint.Stroke.Solid.AsVector4()));
                    }

                    switch (cmd.CmdType)
                    {
                        case PaintCmdType.Points:
                            {
                                uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                WriteIntoDataPointer(sharedShaderData);

                                SpanReader reader = meshBuildCmd.GetSpanReader();
                                UIVector2 radius = new UIVector2(reader.Get<float>(), 0.0f);

                                while (!reader.HasReadAllData)
                                {
                                    Vector128<float> minMax = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                    Vector128<float> middleVerts = Vector128.Shuffle(minMax, Vector128.Create(2, 1, 0, 3));

                                    UIVector2 center = Vector2.LoadUnsafe(ref reader.Get<float>(2));

                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetLower(), radius, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetLower(), radius, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetUpper(), radius, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetUpper(), radius, center, fillColor, dataOffset);

                                    indexOffset += 6;
                                }

                                break;
                            }
                        case PaintCmdType.Lines:
                            {
                                SpanReader reader = meshBuildCmd.GetSpanReader();

                                uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                WriteIntoDataPointer(new LinesShaderData(sharedShaderData, (Half)reader.Get<float>()));

                                while (!reader.HasReadAllData)
                                {
                                    Vector128<float> v01 = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                    Vector128<float> v23 = Vector128.LoadUnsafe(ref reader.Get<float>(4));

                                    UIVector2 from = Unsafe.ReadUnaligned<UIVector2>(ref Unsafe.As<float, byte>(ref reader.Get<float>(2)));
                                    UIVector2 to = Unsafe.ReadUnaligned<UIVector2>(ref Unsafe.As<float, byte>(ref reader.Get<float>(2)));

                                    verticesSpan[vertexOffset++] = new UIVertex(v01.GetLower(), from, to, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(v01.GetUpper(), from, to, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(v23.GetLower(), from, to, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(v23.GetUpper(), from, to, fillColor, dataOffset);

                                    indexOffset += 6;
                                }

                                break;
                            }
                        case PaintCmdType.Rectangle:
                            {
                                SpanReader reader = meshBuildCmd.GetSpanReader();

                                if (reader.Length > Unsafe.SizeOf<float>() * 8)
                                {
                                    Vector128<float> top = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                    Vector128<float> bottom = Vector128.LoadUnsafe(ref reader.Get<float>(4));

                                    uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                    WriteIntoDataPointer(new RectangleShaderData(sharedShaderData, default, new Half4(-1.0f)));

                                    verticesSpan[vertexOffset++] = new UIVertex(top.GetLower(), UIVector2.Zero, UIVector2.Zero, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(top.GetUpper(), UIVector2.UnitX, UIVector2.Zero, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(bottom.GetLower(), UIVector2.UnitY, UIVector2.Zero, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(bottom.GetUpper(), UIVector2.One, UIVector2.Zero, fillColor, dataOffset);
                                }
                                else
                                {
                                    Vector128<float> minMax = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                    Vector128<float> middleVerts = Vector128.Shuffle(minMax, Vector128.Create(2, 1, 0, 3));

                                    Vector4 cornerRadius = Vector4.LoadUnsafe(ref reader.Get<float>(4));

                                    Vector64<float> center = Vector64.Lerp(minMax.GetLower(), minMax.GetUpper(), Vector64.Create(0.5f));
                                    Vector64<float> size = minMax.GetUpper() - minMax.GetLower();

                                    uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                    WriteIntoDataPointer(new RectangleShaderData(sharedShaderData, new UShort2(Unsafe.BitCast<Vector64<float>, Vector2>(size)), new Half4(cornerRadius)));

                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetLower(), UIVector2.Zero, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetLower(), UIVector2.UnitX, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetUpper(), UIVector2.UnitY, center, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetUpper(), UIVector2.One, center, fillColor, dataOffset);
                                }

                                indexOffset += 6;

                                break;
                            }
                        case PaintCmdType.Circle:
                            {
                                uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                WriteIntoDataPointer(sharedShaderData);

                                SpanReader reader = meshBuildCmd.GetSpanReader();

                                Vector128<float> minMax = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                Vector128<float> middleVerts = Vector128.Shuffle(minMax, Vector128.Create(2, 1, 0, 3));

                                UIVector2 center = Vector2.LoadUnsafe(ref reader.Get<float>(2));
                                UIVector2 radius = new UIVector2(reader.Get<float>(), 0.0f);

                                verticesSpan[vertexOffset++] = new UIVertex(minMax.GetLower(), radius, center, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetLower(), radius, center, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetUpper(), radius, center, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(minMax.GetUpper(), radius, center, fillColor, dataOffset);

                                indexOffset += 6;

                                break;
                            }
                        case PaintCmdType.Triangle:
                            {
                                break;
                            }
                        case PaintCmdType.Text:
                            {
                                SpanReader reader = meshBuildCmd.GetSpanReader();

                                Vector2 position = Vector2.LoadUnsafe(ref reader.Get<float>(2));
                                Vector2 maxExtents = Vector2.LoadUnsafe(ref reader.Get<float>(2));
                                TextShapingData shapingData = (TextShapingData)ObjectList[reader.Get<ushort>()];
                                int sectionIndex = reader.Get<int>();

                                uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                WriteIntoDataPointer(new TextShaderData(sharedShaderData, maxExtents));

                                TextShapingSection section = shapingData.Sections[sectionIndex];
                                TextShapingLine line = shapingData.Lines[section.LinePosition];

                                FontStyleData styleData = section.Visual.StyleData;

                                float pxRange = (float)styleData.Family.Setup.PxRange;
                                float spaceAdvance = styleData.Advances.Space * (styleData.Metrics.UnitsPerEm * section.Visual.PixelSize);

                                char previousLetter = '\0';

                                position += new Vector2(section.LeftOffset, line.LineYOffset);
                                for (int j = section.TextRange.Start; j < section.TextRange.End; j++)
                                {
                                    char letter = shapingData.TextBuffer[j];
                                    if (letter == ' ')
                                    {
                                        position.X += spaceAdvance;
                                        continue;
                                    }
                                    else if (char.IsControl(letter))
                                    {
                                        continue;
                                    }

                                    FontGlyph glyph = styleData.FindGlyph(letter);

                                    Vector2 thisPosition = position;
                                    // if (i > 0)
                                    // {
                                    //     ref readonly Vector2 kerning = ref styleData.GetKerning(previousLetter, letter);
                                    //     if (!Unsafe.IsNullRef(in kerning))
                                    //     {
                                    //         thisPosition -= kerning;
                                    //     }
                                    // }

                                    Vector128<float> minMax = glyph.PlaneBounds.AsVector128() * section.Visual.PixelSize + Vector128.Create(GetVector64FromVector2(thisPosition));
                                    Vector128<float> middleVerts = Vector128.Shuffle(minMax, Vector128.Create(2, 1, 0, 3));

                                    Vector128<float> uvs = glyph.UVBounds.AsVector128();
                                    Vector128<float> middleUvs = Vector128.Shuffle(uvs, Vector128.Create(2, 1, 0, 3));

                                    Vector64<float> size = minMax.GetUpper() - minMax.GetLower();
                                    float screenPxRange = size.GetElement(1) / glyph.PixelSize.Y * pxRange;

                                    UIVector2 uv2 = new UIVector2(screenPxRange, 0.0f);

                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetLower(), uvs.GetLower(), uv2, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetLower(), middleUvs.GetLower(), uv2, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetUpper(), middleUvs.GetUpper(), uv2, fillColor, dataOffset);
                                    verticesSpan[vertexOffset++] = new UIVertex(minMax.GetUpper(), uvs.GetUpper(), uv2, fillColor, dataOffset);

                                    indexOffset += 6;

                                    position.X += glyph.Advance * section.Visual.PixelSize;
                                    previousLetter = letter;
                                }

                                break;
                            }
                        case PaintCmdType.Image:
                            {
                                uint dataOffset = (uint)(DataPointer - BaseDataPointer);
                                WriteIntoDataPointer(sharedShaderData);

                                SpanReader reader = meshBuildCmd.GetSpanReader();

                                Vector128<float> minMax = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                Vector128<float> middleVerts = Vector128.Shuffle(minMax, Vector128.Create(2, 1, 0, 3));

                                Vector128<float> uvs = Vector128.LoadUnsafe(ref reader.Get<float>(4));
                                Vector128<float> middleUvs = Vector128.Shuffle(uvs, Vector128.Create(2, 1, 0, 3));

                                verticesSpan[vertexOffset++] = new UIVertex(minMax.GetLower(), uvs.GetLower(), UIVector2.Zero, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetLower(), middleUvs.GetLower(), UIVector2.UnitX, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(middleVerts.GetUpper(), middleUvs.GetUpper(), UIVector2.UnitY, fillColor, dataOffset);
                                verticesSpan[vertexOffset++] = new UIVertex(minMax.GetUpper(), uvs.GetUpper(), UIVector2.Zero, fillColor, dataOffset);

                                indexOffset += 6;

                                break;
                            }
                    }
                }

                for (int i = 0, j = Vertices.Offset; i < Indices.Count; j += 4)
                {
                    indicesSpan[i++] = (ushort)j;
                    indicesSpan[i++] = (ushort)(j + 3);
                    indicesSpan[i++] = (ushort)(j + 2);
                    indicesSpan[i++] = (ushort)j;
                    indicesSpan[i++] = (ushort)(j + 3);
                    indicesSpan[i++] = (ushort)(j + 1);
                }
            }

            private unsafe void WriteIntoDataPointer<T>(T data) where T : unmanaged
            {
                *(T*)DataPointer = data;
                DataPointer += Unsafe.SizeOf<T>();
            }
        }
    }

    public readonly record struct MeshDrawSegment(PaintCmdType CmdType, int IndexStart, ushort Argument, ushort ClipIndex);
}
