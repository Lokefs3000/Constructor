using Editor.Gui.View;
using Editor.Gui.View.Elements;
using Editor.Gui.View.Toolbars;
using Editor.Interaction;
using Editor.Rendering;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Editor.UI.Modifiers;
using Editor.UI.Serialization;
using Primary.Input.Devices;
using Primary.Mathematics;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Gui.Windows
{
    public sealed class EditorViewWindow : UIWindow
    {
        private UIRawImage? _primaryView;
        private UIElement? _snippetElement;
        private UIFrame? _headerBar;

        private ToolViewSnippet? _toolViewSnippet;
        private RenderStatsSnippet? _renderStatsSnippet;

        private List<ViewSnippet> _activeSnippets;
        private List<(IEditorViewToolbar Toolbar, ToolbarGroup Group, bool UseNewerSystem)> _toolbarGroups;

        private Dictionary<StylesheetAsset, int> _externalStylesheets;

        private bool _isFirstLoad;

        public EditorViewWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            _primaryView = null;
            _snippetElement = null;

            _activeSnippets = new List<ViewSnippet>();
            _toolbarGroups = new List<(IEditorViewToolbar Toolbar, ToolbarGroup Group, bool UseNewerSystem)>();

            _externalStylesheets = new Dictionary<StylesheetAsset, int>();

            _isFirstLoad = true;

            _toolViewSnippet = null;
            _renderStatsSnippet = null;

            EditorRuntime editor = EditorRuntime.GlobalSingleton;
            editor.ReflectionManager.TypeLoader.AddCallback<ToolbarButtonAttribute>(OnToolbarButtonTypeLoaded);

            LayoutRecalculated += OnLayoutRecalculated;
            editor.UIManager.OnPreUpdateUI += OnPreUpdateUI;
        }

        protected override void InitializePostLoad()
        {
            _primaryView = FindElementWithId<UIRawImage>("PrimaryView");
            _snippetElement = FindElementWithId<UIElement>("SnippetElement");
            _headerBar = FindElementWithId<UIFrame>("header-bar");

            if (_snippetElement != null)
            {
                _toolViewSnippet ??= AddViewSnippet<ToolViewSnippet>("Editor/UI/Snippets/DefaultViewControls.snippet");
                _renderStatsSnippet ??= AddViewSnippet<RenderStatsSnippet>("Editor/UI/Snippets/RenderStats.snippet");

                _snippetElement.OnMouseMove += OnViewMouseMotion;
                _snippetElement.OnDragStart += OnViewDragCallback;
                _snippetElement.OnMouseDown += OnViewMouseDown;
                _snippetElement.OnMouseUp += OnViewMouseUp;

                foreach (ViewSnippet snippet in _activeSnippets)
                {
                    if (snippet.RootElement != null)
                    {
                        snippet.RootElement.Parent = _snippetElement;
                        snippet.RootElement.FindElementWithId<UIButton>("move-handle")?.OnDragStart += MoveHandleDragCallback;

                        foreach (StylesheetAsset stylesheet in snippet.Stylesheets)
                        {
                            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_externalStylesheets, stylesheet, out bool exists);
                            if (!exists)
                            {
                                count = 1;
                                AddStylesheet(stylesheet);
                            }
                            else
                                ++count;
                        }
                    }
                }
            }

            if (_headerBar != null)
            {
                foreach (var (toolbar, group, isNewSystem) in _toolbarGroups)
                {
                    if (isNewSystem)
                        toolbar.Initialize(!_isFirstLoad);
                    group.RootElement.Parent = _headerBar;
                }
            }

            if (_isFirstLoad)
            {
                LoadToolbar<ToolSpaceToolbar>();
            }

            _isFirstLoad = false;
        }

        protected override void CleanupSelf()
        {
            _snippetElement?.OnMouseMove -= OnViewMouseMotion;
            _snippetElement?.OnDragStart -= OnViewDragCallback;
            _snippetElement?.OnMouseDown -= OnViewMouseDown;
            _snippetElement?.OnMouseUp -= OnViewMouseUp;

            foreach (ViewSnippet snippet in _activeSnippets)
            {
                snippet.RootElement?.ClearParent();
            }

            foreach (var (toolbar, group, isNewSystem) in _toolbarGroups)
            {
                if (isNewSystem)
                    toolbar.Cleanup();
                group.RootElement.ClearParent();
            }

            foreach (var kvp in _externalStylesheets)
            {
                RemoveStylesheet(kvp.Key);
            }

            _externalStylesheets.Clear();
        }

        public override void Update()
        {
            foreach (ViewSnippet snippet in _activeSnippets)
            {
                snippet.Update();
            }
        }

        private void OnToolbarButtonTypeLoaded(Type type, object _)
        {
            if (!type.IsAssignableTo(typeof(IEditorViewToolbar)))
                return;

            object? obj = Activator.CreateInstance(type);
            if (obj == null)
                return;

            UIElement rootElement = new UIElement();
            {
                UIListLayout listLayout = rootElement.AddLayoutModifier<UIListLayout>();
                listLayout.Direction = UIListLayoutDirection.Horizontal;
                listLayout.Padding = new UIValue(4);

                UIFitterLayout fitterLayout = rootElement.AddLayoutModifier<UIFitterLayout>();
                fitterLayout.Axis = UIFitterAxis.Horizontal;

                rootElement.Size = new UIValue2(0.0f, 1.0f);
            }

            foreach (ToolbarButtonAttribute attrib in type.GetCustomAttributes<ToolbarButtonAttribute>())
            {
                FieldInfo? field = type.GetField(attrib.Id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    ToolbarItem? item = null;
                    if (field.FieldType == typeof(ToolbarDropdownItem))
                    {
                        item = new ToolbarDropdownItem();
                    }

                    if (item != null)
                    {
                        item.Parent = rootElement;
                        item.Size = new UIValue2(0.0f, 1.0f);

                        field.SetValue(obj, item);
                    }
                }
            }

            if (_headerBar != null)
                rootElement.Parent = _headerBar;

            ToolbarGroup group = new ToolbarGroup(rootElement, FrozenDictionary<string, ToolbarItem>.Empty);
            IEditorViewToolbar toolbar = (IEditorViewToolbar)obj;

            toolbar.Initialize(false);
            _toolbarGroups.Add((toolbar, group, true));
        }

        private void OnLayoutRecalculated()
        {
            if (_primaryView != null)
            {
                EditorRenderManager renderManager = EditorRuntime.GlobalSingleton.EditorRenderManager;
                renderManager.UpdateViewSize(_primaryView.ViewSize.AsInt2());

                EditorView.Instance.ViewSize = _primaryView.ViewSize;
            }
        }

        private void OnPreUpdateUI()
        {
            if (_primaryView != null)
            {
                EditorRenderManager renderManager = EditorRuntime.GlobalSingleton.EditorRenderManager;

                _primaryView.Image = renderManager.ViewTexture;
                _primaryView.AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        private void SnippetReloaded(LayoutSnippet obj)
        {
            if (obj.RootElement != null && _snippetElement != null)
            {
                foreach (StylesheetAsset stylesheet in obj.Stylesheets)
                {
                    ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_externalStylesheets, stylesheet, out bool exists);
                    if (!exists)
                    {
                        count = 1;
                        AddStylesheet(stylesheet);
                    }
                    else
                        ++count;
                }

                obj.RootElement.Parent = _snippetElement;
                obj.RootElement.FindElementWithId<UIButton>("move-handle")?.OnDragStart += MoveHandleDragCallback;
            }
        }

        private void SnippetCleanup(LayoutSnippet obj, bool isBeingDestroyed)
        {
            foreach (StylesheetAsset stylesheet in obj.Stylesheets)
            {
                ref int count = ref CollectionsMarshal.GetValueRefOrNullRef(_externalStylesheets, stylesheet);
                if (!Unsafe.IsNullRef(in count) && --count <= 0)
                {
                    StyleProvider.RemoveStylesheet(stylesheet);
                    _externalStylesheets.Remove(stylesheet);
                }
            }

            if (isBeingDestroyed && obj.RootElement?.Parent == _snippetElement)
            {
                obj.RootElement?.FindElementWithId<UIButton>("move-handle")?.OnDragStart -= MoveHandleDragCallback;
                obj.RootElement?.ClearParent();

                //obj.OnReloaded -= SnippetReloaded;
                //obj.OnCleanup -= SnippetCleanup;

                //_activeSnippets.Remove((ViewSnippet)obj);
            }
        }

        public T AddViewSnippet<T>(string snippetFile, params object?[]? arguments) where T : ViewSnippet
        {
            T snippet = UIManager.Instance.CreateSnippet<T>(snippetFile, arguments == null ? [this] : [this, .. arguments]);

            if (snippet.RootElement != null && _snippetElement != null)
            {
                snippet.RootElement.Parent = _snippetElement;
                snippet.RootElement.FindElementWithId<UIButton>("move-handle")?.OnDragStart += MoveHandleDragCallback;
            }

            foreach (StylesheetAsset stylesheet in snippet.Stylesheets)
            {
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_externalStylesheets, stylesheet, out bool exists);
                if (!exists)
                {
                    count = 1;
                    AddStylesheet(stylesheet);
                }
                else
                    ++count;
            }

            snippet.OnReloaded += SnippetReloaded;
            snippet.OnCleanup += SnippetCleanup;

            _activeSnippets.Add(snippet);
            return snippet;
        }

        internal (IEditorViewToolbar Toolbar, ToolbarGroup Group) LoadToolbar<T>(params object?[]? arguments) where T : class, IEditorViewToolbar
        {
            Type t = typeof(T);

            T? toolbar = Activator.CreateInstance(t, arguments) as T ?? throw new NotImplementedException();
            ToolbarGroup group = toolbar.SetupToolbar(new ToolbarSetupContext(this));

            if (_headerBar != null)
                group.RootElement.Parent = _headerBar;

            _toolbarGroups.Add((toolbar, group, false));
            return (toolbar, group);
        }

        private void OnViewMouseMotion(Vector2 mousePosition)
        {
            EditorView.Instance.MousePosition = mousePosition - _primaryView!.PixelCoordinates.Minimum;
        }

        private void OnViewDragCallback(OnDragStartContext ctx)
        {
            if (ctx.Button != MouseButton.Right)
                return;

            EditorView.Instance.UpdateButtonState(MouseButton.Right, false);

            Vector2 startPosition = ctx.Position;

            ctx.SetDragCallback((args) =>
            {
                EditorCamera camera = EditorCamera.Instance;

                Vector2 delta = (startPosition - args.Position) * 0.4f;

                camera.EulerAngles += new Vector2(-delta.Y, delta.X);
                startPosition = args.Position;
            });
        }

        private void OnViewMouseDown(MouseButton button)
        {
            EditorView.Instance.UpdateButtonState(button, true);
        }

        private void OnViewMouseUp(MouseButton button)
        {
            EditorView.Instance.UpdateButtonState(button, false);
        }

        private void MoveHandleDragCallback(OnDragStartContext ctx)
        {
            if (_snippetElement == null || ctx.Button != MouseButton.Left)
                return;

            UIButton button = (UIButton)ctx.Interactable;

            Vector2 startPosition = new Vector2(button.Parent.Position.X.Absolute, button.Parent.Position.Y.Absolute);
            ctx.SetDragCallback((args) =>
            {
                Vector2 localPos = Vector2.Round((startPosition + args.Delta) * 0.125f) * 8.0f;
                button.Parent.Position = new UIValue2((int)localPos.X, (int)localPos.Y);
            });
        }
    }
}
