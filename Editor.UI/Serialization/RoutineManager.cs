using CommunityToolkit.Diagnostics;
using Editor.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Editor.UI.Serialization
{
    internal sealed class RoutineManager
    {
        private Dictionary<Type, ISerializationRoutine?> _routines;

        internal RoutineManager()
        {
            _routines = new Dictionary<Type, ISerializationRoutine?>();
        }

        public bool TryGetRoutine(Type type, [NotNullWhen(true)] out ISerializationRoutine? routine)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(ISerializationRoutine)));

            if (_routines.TryGetValue(type, out routine))
                return routine != null;

            ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                UIManager.Logger?.Warning("Failed to create custom serilization routine: {r}, because not public constructor was found", type);

                _routines.Add(type, null);
                return false;
            }

            routine = (ISerializationRoutine?)constructor.Invoke(null);

            _routines.Add(type, routine);
            return routine != null;
        }
    }
}
