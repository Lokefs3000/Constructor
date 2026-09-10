using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;
using Primary.Components;

namespace VoxelizationDemo.Components
{
    [Component]
    public record struct PointLight : IComponent
    {
        private float _radius;
        private float _falloff;

        private Color _diffuse;
        private Color _specular;

        private float _intensity;

        public PointLight()
        {
            _radius = 2.0f;
            _falloff = 2.0f;

            _diffuse = Color.White;
            _specular = Color.White;

            _intensity = 1.0f;
        }

        public float Radius { readonly get => _radius; set => _radius = value; }
        public float Falloff { readonly get => _falloff; set => _falloff = value; }

        public Color Diffuse { readonly get => _diffuse; set => _diffuse = value; }
        public Color Specular { readonly get => _specular; set => _specular = value; }

        public float Intensity { readonly get => _intensity; set => _intensity = value; }
    }
}
