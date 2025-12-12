using CommunityToolkit.HighPerformance;
using CsToml;
using Editor.Assets;
using Editor.Assets.Importers;
using Editor.Processors;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
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
    internal sealed class TextureProperties : IObjectPropertiesViewer
    {
        private string? _localPath;
        private TextureAssetType _assetType;

        private TextureAsset? _texture;
        private string? _configFile;
        private bool _hasLocalConfigFile;

        private TextureProcessorArgs _args;
        private TextureCompositeArgs _compositeArgs;
        private TextureCubemapArgs _cubemapArgs;

        private TextureHeader? _query;
        private long _textureMemorySize;

        private bool _isImported;

        internal TextureProperties()
        {

        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            string imageType = _args.ImageType.ToString();

            if (ImGuiWidgets.ComboBox("Type:", ref imageType, s_imageTypes))
            {
                _args.ImageType = Enum.Parse<TextureImageType>(imageType);
            }

            if (_assetType == TextureAssetType.Composite)
            {
                if (ImGui.CollapsingHeader("Composite channels"u8))
                {
                    ImGui.Indent();
                    Selector("Red"u8, TextureCompositeChannel.Red, ref _compositeArgs.Channels, ref _compositeArgs.Red);
                    Selector("Green"u8, TextureCompositeChannel.Green, ref _compositeArgs.Channels, ref _compositeArgs.Green);
                    Selector("Blue"u8, TextureCompositeChannel.Blue, ref _compositeArgs.Channels, ref _compositeArgs.Blue);
                    Selector("Alpha"u8, TextureCompositeChannel.Alpha, ref _compositeArgs.Channels, ref _compositeArgs.Alpha);
                    ImGui.Unindent();

                    void Selector(in ReadOnlySpan<byte> headerText, TextureCompositeChannel channel, ref TextureCompositeChannel channels, ref TextureCompositeChannelArgs args)
                    {
                        ImGui.PushID(headerText);

                        byte def = 1;
                        bool r = FlagUtility.HasFlag(channels, channel);

                        if (ImGui.Checkbox(ref def, ref r))
                        {
                            if (r)
                                channels |= channel;
                            else
                                channels &= ~channel;
                        }

                        ImGui.SameLine(56.0f);

                        if (!r)
                            ImGui.BeginDisabled();
                        if (ImGui.CollapsingHeader(headerText, ImGuiTreeNodeFlags.DefaultOpen) && r)
                        {
                            string sourceText = args.Source.ToString();

                            ImGuiWidgets.SelectorAssetId<TextureAsset>("Asset:", args.Asset, (x) =>
                            {
                                switch (channel)
                                {
                                    case TextureCompositeChannel.Red: _compositeArgs.Red.Asset = x; break;
                                    case TextureCompositeChannel.Green: _compositeArgs.Green.Asset = x; break;
                                    case TextureCompositeChannel.Blue: _compositeArgs.Blue.Asset = x; break;
                                    case TextureCompositeChannel.Alpha: _compositeArgs.Alpha.Asset = x; break;
                                }
                            });
                            if (ImGuiWidgets.ComboBox("Source:", ref sourceText, s_compositeChannels))
                            {
                                args.Source = Enum.Parse<TextureCompositeChannel>(sourceText);
                            }
                            ImGuiWidgets.Checkbox("Invert:", ref args.Invert);
                        }
                        if (!r)
                            ImGui.EndDisabled();

                        ImGui.PopID();
                    }
                }
            }
            else if (_assetType == TextureAssetType.Cubemap)
            {
                if (ImGui.CollapsingHeader("Cubemap"u8))
                {
                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Positive X:", _cubemapArgs.PositiveX, (x) => _cubemapArgs.PositiveX = x);
                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Positive Y:", _cubemapArgs.PositiveY, (x) => _cubemapArgs.PositiveY = x);
                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Positive Z:", _cubemapArgs.PositiveZ, (x) => _cubemapArgs.PositiveZ = x);

                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Negative X:", _cubemapArgs.NegativeX, (x) => _cubemapArgs.NegativeX = x);
                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Negative Y:", _cubemapArgs.NegativeY, (x) => _cubemapArgs.NegativeY = x);
                    ImGuiWidgets.SelectorAssetId<TextureAsset>("Negative Z:", _cubemapArgs.NegativeZ, (x) => _cubemapArgs.NegativeZ = x);
                }
            }

            switch (_args.ImageType)
            {
                case TextureImageType.Normal:
                    {
                        if (ImGui.CollapsingHeader("Normal", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            string source = _args.ImageMetadata.NormalArgs.Source.ToString();

                            if (ImGuiWidgets.ComboBox("Source:", ref source, s_normalSource))
                            {
                                _args.ImageMetadata.NormalArgs.Source = Enum.Parse<TextureNormalSource>(source);
                            }
                        }

                        break;
                    }
                case TextureImageType.Specular:
                    {
                        if (ImGui.CollapsingHeader("Specular", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            string source = _args.ImageMetadata.SpecularArgs.Source.ToString();

                            if (ImGuiWidgets.ComboBox("Source:", ref source, s_specularSource))
                            {
                                _args.ImageMetadata.SpecularArgs.Source = Enum.Parse<TextureSpecularSource>(source);
                            }
                        }

                        break;
                    }
            }

            if (ImGui.CollapsingHeader("Source"u8, ImGuiTreeNodeFlags.DefaultOpen))
            {
                string imageFormat = _args.ImageFormat.ToString();
                string alphaSource = _args.AlphaSource.ToString();

                bool isActive = _args.ImageFormat > TextureImageFormat.Undefined;
                if (ImGuiWidgets.CheckedComboBox("Format:", ref imageFormat, ref isActive, s_imageFormats.AsSpan(1)))
                {
                    if (isActive && _args.ImageFormat == TextureImageFormat.Undefined)
                        _args.ImageFormat = TextureImageFormat.BC1;
                    else
                        _args.ImageFormat = isActive ? Enum.Parse<TextureImageFormat>(imageFormat) : TextureImageFormat.Undefined;
                }

                if (ImGuiWidgets.ComboBox("Alpha:", ref alphaSource, s_alphaSource))
                {
                    _args.AlphaSource = Enum.Parse<TextureAlphaSource>(alphaSource);
                }
            }

            if (ImGui.CollapsingHeader("Process"u8, ImGuiTreeNodeFlags.DefaultOpen))
            {
                int cutoutThreshold = _args.CutoutThreshold;

                ImGuiWidgets.Checkbox("Gamma correct:", ref _args.GammaCorrect);
                ImGuiWidgets.Checkbox("Premultiplied alpha:", ref _args.PremultipliedAlpha);

                ImGuiWidgets.Checkbox("Cutout dither:", ref _args.CutoutDither);

                if (_args.CutoutDither)
                {
                    ImGui.Indent();
                    if (ImGuiWidgets.InputInt("Threshold:", ref cutoutThreshold, 0, byte.MaxValue))
                        _args.CutoutThreshold = (byte)cutoutThreshold;
                    ImGui.Unindent();
                }

                ImGuiWidgets.Checkbox("Flip vertical:", ref _args.FlipVertical);
            }

            if (ImGui.CollapsingHeader("Mip maps", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiWidgets.Checkbox("Generate mip maps:", ref _args.GenerateMipmaps);

                if (_args.GenerateMipmaps)
                {
                    string mipmapFilter = _args.MipmapFilter.ToString();

                    ImGui.Indent();

                    ImGuiWidgets.Checkbox("Scale alpha for mip maps:", ref _args.ScaleAlphaForMipmaps);

                    ImGuiWidgets.SliderInt("Max mip map count:", ref _args.MaxMipmapCount, 0, 10);
                    ImGuiWidgets.SliderInt("Min mip map size:", ref _args.MinMipmapSize, 1, 1023);

                    if (ImGuiWidgets.ComboBox("Filter:", ref mipmapFilter, s_mipMapFilters))
                    {
                        _args.MipmapFilter = Enum.Parse<TextureMipmapFilter>(mipmapFilter);
                    }

                    ImGui.Unindent();
                }
            }

            SwizzleComboBox("Swizzle:", ref _args.TextureSwizzle);

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
                        EdLog.Gui.Error(ex, "Writing updated texture data to disk failed: {f}", td.LocalPath);
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
                        EdLog.Gui.Error(ex, "Writing importing texture data to disk failed: {f}", td.LocalPath);
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

                if (_texture != null && _texture.Status == ResourceStatus.Success)
                {
                    Vector2 center = screen + avail * 0.5f;
                    Vector2 halfAspectCorrect = ((avail.X > avail.Y) ?
                        new Vector2((float)_texture.Width / (float)_texture.Height * avail.Y, avail.Y) :
                        new Vector2(avail.X, (float)_texture.Height / (float)_texture.Width * avail.X)) * 0.5f;

                    drawList.AddImage(ImGuiUtility.GetTextureRef(_texture.Handle), center - halfAspectCorrect, center + halfAspectCorrect);
                }

                if (_query.HasValue)
                {
                    TextureHeader query = _query.Value;

                    string text = $"{query.Width}x{query.Height} | {query.Format} | {FileUtility.FormatSize(_textureMemorySize, "F3", CultureInfo.InvariantCulture)}";

                    Vector2 textSize = ImGui.CalcTextSize(text);
                    Vector2 start = screen + new Vector2((avail.X - textSize.X) * 0.5f, avail.Y - textSize.Y * 2.0f);

                    drawList.AddRectFilled(start - new Vector2(4.0f, 2.0f), start + textSize + new Vector2(4.0f, 2.0f), 0x80000000);
                    drawList.AddText(start, 0xffffffff, text);
                }

            }
            ImGui.EndChild();
        }

        public void Changed(object? target)
        {
            TargetData? td = target as TargetData;
            if (td != null)
            {
                AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;

                _hasLocalConfigFile = false;

                string localPath = td.LocalPath;
                string altToml = pipeline.Configuration.GetFilePath(localPath, "Texture");
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
                _assetType = Path.GetExtension(localPath) switch
                {
                    ".texcomp" => TextureAssetType.Composite,
                    ".cubemap" => TextureAssetType.Cubemap,
                    _ => TextureAssetType.Texture
                };

                if (_assetType == TextureAssetType.Composite)
                {
                    ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(localPath));
                    if (subFilesystem != null)
                    {
                        string fullPath = subFilesystem.GetFullPath(td.LocalPath);
                        using FileStream? compositeStream = FileUtility.TryWaitOpenNoThrow(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 10, 50);
                        if (compositeStream != null)
                        {
                            TomlTable compositeDoc = Toml.ToModel<TomlTable>(File.ReadAllText(fullPath), localPath);
                            _compositeArgs = TextureAssetImporter.ReadCompositeDocument(compositeDoc);
                        }
                        else
                        {
                            _compositeArgs = new TextureCompositeArgs
                            {
                                Channels = TextureCompositeChannel.None,
                                Red = new TextureCompositeChannelArgs(),
                                Green = new TextureCompositeChannelArgs(),
                                Blue = new TextureCompositeChannelArgs(),
                                Alpha = new TextureCompositeChannelArgs(),
                            };
                        }
                    }
                }
                else if (_assetType == TextureAssetType.Cubemap)
                {
                    ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(localPath));
                    if (subFilesystem != null)
                    {
                        string fullPath = subFilesystem.GetFullPath(td.LocalPath);
                        using FileStream? compositeStream = FileUtility.TryWaitOpenNoThrow(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 10, 50);
                        if (compositeStream != null)
                        {
                            TomlTable cubemapDoc = Toml.ToModel<TomlTable>(File.ReadAllText(fullPath), localPath);
                            _cubemapArgs = TextureAssetImporter.ReadCubemapDocument(cubemapDoc);
                        }
                        else
                        {
                            _cubemapArgs = new TextureCubemapArgs
                            {
                                Source = TextureCubemapSource.Composited,

                                PositiveX = AssetId.Invalid,
                                PositiveY = AssetId.Invalid,
                                PositiveZ = AssetId.Invalid,

                                NegativeX = AssetId.Invalid,
                                NegativeY = AssetId.Invalid,
                                NegativeZ = AssetId.Invalid,
                            };
                        }
                    }
                }

                if (!File.Exists(altToml))
                {
                    _texture = null;
                    _configFile = altToml;
                    _query = null;

                    _args = new TextureProcessorArgs
                    {
                        ImageType = TextureImageType.Color,
                        ImageMetadata = new TextureProcessorArgs.Metadata(),

                        TextureSwizzle = new TextureProcessorArgs.Swizzle(),

                        ImageFormat = TextureImageFormat.Undefined,
                        AlphaSource = TextureAlphaSource.None,

                        GammaCorrect = false,
                        PremultipliedAlpha = false,

                        CutoutDither = false,
                        CutoutThreshold = 127,

                        GenerateMipmaps = true,
                        ScaleAlphaForMipmaps = false,
                        MaxMipmapCount = int.MaxValue,
                        MinMipmapSize = 1,
                        MipmapFilter = TextureMipmapFilter.Box,

                        FlipVertical = false
                    };

                    _isImported = false;
                    return;
                }

                _texture = AssetManager.LoadAsset<TextureAsset>(td.LocalPath);
                _configFile = altToml;

                using FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                TomlTable document = Toml.ToModel<TomlTable>(File.ReadAllText(_configFile), localPath, new TomlModelOptions { IncludeFields = true });

                _args = TextureAssetImporter.ReadTomlDocument(document);
                _isImported = true;

                try
                {
                    using Stream? tempStream = AssetFilesystem.OpenStream(localPath);
                    if (tempStream == null)
                    {
                        EdLog.Gui.Warning("Failed to open stream for texture header query!");

                        _query = null;
                        _textureMemorySize = 0;
                    }
                    else
                    {
                        TextureHeader query = tempStream.Read<TextureHeader>();

                        if (query.FileHeader != TextureHeader.Header || query.FileVersion != TextureHeader.Version)
                        {
                            EdLog.Gui.Warning("Texture header contains invalid data");

                            _query = null;
                            _textureMemorySize = 0;
                        }
                        else
                        {
                            _query = query;
                            _textureMemorySize = tempStream.Length - tempStream.Position;
                        }
                    }
                }
                catch (Exception ex)
                {
                    EdLog.Gui.Warning(ex, "Failed to query texture header!");
                    _query = null;
                    _textureMemorySize = 0;
                }
            }
            else
            {
                _localPath = null;
                _texture = null;
                _configFile = null;
                _query = null;
                _textureMemorySize = 0;
                _hasLocalConfigFile = false;
            }
        }

        private void SerializeArgumentsToDisk()
        {
            Debug.Assert(_configFile != null);
            Debug.Assert(_localPath != null);

            if (_assetType == TextureAssetType.Composite)
            {
                ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(_localPath));
                if (subFilesystem != null)
                {
                    using (FileStream stream = FileUtility.TryWaitOpen(subFilesystem.GetFullPath(_localPath), FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        if (stream == null)
                        {
                            EdLog.Gui.Error("Failed to write composite toml data to disk: {f}", _localPath);
                            return;
                        }

                        TomlTable root = new TomlTable();
                        root["channels"] = (int)_compositeArgs.Channels;

                        if (FlagUtility.HasFlag(_compositeArgs.Channels, TextureCompositeChannel.Red))
                            root["red"] = CreateTable(ref _compositeArgs.Red);
                        if (FlagUtility.HasFlag(_compositeArgs.Channels, TextureCompositeChannel.Green))
                            root["green"] = CreateTable(ref _compositeArgs.Green);
                        if (FlagUtility.HasFlag(_compositeArgs.Channels, TextureCompositeChannel.Blue))
                            root["blue"] = CreateTable(ref _compositeArgs.Blue);
                        if (FlagUtility.HasFlag(_compositeArgs.Channels, TextureCompositeChannel.Alpha))
                            root["alpha"] = CreateTable(ref _compositeArgs.Alpha);

                        string source = Toml.FromModel(root);
                        stream.Write(Encoding.UTF8.GetBytes(source));

                        static TomlTable CreateTable(ref TextureCompositeChannelArgs args) => new TomlTable
                        {
                            { "asset", args.Asset.Value },
                            { "source", args.Source },
                            { "invert", args.Invert },
                        };
                    }
                }
            }
            else if (_assetType == TextureAssetType.Cubemap)
            {
                ProjectSubFilesystem? subFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(_localPath));
                if (subFilesystem != null)
                {
                    using (FileStream stream = FileUtility.TryWaitOpen(subFilesystem.GetFullPath(_localPath), FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        if (stream == null)
                        {
                            EdLog.Gui.Error("Failed to write cubemap toml data to disk: {f}", _localPath);
                            return;
                        }

                        TomlTable root = new TomlTable {
                            { "source", _cubemapArgs.Source },

                            { "positive_x", _cubemapArgs.PositiveX.Value },
                            { "positive_y", _cubemapArgs.PositiveY.Value },
                            { "positive_z", _cubemapArgs.PositiveZ.Value },

                            { "negative_x", _cubemapArgs.NegativeX.Value },
                            { "negative_y", _cubemapArgs.NegativeY.Value },
                            { "negative_z", _cubemapArgs.NegativeZ.Value },
                            };

                        string source = Toml.FromModel(root);
                        stream.Write(Encoding.UTF8.GetBytes(source));
                    }
                }
            }

            using (FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (stream == null)
                {
                    EdLog.Gui.Error("Failed to write texture toml data to disk: {f}", _configFile);
                    return;
                }

                TomlTable root = new TomlTable();
                root["image_type"] = _args.ImageType.ToString();

                root["swizzle"] = new TomlArray() { _args.TextureSwizzle.R.ToString(), _args.TextureSwizzle.G.ToString(), _args.TextureSwizzle.B.ToString(), _args.TextureSwizzle.A.ToString() };

                root["image_format"] = _args.ImageFormat.ToString();
                root["alpha_source"] = _args.AlphaSource.ToString();

                root["gamma_correct"] = _args.GammaCorrect;
                root["premultiplied_alpha"] = _args.PremultipliedAlpha;

                root["cutout_dither"] = _args.CutoutDither;
                root["cutout_threshold"] = _args.CutoutThreshold;

                root["generate_mipmaps"] = _args.GenerateMipmaps;
                root["scale_alpha_for_mipmaps"] = _args.ScaleAlphaForMipmaps;
                root["max_mipmap_count"] = _args.MaxMipmapCount;
                root["min_mipmap_size"] = _args.MinMipmapSize;
                root["mipmap_filter"] = _args.MipmapFilter.ToString();

                root["flip_vertical"] = _args.FlipVertical;

                switch (_args.ImageType)
                {
                    case TextureImageType.Normal:
                        {
                            TomlTable normal = new TomlTable();
                            root["normal"] = normal;

                            TextureProcessorNormalArgs normalArgs = _args.ImageMetadata.NormalArgs;
                            normal["source"] = normalArgs.Source.ToString();

                            break;
                        }
                    case TextureImageType.Specular:
                        {
                            TomlTable specular = new TomlTable();
                            root["specular"] = specular;

                            TextureProcessorSpecularArgs normalArgs = _args.ImageMetadata.SpecularArgs;
                            specular["source"] = normalArgs.Source.ToString();

                            break;
                        }
                }

                string source = Toml.FromModel(root, new TomlModelOptions
                {
                    IncludeFields = true,
                });

                stream.Write(Encoding.UTF8.GetBytes(source));
            }

            if (Editor.GlobalSingleton.AssetPipeline.ImportChangesOrGetRunning(_localPath) != null)
            {
                _texture = AssetManager.LoadAsset<TextureAsset>(_localPath);

                _isImported = true;
            }
        }

        private static void SwizzleComboBox(in string headerText, ref TextureProcessorArgs.Swizzle swizzle)
        {
            Vector3 data = ImGuiWidgets.Header(headerText);

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            float fullWidth = data.Z / 4.0f;
            float individualWidth = data.Z / 4.0f;

            ImGui.PushID(headerText);

            string swizzleR = s_swizzleChannel[(int)swizzle.R];
            string swizzleG = s_swizzleChannel[(int)swizzle.G];
            string swizzleB = s_swizzleChannel[(int)swizzle.B];
            string swizzleA = s_swizzleChannel[(int)swizzle.A];

            ImGui.SetCursorScreenPos(new Vector2(data.X, data.Y));
            ImGui.SetNextItemWidth(individualWidth);
            if (ImGuiWidgets.ComboBox(1, ref swizzleR, s_swizzleChannel))
                swizzle.R = Choose(swizzleR[0]);

            ImGui.SetCursorScreenPos(new Vector2(data.X + fullWidth, data.Y));
            ImGui.SetNextItemWidth(individualWidth);
            if (ImGuiWidgets.ComboBox(2, ref swizzleG, s_swizzleChannel))
                swizzle.G = Choose(swizzleG[0]);

            ImGui.SetCursorScreenPos(new Vector2(data.X + fullWidth * 2.0f, data.Y));
            ImGui.SetNextItemWidth(individualWidth);
            if (ImGuiWidgets.ComboBox(3, ref swizzleB, s_swizzleChannel))
                swizzle.B = Choose(swizzleB[0]);

            ImGui.SetCursorScreenPos(new Vector2(data.X + fullWidth * 3.0f, data.Y));
            ImGui.SetNextItemWidth(individualWidth);
            if (ImGuiWidgets.ComboBox(4, ref swizzleA, s_swizzleChannel))
                swizzle.A = Choose(swizzleA[0]);

            ImGui.PopID();

            static Processors.TextureSwizzleChannel Choose(char c) => c switch
            {
                'R' => Processors.TextureSwizzleChannel.R,
                'G' => Processors.TextureSwizzleChannel.G,
                'B' => Processors.TextureSwizzleChannel.B,
                'A' => Processors.TextureSwizzleChannel.A,
                'Z' => Processors.TextureSwizzleChannel.Zero,
                'O' => Processors.TextureSwizzleChannel.One,
                _ => Processors.TextureSwizzleChannel.R,
            };
        }

        private static void ChannelsSelector(in ReadOnlySpan<byte> headerText, ref TextureCompositeChannel channels)
        {
            Vector3 data = ImGuiWidgets.Header(headerText);

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            int j = 0;
            for (int i = 0; i < 4; i++)
            {
                if (FlagUtility.HasFlag(channels, (TextureCompositeChannel)(1 << i)))
                {
                    s_cachedCompositeChannels[j++] = i switch
                    {
                        0 => (byte)'R',
                        1 => (byte)'G',
                        2 => (byte)'B',
                        3 => (byte)'A',
                        _ => (byte)'?'
                    };
                }
            }
            s_cachedCompositeChannels[j] = 0;

            byte def = 1;

            ImGui.SetCursorScreenPos(new Vector2(data.X, data.Y));
            ImGui.SetNextItemWidth(data.Z);
            if (ImGui.BeginCombo(ref def, "COMBAT"u8))
            {
                //for (int i = 0; i < 4; i++)
                //{
                //    byte c = i switch
                //    {
                //        0 => (byte)'R',
                //        1 => (byte)'G',
                //        2 => (byte)'B',
                //        3 => (byte)'A',
                //        _ => (byte)'?'
                //    };
                //
                //    if (ImGui.Selectable(ref c, FlagUtility.HasFlag(channels, (TextureCompositeChannel)(1 << i))))
                //    {
                //        channels ^= (TextureCompositeChannel)(1 << i);
                //    }
                //}
                //
                //ImGui.EndCombo();
            }
        }

        private static byte[] s_cachedCompositeChannels = new byte[5];

        private static readonly string[] s_imageFormats = Enum.GetNames<TextureImageFormat>();
        private static readonly string[] s_mipMapFilters = Enum.GetNames<TextureMipmapFilter>();
        private static readonly string[] s_imageTypes = Enum.GetNames<TextureImageType>();
        private static readonly string[] s_normalSource = Enum.GetNames<TextureNormalSource>();
        private static readonly string[] s_specularSource = Enum.GetNames<TextureSpecularSource>();
        private static readonly string[] s_alphaSource = Enum.GetNames<TextureAlphaSource>();
        private static readonly string[] s_swizzleChannel = Enum.GetNames<Processors.TextureSwizzleChannel>();
        private static readonly string[] s_compositeChannels = Enum.GetNames<TextureCompositeChannel>();
        private static readonly string[] s_cubemapSource = Enum.GetNames<TextureCubemapSource>();

        internal record class TargetData(string LocalPath);

        private enum TextureAssetType : byte
        {
            Texture = 0,
            Composite,
            Cubemap
        }
    }
}
