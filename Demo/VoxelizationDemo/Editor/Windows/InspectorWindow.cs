using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Assets;
using Primary.Scenes.Components;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Search;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Popups;
using VoxelizationDemo.Editor.Windows.Inspector;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class InspectorWindow : IEditorWindow, IDisposable
    {
        private readonly int _uniqueId;

        private string _windowTitle;
        private InspectorContext? _activeContext;

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
            _activeContext = context ?? manager.Contexts.FirstOrDefault((x) => x != manager.PrimaryContext) ?? manager.PrimaryContext;

            manager.OnInspectStart += OnInspectStart;
            manager.OnInspectEnd += OnInspectEnd;
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
                InspectorContext? context = _activeContext ?? VoxelRuntime.Instance.InspectorManager.PrimaryContext;
                if (context != null)
                {
                    if (context is EntityInspectorContext entity)
                        HandleEntityContext(entity);
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

        private void HandleEntityContext(EntityInspectorContext context)
        {
            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;
            foreach (InspectorGroup group in context.Groups)
            {
                if (ImGui.CollapsingHeader(group.Type.Name))
                {
                    ImGui.Indent();

                    IInspectorValue? currentOwner = null;
                    int currentIndentDepth = 1;

                    ROList<IInspectorValue> values = group.Values;
                    for (int i = 0; i < values.Count; ++i)
                    {
                        IInspectorValue valueInterface = values[i];

                        IInspectorValue? thisParent = valueInterface.OwningValue;
                        if (thisParent != currentOwner)
                        {
                            do
                            {
                                currentOwner = thisParent;

                                if (currentIndentDepth > 0)
                                {
                                    --currentIndentDepth;
                                    ImGui.Unindent();
                                }
                            } while (thisParent != currentOwner);
                        }

                        if (valueInterface is IInspectorObject inspectorObject)
                        {
                            if (DrawObjectExpandingNode(VoxelRuntime.Instance, inspectorObject.TargetName))
                            {
                                currentOwner = inspectorObject;
                                ++currentIndentDepth;
                            }
                            else
                            {
                                for (int j = values.Count - 1; j > i; --j)
                                {
                                    if (values[j].OwningValue == inspectorObject)
                                    {
                                        i = j;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            ImGui.PushID(valueInterface.Name);

                            Type type = valueInterface.TargetType;
                            if (s_builtInTypes.Contains(type))
                            {
                                ViewBuiltInTypeValue(valueInterface, type);
                            }
                            else if (type.IsAssignableTo(typeof(IAssetDefinition)) || type == typeof(Sprite) || type == typeof(IRawRenderMesh))
                            {
                                DrawAssetSelector(valueInterface);
                            }

                            ImGui.PopID();
                        }
                    }

                    while (currentIndentDepth-- > 0)
                    {
                        ImGui.Unindent();
                    }
                }
            }

            {
                ImGui.NewLine();

                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                ImGui.SetCursorPosX(contentAvail.X * 0.125f + ImGui.GetStyle().WindowPadding.X * 2.0f);
                ImGui.Button("Add component"u8, new Vector2(contentAvail.X * 0.75f, 0.0f));

                if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonLeft))
                {
                    SceneEntityManager entityManager = SceneEntityManager.Instance;
                    foreach (var (type, entry) in entityManager.Registry.Entries)
                    {
                        if (!entry.Behaviour.CanBeAdded)
                            continue;

                        if (ImGui.MenuItem(type.Name, false, !context.Entity.HasComponent(type)))
                        {
                            context.Entity.AddComponent(type);
                        }
                    }

                    ImGui.EndPopup();
                }
            }
        }

        private void OnInspectStart(InspectorContext context)
        {
            if (s_primaryWindow == this || s_primaryWindow == null)
            {
                s_primaryWindow = this;
                _activeContext = context;
            }
        }

        private void OnInspectEnd(InspectorContext context)
        {
            if (context == _activeContext)
            {
                if (s_primaryWindow != this && s_primaryWindow != null)
                {
                    VoxelRuntime.Instance.EditorManager.WindowManager.CloseWindow(this);
                    return;
                }

                s_primaryWindow = this;
                _activeContext = null;
            }
        }

        private static void ViewBuiltInTypeValue(IInspectorValue inspectorValue, Type type)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            if (type == typeof(string))
            {

            }
            else
            {
                nint rawValue = 0;
                ImGuiDataType dataType = ImGuiDataType.Count;

                if (type == typeof(bool))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, bool>(ref inspectorValue.GetValueType()));
                    dataType = ImGuiDataType.Bool;
                }
                else if (type == typeof(byte) || type == typeof(sbyte))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, byte>(ref inspectorValue.GetValueType()));
                    dataType = type == typeof(sbyte) ? ImGuiDataType.S8 : ImGuiDataType.U8;
                }
                else if (type == typeof(short) || type == typeof(ushort))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, short>(ref inspectorValue.GetValueType()));
                    dataType = type == typeof(short) ? ImGuiDataType.S16 : ImGuiDataType.U16;
                }
                else if (type == typeof(int) || type == typeof(uint))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, short>(ref inspectorValue.GetValueType()));
                    dataType = type == typeof(int) ? ImGuiDataType.S32 : ImGuiDataType.U32;
                }
                else if (type == typeof(long) || type == typeof(ulong))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, long>(ref inspectorValue.GetValueType()));
                    dataType = type == typeof(long) ? ImGuiDataType.S64 : ImGuiDataType.U64;
                }
                else if (type == typeof(float))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, float>(ref inspectorValue.GetValueType()));
                    dataType = ImGuiDataType.Float;
                }
                else if (type == typeof(double))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref rawValue), Unsafe.As<OpaqueRef, double>(ref inspectorValue.GetValueType()));
                    dataType = ImGuiDataType.Double;
                }
                else
                {
                    return;
                }

                GetPropertyMetrics(context.FontSize + context.Style.FramePadding.Y * 2.0f, out Vector2 textCursor, out Vector2 widgetCursor);

                window.DC.CursorPos = textCursor;
                ImGui.TextUnformatted(inspectorValue.TargetName);

                window.DC.CursorPos = widgetCursor;
                if (dataType == ImGuiDataType.Bool)
                {
                    bool localValue = rawValue != 0;
                    if (ImGui.Checkbox("##"u8, ref localValue))
                    {
                        inspectorValue.SetValueType(localValue);
                    }
                }
                else
                {
                    unsafe
                    {
                        if (ImGui.InputScalar("##"u8, dataType, &rawValue))
                        {
                            if (type == typeof(sbyte))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<sbyte>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(byte))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<byte>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(short))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<short>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(ushort))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(int))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<int>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(uint))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<uint>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(long))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<long>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            else if (type == typeof(ulong))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            if (type == typeof(float))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<float>(ref Unsafe.As<nint, byte>(ref rawValue)));
                            if (type == typeof(double))
                                inspectorValue.SetValueType(Unsafe.ReadUnaligned<double>(ref Unsafe.As<nint, byte>(ref rawValue)));
                        }
                    }
                }
            }
        }

        private static void DrawAssetSelector(IInspectorValue inspectorValue)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 startCursorPos = window.DC.CursorPos;

            GetPropertyMetrics(context.FontSize + context.Style.FramePadding.Y * 2.0f, out Vector2 textCursor, out Vector2 widgetCursor);

            window.DC.CursorPos = textCursor;
            ImGui.TextUnformatted(inspectorValue.TargetName);

            ImRect bb = new ImRect(widgetCursor, new Vector2(startCursorPos.X + contentAvail.X, context.FontSize + context.Style.FramePadding.Y * 2.0f + widgetCursor.Y));

            ImRect handleBb = new ImRect(bb.Min, bb.Min + new Vector2(bb.Max.Y - bb.Min.Y));
            ImRect textBb = new ImRect(bb.Min.WithElement(0, handleBb.Max.X), bb.Max);

            uint handleId = ImGui.GetID("##HANDLE"u8);
            uint textId = ImGui.GetID("##TEXT"u8);

            bool isHandleHovered = false;
            bool isHandleHeld = false;

            if (ImGuiP.ButtonBehavior(handleBb, handleId, ref isHandleHovered, ref isHandleHeld))
            {
                SearchQuery query = runtime.EditorManager.SearchManager.GetPooledSearchQuery();
                query.AddParameter(inspectorValue.TargetType == typeof(IRawRenderMesh) ? typeof(MeshAsset) : inspectorValue.TargetType);

                async void SelectAsset()
                {
                    object? ret = await runtime.EditorManager.WindowManager.AddOpenPopup(new ChooseAssetPopup(query));
                    if (ret is ChooseAssetPopup.AssetChosenResponse value)
                    {
                        if (value.Id.IsInvalid)
                        {
                            inspectorValue.SetObject<object>(null);
                        }
                        else
                        {
                            if (inspectorValue.TargetType == typeof(Sprite))
                            {
                                TextureAtlasAsset textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>(value.Id);
                                await AssetManager.WaitForAssetLoadAsync(textureAtlas.Id);

                                inspectorValue.SetObject(textureAtlas.TryFindSpriteOrNull(value.Key!));
                            }
                            else
                            {
                                inspectorValue.SetObject((IAssetDefinition)AssetManager.LoadAsset(inspectorValue.TargetType == typeof(IRawRenderMesh) ? typeof(MeshAsset) : inspectorValue.TargetType, value.Id));
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
            object? currentValue = inspectorValue.GetObjectValue();

            if (currentValue != null)
            {
                if (currentValue is IAssetDefinition definition)
                    valueText = definition.Name;
                else if (currentValue is Sprite sprite)
                    valueText = sprite.Name;
                else
                    valueText = "<unknown>";
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

        private static void GetPropertyMetrics(float itemHeight, out Vector2 textCursor, out Vector2 widgetCursor)
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            textCursor = window.DC.CursorPos + new Vector2(0.0f, (itemHeight - context.FontSize) * 0.5f);
            widgetCursor = window.DC.CursorPos.WithElement(0, window.ContentSize.X * 0.45f + window.DC.CursorPos.X);
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
            typeof(string),
        }.ToFrozenSet();
    }
}
