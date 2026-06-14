using Editor.Geometry.Mesh;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Editor.Geometry
{
    public sealed class GeoScene
    {
        private readonly int _id;
        private readonly IdGenerator _idGenerator;

        private readonly List<BrushGroup> _groups;
        private readonly HashSet<Brush> _deletedBrushes;

        private readonly MeshGenerator _generator;
        private readonly MeshContainer _container;

        private bool _hasInvalidBrushes;
        private int _updateIndex;

        public GeoScene()
        {
            _id = 1733;
            _idGenerator = new IdGenerator();

            _groups = new List<BrushGroup>();
            _deletedBrushes = new HashSet<Brush>();

            _generator = new MeshGenerator();
            _container = new MeshContainer();

            _hasInvalidBrushes = false;
            _updateIndex = 0;
        }

        /// <summary>Not thread-safe</summary>
        public void RegenerateInvalidData(MaterialAsset? defaultMaterial = null)
        {
            ++_updateIndex;

            _generator.Generate(this, _container, defaultMaterial);
            _hasInvalidBrushes = false;
        }

        /// <summary>Not thread-safe</summary>
        public void ClearMeshData()
        {
            ++_updateIndex;
            _container.ClearMeshData();
        }

        /// <summary>Not thread-safe</summary>
        public BrushGroup CreateGroup(string name)
        {
            BrushGroup group = new BrushGroup(this, name);
            _groups.Add(group);

            OnGroupCreated?.Invoke(group);
            return group;
        }

        /// <summary>DANGEROUS: Not thread-safe</summary>
        public void AddGroup(BrushGroup group)
        {
            if (_groups.AddUnique(group))
            {
                foreach (Brush brush in group.Brushes)
                {
                    AddBrushId(brush.Id.LocalId);
                    _deletedBrushes.Remove(brush);

                    OnBrushCreated?.Invoke(brush);
                }

                if (group.Brushes.Count > 0)
                    InvalidateBrush();

                OnGroupCreated?.Invoke(group);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void DestroyGroup(BrushGroup group)
        {
            if (_groups.Remove(group))
            {
                foreach (Brush brush in group.Brushes)
                {
                    ReturnOldBrushId(brush.Id);
                    _deletedBrushes.Add(brush);

                    OnBrushDestroyed?.Invoke(brush);
                }

                if (group.Brushes.Count > 0)
                    InvalidateBrush();

                OnGroupDestroyed?.Invoke(group);
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void RegisterNewBrush(Brush brush)
        {
            OnBrushCreated?.Invoke(brush);
        }

        /// <summary>Not thread-safe</summary>
        internal void RegisterBrushDeletion(Brush brush)
        {
            InvalidateBrush();
            _deletedBrushes.Add(brush);

            OnBrushDestroyed?.Invoke(brush);
        }

        /// <summary>Not thread-safe</summary>
        internal void ClearDeletedBrushes() => _deletedBrushes.Clear();

        /// <summary>Not thread-safe</summary>
        internal void InvalidateBrush()
        {
            _hasInvalidBrushes = true;
            ++_updateIndex;
        }

        /// <summary>Not thread-safe</summary>
        internal BrushId GetNewBrushId() => new BrushId(_id, _idGenerator.GetId());
        /// <summary>Not thread-safe</summary>
        internal void ReturnOldBrushId(BrushId id) => _idGenerator.ReturnId(id.LocalId);
        /// <summary>Not thread-safe</summary>
        internal void AddBrushId(int id) => _idGenerator.AddId(id);

        public int Id => _id;

        public ROList<BrushGroup> Groups => _groups;
        internal HashSet<Brush> DeletedBrushes => _deletedBrushes;

        public MeshGenerator Generator => _generator;
        public MeshContainer Container => _container;

        public bool HasInvalidBrushes => _hasInvalidBrushes;
        public int UpdateIndex => _updateIndex;

        #region Events
        public event Action<BrushGroup>? OnGroupCreated;
        public event Action<BrushGroup>? OnGroupDestroyed;

        public event Action<Brush>? OnBrushCreated;
        public event Action<Brush>? OnBrushDestroyed;
        #endregion
    }
}
