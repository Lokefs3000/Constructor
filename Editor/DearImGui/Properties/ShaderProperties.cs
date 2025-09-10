using Editor.Assets.Importers;
using Editor.Processors;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using Primary.Utility;
using System.Globalization;
using RHI = Primary.RHI;
using CommunityToolkit.HighPerformance;

namespace Editor.DearImGui.Properties
{
    internal sealed class ShaderProperties : IObjectPropertiesViewer
    {
        private string? _localPath;

        private ShaderAsset? _shader;
        private string? _configFile;

        private ShaderArgs _args;
        private List<BlendArgs> _blends;

        private bool _isImported;

        internal ShaderProperties()
        {
            _blends = new List<BlendArgs>();
        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
            {
                string fillMode = _args.FillMode.ToString();
                string cullMode = _args.CullMode.ToString();
                string primitiveTopology = _args.PrimitiveTopology.ToString();

                if (ImGuiWidgets.ComboBox("Fill mode:", ref fillMode, s_fillMode))
                {
                    _args.FillMode = Enum.Parse<RHI.FillMode>(fillMode);
                }

                if (ImGuiWidgets.ComboBox("Cull mode:", ref cullMode, s_cullMode))
                {
                    _args.CullMode = Enum.Parse<RHI.CullMode>(cullMode);
                }

                if (ImGuiWidgets.ComboBox("Topology:", ref primitiveTopology, s_primitiveTopology))
                {
                    _args.PrimitiveTopology = Enum.Parse<RHI.PrimitiveTopologyType>(primitiveTopology);
                }

                ImGuiWidgets.Checkbox("Front counter clockwise:", ref _args.FrontCounterClockwise);
                ImGuiWidgets.Checkbox("Conservative raster:", ref _args.ConservativeRaster);
            }

            if (ImGui.CollapsingHeader("Depth", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiWidgets.Checkbox("Depth clip enabled:", ref _args.DepthClipEnable);
                ImGuiWidgets.Checkbox("Depth enabled:", ref _args.DepthEnable);
                if (_args.DepthEnable)
                {
                    string depthWriteMask = _args.DepthWriteMask.ToString();
                    string depthFunc = _args.DepthFunc.ToString();

                    ImGui.Indent();

                    ImGuiWidgets.InputInt("Depth bias:", ref _args.DepthBias, 0);
                    ImGuiWidgets.InputFloat("Depth bias clamp:", ref _args.DepthBiasClamp, 0.0f);
                    ImGuiWidgets.InputFloat("Slope scaled depth bias:", ref _args.SlopeScaledDepthBias, 0.0f);

                    if (ImGuiWidgets.ComboBox("Depth write mask:", ref depthWriteMask, s_depthWriteMask))
                    {
                        _args.DepthWriteMask = Enum.Parse<RHI.DepthWriteMask>(depthWriteMask);
                    }

                    if (ImGuiWidgets.ComboBox("Depth func:", ref depthFunc, s_comparisonFunc))
                    {
                        _args.DepthFunc = Enum.Parse<RHI.ComparisonFunc>(depthFunc);
                    }

                    ImGui.Unindent();
                }
            }

            if (ImGui.CollapsingHeader("Stencil", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiWidgets.Checkbox("Stencil enabled:", ref _args.StencilEnable);
                if (_args.StencilEnable)
                {
                    int stencilReadMask = _args.StencilReadMask;
                    int stencilWriteMask = _args.StencilWriteMask;

                    ImGui.Indent();

                    if (ImGuiWidgets.InputInt("Stencil read mask:", ref stencilReadMask, 0, byte.MaxValue))
                        _args.StencilReadMask = (byte)stencilReadMask;
                    if (ImGuiWidgets.InputInt("Stencil write mask:", ref stencilWriteMask, 0, byte.MaxValue))
                        _args.StencilWriteMask = (byte)stencilReadMask;

                    if (ImGui.CollapsingHeader("Front face", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("frontface");
                        Face(ref _args.FrontFace);
                        ImGui.PopID();
                    }

                    if (ImGui.CollapsingHeader("Back face", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("backfaces");
                        Face(ref _args.BackFace);
                        ImGui.PopID();
                    }

                    ImGui.Unindent();

                    void Face(ref StencilFaceArgs args)
                    {
                        string failOp = args.FailOp.ToString();
                        string depthFailOp = args.DepthFailOp.ToString();
                        string passOp = args.PassOp.ToString();
                        string func = args.Func.ToString();

                        if (ImGuiWidgets.ComboBox("Fail op:", ref failOp, s_stencilOp))
                            args.FailOp = Enum.Parse<RHI.StencilOp>(failOp);
                        if (ImGuiWidgets.ComboBox("Depth fail op:", ref depthFailOp, s_stencilOp))
                            args.DepthFailOp = Enum.Parse<RHI.StencilOp>(depthFailOp);
                        if (ImGuiWidgets.ComboBox("Pass op:", ref passOp, s_stencilOp))
                            args.PassOp = Enum.Parse<RHI.StencilOp>(passOp);
                        if (ImGuiWidgets.ComboBox("Func:", ref func, s_comparisonFunc))
                            args.Func = Enum.Parse<RHI.ComparisonFunc>(func);
                    }
                }
            }

            if (ImGui.CollapsingHeader("Blend", ImGuiTreeNodeFlags.DefaultOpen))
            {
                string logicOp = _args.LogicOp.ToString();

                ImGuiWidgets.Checkbox("Alpha to coverage enabled:", ref _args.AlphaToCoverageEnable);
                ImGuiWidgets.Checkbox("Independent blend enabled:", ref _args.IndependentBlendEnable);

                if (ImGuiWidgets.CheckedComboBox("Logic op:", ref logicOp, ref _args.LogicOpEnable, s_logicOp))
                    _args.LogicOp = Enum.Parse<RHI.LogicOp>(logicOp);

                ImGui.Indent();

                if (ImGui.BeginChild("##BLENDS", ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY))
                {
                    Action? executor = null;

                    Span<BlendArgs> argsSpan = _blends.AsSpan();
                    for (int i = 0; i < argsSpan.Length; i++)
                    {
                        ImGui.PushID(i + 1);

                        bool header = ImGui.CollapsingHeader($"Blend {i}");

                        if (ImGui.BeginPopupContextItem("##BLENDCTX"))
                        {
                            if (i > 0)
                                if (ImGui.MenuItem("Move up"))
                                {
                                    int curr = i;
                                    executor = () =>
                                    {
                                        BlendArgs prev = _blends[curr - 1];

                                        _blends[curr - 1] = _blends[curr];
                                        _blends[curr] = prev;
                                    };
                                }
                            if (i < argsSpan.Length - 1)
                                if (ImGui.MenuItem("Move down"))
                                {
                                    int curr = i;
                                    executor = () =>
                                    {
                                        BlendArgs prev = _blends[curr + 1];

                                        _blends[curr + 1] = _blends[curr];
                                        _blends[curr] = prev;
                                    };
                                }
                            if (ImGui.MenuItem("Remove"))
                            {
                                int curr = i;
                                executor = () =>
                                {
                                    _blends.RemoveAt(curr);
                                };
                            }

                            ImGui.Separator();

                            if (ImGui.BeginMenu("Presets"))
                            {
                                if (ImGui.MenuItem("Opaque"))
                                {
                                    ref BlendArgs args = ref argsSpan[i];
                                    args.BlendEnable = true;
                                    args.SourceBlend = RHI.Blend.One;
                                    args.DestinationBlend = RHI.Blend.Zero;
                                    args.BlendOp = RHI.BlendOp.Add;
                                    args.SourceBlendAlpha = RHI.Blend.One;
                                    args.DestinationBlendAlpha = RHI.Blend.Zero;
                                    args.BlendOpAlpha = RHI.BlendOp.Add;
                                }
                                if (ImGui.MenuItem("Premultiplied"))
                                {
                                    ref BlendArgs args = ref argsSpan[i];
                                    args.BlendEnable = true;
                                    args.SourceBlend = RHI.Blend.SourceAlpha;
                                    args.DestinationBlend = RHI.Blend.InverseSourceAlpha;
                                    args.BlendOp = RHI.BlendOp.Add;
                                    args.SourceBlendAlpha = RHI.Blend.One;
                                    args.DestinationBlendAlpha = RHI.Blend.InverseSourceAlpha;
                                    args.BlendOpAlpha = RHI.BlendOp.Add;
                                }
                                if (ImGui.MenuItem("Additive"))
                                {
                                    ref BlendArgs args = ref argsSpan[i];
                                    args.BlendEnable = true;
                                    args.SourceBlend = RHI.Blend.SourceAlpha;
                                    args.DestinationBlend = RHI.Blend.SourceAlpha;
                                    args.BlendOp = RHI.BlendOp.Add;
                                    args.SourceBlendAlpha = RHI.Blend.SourceAlpha;
                                    args.DestinationBlendAlpha = RHI.Blend.InverseSourceAlpha;
                                    args.BlendOpAlpha = RHI.BlendOp.Add;
                                }

                                ImGui.EndMenu();
                            }

                            ImGui.EndPopup();
                        }

                        if (header)
                        {
                            ref BlendArgs args = ref argsSpan[i];

                            ImGuiWidgets.Checkbox("Enabled", ref args.BlendEnable);
                            if (args.BlendEnable)
                            {
                                string srcBlend = args.SourceBlend.ToString();
                                string dstBlend = args.DestinationBlend.ToString();
                                string blendOp = args.BlendOp.ToString();
                                string srcBlendAlpha = args.SourceBlendAlpha.ToString();
                                string dstBlendAlpha = args.DestinationBlendAlpha.ToString();
                                string blendOpAlpha = args.BlendOpAlpha.ToString();

                                ImGui.Indent();

                                if (ImGuiWidgets.ComboBox("Source:", ref srcBlend, s_blend))
                                    args.SourceBlend = Enum.Parse<RHI.Blend>(srcBlend);
                                if (ImGuiWidgets.ComboBox("Destination:", ref dstBlend, s_blend))
                                    args.DestinationBlend = Enum.Parse<RHI.Blend>(dstBlend);
                                if (ImGuiWidgets.ComboBox("Op:", ref blendOp, s_blendOp))
                                    args.BlendOp = Enum.Parse<RHI.BlendOp>(blendOp);

                                if (ImGuiWidgets.ComboBox("Source alpha:", ref srcBlendAlpha, s_blend))
                                    args.SourceBlendAlpha = Enum.Parse<RHI.Blend>(srcBlendAlpha);
                                if (ImGuiWidgets.ComboBox("Destination alpha:", ref dstBlendAlpha, s_blend))
                                    args.DestinationBlendAlpha = Enum.Parse<RHI.Blend>(dstBlendAlpha);
                                if (ImGuiWidgets.ComboBox("Op alpha:", ref blendOpAlpha, s_blendOp))
                                    args.BlendOpAlpha = Enum.Parse<RHI.BlendOp>(blendOpAlpha);

                                ImGui.Unindent();
                            }

                            WriteMaskWidget("Render target write mask:", ref args.RenderTargetWriteMask);
                        }

                        ImGui.PopID();
                    }

                    if (_blends.Count == 0)
                    {
                        ImGui.TextWrapped("No color will be rendered since there are no blend outputs."u8);
                    }

                    if (_blends.Count < 8)
                    {
                        ImGui.Separator();
                        ImGui.Button("Add blend"u8, new Vector2(-1.0f, 0.0f));

                        ImGui.OpenPopupOnItemClick("##ADDBLEND", ImGuiPopupFlags.MouseButtonLeft);
                        if (ImGui.BeginPopup("##ADDBLEND", ImGuiWindowFlags.NoMove))
                        {
                            if (ImGui.MenuItem("Default"))
                                _blends.Add(new BlendArgs
                                {
                                    BlendEnable = false,
                                    SourceBlend = RHI.Blend.One,
                                    DestinationBlend = RHI.Blend.Zero,
                                    BlendOp = RHI.BlendOp.Add,
                                    SourceBlendAlpha = RHI.Blend.One,
                                    DestinationBlendAlpha = RHI.Blend.Zero,
                                    BlendOpAlpha = RHI.BlendOp.Add,
                                    RenderTargetWriteMask = 0xf
                                });
                            if (ImGui.MenuItem("Opaque"))
                                _blends.Add(new BlendArgs
                                {
                                    BlendEnable = true,
                                    SourceBlend = RHI.Blend.One,
                                    DestinationBlend = RHI.Blend.Zero,
                                    BlendOp = RHI.BlendOp.Add,
                                    SourceBlendAlpha = RHI.Blend.One,
                                    DestinationBlendAlpha = RHI.Blend.Zero,
                                    BlendOpAlpha = RHI.BlendOp.Add,
                                    RenderTargetWriteMask = 0xf
                                });
                            if (ImGui.MenuItem("Premultiplied"))
                                _blends.Add(new BlendArgs
                                {
                                    BlendEnable = true,
                                    SourceBlend = RHI.Blend.SourceAlpha,
                                    DestinationBlend = RHI.Blend.InverseSourceAlpha,
                                    BlendOp = RHI.BlendOp.Add,
                                    SourceBlendAlpha = RHI.Blend.One,
                                    DestinationBlendAlpha = RHI.Blend.InverseSourceAlpha,
                                    BlendOpAlpha = RHI.BlendOp.Add,
                                    RenderTargetWriteMask = 0xf
                                });
                            if (ImGui.MenuItem("Additive"))
                                _blends.Add(new BlendArgs
                                {
                                    BlendEnable = true,
                                    SourceBlend = RHI.Blend.SourceAlpha,
                                    DestinationBlend = RHI.Blend.SourceAlpha,
                                    BlendOp = RHI.BlendOp.Add,
                                    SourceBlendAlpha = RHI.Blend.SourceAlpha,
                                    DestinationBlendAlpha = RHI.Blend.InverseSourceAlpha,
                                    BlendOpAlpha = RHI.BlendOp.Add,
                                    RenderTargetWriteMask = 0xf
                                });

                            ImGui.EndPopup();
                        }
                    }

                    executor?.Invoke();
                }
                ImGui.EndChild();

                ImGui.Unindent();
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
                        EdLog.Gui.Error(ex, "Writing updated texture data to disk failed: {f}", td.LocalPath);
                    }
                }
            }
            else
            {
                if (ImGui.Button("Import"))
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
            }
        }

        public void Changed(object? target)
        {
            TargetData? td = target as TargetData;
            if (td != null)
            {
                string localPath = td.LocalPath;
                string altToml = Editor.GlobalSingleton.AssetPipeline.Configuration.GetFilePath(localPath, "Shader");

                _localPath = localPath;

                if (!File.Exists(altToml))
                {
                    _shader = null;
                    _configFile = altToml;

                    _args = new ShaderArgs
                    {
                        FillMode = RHI.FillMode.Solid,
                        CullMode = RHI.CullMode.Back,

                        FrontCounterClockwise = false,

                        DepthBias = 0,
                        DepthBiasClamp = 0.0f,
                        SlopeScaledDepthBias = 0.0f,
                        DepthClipEnable = true,

                        ConservativeRaster = false,

                        DepthEnable = true,
                        DepthWriteMask = RHI.DepthWriteMask.All,
                        DepthFunc = RHI.ComparisonFunc.LessEqual,

                        StencilEnable = false,
                        StencilReadMask = 0xff,
                        StencilWriteMask = 0xff,

                        PrimitiveTopology = RHI.PrimitiveTopologyType.Triangle,

                        AlphaToCoverageEnable = false,
                        IndependentBlendEnable = false,

                        LogicOpEnable = false,
                        LogicOp = RHI.LogicOp.NoOp,

                        FrontFace = new StencilFaceArgs
                        {
                            FailOp = RHI.StencilOp.Keep,
                            DepthFailOp = RHI.StencilOp.Keep,
                            PassOp = RHI.StencilOp.Keep,
                            Func = RHI.ComparisonFunc.Always
                        },
                        BackFace = new StencilFaceArgs
                        {
                            FailOp = RHI.StencilOp.Keep,
                            DepthFailOp = RHI.StencilOp.Keep,
                            PassOp = RHI.StencilOp.Keep,
                            Func = RHI.ComparisonFunc.Always
                        }
                    };

                    _blends.Clear();
                    _blends.Add(new BlendArgs
                    {
                        BlendEnable = false,
                        SourceBlend = RHI.Blend.One,
                        DestinationBlend = RHI.Blend.Zero,
                        BlendOp = RHI.BlendOp.Add,
                        SourceBlendAlpha = RHI.Blend.One,
                        DestinationBlendAlpha = RHI.Blend.Zero,
                        BlendOpAlpha = RHI.BlendOp.Add,
                        RenderTargetWriteMask = 0xf
                    });

                    _isImported = false;
                    return;
                }

                _shader = AssetManager.LoadAsset<ShaderAsset>(td.LocalPath);
                _configFile = altToml;

                using FileStream stream = FileUtility.TryWaitOpen(_configFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                TomlTable document = Toml.ToModel<TomlTable>(File.ReadAllText(_configFile), localPath, new TomlModelOptions { IncludeFields = true });

                ReadTomlDocument(document);
                _isImported = true;
            }
            else
            {
                _localPath = null;
                _shader = null;
                _configFile = null;
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
                    EdLog.Gui.Error("Failed to write texture toml data to disk: {f}", _configFile);
                    return;
                }

                TomlTable root = new TomlTable();
                root["fill_mode"] = _args.FillMode.ToString();
                root["cull_mode"] = _args.CullMode.ToString();
                root["front_counter_clockwise"] = _args.FrontCounterClockwise;
                root["depth_bias"] = _args.DepthBias;
                root["depth_bias_clamp"] = _args.DepthBiasClamp;
                root["slope_scaled_depth_bias"] = _args.SlopeScaledDepthBias;
                root["depth_clip_enable"] = _args.DepthClipEnable;
                root["conservative_raster"] = _args.ConservativeRaster;
                root["depth_enable"] = _args.DepthEnable;
                root["depth_write_mask"] = _args.DepthWriteMask.ToString();
                root["depth_func"] = _args.DepthFunc.ToString();
                root["stencil_enable"] = _args.StencilEnable;
                root["stencil_read_mask"] = _args.StencilReadMask;
                root["stencil_write_mask"] = _args.StencilWriteMask;
                root["primitive_topology"] = _args.PrimitiveTopology.ToString();
                root["alpha_to_coverage_enable"] = _args.AlphaToCoverageEnable;
                root["independent_blend_enable"] = _args.IndependentBlendEnable;
                root["logic_op_enable"] = _args.LogicOpEnable;
                root["logic_op"] = _args.LogicOp.ToString();

                TomlTable frontFace = new TomlTable();
                frontFace["stencil_fail_op"] = _args.FrontFace.FailOp.ToString();
                frontFace["stencil_depth_fail_op"] = _args.FrontFace.DepthFailOp.ToString();
                frontFace["stencil_pass_op"] = _args.FrontFace.PassOp.ToString();
                frontFace["stencil_func"] = _args.FrontFace.Func.ToString();

                TomlTable backFace = new TomlTable();
                backFace["stencil_fail_op"] = _args.BackFace.FailOp.ToString();
                backFace["stencil_depth_fail_op"] = _args.BackFace.DepthFailOp.ToString();
                backFace["stencil_pass_op"] = _args.BackFace.PassOp.ToString();
                backFace["stencil_func"] = _args.BackFace.Func.ToString();

                root["front_face"] = frontFace;
                root["back_face"] = backFace;

                TomlTableArray blends = new TomlTableArray();

                for (int i = 0; i < _blends.Count; i++)
                {
                    BlendArgs args = _blends[i];

                    TomlTable blend = new TomlTable();
                    blend["blend_enable"] = args.BlendEnable;
                    blend["src_blend"] = args.SourceBlend.ToString();
                    blend["dst_blend"] = args.DestinationBlend.ToString();
                    blend["blend_op"] = args.BlendOp.ToString();
                    blend["src_blend_alpha"] = args.SourceBlendAlpha.ToString();
                    blend["dst_blend_alpha"] = args.DestinationBlendAlpha.ToString();
                    blend["blend_op_alpha"] = args.BlendOpAlpha.ToString();
                    blend["render_target_write_mask"] = args.RenderTargetWriteMask;

                    blends.Add(blend);
                }

                root["blends"] = blends;

                string source = Toml.FromModel(root, new TomlModelOptions
                {
                    IncludeFields = true,
                });

                stream.Write(Encoding.UTF8.GetBytes(source));
            }

            if (Editor.GlobalSingleton.AssetPipeline.ImportChangesOrGetRunning(_localPath) != null)
            {
                _isImported = true;
            }
        }

        private void ReadTomlDocument(TomlTable document)
        {
            TomlTable root = document;

            TomlTable stencilFrontFace = (TomlTable)document["front_face"];
            TomlTable stencilBackFace = (TomlTable)document["back_face"];

            TomlTableArray? blendsTable = document.ContainsKey("blends") ? (TomlTableArray)document["blends"] : null;

            _args = new ShaderArgs
            {
                FillMode = Enum.Parse<RHI.FillMode>((string)document["fill_mode"]),
                CullMode = Enum.Parse<RHI.CullMode>((string)document["cull_mode"]),

                FrontCounterClockwise = (bool)document["front_counter_clockwise"],

                DepthBias = (int)(long)document["depth_bias"],
                DepthBiasClamp = (float)(double)document["depth_bias_clamp"],
                SlopeScaledDepthBias = (float)(double)document["slope_scaled_depth_bias"],
                DepthClipEnable = (bool)document["depth_clip_enable"],

                ConservativeRaster = (bool)document["conservative_raster"],

                DepthEnable = (bool)document["depth_enable"],
                DepthWriteMask = Enum.Parse<RHI.DepthWriteMask>((string)document["depth_write_mask"]),
                DepthFunc = Enum.Parse<RHI.ComparisonFunc>((string)document["depth_func"]),

                StencilEnable = (bool)document["stencil_enable"],
                StencilReadMask = (byte)(long)document["stencil_read_mask"],
                StencilWriteMask = (byte)(long)document["stencil_write_mask"],

                PrimitiveTopology = Enum.Parse<RHI.PrimitiveTopologyType>((string)document["primitive_topology"]),

                AlphaToCoverageEnable = (bool)document["alpha_to_coverage_enable"],
                IndependentBlendEnable = (bool)document["independent_blend_enable"],

                LogicOpEnable = (bool)document["logic_op_enable"],
                LogicOp = Enum.Parse<RHI.LogicOp>((string)document["logic_op"]),

                FrontFace = new StencilFaceArgs
                {
                    FailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_fail_op"]),
                    DepthFailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_depth_fail_op"]),
                    PassOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_pass_op"]),
                    Func = Enum.Parse<RHI.ComparisonFunc>((string)stencilFrontFace["stencil_func"])
                },
                BackFace = new StencilFaceArgs
                {
                    FailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_fail_op"]),
                    DepthFailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_depth_fail_op"]),
                    PassOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_pass_op"]),
                    Func = Enum.Parse<RHI.ComparisonFunc>((string)stencilFrontFace["stencil_func"])
                }
            };

            _blends.Clear();
            if (blendsTable != null)
            {
                for (int i = 0; i < blendsTable.Count; i++)
                {
                    TomlTable blendTable = blendsTable[i];
                    _blends.Add(new BlendArgs
                    {
                        BlendEnable = (bool)blendTable["blend_enable"],

                        SourceBlend = Enum.Parse<RHI.Blend>((string)blendTable["src_blend"]),
                        DestinationBlend = Enum.Parse<RHI.Blend>((string)blendTable["dst_blend"]),
                        BlendOp = Enum.Parse<RHI.BlendOp>((string)blendTable["blend_op"]),

                        SourceBlendAlpha = Enum.Parse<RHI.Blend>((string)blendTable["src_blend_alpha"]),
                        DestinationBlendAlpha = Enum.Parse<RHI.Blend>((string)blendTable["dst_blend_alpha"]),
                        BlendOpAlpha = Enum.Parse<RHI.BlendOp>((string)blendTable["blend_op_alpha"]),

                        RenderTargetWriteMask = (byte)(long)blendTable["render_target_write_mask"]
                    });
                }
            }
        }

        private static unsafe void WriteMaskWidget(in string headerText, ref byte mask)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            float checkboxSize = context.FontSize + context.Style.FramePadding.Y * 2.0f;
            float checkboxSpacing = context.FontSize + context.Style.FramePadding.Y * 2.0f + context.Style.FramePadding.X;

            ImGuiWidgets.Header(headerText);

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;
            byte def3 = 3;
            byte def4 = 4;

            bool rEnabled = (mask & 0x1) > 0;
            bool gEnabled = (mask & 0x2) > 0;
            bool bEnabled = (mask & 0x4) > 0;
            bool aEnabled = (mask & 0x8) > 0;

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X - checkboxSpacing * 4.0f + context.Style.FramePadding.X, 0.0f));
            if (ImGui.Checkbox(&def1, ref rEnabled)) mask = (byte)(rEnabled ? (mask | 0x1) : (mask & ~0x1));

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X - checkboxSpacing * 3.0f + context.Style.FramePadding.X, 0.0f));
            if (ImGui.Checkbox(&def2, ref gEnabled)) mask = (byte)(gEnabled ? (mask | 0x2) : (mask & ~0x2));

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X - checkboxSpacing * 2.0f + context.Style.FramePadding.X, 0.0f));
            if (ImGui.Checkbox(&def3, ref bEnabled)) mask = (byte)(bEnabled ? (mask | 0x4) : (mask & ~0x4));

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X - checkboxSize, 0.0f));
            if (ImGui.Checkbox(&def4, ref aEnabled)) mask = (byte)(aEnabled ? (mask | 0x8) : (mask & ~0x8));

            ImGui.PopID();
        }

        private static readonly string[] s_fillMode = Enum.GetNames<RHI.FillMode>();
        private static readonly string[] s_cullMode = Enum.GetNames<RHI.CullMode>();
        private static readonly string[] s_depthWriteMask = Enum.GetNames<RHI.DepthWriteMask>();
        private static readonly string[] s_comparisonFunc = Enum.GetNames<RHI.ComparisonFunc>();
        private static readonly string[] s_primitiveTopology = Enum.GetNames<RHI.PrimitiveTopologyType>();
        private static readonly string[] s_logicOp = Enum.GetNames<RHI.LogicOp>();
        private static readonly string[] s_stencilOp = Enum.GetNames<RHI.StencilOp>();
        private static readonly string[] s_blend = Enum.GetNames<RHI.Blend>();
        private static readonly string[] s_blendOp = Enum.GetNames<RHI.BlendOp>();

        internal record class TargetData(string LocalPath);

        private struct ShaderArgs
        {
            public RHI.FillMode FillMode;
            public RHI.CullMode CullMode;
            public bool FrontCounterClockwise;
            public int DepthBias;
            public float DepthBiasClamp;
            public float SlopeScaledDepthBias;
            public bool DepthClipEnable;
            public bool ConservativeRaster;
            public bool DepthEnable;
            public RHI.DepthWriteMask DepthWriteMask;
            public RHI.ComparisonFunc DepthFunc;
            public bool StencilEnable;
            public byte StencilReadMask;
            public byte StencilWriteMask;
            public RHI.PrimitiveTopologyType PrimitiveTopology;
            public bool AlphaToCoverageEnable;
            public bool IndependentBlendEnable;
            public bool LogicOpEnable;
            public RHI.LogicOp LogicOp;
            public StencilFaceArgs FrontFace;
            public StencilFaceArgs BackFace;
        }

        private struct StencilFaceArgs
        {
            public RHI.StencilOp FailOp;
            public RHI.StencilOp DepthFailOp;
            public RHI.StencilOp PassOp;
            public RHI.ComparisonFunc Func;
        }

        private struct BlendArgs
        {
            public bool BlendEnable;
            public RHI.Blend SourceBlend;
            public RHI.Blend DestinationBlend;
            public RHI.BlendOp BlendOp;
            public RHI.Blend SourceBlendAlpha;
            public RHI.Blend DestinationBlendAlpha;
            public RHI.BlendOp BlendOpAlpha;
            public byte RenderTargetWriteMask;
        }
    }
}
