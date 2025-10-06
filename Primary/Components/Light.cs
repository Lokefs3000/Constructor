using Primary.Common;
using System.Runtime.Serialization;

namespace Primary.Components
{
    [ComponentRequirements(typeof(Transform))]
    [ComponentConnections(typeof(LightRenderingData))]
    public record struct Light : IComponent
    {
        private LightType _type;

        private Color _diffuse;
        private Color _specular;

        private float _brightness;

        private float _outerCutOff;
        private float _innerCutOff;

        private ShadowImportance _shadowImportance;

        private bool _dirty;

        public Light()
        {
            _type = LightType.DirectionalLight;

            _diffuse = new Color(1.0f);
            _specular = new Color(1.0f);

            _brightness = 10.0f;

            _outerCutOff = float.DegreesToRadians(25.0f);
            _innerCutOff = float.DegreesToRadians(20.0f);

            _shadowImportance = ShadowImportance.None;

            _dirty = true;
        }

        public LightType Type { get => _type; set { _type = value; _dirty = true; } }

        public Color Diffuse { get => _diffuse; set { _diffuse = value; _dirty = true; } }
        public Color Specular { get => _specular; set { _specular = value; _dirty = true; } }

        public float Brightness { get => _brightness; set { _brightness = value; _dirty = true; } }

        public float OuterCutOff { get => _outerCutOff; set { _outerCutOff = value; _dirty = true; } }
        public float InnerCutOff { get => _innerCutOff; set { _innerCutOff = value; _dirty = true; } }

        public ShadowImportance ShadowImportance { get => _shadowImportance; set { _shadowImportance = value; _dirty = true; } }

        [IgnoreDataMember]
        internal bool Dirty { get => _dirty; set => _dirty = value; }
    }

    public enum LightType : byte
    {
        DirectionalLight = 0,
        SpotLight,
        PointLight
    }

    public enum ShadowImportance : byte
    {
        None = 0,
        Low,
        Medium,
        High,
        Static
    }
}
