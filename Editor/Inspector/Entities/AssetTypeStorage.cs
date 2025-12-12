using Editor.DearImGui;
using Primary.Assets;
using Primary.Assets.Types;

namespace Editor.Inspector.Entities
{
    internal sealed class VSMesh : ValueStorageBase
    {
        private RenderMesh? _value;
        private RenderMesh? _temp;

        public VSMesh(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = _field.GetValue(@ref) as RenderMesh;

            ImGuiWidgets.SelectorMesh(headerText, _value, (x) => _temp = x);
            if (_temp != null)
            {
                _field.SetValue(@ref, _temp);
                _temp = null;

                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSMaterial : ValueStorageBase
    {
        private MaterialAsset? _value;
        private MaterialAsset? _temp;

        public VSMaterial(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = _field.GetValue(@ref) as MaterialAsset;

            ImGuiWidgets.SelectorAsset(headerText, _value, (x) => _temp = x);
            if (_temp != null)
            {
                _field.SetValue(@ref, _temp);
                _temp = null;

                return @ref;
            }

            return null;
        }
    }

    internal sealed class VSGenericAsset : ValueStorageBase
    {
        private IAssetDefinition? _value;
        private IAssetDefinition? _temp;

        public VSGenericAsset(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = _field.GetValue(@ref) as IAssetDefinition;

            ImGuiWidgets.SelectorAsset(headerText, _field.Type, _value, (x) => _temp = x);
            if (_temp != null)
            {
                _field.SetValue(@ref, _temp);
                _temp = null;

                return @ref;
            }

            return null;
        }
    }
}
