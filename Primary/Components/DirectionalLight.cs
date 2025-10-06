using System.Numerics;

namespace Primary.Components
{
    [ComponentRequirements(typeof(Transform))]
    public record struct DirectionalLight : IComponent
    {
        private Vector3 _ambient;
        private Vector3 _diffuse;
        private Vector3 _specular;

        public DirectionalLight()
        {
            _ambient = new Vector3(0.2f);
            _diffuse = new Vector3(1.0f);
            _specular = new Vector3(1.0f);
        }

        public Vector3 Ambient { get => _ambient; set => _ambient = value; }
        public Vector3 Diffuse { get => _diffuse; set => _diffuse = value; }
        public Vector3 Specular { get => _specular; set => _specular = value; }
    }
}
