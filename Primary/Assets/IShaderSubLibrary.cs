namespace Primary.Assets
{
    public interface IShaderSubLibrary : IDisposable
    {
        public byte[]? ReadFromLibrary(string path);
    }
}
