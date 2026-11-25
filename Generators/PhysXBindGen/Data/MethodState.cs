using System;
using System.Collections.Generic;
using System.Text;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Data
{
    internal sealed class MethodState : DatatypeState
    {
        private readonly string _name;
        private readonly DatatypeState _returnType;
        private readonly string? _overload;
        private readonly string? _comment;

        private readonly ClassState? _owner;

        private readonly List<MethodArgument> _arguments;
    
        internal MethodState(string name, DatatypeState returnType, string? overload, string? comment, ClassState? owner)
        {
            _name = name;
            _returnType = returnType;
            _overload = overload;
            _comment = comment;

            _owner = owner;

            _arguments = [];
        }

        public void AddArgument(string name, DatatypeState type, double? defaultValue)
        {
            foreach (MethodArgument argument in _arguments)
            {
                if (argument.Name == name)
                {
                    throw new DuplicateStateException($"Argument with name '{name}' already exists");
                }
            }

            _arguments.Add(new MethodArgument(name, type, defaultValue));
        }

        public string Name => _name;
        public DatatypeState ReturnType => _returnType;
        public string? Overload => _overload;
        public string? Comment => _comment;

        public ClassState? Owner => _owner;

        public List<MethodArgument> Arguments => _arguments;

        public const double Null = double.NaN;
    }

    internal readonly record struct MethodArgument(string Name, DatatypeState Type, double? DefaultValue);
}
