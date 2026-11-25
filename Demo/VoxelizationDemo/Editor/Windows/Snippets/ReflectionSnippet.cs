using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering.Assets;

namespace VoxelizationDemo.Editor.Windows.Snippets
{
    internal sealed class ReflectionSnippet : SnippetView, IDisposable
    {
        private readonly ShaderAsset _shader;
        private readonly PropertyBlock _propertyBlock;
        private readonly ModelAsset _sphereModel;

        internal ReflectionSnippet()
        {

        }

        void IDisposable.Dispose()
        {
            _propertyBlock.Dispose();
        }
    }
}
