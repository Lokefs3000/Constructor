using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Pooling;
using PrimaryEditor.Inspector.Setup;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Windows.Inspector.Widgets;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class WidgetGroup : IDisposable
    {
        private readonly InspectorWindow _window;

        private int _uniqueHash;
        private ViewDescription? _viewDescription;

        private Type? _targetType;

        private readonly List<InspectorGroup> _groups;
        private readonly Dictionary<int, InspectorWidget> _widgets;

        private readonly WidgetRelationship _containerFrame;

        private bool _disposedValue;

        internal WidgetGroup(InspectorWindow window)
        {
            _window = window;

            _uniqueHash = -1;
            _viewDescription = null;

            _targetType = null;

            _groups = new List<InspectorGroup>();
            _widgets = new Dictionary<int, InspectorWidget>();

            _containerFrame = new WidgetRelationship();
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
            _targetType = group.Type;

            if (VoxelRuntime.Instance.InspectorManager.ViewManager.TryGetCustomView(group.Type, out ViewDescription? view))
            {
                _viewDescription = view;
            }
            else if (VoxelRuntime.Instance.InspectorManager.DescriptionCache.TryGetDescription(group.Type, out InspectorDescription? description))
            {
                _viewDescription = VoxelRuntime.Instance.InspectorManager.ViewManager.GetViewForInspector(description);
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

        internal void RemoveGroup(InspectorGroup group)
        {
            Debug.Assert(group.UniqueHash == _uniqueHash);

            if (_groups.Remove(group))
            {
                if (_viewDescription != null)
                {
                    UnsetupForCustomView(_viewDescription, group.Values);
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

            void SetupSubValues(ImmutableArray<ViewObject> values, WidgetRelationship parentWidget)
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

                            parentWidget.AddChild(widgetField);
                        }
                        else
                        {
                            AppLog.Editor.Warning("Failed to find inspector value for custom view field '{n}'", field.FieldSource.Name);
                            continue;
                        }
                    }
                    else if (value is ViewGroup group && !group.Values.IsEmpty)
                    {
                        InspectorWidgetGroup widgetGroup = GetGroupWidget(group);
                        widgetGroup.AddDisplaySource(null, inspectorValues);

                        parentWidget.AddChild(widgetGroup);
                        SetupSubValues(group.Values, group.Conditions.IsEmpty ? parentWidget : widgetGroup.Relationships);
                    }
                    else if (value is ViewPreset preset && (preset.Presets.Count > 0 || !preset.Values.IsEmpty))
                    {
                        InspectorWidgetPreset widgetPreset = GetPresetWidget(preset);
                        widgetPreset.AddDisplaySource(null, inspectorValues);

                        parentWidget.AddChild(widgetPreset);
                        SetupSubValues(preset.Values, widgetPreset.Relationships);
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
        }

        private void UnsetupForCustomView(ViewDescription viewDescription, ROList<IInspectorValue> inspectorValues)
        {
            UnsetupSubValues(viewDescription.Values, _containerFrame);

            void UnsetupSubValues(ImmutableArray<ViewObject> values, WidgetRelationship parentWidget)
            {
                foreach (ViewObject value in values)
                {
                    if (value is ViewField field)
                    {
                        IInspectorValue? inspectorValue = FindInspectorValueFor(field.FieldSource);
                        if (inspectorValue != null)
                        {
                            InspectorWidgetField widgetField = GetFieldWidget(field);
                            widgetField.RemoveDisplaySource(inspectorValue, inspectorValues);

                            if (widgetField.InspectorValues.Count == 0)
                                parentWidget.RemoveChild(widgetField);
                        }
                    }
                    else if (value is ViewGroup group && !group.Values.IsEmpty)
                    {
                        InspectorWidgetGroup widgetGroup = GetGroupWidget(group);
                        widgetGroup.RemoveDisplaySource(null, inspectorValues);

                        if (widgetGroup.InspectorValues.Count == 0)
                            parentWidget.RemoveChild(widgetGroup);
                        else
                            UnsetupSubValues(group.Values, group.Conditions.IsEmpty ? parentWidget : widgetGroup.Relationships);
                    }
                    else if (value is ViewPreset preset && (preset.Presets.Count > 0 || !preset.Values.IsEmpty))
                    {
                        InspectorWidgetPreset widgetPreset = GetPresetWidget(preset);
                        widgetPreset.RemoveDisplaySource(null, inspectorValues);

                        if (widgetPreset.InspectorValues.Count == 0)
                            parentWidget.RemoveChild(widgetPreset);
                        else
                            UnsetupSubValues(preset.Values, widgetPreset.Relationships);
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
        }

        private InspectorWidgetField GetFieldWidget(ViewField field)
        {
            int hashCode = field.GetHashCode();
            ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

            if (!exists)
            {
                InspectorWidgetPool pool = VoxelRuntime.Instance.InspectorManager.InspectorWidgetPool;
                inspectorWidget = new InspectorWidgetField(); // pool.GetPooledWidget<InspectorWidgetField>();

                ((InspectorWidgetField)inspectorWidget).SetupForDisplay(field);
            }

            return (InspectorWidgetField)inspectorWidget!;
        }

        private InspectorWidgetGroup GetGroupWidget(ViewGroup group)
        {
            int hashCode = group.GetHashCode();
            ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

            if (!exists)
            {
                InspectorWidgetPool pool = VoxelRuntime.Instance.InspectorManager.InspectorWidgetPool;
                inspectorWidget = new InspectorWidgetGroup(); // pool.GetPooledWidget<InspectorWidgetGroup>();

                ((InspectorWidgetGroup)inspectorWidget).SetupForDisplay(group);
            }

            return (InspectorWidgetGroup)inspectorWidget!;
        }

        private InspectorWidgetPreset GetPresetWidget(ViewPreset preset)
        {
            int hashCode = preset.GetHashCode();
            ref InspectorWidget? inspectorWidget = ref CollectionsMarshal.GetValueRefOrAddDefault(_widgets, hashCode, out bool exists);

            if (!exists)
            {
                InspectorWidgetPool pool = VoxelRuntime.Instance.InspectorManager.InspectorWidgetPool;
                inspectorWidget = new InspectorWidgetPreset(); //pool.GetPooledWidget<InspectorWidgetPreset>();

                ((InspectorWidgetPreset)inspectorWidget).SetupForDisplay(preset);
            }

            return (InspectorWidgetPreset)inspectorWidget!;
        }

        public int UniqueHash => _uniqueHash;
        public ViewDescription? ViewDescription => _viewDescription;

        public Type? TargetType => _targetType;

        public WidgetRelationship Relationships => _containerFrame;

        public bool IsEmpty => _groups.Count == 0;

        private record struct ChildStack(IInspectorObject? Object, Widget Widget, int PreviousChildIndex);
    }
}
