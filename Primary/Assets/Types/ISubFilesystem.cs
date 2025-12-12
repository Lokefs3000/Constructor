namespace Primary.Assets.Types
{
    public interface ISubFilesystem : IDisposable
    {
        public string? ReadString(ReadOnlySpan<char> path);
        public Stream? OpenStream(ReadOnlySpan<char> path);

        public bool Exists(ReadOnlySpan<char> path);
    }
}
