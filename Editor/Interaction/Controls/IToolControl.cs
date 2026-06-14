using Editor.Interaction.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interaction.Controls
{
    public interface IToolControl
    {
        public IToolTransform? Selected(object obj);
        public void Deselected(object obj, IToolTransform transform);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ToolControlTypesAttribute(params Type[] types) : Attribute
    {
        public Type[] Types { get; init; } = types;
    }
}
