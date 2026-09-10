using System.Runtime.CompilerServices;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Scenes;
using Primary.Scenes.Components;

namespace PrimaryEditor.Selection
{
    public sealed class SelectionManager : IDisposable
    {
        private readonly HashSet<object> _objects;

        private object? _activeContext;

        private object? _activeObject;
        private SceneEntity _activeEntity;

        private bool _disposedValue;

        public SelectionManager()
        {
            _objects = new HashSet<object>();

            _activeContext = null;

            _activeObject = null;
            _activeEntity = SceneEntity.Null;

            SceneEntityManager.EntityDestroyed += OnEntityDestroyedCallback;
            Engine.GlobalSingleton.SceneManager.SceneUnloaded += OnSceneUnloadedCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Clear();

                    SceneEntityManager.EntityDestroyed -= OnEntityDestroyedCallback;
                    Engine.GlobalSingleton.SceneManager.SceneUnloaded -= OnSceneUnloadedCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnEntityDestroyedCallback(SceneEntity entity)
        {
            object boxedEntity = entity;
            if (_objects.Contains(boxedEntity))
            {
                Deselect(boxedEntity);
            }
        }

        private void OnSceneUnloadedCallback(Scene scene)
        {
            if (_objects.Contains(scene))
            {
                Deselect(scene);
            }
        }

        private void DeselectAllExcept(object obj)
        {
            if (obj is SceneEntity entity)
            {
                if (entity != _activeEntity)
                {
                    OnActiveEntityChanged?.Invoke(_activeEntity, entity);
                    _activeEntity = entity;
                }
            }
            else if (!_activeEntity.IsNull)
            {
                OnActiveEntityChanged?.Invoke(_activeEntity, SceneEntity.Null);
                _activeEntity = SceneEntity.Null;
            }

            if (_activeObject != obj)
            {
                OnActiveObjectChanged?.Invoke(_activeObject, obj);
                _activeObject = obj;
            }

            if (_objects.Count > 1)
            {
                using RentedArray<object> toDeselect = new RentedArray<object>(_objects.Count - 1);

                int currentIndex = 0;
                foreach (object selectedObj in _objects)
                {
                    if (selectedObj != obj)
                    {
                        toDeselect[currentIndex++] = selectedObj;
                    }
                }

                _objects.Clear();
                _objects.Add(obj);

                foreach (object wasSelectedObj in toDeselect)
                {
                    OnObjectDeselected?.Invoke(wasSelectedObj);
                }
            }
        }

        public void Clear()
        {
            if (_activeEntity != SceneEntity.Null)
            {
                OnActiveEntityChanged?.Invoke(_activeEntity, SceneEntity.Null);
                _activeEntity = SceneEntity.Null;
            }

            if (_activeObject != null)
            {
                OnActiveObjectChanged?.Invoke(_activeObject, null);
                _activeObject = null;
            }

            object? val;
            while ((val = _objects.FirstOrDefault()) != null)
            {
                OnObjectDeselected?.Invoke(val);
                _objects.Remove(val);
            }
        }

        public void Select(object obj, bool allowDeselect = false, SelectMode mode = SelectMode.Clear)
        {
            if (_objects.Contains(obj))
            {
                if (allowDeselect)
                {
                    Deselect(obj);
                }
                else if (mode == SelectMode.Clear)
                {
                    DeselectAllExcept(obj);
                }

                return;
            }

            if (_objects.Count > 0 && mode == SelectMode.Clear)
            {
                Clear();
            }

            _objects.Add(obj);

            OnObjectSelected?.Invoke(obj);

            if (obj is SceneEntity entity && _activeEntity != entity)
            {
                OnActiveEntityChanged?.Invoke(_activeEntity, entity);
                _activeEntity = entity;
            }

            if (_activeObject != obj)
            {
                OnActiveObjectChanged?.Invoke(_activeObject, obj);
                _activeObject = obj;
            }
        }

        public void Deselect(object obj)
        {
            if (_objects.Remove(obj))
            {
                OnObjectDeselected?.Invoke(obj);

                if (obj is SceneEntity entity && _activeEntity == entity)
                {
                    SceneEntity entityToSelect = _objects.LastOrDefault(static (x) => x is SceneEntity) is SceneEntity newEntity ? newEntity : SceneEntity.Null;

                    OnActiveEntityChanged?.Invoke(_activeEntity, entityToSelect);
                    _activeEntity = entityToSelect;
                }

                if (_activeObject == obj)
                {
                    object? objectToSelect = _objects.LastOrDefault();

                    OnActiveObjectChanged?.Invoke(_activeObject, objectToSelect);
                    _activeObject = objectToSelect;
                }
            }
        }

        public bool IsSelected(object obj) => _objects.Contains(obj);

        public object? ActiveObject
        {
            get => _activeObject;
            set
            {
                if (value == null)
                {
                    if (_activeObject != null)
                        Deselect(_activeObject);
                }
                else
                {
                    Select(value);
                }
            }
        }

        public SceneEntity ActiveEntity
        {
            get => _activeEntity;
            set
            {
                if (value.IsNull)
                {
                    if (!_activeEntity.IsNull)
                        Deselect(value);
                }
                else
                {
                    Select(value);
                }
            }
        }

        public ROHashSet<object> Objects => _objects;

        public event Action<object>? OnObjectSelected;
        public event Action<object>? OnObjectDeselected;

        public event Action<object?, object?>? OnActiveObjectChanged;
        public event Action<SceneEntity, SceneEntity>? OnActiveEntityChanged;
    }

    public enum SelectMode : byte
    {
        Clear = 0,
        Append
    }
}
