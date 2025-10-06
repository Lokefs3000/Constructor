namespace Editor.Assets
{
    public interface IAssetImporter : IDisposable
    {
        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile);
        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline);
        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline);

        public string? CustomFileIcon { get; }
    }
}
