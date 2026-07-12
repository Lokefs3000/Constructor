using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections;
using Primary.Utility;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class WidgetGroup : IDisposable
    {
        private readonly InspectorWindow _window;

        private int _uniqueHash;
        private readonly List<InspectorGroup> _groups;
        private readonly Dictionary<int, InspectorWidget> _widgets;

        private readonly LayoutFrame _containerFrame;

        private bool _disposedValue;

        internal WidgetGroup(InspectorWindow window)
        {
            _window = window;

            _uniqueHash = -1;

            _groups = new List<InspectorGroup>();
            _widgets = new Dictionary<int, InspectorWidget>();

            _containerFrame = new LayoutFrame
            {
                Size = UIValue2.MaxX,
                AutoResize = AutoResizeMode.ResizeY
            };

            _containerFrame.TryAddClass("value-list");
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _containerFrame.Destroy();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetupGroupForHash(int uniqueHash)
        {
            _uniqueHash = uniqueHash;
        }

        internal void AddGroup(InspectorGroup group)
        {
            Debug.Assert(group.UniqueHash == _uniqueHash);

            if (_groups.AddUnique(group))
            {
                RentedList<ChildStack> previousChildIndexStack = [new ChildStack(null, _containerFrame, _containerFrame.Children.Count)];

                foreach (IInspectorValue inspectorValue in group.Values)
                {
                    ref ChildStack data = ref previousChildIndexStack[^1];
                    if (previousChildIndexStack.Count > 1)
                    {
                        while (previousChildIndexStack[^1].Object != inspectorValue.OwningValue && previousChildIndexStack.Count > 1)
                        {
                            previousChildIndexStack.RemoveAt(previousChildIndexStack.Count - 1);
                        }

                        data = ref previousChildIndexStack[^1];
                    }

                    if (_widgets.TryGetValue(inspectorValue.UniqueHash, out InspectorWidget? widget))
                    {
                        widget.AddValueSource(inspectorValue);
                        data.PreviousChildIndex = data.Widget.Children.IndexOf(widget.RootWidget);
                    }
                    else
                    {
                        widget = _window.WidgetPool.GetWidgetForValue(inspectorValue);

                        data.Widget.AddChild(widget.RootWidget);
                        data.Widget.TryMoveChild(widget.RootWidget, ++data.PreviousChildIndex);

                        _widgets.Add(inspectorValue.UniqueHash, widget);
                    }

                    if (inspectorValue is IInspectorObject obj)
                    {
                        Widget newWidget = ((InspectorWidgetObject)widget).ChildWidget;
                        previousChildIndexStack.Add(new ChildStack(obj, newWidget, newWidget.Children.Count));
                    }
                }
            }
        }

        public LayoutFrame RootWidget => _containerFrame;

        private record struct ChildStack(IInspectorObject? Object, Widget Widget, int PreviousChildIndex);
    }
}
