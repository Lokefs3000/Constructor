using Primary.Assets.Types;

namespace Editor.Assets
{
    public interface IAssetImporter : IDisposable
    {
        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile);
        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline);
        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline);

        public string? CustomFileIcon { get; }
    }

    public sealed class AssetImportException : Exception
    {
        private AssetId _id;

        public AssetImportException()
        {
            _id = AssetId.Invalid;
        }

        public AssetImportException(string? message) : base(message)
        {
            _id = AssetId.Invalid;
        }

        public AssetImportException(string? message, AssetId id) : base(message)
        {
            _id = id;
        }
    }
}
