using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Editor.Gui.Inspector;
using Editor.Reflection;
using Editor.UI.Datatypes;
using Editor.UI.Elements;

namespace Editor.Inspector.Gui
{
    internal sealed class InspectorViewManager
    {
        private Dictionary<Type, InspectorViewSetup> _inspectorViews;

        internal InspectorViewManager()
        {
            _inspectorViews = new Dictionary<Type, InspectorViewSetup>();

            AssemblyTypeLoader typeLoader = EditorRuntime.GlobalSingleton.ReflectionManager.TypeLoader;
            typeLoader.AddCallback<InspectorViewTargetAttribute>(OnInspectorViewTypeLoaded);
        }

        internal bool SetupViewFor(Type type, ObjectCache cache, UIElement parent)
        {
            if (!_inspectorViews.TryGetValue(type, out InspectorViewSetup viewSetup))
            {
                EdLog.Inspector.Warning("Failed to find inspector view for type {t}", type);
                return false;
            }

            float offset = 0.0f;
            foreach (InspectorElement elementData in viewSetup.Elements)
            {
                UIElement element = (UIElement)cache.GetElement(elementData.ElementType);

                element.Parent = parent;
                element.Position = new UIValue2(0, offset, elementData.Line * 30, 0.0f);
                element.Size = new UIValue2(0, elementData.Fill, 28, 0.0f);

                offset += elementData.Fill;
            }

            return true;
        }

        internal bool HasViewFor(Type type) => _inspectorViews.ContainsKey(type);

        private void OnInspectorViewTypeLoaded(Type type, object attribData)
        {
            if (!type.IsAssignableTo(typeof(IInspectorView)) || !type.IsClass)
                return;

            InspectorViewTargetAttribute attribute = (InspectorViewTargetAttribute)attribData;
            if (_inspectorViews.ContainsKey(attribute.Type))
            {
                EdLog.Inspector.Warning("An inspector view has already previously been assigned to the type {t}", attribute.Type);
                return;
            }

            IInspectorView instance;
            try
            {
                instance = (IInspectorView)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                EdLog.Inspector.Error(ex, "Failed to create inspector view {t}", type);
                return;
            }

            InspectorElementContext context = new InspectorElementContext();
            instance.SetupInspectorElements(ref context);

            _inspectorViews.Add(attribute.Type, new InspectorViewSetup(instance, context.Elements.ToImmutableArray(), context.Display));
        }
    }
}
