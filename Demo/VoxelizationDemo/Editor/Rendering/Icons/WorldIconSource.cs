using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;
using Primary.Threading;

namespace VoxelizationDemo.Editor.Rendering.Icons
{
    internal abstract class WorldIconSource : IJob
    {
        protected readonly List<WorldIconData> _icons;
        protected readonly TextureAtlasAsset _textureAtlas;

        protected Frustrum _frustrum;
        protected Vector3 _center;

        public WorldIconSource()
        {
            _icons = new List<WorldIconData>();
            _textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Content/Textures/Editor/FloatingIcons.atlas");
        }

        internal void SetParameters(Frustrum frustrum, Vector3 center)
        {
            _frustrum = frustrum;
            _center = center;
        }

        public abstract void Execute();

        public ROList<WorldIconData> Icons => _icons;

        public abstract Sprite? Sprite { get; }
    }
}
