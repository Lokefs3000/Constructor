using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Common.Streams;
using Primary.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Extra
{
    internal sealed class BundleExplorer
    {
        private bool _isOpen;

        private bool _isModified;
        private BundleReader? _activeReader;
        private List<string> _activeFiles;

        private byte[] _currentHexString;
        private int _lettersOnRowLast;

        private List<HexLine> _activeLines;
        private List<int> _overflowIndexSet;
        private HashSet<int> _activeIndexSet;

        private DataViewMode _viewMode;

        internal BundleExplorer()
        {
            _isOpen = false;

            _isModified = false;
            _activeReader = null;

            _currentHexString = Array.Empty<byte>();
            _lettersOnRowLast = 0;

            _activeLines = new List<HexLine>();
            _overflowIndexSet = new List<int>();
            _activeIndexSet = new HashSet<int>();

            _viewMode = DataViewMode.Hex;
        }

        internal void MenuBar()
        {
            if (ImGui.MenuItem("Bundle explorer"u8, _isOpen))
            {
                _isOpen = !_isOpen;
            }
        }

        internal void Render()
        {
            if (_isOpen)
            {
                ImGuiWindowFlags flags = ImGuiWindowFlags.MenuBar;
                if (_isModified)
                    flags |= ImGuiWindowFlags.UnsavedDocument;

                if (ImGui.Begin("Bundle explorer"u8, ref _isOpen, flags))
                {
                    if (ImGui.BeginMenuBar())
                    {
                        if (ImGui.BeginMenu("File"u8))
                        {
                            if (ImGui.MenuItem("Load"u8))
                            {
                                OpenFileDialogResult result = FileDialog.OpenFile(new OpenFileDialogParams
                                {
                                    DefaultDirectory = EditorFilepaths.ContentPath,
                                    Filters = [new FileFilter("Bundle files", "*.bundle")]
                                });

                                if (result.Result == FileDialogResult.Ok)
                                {
                                    try
                                    {
                                        _activeReader = new BundleReader(File.OpenRead(result.Files[0]), true, result.Files[0]);
                                    }
                                    catch (Exception ex)
                                    {
                                        EdLog.Gui.Error(ex, "Failed to create bundle reader for file: {br}", result.Files[0]);
                                    }
                                }
                            }

                            ImGui.EndMenu();
                        }

                        if (ImGui.BeginMenu(_viewMode switch
                        {
                            DataViewMode.Hex => "Hex"u8,
                            DataViewMode.Text => "Text"u8,
                            _ => "Unknown"u8
                        }))
                        {
                            if (ImGui.MenuItem("Hex"u8, _viewMode == DataViewMode.Hex))
                                _viewMode = DataViewMode.Hex;
                            if (ImGui.MenuItem("Text"u8, _viewMode == DataViewMode.Text))
                                _viewMode = DataViewMode.Text;

                            ImGui.EndMenu();
                        }
                    }
                    ImGui.EndMenuBar();

                    if (ImGui.BeginChild("LIST"u8, new Vector2(MathF.Min(ImGui.GetContentRegionAvail().X * 0.5f, 150.0f), 0.0f), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        if (_activeReader != null)
                        {
                            foreach (string key in _activeReader.Files)
                            {
                                if (ImGui.Selectable(key))
                                {
                                    _currentHexString = _activeReader.ReadBytes(key) ?? Array.Empty<byte>();
                                    _activeLines.Clear();
                                }

                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.MenuItem("Extract"u8))
                                    {
                                        SaveFileDialogResult result = FileDialog.SaveFile(new SaveFileDialogParams
                                        {
                                            DefaultDirectory = Environment.CurrentDirectory,
                                            DefaultFileName = key,
                                            Filters = [new FileFilter("Any file", "*.*")]
                                        });

                                        if (result.Result == FileDialogResult.Ok)
                                        {
                                            File.WriteAllBytes(result.File, _activeReader.ReadBytes(key) ?? Array.Empty<byte>());
                                        }
                                    }

                                    ImGui.EndPopup();
                                }
                            }
                        }
                    }
                    ImGui.EndChild();

                    ImGui.SameLine();

                    if (ImGui.BeginChild("MEM"u8, ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        if (_currentHexString.Length > 0)
                        {
                            ImGuiContextPtr context = ImGui.GetCurrentContext();
                            ref ImGuiWindowPtr currentWindow = ref context.CurrentWindow;

                            if (_viewMode == DataViewMode.Hex)
                            {
                                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                                Vector2 baseCursor = currentWindow.Pos + context.Style.FramePadding;

                                int lettersOnSingleRow = 12;

                                int totalRowCount = (int)MathF.Ceiling(_currentHexString.Length / (float)lettersOnSingleRow);

                                float textPosX = baseCursor.X + 7.0f * ((lettersOnSingleRow * 2) + lettersOnSingleRow - 1 + 4);

                                int scrollIndex = (int)MathF.Round(currentWindow.Scroll.Y / context.FontSize);
                                int maxRowsOnScreen = (int)MathF.Floor(contentAvail.Y / context.FontSize);

                                int maxScrollIndex = scrollIndex + maxRowsOnScreen;

                                ImGui.Dummy(new Vector2(0.0f, totalRowCount * context.FontSize));

                                if (_lettersOnRowLast != lettersOnSingleRow)
                                {
                                    _activeLines.Clear();
                                    _lettersOnRowLast = lettersOnSingleRow;
                                }

                                if (_activeLines.Count < maxRowsOnScreen)
                                    _activeLines.EnsureCapacity(maxRowsOnScreen);

                                _overflowIndexSet.Clear();
                                _activeIndexSet.Clear();

                                Span<HexLine> lines = _activeLines.AsSpan();
                                for (int i = 0; i < _activeLines.Count; i++)
                                {
                                    ref HexLine line = ref lines[i];
                                    if (line.ScrollIndex < scrollIndex || line.ScrollIndex > maxScrollIndex)
                                        _overflowIndexSet.Add(i);
                                    else
                                    {
                                        Vector2 cursor = new Vector2(baseCursor.X, baseCursor.Y + context.FontSize * (line.ScrollIndex - scrollIndex));
                                        currentWindow.DrawList.AddText(cursor, 0xffffffff, line.Hex);

                                        cursor.X = textPosX;
                                        currentWindow.DrawList.AddText(cursor, 0xffffffff, line.Letters.AsSpan(), ref line.Letters.AsSpan().DangerousGetReferenceAt(7));

                                        _activeIndexSet.Add(line.ScrollIndex);
                                    }
                                }

                                currentWindow.DrawList.AddText(baseCursor, 0xff0000ff, $"{scrollIndex} - {maxScrollIndex}");

                                if (_overflowIndexSet.Count > 0)
                                {
                                    int baseOffset = 0;
                                    for (int i = 0; i < _overflowIndexSet.Count; i++)
                                    {
                                        int idx = _overflowIndexSet[i] - baseOffset;
                                        _activeLines.RemoveAt(idx);

                                        baseOffset++;
                                    }
                                }

                                int bufferChunkPerLine = lettersOnSingleRow;
                                for (int i = scrollIndex; i < maxScrollIndex; i++)
                                {
                                    if (!_activeIndexSet.Contains(i))
                                    {
                                        ArraySegment<byte> segment = new ArraySegment<byte>(_currentHexString, i * bufferChunkPerLine, bufferChunkPerLine);

                                        byte[] hexString = new byte[segment.Count - 1 + segment.Count * 2];

                                        int offset = 0;
                                        for (int j = 0; j < segment.Count; j++)
                                        {
                                            string toStr = segment[j].ToString("x2", CultureInfo.InvariantCulture);

                                            hexString[offset++] = (byte)toStr[0];
                                            hexString[offset++] = (byte)toStr[1];
                                            if (j < segment.Count - 1)
                                                hexString[offset++] = (byte)' ';
                                        }

                                        _activeLines.Add(new HexLine(hexString, segment, i));
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                ImGui.TextWrapped(_currentHexString);
                            }
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.End();
            }
        }

        private readonly record struct HexLine(byte[] Hex, ArraySegment<byte> Letters, int ScrollIndex);
        private readonly record struct BundleFile(string Alias, string FullPath, bool IsBundleEmbedded);

        private enum DataViewMode : byte
        {
            Hex,
            Text
        }
    }
}
