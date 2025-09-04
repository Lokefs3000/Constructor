using CsToml;
using Editor.Assets.Importers;
using Editor.Processors;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Properties
{
    internal sealed class TextureProperties : IObjectPropertiesViewer
    {
        private TextureAsset? _texture;

        private bool _flipVertical;
        private TextureImageFormat _imageFormat;
        private bool _cutoutDither;
        private int _cutoutThreshold;
        private bool _gammaCorrect;
        private bool _premultipliedAlpha;
        private TextureMipmapFilter _mipMapFilter;
        private int _maxMipMapCount;
        private int _minMipMapSize;
        private bool _generateMipMaps;
        private TextureImageType _imageType;
        private bool _scaleAlphaForMipMaps;
        
        internal TextureProperties()
        {

        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            string imageFormat = _imageFormat.ToString();
            string imageType = _imageType.ToString();

            if (ImGuiWidgets.ComboBox("Type:", ref imageType, s_imageTypes))
            {
                _imageType = Enum.Parse<TextureImageType>(imageType);
            }

            if (ImGuiWidgets.ComboBox("Format:", ref imageFormat, s_imageFormats))
            {
                _imageFormat = Enum.Parse<TextureImageFormat>(imageFormat);
            }

            ImGuiWidgets.Checkbox("Cut-out dither:", ref _cutoutDither);
            {
                ImGui.Indent();

                if (!_cutoutDither)
                    ImGui.BeginDisabled();

                ImGuiWidgets.SliderInt("Threshold:", ref _cutoutThreshold, byte.MinValue, byte.MaxValue);

                if (!_cutoutDither)
                    ImGui.EndDisabled();

                ImGui.Unindent();
            }

            ImGuiWidgets.Checkbox("Gamma correct:", ref _gammaCorrect);
            ImGuiWidgets.Checkbox("Premultiplied alpha:", ref _premultipliedAlpha);
            
            ImGuiWidgets.Checkbox("Generate mip maps:", ref _generateMipMaps);

            if (ImGui.CollapsingHeader("Mip mapping"))
            {
                string mipMapFilter = _mipMapFilter.ToString();

                if (!_generateMipMaps)
                    ImGui.BeginDisabled();

                if (ImGuiWidgets.ComboBox("Filter:", ref mipMapFilter, s_mipMapFilters))
                {
                    _mipMapFilter = Enum.Parse<TextureMipmapFilter>(mipMapFilter);
                }

                int width = 0;
                int height = 0;
                if (_texture != null && _texture.Status == ResourceStatus.Success)
                {
                    width = _texture.Width;
                    height = _texture.Height;
                }

                int maxMipCount = (int)Math.Log2(Math.Max(width, height)) + 1;

                ImGuiWidgets.SliderInt("Max mipmap count:", ref _maxMipMapCount, -1, maxMipCount);
                ImGuiWidgets.SliderInt("Min mipmap size:", ref _minMipMapSize, 1, Math.Min(width, height) - 1);

                ImGuiWidgets.Checkbox("Scale mip alpha:", ref _scaleAlphaForMipMaps);

                if (!_generateMipMaps)
                    ImGui.EndDisabled();
            }
            
            if (ImGui.Button("Revert"))
            {

            }

            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                try
                {
                    string fullImagePath = Path.Combine(Editor.GlobalSingleton.ProjectPath, td.FullPath);
                    WriteToDisk(Path.ChangeExtension(fullImagePath, ".toml"));
                }
                catch (Exception ex)
                {
                    EdLog.Gui.Error(ex, "Writing updated texture data to disk failed: {f}", td.FullPath);
                }
            }

            ImGui.NewLine();
            ImGui.NewLine();

            if (ImGui.BeginChild("##VIEW", ImGuiChildFlags.Borders))
            {
                Vector2 avail = ImGui.GetContentRegionAvail();
                Vector2 screen = ImGui.GetCursorScreenPos();

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                if (_texture != null && _texture.Status == ResourceStatus.Success)
                {
                    Vector2 center = screen + avail * 0.5f;
                    Vector2 halfAspectCorrect = ((_texture.Width <= _texture.Height) ?
                        new Vector2((float)_texture.Width / (float)_texture.Height * avail.Y, avail.Y) :
                        new Vector2(avail.X, (float)_texture.Height / (float)_texture.Width * avail.X)) * 0.5f;

                    drawList.AddImage(ImGuiUtility.GetTextureRef(_texture.Handle), center - halfAspectCorrect, center + halfAspectCorrect);
                }

                ImGui.EndChild();
            }
        }

        public void Changed(object? target)
        {
        BeginCheck:
            TargetData? td = target as TargetData;
            if (td != null)
            {
                string altToml = Path.ChangeExtension(td.FullPath, ".toml");
                if (!AssetFilesystem.Exists(altToml))
                {
                    td = null;
                    goto BeginCheck;
                }

                _texture = AssetManager.LoadAsset<TextureAsset>(td.FullPath);

                using Stream stream = AssetFilesystem.OpenStream(altToml)!;
                TomlDocument document = CsTomlSerializer.Deserialize<TomlDocument>(stream);

                _flipVertical = document.RootNode["flip_vertical"].GetBool();
                _imageFormat = Enum.Parse<TextureImageFormat>(document.RootNode["image_format"].GetString());
                _cutoutDither = document.RootNode["cutout_dither"].GetBool();
                _cutoutThreshold = (int)document.RootNode["cutout_threshold"].GetInt64();
                _gammaCorrect = document.RootNode["gamma_correct"].GetBool();
                _premultipliedAlpha = document.RootNode["premultiplied_alpha"].GetBool();
                _mipMapFilter = Enum.Parse<TextureMipmapFilter>(document.RootNode["mipmap_filter"].GetString());
                _maxMipMapCount = (int)document.RootNode["max_mipmap_count"].GetInt64();
                _minMipMapSize = (int)document.RootNode["min_mipmap_size"].GetInt64();
                _generateMipMaps = document.RootNode["generate_mipmaps"].GetBool();
                _imageType = Enum.Parse<TextureImageType>(document.RootNode["image_type"].GetString());
                _scaleAlphaForMipMaps = document.RootNode["scale_alpha_for_mipmaps"].GetBool();

                if (_maxMipMapCount == int.MaxValue)
                    _maxMipMapCount = -1;
            }
            else
            {
                _texture = null;
            }
        }

        private void WriteToDisk(string fullPath)
        {
            using FileStream? stream = FileUtility.TryWaitOpen(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            if (stream == null)
            {
                EdLog.Gui.Error("Failed to write texture toml data to disk: {f}", fullPath);
                return;
            }

            TexturePropertiesToml data = new TexturePropertiesToml
            {
                FlipVertical = _flipVertical,
                ImageFormat = _imageFormat,
                CutoutDither = _cutoutDither,
                CutoutThreshold = (byte)_cutoutThreshold,
                GammaCorrect = _gammaCorrect,
                PremultipliedAlpha = _premultipliedAlpha,
                MipmapFilter = _mipMapFilter,
                MaxMipmapCount = _maxMipMapCount == -1 ? int.MaxValue : _maxMipMapCount,
                MinMipmapSize = _minMipMapSize,
                GenerateMipmaps = _generateMipMaps,
                ImageType = _imageType,
                ScaleAlphaForMipmaps = _scaleAlphaForMipMaps
            };

            CsTomlSerializer.Serialize(stream, data);
        }

        private static readonly string[] s_imageFormats = Enum.GetNames<TextureImageFormat>();
        private static readonly string[] s_mipMapFilters = Enum.GetNames<TextureMipmapFilter>();
        private static readonly string[] s_imageTypes = Enum.GetNames<TextureImageType>();

        internal record class TargetData(string FullPath);
    }

    [TomlSerializedObject]
    internal partial struct TexturePropertiesToml
    {
        [TomlValueOnSerialized("flip_vertical")]
        public bool FlipVertical { get; set; }

        [TomlValueOnSerialized("image_format")]
        public TextureImageFormat ImageFormat { get; set; }

        [TomlValueOnSerialized("cutout_dither")]
        public bool CutoutDither { get; set; }

        [TomlValueOnSerialized("cutout_threshold")]
        public byte CutoutThreshold { get; set; }

        [TomlValueOnSerialized("gamma_correct")]
        public bool GammaCorrect { get; set; }

        [TomlValueOnSerialized("premultiplied_alpha")]
        public bool PremultipliedAlpha { get; set; }

        [TomlValueOnSerialized("mipmap_filter")]
        public TextureMipmapFilter MipmapFilter { get; set; }

        [TomlValueOnSerialized("max_mipmap_count")]
        public int MaxMipmapCount { get; set; }

        [TomlValueOnSerialized("min_mipmap_size")]
        public int MinMipmapSize { get; set; }

        [TomlValueOnSerialized("generate_mipmaps")]
        public bool GenerateMipmaps { get; set; }

        [TomlValueOnSerialized("image_type")]
        public TextureImageType ImageType { get; set; }

        [TomlValueOnSerialized("scale_alpha_for_mipmaps")]
        public bool ScaleAlphaForMipmaps { get; set; }
    }
}
