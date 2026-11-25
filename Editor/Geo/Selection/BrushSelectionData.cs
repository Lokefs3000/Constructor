using Editor.Geometry;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Geo.Selection
{
    public record struct BrushSelectionData(bool IsBrushActive, byte ActiveFaces, byte ActiveVertices, byte FaceData, byte VertexData)
    {
        public SelectionActiveData ActiveData;

        internal void CommitData(AllowedSelections selections)
        {
            if (Flags.HasFlag(selections, AllowedSelections.Brush) && IsBrushActive)
            {
                FaceData = AllFaces;
                VertexData = AllVertices;
            }
            else
            {
                FaceData = 0;
                VertexData = 0;

                if (Flags.HasFlag(selections, AllowedSelections.Face))
                {
                    FaceData |= ActiveData.FaceFaceData;
                    VertexData |= ActiveData.FaceVertexData;
                }

                if (Flags.HasFlag(selections, AllowedSelections.Vertex))
                {
                    FaceData |= ActiveData.VertexFaceData;
                    VertexData |= ActiveData.VertexVertexData;
                }
            }
        }

        public readonly bool IsFaceSelected(BrushFaceIndex faceIndex) => ((ActiveData.FaceFaceData >> (int)faceIndex) & 0x1) > 0;
        public readonly bool IsVertexSelected(BrushVertexIndex vertexIndex) => ((ActiveData.VertexVertexData >> (int)vertexIndex) & 0x1) > 0;

        public bool IsEmpty => FaceData == 0 && VertexData == 0;

        public const byte AllFaces = 0x3f;
        public const byte AllVertices = 0xff;
    }

    public struct SelectionActiveData
    {
        public byte FaceVertexData;
        public byte FaceFaceData;

        public byte VertexVertexData;
        public byte VertexFaceData;

        public byte VertexData => (byte)(FaceVertexData | VertexVertexData);
        public byte FaceData => (byte)(FaceFaceData | VertexFaceData);
    }
}
