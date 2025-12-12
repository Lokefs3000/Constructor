using Primary.Common.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Assets.Types
{
    public interface IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id);
        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData);
        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom);
    }
}
