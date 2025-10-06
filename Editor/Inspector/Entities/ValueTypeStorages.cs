using Editor.DearImGui;
using System.Numerics;
using Vortice.Mathematics;

namespace Editor.Inspector.Entities
{
    internal sealed class VSSingle : ValueStorageBase
    {
        private float _value;

        public VSSingle(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = (float)_field.GetValue(@ref)!;
            if (ImGuiWidgets.InputFloat(headerText, ref _value))
            {
                _field.SetValue(@ref, _value);
                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSVector2 : ValueStorageBase
    {
        private Vector2 _value;

        public VSVector2(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = (Vector2)_field.GetValue(@ref)!;
            if (ImGuiWidgets.InputVector2(headerText, ref _value))
            {
                _field.SetValue(@ref, _value);
                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSVector3 : ValueStorageBase
    {
        private Vector3 _value;

        public VSVector3(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = (Vector3)_field.GetValue(@ref)!;
            if (ImGuiWidgets.InputVector3(headerText, ref _value))
            {
                _field.SetValue(@ref, _value);
                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSQuaternion : ValueStorageBase
    {
        private Quaternion _value;
        private Vector3 _euler;

        public VSQuaternion(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            Quaternion newQuat = (Quaternion)_field.GetValue(@ref)!;
            if (newQuat != _value)
            {
                _value = newQuat;
                _euler = newQuat.ToEuler();
            }

            if (ImGuiWidgets.InputVector3(headerText, ref _euler))
            {
                Vector3 rads = Vector3.DegreesToRadians(_euler);
                _value = Quaternion.CreateFromYawPitchRoll(rads.Y, rads.X, rads.Z);

                _field.SetValue(@ref, _value);
                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSEnum : ValueStorageBase
    {
        private Enum _value;
        private string[] _temp;

        public VSEnum(ReflectionField field) : base(field) { _temp = Enum.GetNames(field.Type); }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = (Enum)_field.GetValue(@ref)!;

            string selected = _value.ToString();
            if (ImGuiWidgets.ComboBox(headerText, ref selected, _temp))
            {
                _field.SetValue(@ref, Enum.Parse(_field.Type, selected));
                return @ref;
            }

            return null;
        }
    }
}
