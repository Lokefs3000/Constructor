using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Pooling;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;
using PrimaryEditor.Inspector.Widgets;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class WidgetGroup : IDisposable
    {
        private readonly InspectorWindow _window;

        private int _uniqueHash;
        private ViewDescription? _viewDescription;

        private readonly List<InspectorGroup> _groups;
        private readonly Dictionary<int, InspectorWidget> _widgets;

        private readonly ScrollView _containerFrame;

        private bool _disposedValue;

        internal WidgetGroup(InspectorWindow window)
        {
            _window = window;

            _uniqueHash = -1;
            _viewDescription = null;

            _groups = new List<InspectorGroup>();
            _widgets = new Dictionary<int, InspectorWidget>();

            _containerFrame = new ScrollView
            {
                Width = UIValue.Max,
                Scrollbars = Scrollbars.None
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

        internal void SetupGroupForHash(InspectorGroup group)
        {
            _uniqueHash = group.UniqueHash;

            if (EditorRuntime.Instance.InspectorManager.ViewManager.TryGetCustomView(group.Type, out ViewDescription? view))
            {
                _viewDescription = view;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal void AddGroup(InspectorGroup group)
        {
            Debug.Assert(group.UniqueHash == _uniqueHash);

            if (_groups.AddUnique(group))
            {
                if (_viewDescription != null)
                {
                    SetupForCustomView(_viewDescription, group.Values);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void SetupForCustomView(ViewDescription viewDescription, ROList<IInspectorValue> inspectorValues)
        {
            SetupSubValues(viewDescription.Values, _containerFrame);

            void SetupSubValues(ImmutableArray<ViewObject> values, Widget parentWidget)
            {
                foreach (ViewObject value in values)
                {
                    if (value is ViewField field)
                    {
                        IInspectorValue? inspectorValue = FindInspectorValueFor(field.FieldSource);
                        if (inspectorValue != null)
                        {
                            InspectorWidgetField widgetField = GetFieldWidget(field);
                            widgetField.AddDisplaySource(inspectorValue, inspectorValues);

                            parentWidget.AddChild(widgetField.RootWidget);
                        }
                        else
                        {
                            EdLog.Inspector.Warning("Failed to find inspector value for custom view field '{n}'", field.FieldSource.Name);
                            continue;
                        }
                    }
                    else if (value is ViewGroup group && !group.Values.IsEmpty)
                    {
                        InspectorWidgetGroup widgetGroup = GetGroupWidget(group);
                        widgetGroup.AddDisplaySource(null, inspectorValues);

                        parentWidget.AddChild(widgetGroup.RootWidget);
                        SetupSubValues(group.Values, group.Conditions.IsEmpty ? parentWidget : widgetGroup.RootWidget);
                    }
                    else if (value is ViewPreset preset && (preset.Presets.Count > 0 || !preset.Values.IsEmpty))
                    {
                        InspectorWidgetPreset widgetPreset = GetPresetWidget(preset);
                        widgetPreset.AddDisplaySource(null, inspectorValues);

                        parentWidget.AddChild(widgetPreset.RootWidget);
                        SetupSubValues(preset.Values, widgetPreset.ChildWidget);
                    }
                }
            }

            IInspectorValue? FindInspectorValueFor(ViewSource source)
            {
                foreach (IInspectorValue inspectorValue in inspectorValues)
                {
                    if (source.Type == inspectorValue.TargetType && inspectorValue.Name == source.Name)
                    {
                        return inspectorValue;
                    }
                }

                return null;
            }

            InspectorWidgetField GetFieldWidget(ViewField field)
            {
                int hashCode = field.GetHashCode();
                ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

                if (!exists)
                {
                    InspectorWidgetPool pool = EditorRuntime.Instance.InspectorManager.InspectorWidgetPool;
                    inspectorWidget = pool.GetPooledWidget<InspectorWidgetField>();

                    ((InspectorWidgetField)inspectorWidget).SetupForDisplay(field);
                }

                return (InspectorWidgetField)inspectorWidget!;
            }

            InspectorWidgetGroup GetGroupWidget(ViewGroup group)
            {
                int hashCode = group.GetHashCode();
                ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

                if (!exists)
                {
                    InspectorWidgetPool pool = EditorRuntime.Instance.InspectorManager.InspectorWidgetPool;
                    inspectorWidget = pool.GetPooledWidget<InspectorWidgetGroup>();

                    ((InspectorWidgetGroup)inspectorWidget).SetupForDisplay(group);
                }

                return (InspectorWidgetGroup)inspectorWidget!;
            }

            InspectorWidgetPreset GetPresetWidget(ViewPreset preset)
            {
                int hashCode = preset.GetHashCode();
                ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

                if (!exists)
                {
                    InspectorWidgetPool pool = EditorRuntime.Instance.InspectorManager.InspectorWidgetPool;
                    inspectorWidget = pool.GetPooledWidget<InspectorWidgetPreset>();

                    ((InspectorWidgetPreset)inspectorWidget).SetupForDisplay(preset);
                }

                return (InspectorWidgetPreset)inspectorWidget!;
            }
        }

        public Widget RootWidget => _containerFrame;

        private record struct ChildStack(IInspectorObject? Object, Widget Widget, int PreviousChildIndex);
    }
}
