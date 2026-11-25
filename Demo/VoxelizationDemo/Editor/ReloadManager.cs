using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using CommunityToolkit.Diagnostics;
using Primary.Common;
using Primary.Components;
using Primary.Reflection;
using Primary.Scenes;
using Primary.Scripting;
using Primary.Streams;
using PrimaryEditor.Assets;
using PrimaryEditor.Project;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Serialization;

namespace VoxelizationDemo.Editor
{
    internal sealed class ReloadManager
    {
        private bool _wantsToReload;

        internal ReloadManager()
        {
            _wantsToReload = false;
        }

        internal void Update()
        {
            if (_wantsToReload)
            {
                PerformAssemblyReload();
                _wantsToReload = false;
            }
        }

        internal void RequestReload()
        {
            _wantsToReload = true;
        }

        private void PerformAssemblyReload()
        {
            AppLog.Primary.Information("!! RELOADING ASSEMBLIES AS PER REQUEST !!");

            VoxelRuntime runtime = VoxelRuntime.Instance;
            ScriptContext context = runtime.ScriptContext;

            HashSet<Assembly> assembliesToUnload = [.. context.LoadedAssemblies];

            SaveAssemblyComponents(runtime, assembliesToUnload, out Action loadAssemblyComponents);

            context.Dispose();
            runtime.ScriptingManager.ProcessPendingScripts();

            if (!context.IsUnloaded)
            {
                AppLog.Primary.Error("Script context did not unload successfully and will remain a spectre in memory or until the GC finally decides to be useful");
            }

            context = runtime.ScriptContext = runtime.ScriptingManager.CreateContext("User");
            context.LoadAssembly(FilesystemManager.GetFullPath("Library/Binary/Game.dll")!);

            loadAssemblyComponents();

            AppLog.Primary.Information("Assembly reload has finished");
        }

        private void SaveAssemblyComponents(VoxelRuntime runtime, HashSet<Assembly> assemblies, out Action onLoadAction)
        {
            SerializationManager serializer = runtime.EditorManager.SerializationManager;

            MemoryStream stream = new MemoryStream();
            List<SceneEntity> entityList = new List<SceneEntity>();

            {
                Dictionary<SceneEntity, int> entityIndices = new Dictionary<SceneEntity, int>();

                using Utf8JsonWriter writer = new Utf8JsonWriter(stream);
                writer.WriteStartArray();

                foreach (Archetype archetype in runtime.SceneManager.World.Archetypes.Items)
                {
                    foreach (ComponentType type in archetype.Signature.Components)
                    {
                        Type nativeType = type.Type;
                        if (assemblies.Contains(nativeType.Assembly))
                        {
                            if (nativeType.IsAssignableTo(typeof(EntityBehaviour)))
                            {
                                int index = archetype.GetLookupArray()[type.Id];
                                foreach (ref Chunk chunk in archetype.Chunks.AsSpan()[..archetype.ChunkCount])
                                {
                                    ref Entity entityFirst = ref chunk.Entity(0);
                                    Array behaviourArray = chunk.GetArray(type);

                                    foreach (int entityIndex in chunk)
                                    {
                                        HandleEntityBehavour(
                                            Unsafe.Add(ref entityFirst, entityIndex),
                                            (EntityBehaviour)behaviourArray.GetValue(entityIndex)!);
                                    }
                                }
                            }
                        }
                    }
                }

                writer.WriteEndArray();

                void HandleEntityBehavour(Entity rawEntity, EntityBehaviour behaviour)
                {
                    Type type = behaviour.GetType();
                    if (!assemblies.Contains(type.Assembly))
                        return;

                    SceneEntity entity = rawEntity;

                    ref int idx = ref CollectionsMarshal.GetValueRefOrAddDefault(entityIndices, entity, out bool exists);
                    if (!exists)
                    {
                        idx = entityList.Count;
                        entityList.Add(entity);
                    }

                    string? fullName = TypeKeyReflector.TypeToKey(type);
                    Guard.IsNotNull(fullName, $"Failed to get component key for type '{type}'");

                    writer.WriteStartObject();
                    writer.WriteNumber("entityId"u8, idx);
                    writer.WriteString("key"u8, fullName);

                    writer.WriteStartObject("values"u8);
                    serializer.ObjectSerializer.SerializeFromBox(writer, behaviour);
                    writer.WriteEndObject();

                    writer.WriteEndObject();
                }
            }

#if DEBUG
            FileUtility.TryWriteAllBytes(
                Path.Combine(ProjectData.Instance!.Paths.LibraryFolder, "behaviourReloadData.json"),
                stream.GetBuffer().AsSpan(0, (int)stream.Length));
#endif

            onLoadAction = LoadAssemblyComponents;

            void LoadAssemblyComponents()
            {
                Utf8JsonReader reader = new Utf8JsonReader(stream.GetBuffer().AsSpan(0, (int)stream.Length));

                reader.Read();
                Guard.IsTrue(reader.TokenType == JsonTokenType.StartArray);

                while (reader.TokenType != JsonTokenType.EndArray && reader.BytesConsumed < stream.Length)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        Guard.IsGreaterThanOrEqualTo(reader.BytesConsumed, stream.Length);
                        break;
                    }

                    Guard.IsTrue(reader.TokenType == JsonTokenType.StartObject);

                    {
                        {
                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.PropertyName);
                            Guard.IsTrue(reader.ValueTextEquals("entityId"u8));

                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.Number);
                        }
                        SceneEntity entity = entityList[reader.GetInt32()];

                        {
                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.PropertyName);
                            Guard.IsTrue(reader.ValueTextEquals("key"u8));

                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.String);
                        }
                        string componentKey = reader.GetString() ?? string.Empty;

                        // TODO: Handle when components are removed!!
                        Type? componentType = TypeKeyReflector.KeyToType(componentKey);
                        Guard.IsNotNull(componentType);

                        // TODO: For that matter this should also do a check instead of blindly casting
                        EntityBehaviour? behaviour = (EntityBehaviour?)entity.AddComponent(componentType);
                        Guard.IsNotNull(behaviour);

                        {
                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.PropertyName);
                            Guard.IsTrue(reader.ValueTextEquals("values"u8));

                            reader.Read();
                            Guard.IsTrue(reader.TokenType == JsonTokenType.StartObject);
                        }

                        while (reader.TokenType != JsonTokenType.EndObject && reader.BytesConsumed < stream.Length)
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.EndObject)
                                break;

                            Guard.IsTrue(reader.TokenType == JsonTokenType.PropertyName);
                            ReadOnlySpan<byte> propertyName = reader.ValueSpan;

                            reader.Read();
                            serializer.ObjectSerializer.DeserializeIntoBox(behaviour, propertyName, ref reader);
                        }

                        Guard.IsTrue(reader.TokenType == JsonTokenType.EndObject);
                    }

                    reader.Read();
                    Guard.IsTrue(reader.TokenType == JsonTokenType.EndObject);
                }

                Guard.IsTrue(reader.TokenType == JsonTokenType.EndArray);
            }
        }
    }
}
