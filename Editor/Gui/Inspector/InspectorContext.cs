using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;

namespace Editor.Gui.Inspector
{
    public readonly record struct InspectorContext
    {
        private readonly List<PropertyFieldData> _propertyFields;

        public InspectorContext()
        {
            _propertyFields = new List<PropertyFieldData>();
        }

        public void PropertyField(string propertyName, string classPropertyName)
        {
            _propertyFields.Add(new PropertyFieldData(propertyName, classPropertyName));
        }

        public void PropertyField(string propertyName, string classPropertyName, int arrayIndex)
            => PropertyField(propertyName, $"{classPropertyName}[{arrayIndex}]");

        public CollapsingHeaderScope CollapsingHeader(string? headerName, string headerText)
        {
            throw new NotImplementedException();
        }

        public ROList<PropertyFieldData> PropertyFields => _propertyFields;
    }

    public readonly record struct CollapsingHeaderScope : IDisposable
    {
        public void Dispose()
        {

        }
    }

    public readonly record struct PropertyFieldData(string PropertyName, string PathExpression);

    public enum InspectorFieldType : byte
    {
        Int32 = 0,
        UInt32,
        Int64,
        UInt64,
        Boolean,

        Enum,

        Color,
        Color32,

        Vector2,
        Vector3,
        Vector4,
        Quaternion,

        Int2,
        Int3,

        AABB,
        Boundaries,
        Rect,

        Object,
        Asset
    }
}
