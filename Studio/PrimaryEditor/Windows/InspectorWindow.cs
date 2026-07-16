using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using EditorUI.Serialization;
using EditorUI.Visual;
using EditorUI.Widgets;
using EditorUI.Windowing;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector;
using PrimaryEditor.UI.Diagnostics;
using PrimaryEditor.Windows.Inspector;

namespace PrimaryEditor.Windows
{
    public sealed class InspectorWindow : EditorWindow
    {
        private readonly GroupPool _groupPool;
        private readonly Dictionary<int, WidgetGroup> _groups;

        private LayoutFrame? _valuesView;

        public InspectorWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _groupPool = new GroupPool(this);
            _groups = new Dictionary<int, WidgetGroup>();

            InspectorManager inspector = EditorRuntime.Instance.InspectorManager;
            inspector.OnInspectStart += OnNewInspectionStart;

            LoadLayout("Editor/UI/Inspector.layout");
        }

        protected internal override void InitializeSelf()
        {
            _valuesView = RootWidget.FindWidgetWithId<LayoutFrame>("values-view", true);
        }

        protected internal override void CleanupReloadSelf()
        {
            _valuesView = null;
        }

        private void OnNewInspectionStart(InspectorContext context)
        {
            int previousChildIndex = _valuesView!.Children.Count;
            foreach (InspectorGroup group in context.Groups)
            {
                if (_groups.TryGetValue(group.UniqueHash, out WidgetGroup? widgetGroup))
                {
                    widgetGroup.AddGroup(group);
                    previousChildIndex = _valuesView!.Children.IndexOf(widgetGroup.RootWidget);
                }
                else
                {
                    widgetGroup = _groupPool.GetWidgetGroup(group);

                    _valuesView!.AddChild(widgetGroup.RootWidget);
                    _valuesView!.TryMoveChild(widgetGroup.RootWidget, ++previousChildIndex);

                    _groups.Add(group.UniqueHash, widgetGroup);
                }
            }
        }

        internal GroupPool GroupPool => _groupPool;
    }
}
