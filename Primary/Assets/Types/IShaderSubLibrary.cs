namespace Primary.Assets.Types
{
    public interface IShaderSubLibrary : IDisposable
    {
        public byte[]? ReadFromLibrary(string path);
    }
}
