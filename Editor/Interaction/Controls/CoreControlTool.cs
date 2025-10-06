using CommunityToolkit.HighPerformance;
using Editor.Interaction.Tools;
using Primary.Components;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Interaction.Controls
{
    internal sealed class CoreControlTool : IControlTool
    {
        private List<IToolTransform> _transformList;

        internal CoreControlTool(ToolManager tools)
        {
            _transformList = new List<IToolTransform>();
        }

        public void Activated()
        {
            _transformList.Clear();

            SelectionManager.NewSelected += Event_NewSelected;
            SelectionManager.OldDeselected += Event_OldDeselected;
        }

        public void Deactivated()
        {
            _transformList.Clear();

            SelectionManager.NewSelected -= Event_NewSelected;
            SelectionManager.OldDeselected -= Event_OldDeselected;
        }

        private void Event_NewSelected(SelectedBase @base)
        {
            if (@base is SelectedSceneEntity selected)
            {
                if (!_transformList.Exists((x) => x is EntityToolTransform transform && transform.Entity != selected.Entity))
                {
                    _transformList.Add(new EntityToolTransform(selected.Entity));
                    NewTransformSelected?.Invoke(_transformList.Last());
                }
            }
        }

        private void Event_OldDeselected(SelectedBase @base)
        {
            if (@base is SelectedSceneEntity selected)
            {
                int idx = _transformList.FindIndex((x) => x is EntityToolTransform transform && transform.Entity != selected.Entity);
                if (idx != -1)
                {
                    OldTransformDeselected?.Invoke(_transformList[idx]);
                    _transformList.RemoveAt(idx);
                }
            }
        }

        public ReadOnlySpan<IToolTransform> Transforms => _transformList.AsSpan();

        public event Action<IToolTransform>? NewTransformSelected;
        public event Action<IToolTransform>? OldTransformDeselected;
    }

    internal readonly record struct EntityToolTransform : IToolTransform
    {
        private readonly SceneEntity _entity;

        internal EntityToolTransform(SceneEntity entity)
        {
            _entity = entity;
        }

        public void SetWorldTransform(Vector3 position, Vector3 delta)
        {
            ref Transform transform = ref _entity.GetComponent<Transform>();
            if (!_entity.Parent.IsNull)
                transform.Position = position - -_entity.Parent.GetComponent<WorldTransform>().Transformation.Translation;
            else
                transform.Position = position;
        }

        public void CommitTransform()
        {
            
        }

        public SceneEntity Entity => _entity;

        public Vector3 Position { get => _entity.GetComponent<Transform>().Position; set => _entity.GetComponent<Transform>().Position = value; }
        public Quaternion Rotation { get => _entity.GetComponent<Transform>().Rotation; set => _entity.GetComponent<Transform>().Rotation = value; }
        public Vector3 Scale { get => _entity.GetComponent<Transform>().Scale; set => _entity.GetComponent<Transform>().Scale = value; }

        public Matrix4x4 WorldMatrix => _entity.GetComponent<WorldTransform>().Transformation;
    }
}
