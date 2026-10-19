using System;
using System.Collections.Generic;
using System.Text;
using Primary.Scenes;

namespace Primary.Components
{
    public abstract class EntityBehaviour : IComponent
    {
        private SceneEntity _entity;
        private BehaviourFeatures _features;

        internal void SetBehaviourParams(SceneEntity entity, BehaviourFeatures features)
        {
            _entity = entity;
            _features = features;
        }

        protected internal virtual void OnEarlyInit()
        {
        }

        protected internal virtual void OnInit()
        {
        }

        protected internal virtual void OnDestroy()
        {
        }

        protected internal virtual void OnEnabled()
        {
        }

        protected internal virtual void OnDisabled()
        {
        }

        public SceneEntity Entity => _entity;
    }

    internal enum BehaviourFeatures : byte
    {
        None = 0,

        Update = 1 << 0,                // inherites IUpdateListener
        TransformUpdate = 1 << 1,       // inherites ITransformUpdateListener
    }
}
