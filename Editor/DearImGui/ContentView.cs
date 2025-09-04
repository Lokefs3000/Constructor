using CommunityToolkit.HighPerformance;
using Editor.Assets;
using Editor.DearImGui.Components;
using Editor.DearImGui.Properties;
using Editor.Gui;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.RenderLayer;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui
{
    internal class ContentView
    {
        private AssetFilesystemWatcher _activeWatcher;
        private Stack<string> _directoryDepth;
        private string _directoryPath;

        private List<string> _pastDirectories;
        private int _index;

        private ContentIconBuilder _iconBuilder;
        private FrozenDictionary<int, int> _customIcons;
        private int _unknownFileIconId;
        private int _folderFileIconId;

        private DynamicIconSet _iconSet;

        private string? _activeTooltipPath = null;
        private long _fileLength;

        internal ContentView()
        {
            _activeWatcher = Editor.GlobalSingleton.AssetPipeline.ContentWatcher;
            _directoryDepth = new Stack<string>(["Content"]);
            _directoryPath = "Content";

            _pastDirectories = new List<string>(["Content"]);
            _index = 1;

            _iconBuilder = new ContentIconBuilder();
            _customIcons = FrozenDictionary<int, int>.Empty;
            _unknownFileIconId = -1;
            _folderFileIconId = -1;

            _iconSet = Editor.GlobalSingleton.GuiAtlasManager.CreateIconSet(
                "Content/Icons/HierchyArrow.png");

            _activeTooltipPath = null;
            _fileLength = -1;
        }

        private List<DeferredIcon> _deferredIconStack = new List<DeferredIcon>();
        internal void Render()
        {
            if (_iconBuilder.Texture == null)
            {
                BuildNewIconSet();
            }

            if (ImGui.Begin("Content", ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    float height = ImGui.GetFrameHeight();

                    GfxTexture atlas = _iconSet.AtlasTexture;
                    _iconSet.TryGetAtlasIcon("Content/Icons/HierchyArrow.png", out DynAtlasIcon icon);

                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    ImGuiStylePtr style = ImGui.GetStyle();

                    ImTextureRef texture = ImGuiUtility.GetTextureRef(atlas.Handle);

                    if (DrawIconLogic(1,
                        new Vector2(icon.UVs.Maximum.X, icon.UVs.Minimum.Y),
                        new Vector2(icon.UVs.Minimum.X, icon.UVs.Minimum.Y),
                        new Vector2(icon.UVs.Minimum.X, icon.UVs.Maximum.Y),
                        new Vector2(icon.UVs.Maximum.X, icon.UVs.Maximum.Y),
                        _pastDirectories.Count == 0 || _index == 0))
                    {
                        _index--;
                        SetCurrentStackDirectory(_pastDirectories[_index], false);
                    }

                    if (DrawIconLogic(2,
                        icon.UVs.Minimum,
                        new Vector2(icon.UVs.Maximum.X, icon.UVs.Minimum.Y),
                        icon.UVs.Maximum,
                        new Vector2(icon.UVs.Minimum.X, icon.UVs.Maximum.Y),
                        _index + 1 >= _pastDirectories.Count))
                    {
                        _index++;
                        SetCurrentStackDirectory(_pastDirectories[_index], false);
                    }

                    ImGui.BeginDisabled();

                    DrawIconLogic(3,
                        new Vector2(icon.UVs.Maximum.X, icon.UVs.Minimum.Y),
                        icon.UVs.Maximum,
                        new Vector2(icon.UVs.Minimum.X, icon.UVs.Maximum.Y),
                        icon.UVs.Minimum,
                        _directoryDepth.Count <= 1);

                    ImGui.EndDisabled();

                    int position = 0;
                    foreach (ReadOnlySpan<char> dir in _directoryPath.Tokenize('/'))
                    {
                        string @str = dir.ToString();
                        position += @str.Length + 1;

                        if (ImGui.MenuItem(@str) && _directoryDepth.Count > 1 && position < _directoryPath.Length)
                        {
                            SetCurrentStackDirectory(_directoryPath.Substring(0, position), true);
                        }

                        byte sep = (byte)'/';
                        ImGui.Text(ref sep);
                    }

                    //ImGui.ImageButton("##BACK", ImGuiUtility.GetTextureRef(atlas.Handle), new Vector2(height), new Vector2(icon.UVs.Maximum.X, icon.UVs.Minimum.Y), new Vector2(icon.UVs.Minimum.X, icon.UVs.Maximum.Y));
                    //ImGui.ImageButton("##FORWARD", ImGuiUtility.GetTextureRef(atlas.Handle), new Vector2(height), icon.UVs.Minimum, icon.UVs.Maximum);
                    //ImGui.ImageButton("##UP", ImGuiUtility.GetTextureRef(atlas.Handle), new Vector2(height), new Vector2(icon.UVs.Minimum.X, icon.UVs.Minimum.Y), new Vector2(icon.UVs.Maximum.X, icon.UVs.Maximum.Y));

                    ImGui.EndMenuBar();

                    bool DrawIconLogic(int id, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, bool disabled)
                    {
                        Vector2 screenPos = ImGui.GetCursorScreenPos() + new Vector2(2.0f);
                        Vector2 screenExtent = screenPos + new Vector2(height - 4.0f);

                        ImRect bb = new ImRect(screenPos - new Vector2(2.0f), screenPos + new Vector2(height));

                        uint iId = ImGui.GetID(id);

                        if (disabled)
                            ImGui.BeginDisabled();

                        bool hovered = false, held = false;
                        bool pressed = ImGuiP.ButtonBehavior(bb, iId, ref hovered, ref held);

                        ImGuiP.ItemAdd(bb, iId);
                        ImGuiP.ItemSize(bb);

                        if (disabled)
                            ImGui.EndDisabled();

                        uint bgColor =
                            held ? ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonActive]) :
                            (hovered ? ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonHovered]) : 0);

                        if (((bgColor >> 24) & 0xff) > 0)
                            drawList.AddRectFilled(bb.Min, bb.Max, bgColor);

                        drawList.AddImageQuad(texture,
                            screenPos,
                            new Vector2(screenExtent.X, screenPos.Y),
                            screenExtent,
                            new Vector2(screenPos.X, screenExtent.Y),
                            uv0,
                            uv1,
                            uv2,
                            uv3,
                            disabled ? 0x80ffffff : 0xffffffff);

                        return pressed;
                    }
                }

                if (ImGui.BeginChild("##HIERCHY", new Vector2(150.0f, 0.0f), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    DrawHierchyForWatcher(Editor.GlobalSingleton.AssetPipeline.ContentWatcher, "Content");
                    DrawHierchyForWatcher(Editor.GlobalSingleton.AssetPipeline.SourceWatcher, "Source");

                }
                ImGui.EndChild();

                ImGui.SameLine();

                if (ImGui.BeginChild("##VIEWER", ImGuiChildFlags.Borders))
                {
                    _deferredIconStack.Clear();

                    DrawWatcherIconSet();

                    unsafe
                    {
                        ImTextureRef texId = new ImTextureRef(null, new ImTextureID(_iconBuilder.Texture!.Handle));

                        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                        for (int i = 0; i < _deferredIconStack.Count; i++)
                        {
                            DeferredIcon icon = _deferredIconStack[i];
                            if (_iconBuilder.TryGetIcon(icon.IconId, out Boundaries uvBounds))
                                drawList.AddImage(texId, icon.Min, icon.Max, uvBounds.Minimum, uvBounds.Maximum, 0xffffffff);
                        }
                    }

                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        private void BuildNewIconSet()
        {
            _iconBuilder.ClearIcons();

            int fileUnknownIcon = _iconBuilder.AddIcon("Content/Icons/FileUnknown.png");
            int fileMaterialIcon = _iconBuilder.AddIcon("Content/Icons/FileMaterial.png");
            int fileShaderVariantIcon = _iconBuilder.AddIcon("Content/Icons/FileShaderVariant.png");

            _folderFileIconId = _iconBuilder.AddIcon("Content/Icons/Folder.png");

            Dictionary<int, int> iconAssocations = new Dictionary<int, int>
            {
                { ".mat".GetDjb2HashCode(), fileMaterialIcon },
                { ".shvar".GetDjb2HashCode(), fileShaderVariantIcon }
            };

            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
            foreach (AssetPipeline.AssetImporterData importer in pipeline.Importers)
            {
                string? customFileIcon = importer.Importer.CustomFileIcon;
                if (customFileIcon != null)
                {
                    int iconId = _iconBuilder.AddIcon(customFileIcon);
                    foreach (string extension in importer.AssociatedExtensions)
                    {
                        iconAssocations.TryAdd(extension.GetDjb2HashCode(), iconId);
                    }
                }
            }

            _iconBuilder.Build();

            _customIcons = iconAssocations.ToFrozenDictionary();
            _unknownFileIconId = fileUnknownIcon;
        }

        private void DrawWatcherIconSet()
        {
            lock (_activeWatcher.LockableTree)
            {
                if (_activeWatcher.GetDirectory(_directoryPath, out AssetDirectory directory))
                {
                    ImGuiContextPtr context = ImGui.GetCurrentContext();
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    ImGuiStylePtr style = ImGui.GetStyle();

                    Vector2 avail = ImGui.GetContentRegionAvail();
                    Vector2 absolute = ImGui.GetWindowPos() + style.WindowPadding;

                    Vector2 screen = ImGui.GetCursorScreenPos();

                    Vector2 fullSize = new Vector2(48.0f, 48.0f + style.ItemInnerSpacing.Y + context.FontSize) + style.ItemInnerSpacing * 2.0f;
                    Vector2 iconSize = new Vector2(fullSize.X, fullSize.Y - context.FontSize - style.ItemInnerSpacing.Y) - style.ItemInnerSpacing * 2.0f;

                    Boundaries fullBounds = new Boundaries(absolute, absolute + avail);

                    Vector2 cursor = screen;
                    for (int i = 0; i < directory.Subdirectories.Count; i++)
                    {
                        string dir = directory.Subdirectories[i];
                        string fullLocalPath = $"{_directoryPath}/{directory.Subdirectories[i]}";

                        HandleInput(ref dir, ref fullLocalPath, true);
                    }

                    for (int i = 0; i < directory.Files.Count; i++)
                    {
                        string file = directory.Files[i];
                        string fullLocalPath = $"{_directoryPath}/{file}";

                        HandleInput(ref file, ref fullLocalPath, false);
                    }

                    void HandleInput(ref readonly string iconText, ref readonly string fullLocalPath, bool isDirectory)
                    {
                        int iconId = _unknownFileIconId;

                        if (isDirectory)
                        {
                            iconId = _folderFileIconId;
                        }
                        else
                        {
                            ReadOnlySpan<char> pathSpan = fullLocalPath.AsSpan();
                            int dotLastFind = pathSpan.LastIndexOf('.');
                            if (dotLastFind != -1)
                            {
                                ReadOnlySpan<char> extension = pathSpan.Slice(dotLastFind);
                                if (!_customIcons.TryGetValue(extension.GetDjb2HashCode(), out iconId))
                                    iconId = _unknownFileIconId;
                            }
                        }

                        uint id = ImGui.GetID(fullLocalPath);
                        ImRect bb = new ImRect(cursor, cursor + fullSize);

                        ImGui.SetCursorScreenPos(cursor);

                        bool hovered = false, held = false;
                        bool pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);
                        ImGuiP.ItemAdd(bb, id);
                        ImGuiP.ItemSize(bb);

                        if (hovered)
                        {
                            if (!isDirectory && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            {
                                Process.Start("explorer.exe", $@"""{Path.Combine(Editor.GlobalSingleton.ProjectPath, fullLocalPath).Replace('/', '\\')}""");
                            }
                        }
                        if (pressed)
                        {
                            if (isDirectory)
                            {
                                SetCurrentStackDirectory(fullLocalPath, true);
                            }
                            else
                            {
                                string ext = Path.GetExtension(fullLocalPath);
                                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                                    Editor.GlobalSingleton.PropertiesView.SetInspected(new TextureProperties.TargetData(fullLocalPath));
                            }
                        }

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (!isDirectory)
                            {
                                if (ImGui.MenuItem("Edit"))
                                {
                                    
                                }
                            }

                            if (ImGui.MenuItem("Open with OS"))
                            {
                                Process.Start("explorer.exe", $@"""{Path.Combine(Editor.GlobalSingleton.ProjectPath, fullLocalPath).Replace('/', '\\')}""");
                            }

                            ImGui.Separator();

                            ImGui.BeginDisabled();
                            if (ImGui.MenuItem("Rename (WIP)")) ;
                            if (ImGui.MenuItem("Copy (WIP)")) ;
                            if (ImGui.MenuItem("Paste (WIP)")) ;
                            if (ImGui.MenuItem("Delete (WIP)")) ;
                            ImGui.EndDisabled();

                            ImGui.EndPopup();
                        }

                        if (ImGui.BeginItemTooltip())
                        {
                            if (_activeTooltipPath != fullLocalPath)
                            {
                                if (!isDirectory)
                                {
                                    try
                                    {
                                        _fileLength = new FileInfo(Path.Combine(Editor.GlobalSingleton.ProjectPath, fullLocalPath)).Length;
                                    }
                                    catch (Exception ex)
                                    {
                                        EdLog.Gui.Error(ex, "Failed to resolve file length");
                                    }
                                }

                                _activeTooltipPath = fullLocalPath;
                            }

                            string underlineText = isDirectory ?
                                string.Empty :
                                FileUtility.FormatSize(_fileLength, "F", CultureInfo.InvariantCulture);

                            ImDrawListPtr tooltipDrawList = ImGui.GetWindowDrawList();

                            Vector2 tooltipScreen = ImGui.GetCursorScreenPos();

                            Vector2 textSizeA = ImGui.CalcTextSize(iconText);
                            Vector2 textSizeB = ImGui.CalcTextSize(fullLocalPath);
                            Vector2 textSizeC = ImGui.CalcTextSize(underlineText);

                            float totalImageSize = context.FontSize * 3.0f + style.ItemInnerSpacing.Y * 2.0f;
                            float xOffset = totalImageSize + style.FramePadding.X;

                            tooltipDrawList.AddRectFilled(tooltipScreen, tooltipScreen + new Vector2(totalImageSize), 0x10ffffff, style.FrameRounding);
                            tooltipDrawList.AddText(tooltipScreen + new Vector2(xOffset + style.FramePadding.X, 0.0f), 0xffffffff, iconText);
                            tooltipDrawList.AddText(tooltipScreen + new Vector2(xOffset + style.FramePadding.Y, context.FontSize + style.ItemInnerSpacing.Y), 0x80ffffff, fullLocalPath);
                            if (underlineText.Length > 0)
                                tooltipDrawList.AddText(tooltipScreen + new Vector2(xOffset + style.FramePadding.Y, (context.FontSize + style.ItemInnerSpacing.Y) * 2.0f), 0x80ffffff, underlineText);

                            unsafe
                            {
                                if (_iconBuilder.TryGetIcon(iconId, out Boundaries iconUVs))
                                    tooltipDrawList.AddImage(new ImTextureRef(null, new ImTextureID(_iconBuilder.Texture!.Handle)), tooltipScreen + new Vector2(2.0f), tooltipScreen + new Vector2(totalImageSize - 4.0f), iconUVs.Minimum, iconUVs.Maximum);

                            }

                            ImGui.Dummy(new Vector2(MathF.Max(MathF.Max(textSizeA.X, textSizeB.X), textSizeC.X) + xOffset, totalImageSize));
                            ImGui.EndTooltip();
                        }

                        if (fullBounds.IsIntersecting(new Boundaries(bb.Min, bb.Max)))
                        {
                            Vector2 iconMin = cursor + style.ItemInnerSpacing;
                            Vector2 textMin = cursor + new Vector2(iconSize.X * 0.5f - MathF.Min(ImGui.CalcTextSize(iconText).X * 0.5f, iconSize.X * 0.5f - style.ItemInnerSpacing.X), fullSize.Y - style.ItemInnerSpacing.Y - context.FontSize);

                            Vector4 clipRect = new Vector4(cursor + style.ItemInnerSpacing, 0.0f, 0.0f);
                            Vector2 innerClipRect = cursor + fullSize - style.ItemInnerSpacing;

                            uint bgColor = 0x10ffffff;
                            if (held)
                                bgColor = 0x20ffffff;
                            else if (hovered)
                                bgColor = 0x30ffffff;

                            clipRect.Z = innerClipRect.X;
                            clipRect.W = innerClipRect.Y;

                            drawList.AddRectFilled(cursor, cursor + fullSize, bgColor, style.FrameRounding);
                            _deferredIconStack.Add(new DeferredIcon(iconId, iconMin, iconMin + iconSize));
                            drawList.AddText(ref Unsafe.NullRef<ImFont>(), context.FontSize, textMin, 0xffffffff, iconText, ref clipRect);
                        }

                        cursor.X += fullSize.X + style.FramePadding.X;
                        if (cursor.X + fullSize.X + style.FramePadding.X > avail.X + screen.X)
                        {
                            cursor.X = screen.X;
                            cursor.Y += fullSize.Y + style.FramePadding.Y;
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException("placeholder error");
                }
            }
        }

        private int _hierchyTreeDepth;
        private List<HierchyLinePoint> _hierchyLines = new List<HierchyLinePoint>();

        private void DrawHierchyForWatcher(AssetFilesystemWatcher watcher, string rootName)
        {
            _hierchyTreeDepth = 0;
            _hierchyLines.Clear();

            lock (watcher.LockableTree)
            {
                bool r = watcher.GetDirectory(rootName, out AssetDirectory directory);
                Debug.Assert(r);

                if (IconTreeNode(rootName, rootName, directory.Subdirectories.Count == 0, -1))
                {
                    if (directory.Subdirectories.Count > 0)
                    {
                        foreach (string subDirectory in directory.Subdirectories)
                        {
                            string fullPath = $"{rootName}/{subDirectory}";

                            r = watcher.GetDirectory(fullPath, out directory);
                            if (r)
                                AssetDirectoryRecursive(watcher, fullPath, subDirectory, directory, 1);
                            else
                                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "BAD DIRECTORY");
                        }
                    }

                    ImGui.TreePop();
                }
            }

            if (false && _hierchyLines.Count > 0)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                _hierchyLines.Sort((x, y) => x.Depth.CompareTo(y.Depth));

                Vector2 lastPosition = _hierchyLines[0].Position;
                int currentDepth = _hierchyLines[0].Depth;

                for (int i = 1; i < _hierchyLines.Count; i++)
                {
                    HierchyLinePoint point = _hierchyLines[i];
                    if (point.Depth != currentDepth)
                    {
                        currentDepth = point.Depth;
                    }
                    else
                    {
                        drawList.AddLine(lastPosition, point.Position, 0xffffffff);
                        drawList.AddLine(point.Position + new Vector2(1.0f, 0.0f), point.Position + new Vector2(8.0f, 0.0f), 0xffffffff);
                    }

                    lastPosition = point.Position;
                }
            }
        }

        private void AssetDirectoryRecursive(AssetFilesystemWatcher watcher, string rootPath, string rootName, AssetDirectory directory, int treeDepth)
        {
            if (IconTreeNode(rootPath, rootName, directory.Subdirectories.Count == 0, treeDepth))
            {
                if (directory.Subdirectories.Count > 0)
                {
                    treeDepth++;
                    foreach (string subDirectory in directory.Subdirectories)
                    {
                        string fullPath = $"{rootPath}/{subDirectory}";

                        bool r = watcher.GetDirectory(fullPath, out directory);
                        if (r)
                            AssetDirectoryRecursive(watcher, fullPath, subDirectory, directory, treeDepth);
                        else
                            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "BAD DIRECTORY");
                    }
                }

                ImGui.TreePop();
            }
        }

        private void SetCurrentStackDirectory(string fullLocalPath, bool addAsPast)
        {
            _directoryDepth.Clear();

            ReadOnlySpan<char> chars = fullLocalPath.AsSpan();
            foreach (ReadOnlySpan<char> token in chars.Tokenize('/'))
            {
                _directoryDepth.Push(token.ToString());
            }

            string newDirPath = BuildPathFromDirectoryStack();
            if (addAsPast)
            {
                if (_index != _pastDirectories.Count)
                    _pastDirectories.RemoveRange(_index, _pastDirectories.Count - _index);
                _pastDirectories.Add(newDirPath);
                _index = _pastDirectories.Count;
            }

            _directoryPath = newDirPath;
        }

        private string BuildPathFromDirectoryStack()
        {
            if (_directoryDepth.Count == 0)
                return string.Empty;

            string str = string.Empty;
            foreach (string path in _directoryDepth)
            {
                if (str.Length == 0)
                    str = path;
                else
                    str = $"{path}/{str}";
            }

            return str;
        }

        private unsafe bool IconTreeNode(string fullPath, string label, bool isLeafNode, int treeDepth)
        {
            ImGuiContextPtr g = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.PushID(fullPath);

            uint id = ImGui.GetID("a");
            uint ctx_id = ImGui.GetID("b");

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 labelSize = ImGui.CalcTextSize(label);

            ImRect full_bb = new ImRect(pos, new Vector2(pos.X + MathF.Max(ImGui.GetContentRegionAvail().X + g.FontSize + g.Style.FramePadding.Y * 2.0f + g.Style.ItemInnerSpacing.X, labelSize.X), pos.Y + g.FontSize + g.Style.FramePadding.Y * 2));
            ImRect arrow_bb = new ImRect(pos, pos + new Vector2(full_bb.Max.Y - full_bb.Min.Y));
            ImRect context_bb = new ImRect(new Vector2(arrow_bb.Max.X + g.Style.ItemInnerSpacing.X, arrow_bb.Min.Y), full_bb.Max + new Vector2(g.Style.ItemInnerSpacing.X, 0.0f));
            bool opened = ImGuiP.TreeNodeGetOpen(id);
            bool hovered, held;

            if (ImGuiP.ButtonBehavior(arrow_bb, id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
                ImGuiP.TreeNodeSetOpen(id, !opened);
            if (hovered || held)
                drawList.AddRectFilled(arrow_bb.Min, arrow_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            if (ImGuiP.ButtonBehavior(context_bb, ctx_id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
                ImGuiP.TreeNodeSetOpen(ctx_id, !opened);
            if (hovered || held)
                drawList.AddRectFilled(context_bb.Min, context_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            // Icon, text
            float button_sz = g.FontSize + g.Style.FramePadding.Y * 2;
            if (!isLeafNode)
                ImGuiP.RenderArrow(drawList, pos + new Vector2(3.0f), 0xffffffff, opened ? ImGuiDir.Down : ImGuiDir.Right);
            //_iconPositionStack.Add(pos + new Vector2(button_sz, 0.0f));
            drawList.AddRectFilled(context_bb.Min, context_bb.Min + new Vector2(button_sz, button_sz), 0xffff00ff);
            drawList.AddText(new Vector2(context_bb.Min.X + button_sz + g.Style.ItemInnerSpacing.X, context_bb.Min.Y + g.Style.FramePadding.Y), 0xffffffff, label);

            ImGuiP.ItemAdd(arrow_bb, id);
            ImGuiP.ItemAdd(context_bb, ctx_id);

            ImGuiP.ItemSize(full_bb, g.Style.FramePadding.Y);

            ImGui.PopID();

            if (opened)
            {
                ImGui.TreePush(label);
                _hierchyTreeDepth++;
            }

            if (treeDepth != -1)
            {
                _hierchyLines.Add(new HierchyLinePoint(Vector2.Lerp(arrow_bb.Min, arrow_bb.Max, 0.5f) - new Vector2(button_sz, 0.0f), treeDepth));
            }

            return opened;
        }

        private record struct HierchyLinePoint(Vector2 Position, int Depth);
        private record struct DeferredIcon(int IconId, Vector2 Min, Vector2 Max);
    }
}
