using System;
using System.Collections.Generic;
using System.Text;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Data
{
    internal sealed class ClassState : DatatypeState
    {
        private readonly string _className;
        private readonly ClassState? _inheritedClass;

        private readonly List<MethodState> _methods;

        internal ClassState(string className, ClassState? inheritedClass)
        {
            _className = className;
            _inheritedClass = inheritedClass;

            _methods = new List<MethodState>();
        }

        public MethodState CreateMethod(string name, DatatypeState returnType, string? overload, string? comment)
        {
            foreach (MethodState method in _methods)
            {
                if (method.Name == name && method.Overload == overload)
                {
                    throw new DuplicateStateException($"Argument with name '{name}' already exists");
                }
            }

            _methods.Add(new MethodState(name, returnType, overload, comment, this));
            return _methods[^1];
        }

        public string Name => _className;
        public ClassState? InheritedClass => _inheritedClass;

        public List<MethodState> Methods => _methods;
    }
}
