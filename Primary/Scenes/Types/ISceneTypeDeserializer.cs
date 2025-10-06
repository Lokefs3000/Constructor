using Primary.Serialization.Structural;

namespace Primary.Scenes.Types
{
    public interface ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type);
    }
}
