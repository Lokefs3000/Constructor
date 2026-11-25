using Primary.Assets;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Commands;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Rendering.Passes
{
    internal sealed class GizmoRenderPass
    {
        private ShaderAsset? _lineShader;
        private ShaderAsset? _triangleShader;

        private ShaderAsset? _screenLineShader;
        private ShaderAsset? _screenTriangleShader;

        private PropertyBlock? _block;

        public GizmoRenderPass()
        {
            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoLine.shader");
            _triangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoTriangle.shader");

            _screenLineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/ScreenGizmoLine.shader");
            _screenTriangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/ScreenGizmoTriangle.shader");

            _block = _lineShader.CreatePropertyBlock();
        }

        
    }
}
