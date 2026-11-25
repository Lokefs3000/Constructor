using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Scenes;
using Primary.Scenes.Components;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Tracking
{
    public sealed class EntityTracker
    {
        private readonly Dictionary<Scene, Dictionary<SceneEntity, Guid>> _uuids;
        private readonly Dictionary<Guid, SceneEntity> _entities;

        internal EntityTracker()
        {
            _uuids = new Dictionary<Scene, Dictionary<SceneEntity, Guid>>();
            _entities = new Dictionary<Guid, SceneEntity>();

            SceneEntityManager.EntityCreated += OnEntityCreated;
            SceneEntityManager.EntityDestroyed += OnEntityDestroyed;

            VoxelRuntime.Instance.SceneManager.SceneUnloaded += OnSceneUnloaded;
        }

        private void OnEntityCreated(SceneEntity entity)
        {
            ref Dictionary<SceneEntity, Guid>? dict = ref CollectionsMarshal.GetValueRefOrAddDefault(_uuids, entity.Scene, out bool exists);
            if (dict == null || !exists)
            {
                dict = new Dictionary<SceneEntity, Guid>();
            }

            Guid uuid = Guid.CreateVersion7();

            dict.Add(entity, uuid);
            _entities.Add(uuid, entity);
        }

        private void OnEntityDestroyed(SceneEntity entity)
        {
            ref Dictionary<SceneEntity, Guid> dict = ref CollectionsMarshal.GetValueRefOrNullRef(_uuids, entity.Scene);
            if (!Unsafe.IsNullRef(in dict) && dict.Remove(entity, out Guid uuid))
            {
                _entities.Remove(uuid);
                if (dict.Count == 0)
                    _uuids.Remove(entity.Scene);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (_uuids.Remove(scene, out Dictionary<SceneEntity, Guid>? dict))
            {
                foreach (var (_, uuid) in dict)
                {
                    _entities.Remove(uuid);
                }
            }
        }

        public static Guid GetUuid(SceneEntity entity) => EditorManager.Instance.EntityTracker._uuids[entity.Scene][entity];
        public static SceneEntity GetEntity(Guid uuid) => EditorManager.Instance.EntityTracker._entities[uuid];
    }
}
