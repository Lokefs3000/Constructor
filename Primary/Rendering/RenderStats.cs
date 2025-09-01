namespace Primary.Rendering
{
    public record struct RenderStats
    {
        public uint MeshesDrawn;
        public uint BatchesDrawn;
        public uint ShadersDrawn;

        public uint UniqueMeshes;
        public uint UniqueShaders;

        public uint DrawCalls;
        public uint IndividualDrawn;
        public uint InstancesDrawn;
        public uint SavedByBatching;
    }
}
