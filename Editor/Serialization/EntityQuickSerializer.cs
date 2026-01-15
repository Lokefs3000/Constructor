using Primary.Scenes;

namespace Editor.Serialization
{
    public static class EntityQuickSerializer
    {
        public static string? Serialize(SceneEntity entity)
        {
            try
            {
                string ret = string.Empty;//SceneSerializer.Serialize(entity);
                return ret;
            }
            catch (Exception ex)
            {
                EdLog.Serialization.Debug(ex, "Serialization issue occured");
            }

            return null;
        }

        public static SceneEntity Deserialize(ReadOnlySpan<char> source, Scene scene)
        {
            try
            {
                SceneDeserializer deserializer = Editor.GlobalSingleton.SceneManager.Deserializer;
                return deserializer.DeserializeEntity(source, scene);
            }
            catch (Exception ex)
            {
                EdLog.Serialization.Debug(ex, "Deserialization issue occured");
            }

            return SceneEntity.Null;
        }
    }
}
