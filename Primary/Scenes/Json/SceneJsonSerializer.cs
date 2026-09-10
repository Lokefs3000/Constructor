using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json
{
    public sealed class SceneJsonSerializer
    {
        private FrozenDictionary<string, ComponentSerializer> _deserializers;

        internal SceneJsonSerializer()
        {
            _deserializers = FrozenDictionary<string, ComponentSerializer>.Empty;

            Assembly thisAssembly = typeof(RegisterComponentsDefault).Assembly;
            Assembly? entryAssembly = Assembly.GetEntryAssembly();

            DiscoverAssembly(thisAssembly);
            if (entryAssembly != null && entryAssembly != thisAssembly)
                DiscoverAssembly(entryAssembly);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        private void DiscoverAssembly(Assembly assembly)
        {
            EngLog.Scene.Information("Discovering assembly for deserializers: {n}", assembly.GetName().Name ?? assembly.FullName);

            ConcurrentBag<Type> potentialSerializers = new ConcurrentBag<Type>();

            // the trimming warning does not matter because neither generated deserializers nor components should get trimmed
            Parallel.ForEach(assembly.GetTypes(), (type) =>
            {
                if (type.Name.EndsWith("_GenSerializer") && type.GetCustomAttribute<ComponentDeserializerAttribute>() != null)
                {
                    potentialSerializers.Add(type);
                }
            });

            Dictionary<string, ComponentSerializer> tempDict = _deserializers.ToDictionary();

            while (potentialSerializers.TryTake(out Type? result))
            {
                ComponentDeserializerAttribute attribute = result.GetCustomAttribute<ComponentDeserializerAttribute>()!;
                
                if (!attribute.Target.IsAssignableTo(typeof(IComponent)))
                {
                    EngLog.Scene.Error("Deserializer {c} target is not a real component", result);
                    continue;
                }

                string? assemblyName = attribute.Target.Assembly.GetName().Name;
                if (assemblyName != null && attribute.Target.FullName != null)
                {
                    string keyName = $"{assemblyName}?{attribute.Target.FullName}";
                    if (!_deserializers.ContainsKey(keyName))
                    {
                        DeserializeDelegate? deserialize = result.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public)?.CreateDelegate<DeserializeDelegate>();
                        if (deserialize == null)
                        {
                            EngLog.Scene.Error("Deserializer {c} is missing a proper Deserialize method", result);
                            continue;
                        }
                        
                        AddDelegate? add = result.GetMethod("Add", BindingFlags.Static | BindingFlags.Public)?.CreateDelegate<AddDelegate>();
                        if (add == null)
                        {
                            EngLog.Scene.Error("Deserializer {c} is missing a proper Add method", result);
                            continue;
                        }

                        tempDict.Add(keyName, new ComponentSerializer(attribute.Target, deserialize, add));
                    }
                    else
                        EngLog.Scene.Warning("Component {c} already has a serializer registered", attribute.Target);
                }
            }

            _deserializers = tempDict.ToFrozenDictionary();
        }

        internal bool CreateComponent(SceneEntity entity, string componentKey)
        {
            if (_deserializers.TryGetValue(componentKey, out ComponentSerializer serializer))
            {
                serializer.Add(ref entity);
                return true;
            }

            return false;
        }

        internal bool DeserializeComponent(SceneEntity entity, string componentKey, ref Utf8JsonReader reader)
        {
            if (_deserializers.TryGetValue(componentKey, out ComponentSerializer serializer))
            {
                try
                {
                    serializer.Deserialize(ref reader, ref entity);
                }
                catch (SceneLoadException ex)
                {
                    if (ex.Type != null)
                        EngLog.Scene.Error($"Deserialization error: {ex.Message}\n\r      {{entity}} {{type}}", ex.Entity, ex.Type);
                    else
                        EngLog.Scene.Error($"Deserialization error: {ex.Message}\n\r      {{entity}}", ex.Entity);

                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool DeserializeGeneric<TComp, T>(ref Utf8JsonReader reader, ref SceneEntity entity, out T? value) where TComp : IComponent
        {
            return SceneValueConverter.Deserialize<TComp, T>(ref reader, ref entity, out value);
        }

        public static bool SerializeGeneric(Utf8JsonWriter writer, object value, ref SceneEntity entity)
        {
            return SceneValueConverter.Serialize(writer, value, ref entity);
        }

        public static void PrintWarning(string message, SceneEntity entity, Type? type = null)
        {
            if (type != null)
                EngLog.Scene.Warning(message + "\n\r        {entity} ({t})", entity, type);
            else
                EngLog.Scene.Warning(message + "\n\r        {entity}", entity);
        }

        public static string? GetComponentKey(Type type)
        {
            string? assemblyName = type.Assembly.GetName().Name;
            return assemblyName == null ? null : $"{assemblyName}?{type.FullName}";
        }

        private readonly record struct ComponentSerializer(Type TargetType, DeserializeDelegate Deserialize, AddDelegate Add);

        private delegate void DeserializeDelegate(ref Utf8JsonReader reader, ref SceneEntity entity);
        private delegate void AddDelegate(ref SceneEntity entity);
    }
}
