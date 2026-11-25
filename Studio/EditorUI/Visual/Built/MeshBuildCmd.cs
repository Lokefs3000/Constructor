using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Built;
using EditorUI.Utility;

namespace EditorUI.Visual.Built
{
    internal readonly record struct MeshBuildCmd
    {
        public readonly PaintCmdType Type;
        public readonly nint SourceData;
        public readonly int SourceLength;
        public readonly uint Depth;
        public readonly BuiltPaint Paint;
        public readonly int VertexCount;
        public readonly int IndexCount;
    
        public MeshBuildCmd(PaintCmdType type, nint sourceData, int sourceLength, uint depth, BuiltPaint paint, int vtxCount, int idxCount)
        {
            Type = type;
            SourceData = sourceData;
            SourceLength = sourceLength;
            Depth = depth;
            Paint = paint;
            VertexCount = vtxCount;
            IndexCount = idxCount;
        }

        public readonly unsafe SpanReader GetSpanReader() => new SpanReader(new Span<byte>(SourceData.ToPointer(), SourceLength));
    }
}
