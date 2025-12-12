using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Rendering2.Assets.Loaders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Assets
{
    public static class RegisterAssetsDefault
    {
        public static void RegisterDefault()
        {
            AssetManager assets = Engine.GlobalSingleton.AssetManager;

            assets.RegisterCustomAsset<ShaderAsset2>(new ShaderAsset2Loader());
            assets.RegisterCustomAsset<MaterialAsset2>(new MaterialAsset2Loader());
        }
    }
}
