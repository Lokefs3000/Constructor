namespace Primary.RHI
{
    public abstract class PipelineLibrary : IDisposable
    {
        public abstract void Dispose();

        public abstract void GetPipelineLibraryData(Span<byte> data);

        public abstract long PipelineDataSize { get; }
    }
}
