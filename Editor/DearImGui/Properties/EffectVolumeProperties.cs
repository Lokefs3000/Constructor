using Arch.Core;
using Editor.Assets;
using Editor.Inspector.Components;
using Editor.Inspector.Editors;
using Editor.Processors;
using Editor.Serialization.Json;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.PostProcessing;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.DearImGui.Properties
{
    internal sealed class EffectVolumeProperties : IObjectPropertiesViewer
    {
        private PostProcessingVolumeAsset? _asset;
        private Dictionary<Type, ObjectEditor> _editors;

        internal EffectVolumeProperties()
        {
            _asset = null;
            _editors = new Dictionary<Type, ObjectEditor>();

            s_serializerOptions.Converters.Add(new AssetJsonConverter());
            s_serializerOptions.Converters.Add(new TextureJsonConverter());
        }

        public void Render(object target)
        {
            TargetData? td = Unsafe.As<TargetData>(target);
            if (td != null && _asset != null)
            {
                foreach (IPostProcessingData effect in _asset.Effects)
                {
                    if (!_editors.TryGetValue(effect.GetType(), out ObjectEditor? editor))
                    {
                        editor = new DefaultObjectEditor();
                        editor.SetupInspectorFields(effect);

                        _editors.Add(effect.GetType(), editor);
                    }    

                    editor?.DrawInspector();
                }

                ImGui.Button("Add effect"u8, new Vector2(-1.0f, 0.0f));
                ImGui.OpenPopupOnItemClick("##ADDFX"u8, ImGuiPopupFlags.MouseButtonLeft);

                if (ImGui.BeginPopup("##ADDFX"u8, ImGuiWindowFlags.NoMove))
                {
                    if (ImGui.Selectable("Enviorment effect"u8))
                        _asset.AddEffect<EnviormentEffectData>();

                    ImGui.EndPopup();
                }

                if (ImGui.Button("Revert"))
                {

                }

                ImGui.SameLine();

                if (ImGui.Button("Apply"))
                {
                    try
                    {
                        SerializeArgumentsToDisk(td.LocalPath);
                    }
                    catch (Exception ex)
                    {
                        EdLog.Gui.Error(ex, "Writing updated texture data to disk failed: {f}", td.LocalPath);
                    }
                }
            }
        }

        public void Changed(object? target)
        {
            if (target != null && target is TargetData td)
            {
                _asset = AssetManager.LoadAsset<PostProcessingVolumeAsset>(td.LocalPath, true);
                _editors = new Dictionary<Type, ObjectEditor>();

                foreach (IPostProcessingData effect in _asset.Effects)
                {
                    DefaultObjectEditor objectEditor = new DefaultObjectEditor();
                    objectEditor.SetupInspectorFields(effect);

                    _editors.Add(effect.GetType(), objectEditor);
                }
            }
            else
            {
                _asset = null;
                _editors.Clear();
            }
        }

        private void SerializeArgumentsToDisk(string localPath)
        {
            if (_asset == null)
                return;

            ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(localPath));
            if (subFilesystem != null)
            {
                using (FileStream stream = FileUtility.TryWaitOpen(subFilesystem.GetFullPath(localPath), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (stream == null)
                    {
                        EdLog.Gui.Error("Failed to write effect volume data to disk: {f}", localPath);
                        return;
                    }

                    string s = JsonSerializer.Serialize(_asset.Effects, s_serializerOptions);
                    stream.Write(Encoding.UTF8.GetBytes(s));
                }
            }

            Editor.GlobalSingleton.AssetPipeline.ImportChangesOrGetRunning(localPath);
        }

        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            IndentSize = 4
        };

        internal sealed record class TargetData(string LocalPath);
    }
}
