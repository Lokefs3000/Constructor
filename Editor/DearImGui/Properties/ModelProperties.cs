using CsToml;
using Editor.Assets;
using Editor.Assets.Importers;
using Editor.Processors;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Utility;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.DearImGui.Properties
{
    internal sealed class ModelProperties : IObjectPropertiesViewer
    {
        private string? _localPath;

        private ModelAsset? _model;
        private string? _configFile;
        private bool _hasLocalConfigFile;

        private ModelProcessorArgs _args;

        private bool _isImported;

        internal ModelProperties()
        {

        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            ImGuiWidgets.Checkbox("Compressed:", ref _args.IsCompressed);

            string indexStrideModeString = _args.IndexStrideMode.ToString();
            if (ImGuiWidgets.ComboBox("Index stride mode:", ref indexStrideModeString, s_indexStrideMode))
                _args.IndexStrideMode = Enum.Parse<ModelIndexStrideMode>(indexStrideModeString);

            ImGuiWidgets.Checkbox("Half transform precision:", ref _args.UseHalfPrecisionNodes);
            ImGuiWidgets.Checkbox("Half vertex precision:", ref _args.UseHalfPrecisionVertices);

            if (_isImported)
            {
                if (ImGui.Button("Revert"))
                {

                }

                ImGui.SameLine();

                if (ImGui.Button("Apply"))
                {
                    try
                    {
                        SerializeArgumentsToDisk();
                    }
                    catch (Exception ex)
                    {
                        EdLog.Gui.Error(ex, "Writing updated model data to disk failed: {f}", td.LocalPath);
                    }
                }
            }
            else
            {
                if (ImGui.Button(_hasLocalConfigFile ? "Import (L)"u8 : "Import (P)"u8))
                {
                    try
                    {
                        SerializeArgumentsToDisk();
                    }
                    catch (Exception ex)
                    {
                        EdLog.Gui.Error(ex, "Writing importing model data to disk failed: {f}", td.LocalPath);
                    }
                }

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Project"u8, !_hasLocalConfigFile))
                    {
                        if (_hasLocalConfigFile)
                        {
                            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;

                            _configFile = pipeline.Configuration.GetFilePath(td.LocalPath, "Shader");
                            _hasLocalConfigFile = false;
                        }
                    }

                    if (ImGui.MenuItem("Local"u8, _hasLocalConfigFile))
                    {
                        if (!_hasLocalConfigFile)
                        {
                            ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(td.LocalPath));
                            if (subFilesystem != null)
                            {
                                _configFile = Path.ChangeExtension(subFilesystem.GetFullPath(td.LocalPath), ".toml");
                                _hasLocalConfigFile = true;
                            }
                        }
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.NewLine();
            ImGui.NewLine();

            if (ImGui.BeginChild("##VIEW", ImGuiChildFlags.Borders))
            {
                Vector2 avail = ImGui.GetContentRegionAvail();
                Vector2 screen = ImGui.GetCursorScreenPos();

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                if (_model != null && _model.Status == ResourceStatus.Success)
                {
                    
                }

                string text = string.Empty;//$"{query.Width}x{query.Height} | {query.Format} | {FileUtility.FormatSize(_textureMemorySize, "F3", CultureInfo.InvariantCulture)}";

                Vector2 textSize = ImGui.CalcTextSize(text);
                Vector2 start = screen + new Vector2((avail.X - textSize.X) * 0.5f, avail.Y - textSize.Y * 2.0f);

                drawList.AddRectFilled(start - new Vector2(4.0f, 2.0f), start + textSize + new Vector2(4.0f, 2.0f), 0x80000000);
                drawList.AddText(start, 0xffffffff, text);

                ImGui.EndChild();
            }
        }

        public void Changed(object? target)
        {
            TargetData? td = target as TargetData;
            if (td != null)
            {
                AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;

                _hasLocalConfigFile = false;

                string localPath = td.LocalPath;
                string altToml = pipeline.Configuration.GetFilePath(localPath, "Model");
                if (!File.Exists(altToml))
                {
                    ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(localPath));
                    if (subFilesystem != null)
                    {
                        string newToml = Path.ChangeExtension(subFilesystem.GetFullPath(td.LocalPath), ".toml");
                        if (File.Exists(newToml))
                        {
                            altToml = newToml;
                            _hasLocalConfigFile = true;
                        }
                    }
                }

                _localPath = localPath;

                if (!File.Exists(altToml))
                {
                    _model = null;
                    _configFile = altToml;

                    _args = new ModelProcessorArgs
                    {
                        IsCompressed = false,

                        IndexStrideMode = ModelIndexStrideMode.Auto,

                        UseHalfPrecisionNodes = false,
                        UseHalfPrecisionVertices = false,
                    };

                    _isImported = false;
                    return;
                }

                _model = AssetManager.LoadAsset<ModelAsset>(td.LocalPath);
                _configFile = altToml;

                using FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                TomlTable document = Toml.ToModel<TomlTable>(File.ReadAllText(_configFile), localPath, new TomlModelOptions { IncludeFields = true });

                _args = ModelAssetImporter.ReadTomlDocument(document);
                _isImported = true;
            }
            else
            {
                _localPath = null;
                _model = null;
                _configFile = null;
                _hasLocalConfigFile = false;
            }
        }

        private void SerializeArgumentsToDisk()
        {
            Debug.Assert(_configFile != null);
            Debug.Assert(_localPath != null);

            using (FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (stream == null)
                {
                    EdLog.Gui.Error("Failed to write model toml data to disk: {f}", _configFile);
                    return;
                }

                TomlTable root = new TomlTable();
                root["is_compressed"] = _args.IsCompressed;

                root["index_stride_mode"] = _args.IndexStrideMode.ToString();

                root["use_half_precision_nodes"] = _args.UseHalfPrecisionNodes;
                root["use_half_precision_vertices"] = _args.UseHalfPrecisionVertices;

                string source = Toml.FromModel(root, new TomlModelOptions
                {
                    IncludeFields = true,
                });

                stream.Write(Encoding.UTF8.GetBytes(source));
            }

            if (Editor.GlobalSingleton.AssetPipeline.ImportChangesOrGetRunning(_localPath) != null)
            {
                _model = AssetManager.LoadAsset<ModelAsset>(_localPath);

                _isImported = true;
            }
        }

        private static readonly string[] s_indexStrideMode = Enum.GetNames<ModelIndexStrideMode>();

        internal record class TargetData(string LocalPath);
    }
}
