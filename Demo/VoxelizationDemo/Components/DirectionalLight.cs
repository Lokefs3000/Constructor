using Primary.Common;
using Primary.Components;

namespace VoxelizationDemo.Components
{
    [Component]
    public record struct DirectionalLight : IComponent
    {
        private Color _diffuse;

        private float _intensity;

        public DirectionalLight()
        {
            _diffuse = Color.White;
 
            _intensity = 1.0f;
        }

        public Color Diffuse { readonly get => _diffuse; set => _diffuse = value; }

        public float Intensity { readonly get => _intensity; set => _intensity = value; }
    }
}
