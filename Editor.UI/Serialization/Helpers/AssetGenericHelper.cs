using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.UI.Serialization.Helpers
{
    internal static class AssetGenericHelper<T>
    {
        internal static object LoadAsset(ReadOnlySpan<char> sourcePath, bool synchonous = false) => ValueDelegate(sourcePath, synchonous);

        internal static readonly LoadAssetDelegate ValueDelegate = (LoadAssetDelegate)Delegate.CreateDelegate(
            typeof(LoadAssetDelegate), typeof(Enum)
                .GetMethod(nameof(AssetManager.LoadAsset), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(Type), typeof(ReadOnlySpan<char>), typeof(bool)));
        internal delegate object LoadAssetDelegate(ReadOnlySpan<char> sourcePath, bool synchonous);
    }
}
