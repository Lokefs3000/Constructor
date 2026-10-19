using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Utility;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Search;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Popups;
using VoxelizationDemo.Editor.Windows.Inspector;
using VoxelizationDemo.Editor.Windows.Inspector.Views;
using VoxelizationDemo.Editor.Windows.Inspector.Widgets;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class InspectorWindow : IEditorWindow, IDisposable
    {
        private readonly int _uniqueId;

        private string _windowTitle;
        private List<InspectorContext> _activeContext;

        private readonly Dictionary<int, WidgetGroup> _groups;

        private readonly TextureAsset _contextIcons;

        internal InspectorWindow(InspectorContext? context)
        {
            InspectorManager manager = VoxelRuntime.Instance.InspectorManager;

            _uniqueId = 0;

            while (true)
            {
                foreach (IEditorWindow window in VoxelRuntime.Instance.EditorManager.WindowManager.Windows)
                {
                    if (window is InspectorWindow inspectorWindow && inspectorWindow._uniqueId == _uniqueId)
                    {
                        goto TryIdSearchAgain;
                    }
                }

                break;
            TryIdSearchAgain:
                ++_uniqueId;
            }

            _windowTitle = $"Inspector##{_uniqueId}";
            _activeContext = new List<InspectorContext>();

            _groups = new Dictionary<int, WidgetGroup>();

            _contextIcons = AssetManager.LoadAsset<TextureAsset>("Content/Textures/Editor/InspectorContextIcons.png");

            manager.OnInspectStart += OnInspectStart;
            manager.OnInspectEnd += OnInspectEnd;

            InspectorContext? contextToUse = context ?? manager.Contexts.FirstOrDefault((x) => x != manager.PrimaryContext) ?? manager.PrimaryContext;
            if (contextToUse != null)
                OnInspectStart(contextToUse);
        }

        void IDisposable.Dispose()
        {
            InspectorManager manager = VoxelRuntime.Instance.InspectorManager;
            manager.OnInspectStart -= OnInspectStart;
            manager.OnInspectEnd -= OnInspectEnd;

            if (s_primaryWindow == this)
                s_primaryWindow = null;
        }

        public bool UpdateAndRender()
        {
            if (ImGui.Begin(_windowTitle))
            {
                if (_activeContext.Count > 0)
                {
                    bool areAllContextsSame = true;
                    InspectorContext firstContext = _activeContext[0];

                    for (int i = 1; i < _activeContext.Count; ++i)
                    {
                        if (firstContext.GetType() != _activeContext[i].GetType())
                        {
                            areAllContextsSame = false;
                            break;
                        }
                    }

                    if (areAllContextsSame)
                    {
                        {
                            if (firstContext is EntityInspectorContext entityContext)
                            {
                                string firstEntityName = entityContext.Entity.Name;
                                bool firstEntityEnabled = entityContext.Entity.Enabled;

                                bool allNamesEqual = true;
                                bool allEnabledEqual = true;

                                for (int i = 1; i < _activeContext.Count; ++i)
                                {
                                    EntityInspectorContext entityContextLoop = (EntityInspectorContext)_activeContext[i];
                                    if (allNamesEqual && entityContextLoop.Entity.Name != firstEntityName)
                                    {
                                        allNamesEqual = false;
                                    }

                                    if (allEnabledEqual && entityContextLoop.Entity.Enabled != firstEntityEnabled)
                                    {
                                        allEnabledEqual = false;
                                    }

                                    if (!allNamesEqual && !allEnabledEqual)
                                        break;
                                }

                                ImGui.Image(EditorManager.Instance.HandleAllocator.GetHandleAsImTextureRef(_contextIcons), new Vector2(ImGui.GetFrameHeight()));
                                ImGui.SameLine();

                                ImGui.PushItemFlag((ImGuiItemFlags)ImGuiItemFlagsPrivate.MixedValue, !allEnabledEqual);
                                if (ImGui.Checkbox("###ENABLED"u8, ref firstEntityEnabled))
                                {
                                    foreach (InspectorContext context in _activeContext)
                                    {
                                        EntityInspectorContext entityContextLoop = (EntityInspectorContext)context;
                                        SceneEntity entity = entityContextLoop.Entity;
                                        entity.Enabled = firstEntityEnabled;
                                    }
                                }
                                ImGui.PopItemFlag();

                                if (!allNamesEqual)
                                    firstEntityName = "-";

                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                if (ImGui.InputText("###NAME"u8, ref firstEntityName, 128))
                                {
                                    foreach (InspectorContext context in _activeContext)
                                    {
                                        EntityInspectorContext entityContextLoop = (EntityInspectorContext)context;
                                        SceneEntity entity = entityContextLoop.Entity;
                                        entity.Name = firstEntityName;
                                    }
                                }

                                ImGui.Separator();
                            }
                        }

                        foreach (var (_, group) in _groups)
                        {
                            if (!group.IsEmpty)
                            {
                                if (ImGui.CollapsingHeader(group.TargetType!.Name, ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    ImGui.Indent();
                                    DrawWidgetsRecursive(group.TargetType!.FullName!, group.Relationships);
                                    ImGui.Unindent();
                                }
                            }
                        }

                        {
                            if (firstContext is EntityInspectorContext entityContext)
                            {
                                ImGui.NewLine();

                                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                                ImGui.SetCursorPosX(contentAvail.X * 0.125f + ImGui.GetStyle().WindowPadding.X);
                                ImGui.Button("Add component"u8, new Vector2(contentAvail.X * 0.75f, 0.0f));

                                if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonLeft))
                                {
                                    SceneEntityManager entityManager = SceneEntityManager.Instance;
                                    foreach (var (type, entry) in entityManager.Registry.Entries)
                                    {
                                        if (!entry.Behaviour.CanBeAdded)
                                            continue;

                                        if (ImGui.MenuItem(type.Name, false, !entityContext.Entity.HasComponent(type)))
                                        {
                                            foreach (InspectorContext context in _activeContext)
                                            {
                                                if (context is EntityInspectorContext entityContextLoop)
                                                {
                                                    entityContextLoop.Entity.AddComponent(type);
                                                }
                                            }
                                        }
                                    }

                                    ImGui.EndPopup();
                                }
                            }
                        }
                    }
                }

                if (ImGui.IsWindowFocused())
                    s_primaryWindow = this;
                else if (s_primaryWindow == this)
                    s_primaryWindow = null;
            }
            else if (s_primaryWindow == this)
            {
                s_primaryWindow = null;
            }

            ImGui.End();

            return true;
        }

        private void DrawWidgetsRecursive(string id, WidgetRelationship relationship)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImGuiWindowPtr window = context.CurrentWindow;

            float valueEndX = ImGui.GetContentRegionAvail().X;
            float valueStartX = window.DC.CursorPos.X + valueEndX * 0.5f;
            valueEndX += window.DC.CursorPos.X;

            float itemHeight = context.FontSize + context.Style.FramePadding.Y * 2.0f;

            // The table sizing breaks without borders for some stupid ass reason
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, 0);
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, 0);
            bool shouldDrawTable = ImGui.BeginTable(id, 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.Borders);
            ImGui.PopStyleColor(2);

            if (shouldDrawTable)
            {
                GetPropertyMetrics(itemHeight, out Boundaries textCursor, out Boundaries widgetCursor);
                unsafe
                {
                    ImGui.TableSetupColumn((byte*)null, ImGuiTableColumnFlags.WidthFixed, textCursor.Size.X);
                    ImGui.TableSetupColumn((byte*)null, ImGuiTableColumnFlags.WidthFixed, widgetCursor.Size.X);
                }

                ImGui.TableNextRow(itemHeight);
                foreach (InspectorWidget widget in relationship.Children)
                {
                    if (widget is InspectorWidgetField field)
                    {
                        bool hasMetAllConditions = true;
                        foreach (WidgetCondition condition in field.Conditions)
                        {
                            foreach (IInspectorValue valueToCompare in condition.Values)
                            {
                                if (!condition.View.HasPassedCondition(valueToCompare))
                                {
                                    hasMetAllConditions = false;
                                    break;
                                }
                            }

                            if (!hasMetAllConditions)
                                break;
                        }

                        bool shouldDrawInside = ImGui.TableNextColumn();

                        if (shouldDrawInside)
                        {
                            textCursor = new Boundaries(window.DC.CursorPos, window.DC.CursorPos + new Vector2(ImGui.GetContentRegionAvail().X, itemHeight));
                            field.DrawSelf(window.DrawList, textCursor.Minimum, textCursor.Maximum);

                            ImGui.Dummy(textCursor.Size);
                        }

                        shouldDrawInside = ImGui.TableNextColumn();

                        if (shouldDrawInside)
                        {
                            widgetCursor = new Boundaries(window.DC.CursorPos, window.DC.CursorPos + new Vector2(ImGui.GetContentRegionAvail().X, itemHeight));

                            IInspectorValue firstValue = field.InspectorValues[0];
                            ImGui.PushID(firstValue.FullTargetName);

                            Type type = firstValue.TargetType;
                            if (s_builtInTypes.Contains(type))
                            {
                                ViewBuiltInTypeValue(widgetCursor, field.InspectorValues, hasMetAllConditions, type);
                            }
                            else if (s_builtInStructs.Contains(type))
                            {
                                ViewBuiltInStructValue(widgetCursor, field.InspectorValues, hasMetAllConditions, type);
                            }
                            else if (type.IsAssignableTo(typeof(IAssetDefinition)) || type == typeof(Sprite) || type == typeof(IRawRenderMesh))
                            {
                                DrawAssetSelector(widgetCursor, field.InspectorValues, hasMetAllConditions);
                            }
                            else
                            {
                                ImGui.Dummy(widgetCursor.Size);
                            }

                            ImGui.PopID();
                        }
                    }
                    else if (widget is InspectorWidgetGroup group)
                    {

                    }
                    else if (widget is InspectorWidgetPreset preset)
                    {

                    }
                }

                ImGui.EndTable();
            }
        }

        private void OnInspectStart(InspectorContext context)
        {
            if (s_primaryWindow == this || s_primaryWindow == null)
            {
                if (!_activeContext.AddUnique(context))
                    return;

                if (_activeContext.Count == 1)
                {
                    s_primaryWindow ??= this;

                    foreach (var (_, group) in _groups)
                    {
                        group.Dispose();
                    }

                    _groups.Clear();
                }

                foreach (InspectorGroup group in context.Groups)
                {
                    if (_groups.TryGetValue(group.UniqueHash, out WidgetGroup? widgetGroup))
                    {
                        widgetGroup.AddGroup(group);
                    }
                    else
                    {
                        widgetGroup = new WidgetGroup(this);
                        widgetGroup.SetupGroupForHash(group);
                        widgetGroup.AddGroup(group);

                        _groups.Add(group.UniqueHash, widgetGroup);
                    }
                }

                context.OnGroupAdded += OnInspectGroupAdded;
                context.OnGroupRemoved += OninspectGroupRemoved;
            }
        }

        private void OnInspectEnd(InspectorContext context)
        {
            if (_activeContext.Remove(context))
            {
                if (s_primaryWindow != this && _activeContext.Count == 0)
                {
                    VoxelRuntime.Instance.EditorManager.WindowManager.CloseWindow(this);
                    return;
                }

                s_primaryWindow ??= this;

                foreach (InspectorGroup group in context.Groups)
                {
                    if (_groups.TryGetValue(group.UniqueHash, out WidgetGroup? widgetGroup))
                    {
                        widgetGroup.RemoveGroup(group);

                        if (widgetGroup.IsEmpty)
                        {
                            widgetGroup.Dispose();
                            _groups.Remove(group.UniqueHash);
                        }
                    }
                }

                context.OnGroupAdded -= OnInspectGroupAdded;
                context.OnGroupRemoved -= OninspectGroupRemoved;
            }
        }

        private void OnInspectGroupAdded(InspectorGroup group)
        {
            if (_groups.TryGetValue(group.UniqueHash, out WidgetGroup? widgetGroup))
            {
                widgetGroup.AddGroup(group);
            }
            else
            {
                widgetGroup = new WidgetGroup(this);
                widgetGroup.SetupGroupForHash(group);
                widgetGroup.AddGroup(group);

                _groups.Add(group.UniqueHash, widgetGroup);
            }
        }

        private void OninspectGroupRemoved(InspectorGroup group)
        {
            if (_groups.TryGetValue(group.UniqueHash, out WidgetGroup? widgetGroup))
            {
                widgetGroup.RemoveGroup(group);

                if (widgetGroup.IsEmpty)
                {
                    widgetGroup.Dispose();
                    _groups.Remove(group.UniqueHash);
                }
            }
        }

        private static void ViewBuiltInTypeValue(Boundaries cursor, ROList<IInspectorValue> inspectorValues, bool hasEquality, Type type)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            IInspectorValue firstValue = inspectorValues[0];

            if (type == typeof(string))
            {

            }
            else
            {
                nint rawValue = 0;
                ImGuiDataType dataType = ImGuiDataType.Count;

                if (type == typeof(bool))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, bool>(ref firstValue.GetValueType()));
                    dataType = ImGuiDataType.Bool;
                }
                else if (type == typeof(byte) || type == typeof(sbyte))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, byte>(ref firstValue.GetValueType()));
                    dataType = type == typeof(sbyte) ? ImGuiDataType.S8 : ImGuiDataType.U8;
                }
                else if (type == typeof(short) || type == typeof(ushort))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, short>(ref firstValue.GetValueType()));
                    dataType = type == typeof(short) ? ImGuiDataType.S16 : ImGuiDataType.U16;
                }
                else if (type == typeof(int) || type == typeof(uint))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, short>(ref firstValue.GetValueType()));
                    dataType = type == typeof(int) ? ImGuiDataType.S32 : ImGuiDataType.U32;
                }
                else if (type == typeof(long) || type == typeof(ulong))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, long>(ref firstValue.GetValueType()));
                    dataType = type == typeof(long) ? ImGuiDataType.S64 : ImGuiDataType.U64;
                }
                else if (type == typeof(float))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, float>(ref firstValue.GetValueType()));
                    dataType = ImGuiDataType.Float;
                }
                else if (type == typeof(double))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, double>(ref firstValue.GetValueType()));
                    dataType = ImGuiDataType.Double;
                }
                else
                {
                    return;
                }

                window.DC.CursorPos = cursor.Minimum;
                if (dataType == ImGuiDataType.Bool)
                {
                    ImGui.PushItemFlag((ImGuiItemFlags)ImGuiItemFlagsPrivate.MixedValue, !hasEquality);

                    bool localValue = rawValue != 0;
                    if (ImGui.Checkbox("##"u8, ref localValue))
                    {
                        SetAllValueTypes(inspectorValues, localValue);
                    }

                    ImGui.PopItemFlag();
                }
                else
                {
                    unsafe
                    {
                        ImGui.SetNextItemWidth(cursor.Size.X);
                        if (ImGui.InputScalar("##"u8, dataType, &rawValue))
                        {
                            if (type == typeof(sbyte))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<sbyte>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(byte))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<byte>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(short))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<short>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(ushort))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(int))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<int>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(uint))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<uint>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(long))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<long>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(ulong))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            if (type == typeof(float))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<float>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            if (type == typeof(double))
                                SetAllValueTypes(inspectorValues, Unsafe.ReadUnaligned<double>(ref Unsafe.As<nint, byte>(ref rawValue)));
                        }
                    }
                }
            }
        }

        private static unsafe void ViewBuiltInStructValue(Boundaries cursor, ROList<IInspectorValue> inspectorValues, bool hasEquality, Type type)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            IInspectorValue firstValue = inspectorValues[0];

            if (type == typeof(Vector2))
            {
                Vector2 v = Unsafe.As<OpaqueRef, Vector2>(ref firstValue.GetValueType());
                bool didChange = false;

                if (ImGui.BeginTable("##"u8, 2, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoClip, cursor.Size))
                {
                    ImGui.TableNextRow(cursor.Size.Y);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##0"u8, ImGuiDataType.Float, &v.X, "%f"u8, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##1"u8, ImGuiDataType.Float, &v.Y, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.EndTable();
                }

                if (didChange)
                    SetAllValueTypes(inspectorValues, v);
            }
            else if (type == typeof(Vector3))
            {
                Vector3 v = Unsafe.As<OpaqueRef, Vector3>(ref firstValue.GetValueType());
                bool didChange = false;

                if (ImGui.BeginTable("##"u8, 3, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoClip, cursor.Size))
                {
                    ImGui.TableNextRow(cursor.Size.Y);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##0"u8, ImGuiDataType.Float, &v.X, "%f"u8, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##1"u8, ImGuiDataType.Float, &v.Y, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##2"u8, ImGuiDataType.Float, &v.Z, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.EndTable();
                }

                if (didChange)
                    SetAllValueTypes(inspectorValues, v);
            }
            else if (type == typeof(Vector4))
            {
                Vector4 v = Unsafe.As<OpaqueRef, Vector4>(ref firstValue.GetValueType());
                bool didChange = false;

                if (ImGui.BeginTable("##"u8, 4, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoClip, cursor.Size))
                {
                    ImGui.TableNextRow(cursor.Size.Y);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##0"u8, ImGuiDataType.Float, &v.X, "%f"u8, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##1"u8, ImGuiDataType.Float, &v.Y, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##2"u8, ImGuiDataType.Float, &v.Z, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##3"u8, ImGuiDataType.Float, &v.W, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.EndTable();
                }

                if (didChange)
                    SetAllValueTypes(inspectorValues, v);
            }
            else if (type == typeof(Quaternion))
            {
                Quaternion v = Unsafe.As<OpaqueRef, Quaternion>(ref firstValue.GetValueType());
                bool didChange = false;

                if (ImGui.BeginTable("##"u8, 4, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoClip, cursor.Size))
                {
                    ImGui.TableNextRow(cursor.Size.Y);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##0"u8, ImGuiDataType.Float, &v.X, "%f"u8, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##1"u8, ImGuiDataType.Float, &v.Y, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##2"u8, ImGuiDataType.Float, &v.Z, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##3"u8, ImGuiDataType.Float, &v.W, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.EndTable();
                }

                if (didChange)
                    SetAllValueTypes(inspectorValues, v);
            }
            else if (type == typeof(Color))
            {
                Color v = Unsafe.As<OpaqueRef, Color>(ref firstValue.GetValueType());
                bool didChange = false;

                /*if (ImGui.BeginTable("##"u8, 5, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoClip, cursor.Size))
                {
                    ImGui.TableNextRow(cursor.Size.Y);
                    

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##0"u8, ImGuiDataType.Float, &v.R, "%f"u8, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##1"u8, ImGuiDataType.Float, &v.G, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##2"u8, ImGuiDataType.Float, &v.B, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputScalar("##3"u8, ImGuiDataType.Float, &v.A, ImGuiInputTextFlags.AutoSelectAll))
                        didChange = true;

                    ImGui.EndTable();
                }*/

                ImGui.SetNextItemWidth(cursor.Size.X);
                if (ImGui.ColorEdit4("##"u8, (float*)&v, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoLabel))
                    didChange = true;

                if (didChange)
                    SetAllValueTypes(inspectorValues, Color.Clamp(v));
            }
        }

        private static void DrawAssetSelector(Boundaries cursor, ROList<IInspectorValue> inspectorValues, bool hasEquality)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            IInspectorValue firstValue = inspectorValues[0];

            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 startCursorPos = window.DC.CursorPos;

            ImRect bb = new ImRect(cursor.Minimum, new Vector2(startCursorPos.X + contentAvail.X, context.FontSize + context.Style.FramePadding.Y * 2.0f + cursor.Minimum.Y));

            ImRect handleBb = new ImRect(bb.Min, bb.Min + new Vector2(bb.Max.Y - bb.Min.Y));
            ImRect textBb = new ImRect(bb.Min.WithElement(0, handleBb.Max.X), bb.Max);

            uint handleId = ImGui.GetID("##HANDLE"u8);
            uint textId = ImGui.GetID("##TEXT"u8);

            bool isHandleHovered = false;
            bool isHandleHeld = false;

            if (ImGuiP.ButtonBehavior(handleBb, handleId, ref isHandleHovered, ref isHandleHeld))
            {
                SearchQuery query = runtime.EditorManager.SearchManager.GetPooledSearchQuery();
                query.AddParameter(firstValue.TargetType == typeof(IRawRenderMesh) ? typeof(MeshAsset) : firstValue.TargetType);

                async void SelectAsset()
                {
                    object? ret = await runtime.EditorManager.WindowManager.AddOpenPopup(new ChooseAssetPopup(query));
                    if (ret is ChooseAssetPopup.AssetChosenResponse value)
                    {
                        if (value.Id.IsInvalid)
                        {
                            SetAllObjects<object>(inspectorValues, null);
                        }
                        else
                        {
                            if (firstValue.TargetType == typeof(Sprite))
                            {
                                TextureAtlasAsset textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>(value.Id);
                                await AssetManager.WaitForAssetLoadAsync(textureAtlas.Id);

                                SetAllObjects(inspectorValues, textureAtlas.TryFindSpriteOrNull(value.Key!));
                            }
                            else
                            {
                                SetAllObjects(inspectorValues, (IAssetDefinition)AssetManager.LoadAsset(firstValue.TargetType == typeof(IRawRenderMesh) ? typeof(MeshAsset) : firstValue.TargetType, value.Id));
                            }
                        }
                    }
                }

                SelectAsset();
            }

            bool isTextHovered = false;
            bool isTextHeld = false;

            if (ImGuiP.ButtonBehavior(textBb, textId, ref isTextHovered, ref isTextHeld))
            {
            }

            ImGuiCol handleBgCol = isHandleHeld ? ImGuiCol.FrameBgActive : (isHandleHovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            ImGuiCol textBgCol = isTextHeld ? ImGuiCol.FrameBgActive : (isTextHovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);

            window.DrawList.AddRectFilled(handleBb.Min, handleBb.Max, ImGui.GetColorU32(handleBgCol), context.Style.FrameRounding);
            window.DrawList.AddRectFilled(textBb.Min, textBb.Max, ImGui.GetColorU32(textBgCol), context.Style.FrameRounding);

            window.DrawList.AddRectFilled(handleBb.Min, handleBb.Max, 0x10ffffff, context.Style.FrameRounding);
            window.DrawList.AddCircle((handleBb.Min + handleBb.Max) * 0.5f, context.FontSize * 0.5f - 1.0f, 0x80ffffff, 2.0f);

            string valueText = "<null>";
            object? currentValue = firstValue.GetObjectValue();

            if (hasEquality)
            {
                if (currentValue != null)
                {
                    if (currentValue is IAssetDefinition definition)
                        valueText = definition.Name;
                    else if (currentValue is Sprite sprite)
                        valueText = sprite.Name;
                    else
                        valueText = "<unknown>";
                }
            }
            else
            {
                valueText = "<mixed>";
            }

            ImRect clippedTextBb = new ImRect(textBb.Min + context.Style.FramePadding, textBb.Max - context.Style.FramePadding);

            Vector2 textSize = ImGui.CalcTextSize(valueText);
            ImGui.RenderText(context.Font, window.DrawList, context.FontSize, new Vector2(clippedTextBb.Min.X - Math.Max(textSize.X - (clippedTextBb.Max.X - clippedTextBb.Min.X), 0.0f), clippedTextBb.Min.Y), 0xffffffff, Unsafe.BitCast<ImRect, Vector4>(clippedTextBb), valueText, ref Unsafe.NullRef<byte>());

            if (context.Style.FrameBorderSize > 0.0f)
                window.DrawList.AddRect(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.Border), context.Style.FrameRounding);

            ImGuiP.ItemAdd(handleBb, handleId);
            ImGuiP.ItemAdd(textBb, textId);

            // ImGuiP.ItemSize(bb, context.Style.FramePadding.Y);
        }

        private static void SetAllValueTypes<T>(ROList<IInspectorValue> inspectorValues, T value) where T : unmanaged
        {
            foreach (IInspectorValue inspectorValue in inspectorValues)
            {
                inspectorValue.SetValueType(value);
            }
        }

        private static void SetAllObjects<T>(ROList<IInspectorValue> inspectorValues, T? value) where T : class
        {
            foreach (IInspectorValue inspectorValue in inspectorValues)
            {
                inspectorValue.SetObject(value);
            }
        }

        private static void GetPropertyMetrics(float itemHeight, out Boundaries textCursor, out Boundaries widgetCursor)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            float itemWidth = window.ContentSize.X - Math.Min(100.0f, window.ContentSize.X * 0.45f);

            textCursor = Boundaries.FromRect(
                window.DC.CursorPos + new Vector2(0.0f, (itemHeight - context.FontSize) * 0.5f),
                new Vector2(window.ContentSize.X - itemWidth, context.FontSize));
            widgetCursor = Boundaries.FromRect(
                window.DC.CursorPos.WithElement(0, window.ContentSize.X - itemWidth + window.DC.CursorPos.X),
                new Vector2(itemWidth, itemHeight));
        }

        private static bool DrawObjectExpandingNode(VoxelRuntime runtime, string name)
        {
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            uint arrowId = ImGuiP.GetID(window, name);

            Vector2 cursorPos = window.DC.CursorPos;

            ImRect bb = new ImRect(cursorPos, cursorPos + new Vector2(ImGui.GetContentRegionAvail().X, context.FontSize + context.Style.FramePadding.Y * 2.0f));

            ImRect arrowBb = new ImRect(cursorPos, bb.Min + new Vector2(bb.Max.Y - bb.Min.Y));

            unsafe
            {
                if (window.StateStorage.GetIntRef(arrowId) == null)
                    ImGuiP.TreeNodeSetOpen(arrowId, true);
            }

            bool isOpened = ImGuiP.TreeNodeGetOpen(arrowId);

            bool isArrowHovered = false;
            bool isArrowHeld = false;

            if (ImGuiP.ButtonBehavior(arrowBb, arrowId, ref isArrowHovered, ref isArrowHeld))
                ImGuiP.TreeNodeSetOpen(arrowId, !isOpened);

            ImGuiCol backgroundColor = isArrowHeld ? ImGuiCol.HeaderActive : (isArrowHovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header);
            if (backgroundColor != ImGuiCol.Header)
                window.DrawList.AddRectFilled(arrowBb.Min, arrowBb.Max, ImGui.GetColorU32(backgroundColor));

            ImGuiP.RenderArrow(window.DrawList, arrowBb.Min + context.Style.FramePadding, 0xffffffff, isOpened ? ImGuiDir.Down : ImGuiDir.Right);
            ImGuiP.RenderText(arrowBb.Min.WithElement(0, arrowBb.Max.X) + new Vector2(context.Style.ItemInnerSpacing.X, context.Style.FramePadding.Y), name);

            ImGuiP.ItemSize(bb, context.Style.FramePadding.Y);
            ImGuiP.ItemAdd(arrowBb, arrowId);

            if (isOpened)
            {
                ImGui.Indent();
            }

            return isOpened;
        }

        private static InspectorWindow? s_primaryWindow = null;

        public static bool HasPrimaryWindow => s_primaryWindow != null;

        private static readonly FrozenDictionary<Type, IValueView> s_builtInViews = new Dictionary<Type, IValueView>
        {
            { typeof(bool), new BoolValueView() },
            { typeof(sbyte), new SByteValueView() },
            { typeof(byte), new ByteValueView() },
            { typeof(short), new ShortValueView() },
            { typeof(ushort), new UShortValueView() },
            { typeof(int), new IntValueView() },
            { typeof(uint), new UIntValueView() },
            { typeof(long), new LongValueView() },
            { typeof(ulong), new ULongValueView() },
            { typeof(float), new SingleValueView() },
            { typeof(double), new DoubleValueView() },
            { typeof(string), new StringValueView() },
        }.ToFrozenDictionary();

        private static readonly FrozenSet<Type> s_builtInTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string)
        }.ToFrozenSet();

        private static readonly FrozenSet<Type> s_builtInStructs = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color)
        }.ToFrozenSet();

        private readonly record struct WidgetList(InspectorWidget[] Widgets);
    }
}
