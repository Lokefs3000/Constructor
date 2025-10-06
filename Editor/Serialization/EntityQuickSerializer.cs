using Primary.Components;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Editor.Serialization
{
    public static class EntityQuickSerializer
    {
        public static string? Serialize(SceneEntity entity)
        {
            try
            {
                string ret = SceneSerializer.Serialize(entity);
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
