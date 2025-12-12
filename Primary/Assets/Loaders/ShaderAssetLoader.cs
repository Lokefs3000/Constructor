using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common.Streams;
using Primary.Rendering;

namespace Primary.Assets.Loaders
{
    internal unsafe class ShaderAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ShaderAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ShaderAssetData modelData)
                throw new ArgumentException(nameof(assetData));

            return new ShaderAsset(modelData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ShaderAsset shader)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ShaderAssetData shaderData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                if (!AssetFilesystem.ShaderLibrary.ReadGraphicsShader(sourcePath, out RHI.GraphicsPipelineDescription desc, out RHI.GraphicsPipelineBytecode bytecode, out ShaderVariable[] variables))
                {
                    return;
                }

                int j;

                Dictionary<string, ShaderVariable> lookupVars = new Dictionary<string, ShaderVariable>();
                for (int i = 0; i < variables.Length; i++)
                {
                    ref ShaderVariable variable = ref variables[i];
                    lookupVars.Add(variable.Name, variable);
                }

                RHI.GraphicsPipeline graphicsPipeline = RenderingManager.Device.CreateGraphicsPipeline(desc, bytecode);
                shaderData.UpdateAssetData(shader, graphicsPipeline, sourcePath.GetDjb2HashCode(), lookupVars);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                shaderData.UpdateAssetFailed(shader);
                EngLog.Assets.Error(ex, "Failed to load shader: {name}", sourcePath);
            }
#endif
        }

        private const uint HeaderId = 0x204c4243;
        private const uint HeaderVersion = 0;
    }
}
