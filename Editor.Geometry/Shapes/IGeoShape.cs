namespace Editor.Geometry.Shapes
{
    public interface IGeoShape
    {
        public Span<GeoShapeFace> Faces { get; }

        public bool IsDirty { get; }

        public GeoMesh GenerateMesh();
        public void ForceDirty();
    }
}
