using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interaction
{
    public interface ISelectionLocator
    {
        public SelectionGroup? Locate(SelectionManager selection, object obj);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SelectionLocatorTypesAttribute(params Type[] types) : Attribute
    {
        public Type[] Types { get; init; } = types;
    }
}
