using Arch.Buffer;
using Arch.Core;
using Editor.Assets.Types;
using Editor.Components;
using Editor.Geometry;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Scenes;
using Primary.Systems;
using Primary.Threading;
using Schedulers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Systems
{
    public struct GeoSceneSystem : ISystem, IDisposable, IForEachWithEntity<GeoSceneComponent>
    {
        private readonly CommandBuffer _commandBuffer;

        private readonly Dictionary<RawRenderMesh, SceneEntity> _existingMeshes;

        public GeoSceneSystem()
        {
            _commandBuffer = new CommandBuffer();

            _existingMeshes = new Dictionary<RawRenderMesh, SceneEntity>();
        }

        public readonly void Dispose() => _commandBuffer.Dispose();

        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Geoscene"))
            {
                world.InlineEntityQuery<GeoSceneSystem, GeoSceneComponent>(s_query, ref this);

                _commandBuffer.Playback(world);
            }
        }

        public void Update(Entity entity, ref GeoSceneComponent geoScene)
        {
            SceneEntity sceneEntity = entity;
            if (geoScene.Scene == null || !geoScene.Scene.IsLoaded)
                return;

            GeoScene? scene = geoScene.Scene.Scene;
            if (scene == null)
                return;

            long updateIndex = ((long)geoScene.Scene.LoadIndex << 32) | (long)scene.UpdateIndex;
            if (updateIndex != geoScene.UpdateIndex)
            {
                TimeSpan timeSincePreviousUpdate = DateTime.Now - geoScene.LastUpdateTime;
                if (timeSincePreviousUpdate.TotalSeconds > 0.1)
                {
                    if (geoScene.Scene.RegenerateMeshes())
                    {
                        foreach (SceneEntity child in sceneEntity.Children)
                        {
                            ref MeshRenderer comp = ref child.GetComponent<MeshRenderer>();
                            if (comp.Mesh != null)
                                _existingMeshes.Add(comp.Mesh, child);
                        }

                        foreach (var kvp in geoScene.Scene.RenderMeshes!)
                        {
                            var stored = kvp;
                            if (!_existingMeshes.Remove(kvp.Value))
                            {
                                GeoSceneAsset asset = geoScene.Scene;
                                ThreadHelper.ExecuteOnMainThread(() =>
                                {
                                    SceneEntity newEntity = sceneEntity.Scene.CreateEntity(sceneEntity);

                                    newEntity.SetComponent(new GeoSceneGeneratedTag
                                    {
                                        SceneAssetId = asset.Id
                                    });

                                    newEntity.SetComponent(new MeshRenderer
                                    {
                                        Mesh = stored.Value,
                                        Material = stored.Key.Material
                                    });
                                });
                            }
                        }

                        if (_existingMeshes.Count > 0)
                        {
                            foreach (var kvp in _existingMeshes)
                            {
                                kvp.Value.Destroy();
                            }

                            _existingMeshes.Clear();
                        }

                        geoScene.UpdateIndex = ((long)geoScene.Scene.LoadIndex << 32) | (long)scene.UpdateIndex;
                        geoScene.LastUpdateTime = DateTime.Now;
                    }
                }
            }
        }

        public ref readonly QueryDescription Description => ref s_query;
        public bool SystemNeedsFullExecutionTime => true;

        private static readonly QueryDescription s_query = new QueryDescription()
            .WithAll<GeoSceneComponent>();
    }
}
