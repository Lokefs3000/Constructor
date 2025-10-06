namespace Primary.Components
{
#if false
    [ComponentCluster(Subcomponents = [typeof(Light), typeof(LightRenderingData)])]
    public record struct LightSyntaxTest
    {
        private WeakRef<Light> _light;
        private WeakRef<LightRenderingData> _lightRenderingData;

        public LightSyntaxTest()
        {
            
        }

        public LightType Type
        {
            get => _light.Ref.Type;
            set => _light.Ref.Type = value;
        }

        [ComponentField(Type = typeof(Light), Accessor = "Diffuse")]
        public Vector3 Diffuse;
    }
#endif
}
