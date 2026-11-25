using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Scenes.Json
{
    public sealed class SceneLoadException : Exception
    {
        private readonly SceneEntity _entity;
        private readonly Type? _type;

        public SceneLoadException(string message, SceneEntity entity) : base(message)
        {
            _entity = entity;
            _type = null;
        }

        public SceneLoadException(string message, SceneEntity entity, Type type) : base(message)
        {
            _entity = entity;
            _type = type;
        }

        public SceneEntity Entity => _entity;
        public Type? Type => _type;
    }
}
