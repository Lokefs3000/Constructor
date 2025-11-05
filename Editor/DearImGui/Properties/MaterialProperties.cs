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
    internal sealed class MaterialProperties : IObjectPropertiesViewer
    {
        private string? _localPath;

        private MaterialAsset? _material;
        //private string? _configFile;
        //private bool _hasLocalConfigFile;

        private bool _isImported;

        internal MaterialProperties()
        {

        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            if (_material != null)
            {
                ImGuiWidgets.SelectorAsset("Shader:", _material.Shader, (x) => _material.Shader = x);

                ImGui.Separator();

                foreach (var kvp in _material.Properties)
                {
                    MaterialProperty property = kvp.Value;
                    switch (property.Type)
                    {
                        case MaterialVariableType.Texture:
                            {
                                ImGuiWidgets.SelectorAsset(kvp.Key, _material.GetResource(kvp.Key) as TextureAsset, (x) => _material.SetResource(kvp.Key, x));
                                break;
                            }
                    }
                }
            }

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
                        EdLog.Gui.Error(ex, "Writing updated material data to disk failed: {f}", td.LocalPath);
                    }
                }
            }
            else
            {
                if (ImGui.Button(/*_hasLocalConfigFile ? "Import (L)"u8 : "Import (P)"u8*/"Import (A)"u8))
                {
                    try
                    {
                        SerializeArgumentsToDisk();
                    }
                    catch (Exception ex)
                    {
                        EdLog.Gui.Error(ex, "Writing importing material data to disk failed: {f}", td.LocalPath);
                    }
                }

                //if (ImGui.BeginPopupContextItem())
                //{
                //    if (ImGui.MenuItem("Project"u8, !_hasLocalConfigFile))
                //    {
                //        if (_hasLocalConfigFile)
                //        {
                //            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
                //
                //            _configFile = pipeline.Configuration.GetFilePath(td.LocalPath, "Shader");
                //            _hasLocalConfigFile = false;
                //        }
                //    }
                //
                //    if (ImGui.MenuItem("Local"u8, _hasLocalConfigFile))
                //    {
                //        if (!_hasLocalConfigFile)
                //        {
                //            ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(td.LocalPath));
                //            if (subFilesystem != null)
                //            {
                //                _configFile = Path.ChangeExtension(subFilesystem.GetFullPath(td.LocalPath), ".toml");
                //                _hasLocalConfigFile = true;
                //            }
                //        }
                //    }
                //
                //    ImGui.EndPopup();
                //}
            }

            ImGui.NewLine();
            ImGui.NewLine();

            if (ImGui.BeginChild("##VIEW", ImGuiChildFlags.Borders))
            {
                Vector2 avail = ImGui.GetContentRegionAvail();
                Vector2 screen = ImGui.GetCursorScreenPos();

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                if (_material != null && _material.Status == ResourceStatus.Success)
                {

                }

                string text = string.Empty;//$"{query.Width}x{query.Height} | {query.Format} | {FileUtility.FormatSize(_textureMemorySize, "F3", CultureInfo.InvariantCulture)}";

                Vector2 textSize = ImGui.CalcTextSize(text);
                Vector2 start = screen + new Vector2((avail.X - textSize.X) * 0.5f, avail.Y - textSize.Y * 2.0f);

                drawList.AddRectFilled(start - new Vector2(4.0f, 2.0f), start + textSize + new Vector2(4.0f, 2.0f), 0x80000000);
                drawList.AddText(start, 0xffffffff, text);

            }
            ImGui.EndChild();
        }

        public void Changed(object? target)
        {
            TargetData? td = target as TargetData;
            if (td != null)
            {
                AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;

                //_hasLocalConfigFile = false;

                string localPath = td.LocalPath;
                //string altToml = pipeline.Configuration.GetFilePath(localPath, "Model");
                //if (!File.Exists(altToml))
                //{
                //    ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(localPath));
                //    if (subFilesystem != null)
                //    {
                //        string newToml = Path.ChangeExtension(subFilesystem.GetFullPath(td.LocalPath), ".toml");
                //        if (File.Exists(newToml))
                //        {
                //            altToml = newToml;
                //            _hasLocalConfigFile = true;
                //        }
                //    }
                //}

                _localPath = localPath;

                //if (!File.Exists(altToml))
                //{
                //    _model = null;
                //    _configFile = altToml;
                //
                //    _isImported = false;
                //    return;
                //}

                _material = AssetManager.LoadAsset<MaterialAsset>(td.LocalPath);
                //_configFile = altToml;

                //using FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                //TomlTable document = Toml.ToModel<TomlTable>(File.ReadAllText(_configFile), localPath, new TomlModelOptions { IncludeFields = true });

                //_args = ModelAssetImporter.ReadTomlDocument(document);
                _isImported = true;
            }
            else
            {
                _localPath = null;
                _material = null;
                //_configFile = null;
                //_hasLocalConfigFile = false;
            }
        }

        private void SerializeArgumentsToDisk()
        {
            //Debug.Assert(_configFile != null);
            Debug.Assert(_material != null);
            Debug.Assert(_localPath != null);

            ProjectSubFilesystem? filesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(_localPath));
            if (filesystem == null)
            {
                EdLog.Gui.Error("Failed to find filesystem for material: {f}", _localPath);
                return;
            }

            using (FileStream stream = FileUtility.TryWaitOpen(filesystem.GetFullPath(_localPath), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (stream == null)
                {
                    EdLog.Gui.Error("Failed to write material toml data to disk: {f}", _localPath);
                    return;
                }

                TomlTable root = new TomlTable();
                root["shader"] = _material.Shader!.Id.Value;

                TomlTable properties = new TomlTable();
                foreach (var kvp in _material.Properties)
                {
                    if (_material.GetResource(kvp.Key) is IAssetDefinition asset)
                        properties[kvp.Key] = asset.Id.Value;
                }

                root["properties"] = properties;

                string source = Toml.FromModel(root, new TomlModelOptions
                {
                    //IncludeFields = true,
                });

                stream.Write(Encoding.UTF8.GetBytes(source));
            }

            if (Editor.GlobalSingleton.AssetPipeline.ImportChangesOrGetRunning(_localPath) != null)
            {
                _material = AssetManager.LoadAsset<MaterialAsset>(_localPath);

                _isImported = true;
            }
        }

        internal record class TargetData(string LocalPath);
    }
}
