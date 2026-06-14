using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geometry
{
    public record struct BrushFace
    {
        public readonly BrushFaceIndex FaceIndex;

        public MaterialAsset? Material;

        public Vector2 UVScale;
        public Vector2 UVOffset;

        public BrushFaceFlags Flags;

        public BrushFace(BrushFaceIndex faceIndex)
        {
            FaceIndex = faceIndex;

            Material = null;

            UVScale = Vector2.One;
            UVOffset = Vector2.Zero;

            Flags = BrushFaceFlags.None;
        }

        public BrushFace(BrushFaceIndex faceIndex, MaterialAsset? material, Vector2 uvScale, Vector2 uvOffset, BrushFaceFlags flags)
        {
            FaceIndex = faceIndex;

            Material = material;

            UVScale = uvScale;
            UVOffset = uvOffset;

            Flags = flags;
        }
    }

    [Flags]
    public enum BrushFaceFlags : byte
    {
        None = 0,

        Invisible = 1 << 0,
        NoCollider = 1 << 1
    }
}
