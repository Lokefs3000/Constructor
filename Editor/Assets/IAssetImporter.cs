using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets
{
    public interface IAssetImporter : IDisposable
    {
        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath);

        public string? CustomFileIcon { get; }
    }
}
