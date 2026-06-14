using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Gui.Inspector;
using Editor.Inspector;
using Editor.Interaction;
using Editor.UI;
using Editor.UI.Elements;

namespace Editor.Gui.Windows
{
    internal sealed class InspectorWindow : UIWindow
    {
        private UIElement? _objectListElement;

        private InspectorHolder _inspectorHolder;
        private List<object> _inspectedValues;

        private ObjectCache _objectCache;

        public InspectorWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "Inspector";

            _inspectorHolder = new InspectorHolder();
            _inspectedValues = new List<object>();

            _objectCache = new ObjectCache();
        }

        protected override void InitializePostLoad()
        {
            _objectListElement = FindElementWithId<UIElement>("object-list");

            SelectionManager.ObjectSelected += OnObjectSelected;
            SelectionManager.ObjectDeselected += OnObjectDeselected;
        }

        protected override void CleanupSelf()
        {
            SelectionManager.ObjectSelected -= OnObjectSelected;
            SelectionManager.ObjectDeselected -= OnObjectDeselected;
        }

        public override void Update()
        {
            _inspectorHolder.UpdateTables(_inspectedValues.AsSpan());
        }

        private void SetupInspectorGui()
        {
            if (_objectListElement == null)
                return;

            InspectorValueTable valueTable = _inspectorHolder.ValueTables[0];
            InspectorManager manager = EditorRuntime.GlobalSingleton.InspectorManager;

            Dictionary<IInspectorBase, ObjectDropdown> parentMapping = new Dictionary<IInspectorBase, ObjectDropdown>();
            for (int i = 0; i < valueTable.Values.Count; i++)
            {
                IInspectorBase @base = valueTable.Values[i];

                UIElement? parentElement = _objectListElement;
                if (@base.Parent != null)
                {
                    if (parentMapping.TryGetValue(@base.Parent, out ObjectDropdown? parentDropdown))
                        parentElement = parentDropdown.ContentElement;
                    else
                        continue;
                }

                Type type = @base.PropertyType;
                if (!s_coreRuntimeTypes.Contains(type) && !manager.ViewManager.HasViewFor(type))
                {
                    ObjectDropdown dropdown = _objectCache.GetElement<ObjectDropdown>();

                    dropdown.RootElement.Parent = parentElement;
                    dropdown.Label.Text = @base.PropertyName;

                    parentMapping.Add(@base, dropdown);
                }
                else
                {
                    PropertyElements elements = _objectCache.GetPropertyTemplate();

                    elements.RootElement.Parent = parentElement;
                    elements.Label.Text = @base.PropertyName;

                    manager.ViewManager.SetupViewFor(type, _objectCache, elements.RootElement);
                }
            }
        }

        private void OnObjectSelected(object obj)
        {
            InspectorManager manager = EditorRuntime.GlobalSingleton.InspectorManager;
            manager.StartInspectingElement(_inspectorHolder, obj);

            _inspectedValues.Add(obj);

            if (_inspectorHolder.ValueTables.Count == 1)
            {
                SetupInspectorGui();
            }
        }

        private void OnObjectDeselected(object obj)
        {
            InspectorManager manager = EditorRuntime.GlobalSingleton.InspectorManager;
            manager.StopInspectingElement(_inspectorHolder, obj);

            _inspectedValues.Remove(obj);
        }

        private static FrozenSet<Type> s_coreRuntimeTypes = [
    typeof(sbyte),
    typeof(byte),
    typeof(short),
    typeof(ushort),
    typeof(int),
    typeof(uint),
    typeof(long),
    typeof(ulong),
    typeof(bool),
    typeof(nint),
    typeof(nuint),
    typeof(char),
    typeof(float),
    typeof(double),
    typeof(decimal),
    typeof(string)
    ];
    }
}
