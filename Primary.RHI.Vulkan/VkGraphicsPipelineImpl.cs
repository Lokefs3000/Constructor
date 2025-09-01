using CommunityToolkit.HighPerformance;
using Primary.Common;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkGraphicsPipelineImpl : GraphicsPipeline
    {
        private readonly VkGraphicsDeviceImpl _device;

        private VkDescriptorSetLayout _descriptorSetLayout;
        private VkPipelineLayout _pipelineLayout;
        private KeyValuePair<VkShaderStageFlags, VkShaderModule>[] _shaderModules;
        private Dictionary<int, VkPipeline> _pipelines;

        private bool _disposedValue;

        internal VkGraphicsPipelineImpl(VkGraphicsDeviceImpl device, ref GraphicsPipelineDescription description, ref GraphicsPipelineBytecode bytecode)
        {
            _device = device;
            _pipelines = new Dictionary<int, VkPipeline>();

            uint bindlessResourceSize = (uint)(description.BoundResources.Length * sizeof(uint));
            bool useBindlessConstantBuffer = description.ExpectedConstantsSize + bindlessResourceSize > 128;

            if (description.BoundResources.Length > 0 || useBindlessConstantBuffer || description.ImmutableSamplers.Length > 0)
            {
                int required = 1 + (useBindlessConstantBuffer ? 1 : 0) + (description.ImmutableSamplers.Length);
                if (required > 0)
                {
                    Span<VkDescriptorSetLayoutBinding> bindings = stackalloc VkDescriptorSetLayoutBinding[required];

                    int index = 0;
                    bindings[index++] = new VkDescriptorSetLayoutBinding
                    {
                        binding = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.MutableEXT,
                        pImmutableSamplers = null,
                        stageFlags = VkShaderStageFlags.AllGraphics
                    };

                    if (useBindlessConstantBuffer)
                    {
                        bindings[index++] = new VkDescriptorSetLayoutBinding
                        {
                            binding = (uint)(index - 1),
                            descriptorCount = 1,
                            descriptorType = VkDescriptorType.UniformBuffer,
                            pImmutableSamplers = null,
                            stageFlags = VkShaderStageFlags.AllGraphics
                        };
                    }

                    if (description.ImmutableSamplers.Length > 0)
                    {
                        Span<VkSampler> samplers = stackalloc VkSampler[description.ImmutableSamplers.Length];
                        for (int i = 0; i < description.ImmutableSamplers.Length; i++)
                        {
                            samplers[i] = device.SamplerPool.Get(description.ImmutableSamplers[i].Value);
                        }

                        bindings[index++] = new VkDescriptorSetLayoutBinding
                        {
                            binding = (uint)(index - 1),
                            descriptorCount = (uint)description.ImmutableSamplers.Length,
                            descriptorType = VkDescriptorType.Sampler,
                            pImmutableSamplers = (VkSampler*)Unsafe.AsPointer(ref samplers[0]),
                            stageFlags = VkShaderStageFlags.AllGraphics
                        };
                    }

                    using PoolArray<VkDescriptorType> bindlessDescriptors = ArrayPool<VkDescriptorType>.Shared.Rent(description.BoundResources.Length);
                    for (int i = 0; i < description.BoundResources.Length; i++)
                    {
                        ref BoundResourceDescription res = ref description.BoundResources[i];
                        switch (res.Type)
                        {
                            case ResourceType.Texture: bindlessDescriptors[i] = VkDescriptorType.SampledImage; break;
                            case ResourceType.ConstantBuffer: bindlessDescriptors[i] = VkDescriptorType.UniformBuffer; break;
                            case ResourceType.ShaderBuffer: bindlessDescriptors[i] = VkDescriptorType.StorageBuffer; break;
                        }
                    }

                    VkMutableDescriptorTypeListEXT mutableDescriptorTypeList = new()
                    {
                        descriptorTypeCount = (uint)description.BoundResources.Length,
                        pDescriptorTypes = (VkDescriptorType*)Unsafe.AsPointer(ref bindlessDescriptors[0])
                    };

                    VkMutableDescriptorTypeCreateInfoEXT mutableDescriptor = new()
                    {
                        sType = VkStructureType.MutableDescriptorTypeCreateInfoEXT,
                        pNext = null,
                        mutableDescriptorTypeListCount = 1,
                        pMutableDescriptorTypeLists = &mutableDescriptorTypeList
                    };

                    VkDescriptorSetLayoutCreateInfo createInfo = new()
                    {
                        pBindings = (VkDescriptorSetLayoutBinding*)Unsafe.AsPointer(ref bindings[0]),
                        bindingCount = (uint)bindings.Length,
                        flags = VkDescriptorSetLayoutCreateFlags.None,
                        pNext = &mutableDescriptor,
                        sType = VkStructureType.DescriptorSetLayoutCreateInfo
                    };

                    Vk.vkCreateDescriptorSetLayout(device.VkDevice, createInfo, null, out _descriptorSetLayout).CheckResult();
                }
                else
                    _descriptorSetLayout = VkDescriptorSetLayout.Null;
            }
            else
                _descriptorSetLayout = VkDescriptorSetLayout.Null;

            {
                int requiredRange = 0;
                if (bindlessResourceSize > 0 && !useBindlessConstantBuffer)
                    requiredRange++;
                if (description.ExpectedConstantsSize > 0)
                    requiredRange++;

                Span<VkPushConstantRange> pushConstantRanges = requiredRange > 0 ? stackalloc VkPushConstantRange[requiredRange] : Span<VkPushConstantRange>.Empty;

                int index = 0;
                if (bindlessResourceSize > 0 && !useBindlessConstantBuffer)
                {
                    pushConstantRanges[index++] = new VkPushConstantRange
                    {
                        offset = description.ExpectedConstantsSize,
                        size = bindlessResourceSize,
                        stageFlags = VkShaderStageFlags.All
                    };
                }

                if (description.ExpectedConstantsSize > 0)
                {
                    pushConstantRanges[index++] = new VkPushConstantRange
                    {
                        offset = 0,
                        size = description.ExpectedConstantsSize,
                        stageFlags = VkShaderStageFlags.All
                    };
                }

                fixed (VkDescriptorSetLayout* layout = &_descriptorSetLayout)
                {
                    VkPipelineLayoutCreateInfo createInfo = new()
                    {
                        sType = VkStructureType.PipelineLayoutCreateInfo,
                        pNext = null,
                        flags = VkPipelineLayoutCreateFlags.None,
                        pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref pushConstantRanges[0]),
                        pushConstantRangeCount = (uint)pushConstantRanges.Length,
                        pSetLayouts = _descriptorSetLayout.IsNull ? null : layout,
                        setLayoutCount = (uint)(_descriptorSetLayout.IsNull ? 0 : 1)
                    };

                    Vk.vkCreatePipelineLayout(device.VkDevice, createInfo, null, out _pipelineLayout).CheckResult();
                }
            }

            int stageCount = 0;
            if (!bytecode.Vertex.IsEmpty)
                stageCount++;
            if (!bytecode.Pixel.IsEmpty)
                stageCount++;

            {
                _shaderModules = new KeyValuePair<VkShaderStageFlags, VkShaderModule>[stageCount];

                int shaderIndex = 0;
                if (!bytecode.Vertex.IsEmpty)
                {
                    Vk.vkCreateShaderModule(_device.VkDevice, bytecode.Vertex.Span, null, out VkShaderModule shaderModule).CheckResult();
                    _shaderModules[shaderIndex++] = new KeyValuePair<VkShaderStageFlags, VkShaderModule>(VkShaderStageFlags.Vertex, shaderModule);
                }
                if (!bytecode.Pixel.IsEmpty)
                {
                    Vk.vkCreateShaderModule(_device.VkDevice, bytecode.Pixel.Span, null, out VkShaderModule shaderModule).CheckResult();
                    _shaderModules[shaderIndex++] = new KeyValuePair<VkShaderStageFlags, VkShaderModule>(VkShaderStageFlags.Fragment, shaderModule);
                }
            }

            {
                VkUtf8String vertexEntryName = "VertexMain"u8;
                VkUtf8String pixelEntryName = "PixelMain"u8;

                using PoolArray<VkPipelineShaderStageCreateInfo> pipelineShaderStages = ArrayPool<VkPipelineShaderStageCreateInfo>.Shared.Rent(stageCount);
                for (int i = 0; i < _shaderModules.Length; i++)
                {
                    byte* name = null;

                    //i get why but its still annoying
                    if (_shaderModules[i].Key == VkShaderStageFlags.Vertex)
                        name = vertexEntryName.Buffer;
                    else if (_shaderModules[i].Key == VkShaderStageFlags.Fragment)
                        name = pixelEntryName.Buffer;

                    pipelineShaderStages[i] = new VkPipelineShaderStageCreateInfo
                    {
                        sType = VkStructureType.PipelineShaderStageCreateInfo,
                        pNext = null,
                        flags = VkPipelineShaderStageCreateFlags.None,
                        module = _shaderModules[i].Value,
                        pName = name,
                        pSpecializationInfo = null,
                        stage = _shaderModules[i].Key
                    };
                }

                using PoolArray<VkVertexInputAttributeDescription> vertexInputAttribs = ArrayPool<VkVertexInputAttributeDescription>.Shared.Rent(description.InputElements.Length);

                Dictionary<int, BindingDesc> bindingsDescs = new Dictionary<int, BindingDesc>();
                for (int i = 0; i < description.InputElements.Length; i++)
                {
                    ref InputElementDescription inputElement = ref description.InputElements[i];
                    ref BindingDesc desc = ref CollectionsMarshal.GetValueRefOrAddDefault(bindingsDescs, (int)inputElement.InputSlot, out bool exists);

                    if (!exists)
                    {
                        desc.Classification = inputElement.InputSlotClass;
                        desc.Stride = 0;
                    }

                    desc.Stride = Math.Max(desc.Stride, inputElement.InstanceDataStepRate + FindStrideOfFormat(inputElement.Format));

                    VkFormat format = VkFormat.Undefined;
                    switch (inputElement.Format)
                    {
                        case InputElementFormat.Padding: continue;
                        case InputElementFormat.Float1: format = VkFormat.R32Sfloat; break;
                        case InputElementFormat.Float2: format = VkFormat.R32G32Sfloat; break;
                        case InputElementFormat.Float3: format = VkFormat.R32G32B32Sfloat; break;
                        case InputElementFormat.Float4: format = VkFormat.R32G32B32A32Sfloat; break;
                        case InputElementFormat.UInt1: format = VkFormat.R32Uint; break;
                        case InputElementFormat.UInt2: format = VkFormat.R32G32Uint; break;
                        case InputElementFormat.UInt3: format = VkFormat.R32G32B32Uint; break;
                        case InputElementFormat.UInt4: format = VkFormat.R32G32B32A32Uint; break;
                    }

                    vertexInputAttribs[i] = new VkVertexInputAttributeDescription
                    {
                        location = (uint)i,
                        binding = (uint)inputElement.InputSlot,
                        format = format,
                        offset = (uint)inputElement.ByteOffset
                    };
                }

                using PoolArray<VkVertexInputBindingDescription> vertexinputBindings = ArrayPool<VkVertexInputBindingDescription>.Shared.Rent(bindingsDescs.Count);

                int index = 0;
                foreach (var kvp in bindingsDescs)
                {
                    BindingDesc desc = kvp.Value;

                    vertexinputBindings[index++] = new VkVertexInputBindingDescription
                    {
                        binding = (uint)kvp.Key,
                        stride = (uint)desc.Stride,
                        inputRate = desc.Classification == InputClassification.Instance ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex
                    };
                }

                VkPipelineVertexInputStateCreateInfo vertexInputState = new()
                {
                    sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineVertexInputStateCreateFlags.None,
                    pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Unsafe.AsPointer(ref vertexinputBindings[0]),
                    vertexBindingDescriptionCount = (uint)bindingsDescs.Count,
                    pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Unsafe.AsPointer(ref vertexInputAttribs[0]),
                    vertexAttributeDescriptionCount = (uint)description.InputElements.Length,
                };

                VkPipelineInputAssemblyStateCreateInfo inputAssemblyState = new()
                {
                    sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineInputAssemblyStateCreateFlags.None,
                    topology = description.PrimitiveTopology switch
                    {
                        PrimitiveTopologyType.Triangle => VkPrimitiveTopology.TriangleList,
                        PrimitiveTopologyType.Line => VkPrimitiveTopology.LineList,
                        PrimitiveTopologyType.Point => VkPrimitiveTopology.PointList,
                        PrimitiveTopologyType.Patch => VkPrimitiveTopology.PatchList,
                        _ => throw new ArgumentException(nameof(description.PrimitiveTopology)),
                    },
                    primitiveRestartEnable = false,
                };

                VkPipelineTessellationStateCreateInfo tessellationState = new()
                {
                    sType = VkStructureType.PipelineTessellationStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineTessellationStateCreateFlags.None,
                    patchControlPoints = 0
                };

                VkViewport dummyViewport = new VkViewport();
                VkRect2D dummyScissor = VkRect2D.Zero;

                VkPipelineViewportStateCreateInfo viewportState = new()
                {
                    sType = VkStructureType.PipelineViewportStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineViewportStateCreateFlags.None,
                    pViewports = &dummyViewport,
                    viewportCount = 1,
                    pScissors = &dummyScissor,
                    scissorCount = 1,
                };

                VkPipelineRasterizationStateCreateInfo rasterizationState = new()
                {
                    sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineRasterizationStateCreateFlags.None,
                    depthClampEnable = false,
                    rasterizerDiscardEnable = false,
                    polygonMode = VkPolygonMode.Fill,
                    cullMode = description.CullMode switch
                    {
                        CullMode.None => VkCullModeFlags.FrontAndBack,
                        CullMode.Back => VkCullModeFlags.Back,
                        CullMode.Front => VkCullModeFlags.Front,
                        _ => throw new ArgumentException(nameof(description.CullMode))
                    },
                    frontFace = description.FrontCounterClockwise ? VkFrontFace.CounterClockwise : VkFrontFace.Clockwise,
                    depthBiasEnable = description.DepthBias != 0,
                    depthBiasConstantFactor = description.DepthBias,
                    depthBiasSlopeFactor = description.SlopeScaledDepthBias,
                    depthBiasClamp = description.DepthBiasClamp,
                    lineWidth = 1
                };

                VkPipelineMultisampleStateCreateInfo multisampleState = new()
                {
                    sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineMultisampleStateCreateFlags.None,
                    rasterizationSamples = VkSampleCountFlags.Count1,
                    sampleShadingEnable = false,
                    minSampleShading = 0.0f,
                    pSampleMask = null,
                    alphaToCoverageEnable = false,
                    alphaToOneEnable = false,
                };

                VkPipelineDepthStencilStateCreateInfo depthStencilState = new()
                {
                    sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineDepthStencilStateCreateFlags.None,
                    depthTestEnable = description.DepthEnable,
                    depthWriteEnable = description.DepthWriteMask == DepthWriteMask.All,
                    depthCompareOp = TranslateComparisonFunc(description.DepthFunc),
                    depthBoundsTestEnable = description.DepthEnable,
                    stencilTestEnable = description.StencilEnable,
                    front = new VkStencilOpState
                    {
                        failOp = TranslateStencilOp(description.FrontFace.StencilFailOp),
                        passOp = TranslateStencilOp(description.FrontFace.StencilPassOp),
                        depthFailOp = TranslateStencilOp(description.FrontFace.StencilDepthFailOp),
                        compareOp = TranslateComparisonFunc(description.FrontFace.StencilFunc),
                        compareMask = description.StencilReadMask,
                        writeMask = description.StencilWriteMask,
                        reference = 0,
                    },
                    back = new VkStencilOpState
                    {
                        failOp = TranslateStencilOp(description.BackFace.StencilFailOp),
                        passOp = TranslateStencilOp(description.BackFace.StencilPassOp),
                        depthFailOp = TranslateStencilOp(description.BackFace.StencilDepthFailOp),
                        compareOp = TranslateComparisonFunc(description.BackFace.StencilFunc),
                        compareMask = description.StencilReadMask,
                        writeMask = description.StencilWriteMask,
                        reference = 0,
                    },
                    minDepthBounds = 0.0f,
                    maxDepthBounds = 1.0f
                };

                using PoolArray<VkPipelineColorBlendAttachmentState> colorBlendAttachments = ArrayPool<VkPipelineColorBlendAttachmentState>.Shared.Rent(description.Blends.Length);
                for (int i = 0; i < description.Blends.Length; i++)
                {
                    ref BlendDescription desc = ref description.Blends[i];
                    colorBlendAttachments[i] = new VkPipelineColorBlendAttachmentState
                    {
                        blendEnable = desc.BlendEnable,
                        srcColorBlendFactor = TranslateBlend(desc.SrcBlend),
                        dstColorBlendFactor = TranslateBlend(desc.DstBlend),
                        colorBlendOp = TranslateBlendOp(desc.BlendOp),
                        srcAlphaBlendFactor = TranslateBlend(desc.SrcBlendAlpha),
                        dstAlphaBlendFactor = TranslateBlend(desc.DstBlendAlpha),
                        alphaBlendOp = TranslateBlendOp(desc.BlendOpAlpha),
                        colorWriteMask = (VkColorComponentFlags)desc.RenderTargetWriteMask,
                    };
                }

                VkPipelineColorBlendStateCreateInfo blendState = new()
                {
                    sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineColorBlendStateCreateFlags.None,
                    logicOpEnable = description.LogicOpEnable,
                    logicOp = TranslateLogicOp(description.LogicOp),
                    attachmentCount = (uint)description.Blends.Length,
                    pAttachments = (VkPipelineColorBlendAttachmentState*)Unsafe.AsPointer(ref colorBlendAttachments[0]),
                };

                blendState.blendConstants[0] = 1.0f;
                blendState.blendConstants[1] = 1.0f;
                blendState.blendConstants[2] = 1.0f;
                blendState.blendConstants[3] = 1.0f;

                VkDynamicState* dynamicStateEnum = stackalloc VkDynamicState[] { VkDynamicState.Viewport, VkDynamicState.Scissor };
                VkPipelineDynamicStateCreateInfo dynamicState = new()
                {
                    sType = VkStructureType.PipelineDynamicStateCreateInfo,
                    pNext = null,
                    flags = VkPipelineDynamicStateCreateFlags.None,
                    pDynamicStates = dynamicStateEnum,
                    dynamicStateCount = 2
                };

                VkPipelineRenderingCreateInfo rendering = new()
                {
                    sType = VkStructureType.PipelineRenderingCreateInfo,
                    pNext = null,
                    viewMask = 0,
                    colorAttachmentCount = 0,
                    pColorAttachmentFormats = null,
                    depthAttachmentFormat = VkFormat.Undefined,
                    stencilAttachmentFormat = VkFormat.Undefined,
                };

                VkGraphicsPipelineCreateInfo createInfo = new()
                {
                    sType = VkStructureType.GraphicsPipelineCreateInfo,
                    pNext = &rendering,
                    flags = VkPipelineCreateFlags.None,
                    pStages = (VkPipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref pipelineShaderStages[0]),
                    stageCount = (uint)stageCount,
                    pVertexInputState = &vertexInputState,
                    pInputAssemblyState = &inputAssemblyState,
                    pTessellationState = &tessellationState,
                    pViewportState = &viewportState,
                    pRasterizationState = &rasterizationState,
                    pMultisampleState = &multisampleState,
                    pDepthStencilState = &depthStencilState,
                    pColorBlendState = &blendState,
                    pDynamicState = &dynamicState,
                    layout = _pipelineLayout,
                    renderPass = VkRenderPass.Null,
                    subpass = 0,
                    basePipelineHandle = VkPipeline.Null,
                    basePipelineIndex = 0,
                };

                Vk.vkCreateGraphicsPipeline(device.VkDevice, createInfo, out VkPipeline pipeline).CheckResult();

                _pipelines[PipelineHashable.DefaultHashCode] = pipeline;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (var kvp in _pipelines)
                {
                    Vk.vkDestroyPipeline(_device.VkDevice, kvp.Value);
                }

                foreach (var kvp in _shaderModules)
                {
                    Vk.vkDestroyShaderModule(_device.VkDevice, kvp.Value);
                }

                Vk.vkDestroyPipelineLayout(_device.VkDevice, _pipelineLayout);
                if (_descriptorSetLayout.IsNotNull)
                    Vk.vkDestroyDescriptorSetLayout(_device.VkDevice, _descriptorSetLayout);

                _pipelines.Clear();
                _shaderModules = Array.Empty<KeyValuePair<VkShaderStageFlags, VkShaderModule>>();
                _disposedValue = true;
            }
        }

        ~VkGraphicsPipelineImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private VkPipeline CreatePipelineFromData(ref PipelineHashable pipelineData)
        {
            VkPipelineRenderingCreateInfo rendering = new()
            {
                sType = VkStructureType.PipelineRenderingCreateInfo,
                pNext = null,
                viewMask = 0,
                colorAttachmentCount = 0,
                pColorAttachmentFormats = null,
                depthAttachmentFormat = VkFormat.Undefined,
                stencilAttachmentFormat = VkFormat.Undefined,
            };

            VkPipelineViewportStateCreateInfo viewportState = new()
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                pNext = null,
                flags = VkPipelineViewportStateCreateFlags.None,
                pViewports = null,
                pScissors = null,
                viewportCount = pipelineData.ViewportStencilCount.Lower,
                scissorCount = pipelineData.ViewportStencilCount.Higher
            };

            VkGraphicsPipelineCreateInfo createInfo = new()
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo,
                pNext = &rendering,
                flags = VkPipelineCreateFlags.None,
                pStages = null,
                stageCount = 0,
                pVertexInputState = null,
                pInputAssemblyState = null,
                pTessellationState = null,
                pViewportState = &viewportState,
                pRasterizationState = null,
                pMultisampleState = null,
                pDepthStencilState = null,
                pColorBlendState = null,
                pDynamicState = null,
                layout = _pipelineLayout,
                renderPass = VkRenderPass.Null,
                subpass = 0,
                basePipelineHandle = _pipelines[PipelineHashable.DefaultHashCode],
                basePipelineIndex = 0,
            };

            Vk.vkCreateGraphicsPipeline(_device.VkDevice, createInfo, out VkPipeline pipeline).CheckResult();
            return pipeline;
        }

        internal VkPipeline GetPipeline(ref PipelineHashable pipelineData)
        {
            int hashCode = pipelineData.GetHashCode();
            if (_pipelines.TryGetValue(hashCode, out VkPipeline pipeline))
            {
                pipeline = CreatePipelineFromData(ref pipelineData);
                _pipelines[hashCode] = pipeline;
            }

            return pipeline;
        }

        private static int FindStrideOfFormat(InputElementFormat format)
        {
            switch (format)
            {
                case InputElementFormat.Padding: return 0;
                case InputElementFormat.Float1: return 4;
                case InputElementFormat.Float2: return 8;
                case InputElementFormat.Float3: return 12;
                case InputElementFormat.Float4: return 16;
                case InputElementFormat.UInt1: return 4;
                case InputElementFormat.UInt2: return 8;
                case InputElementFormat.UInt3: return 12;
                case InputElementFormat.UInt4: return 16;
                default: return 0;
            }
        }

        private static VkBlendFactor TranslateBlend(Blend blend)
        {
            switch (blend)
            {
                case Blend.Zero: return VkBlendFactor.Zero;
                case Blend.One: return VkBlendFactor.One;
                case Blend.SourceColor: return VkBlendFactor.SrcColor;
                case Blend.InverseSourceColor: return VkBlendFactor.OneMinusSrcColor;
                case Blend.SourceAlpha: return VkBlendFactor.SrcAlpha;
                case Blend.InverseSourceAlpha: return VkBlendFactor.OneMinusSrcAlpha;
                case Blend.DestinationAlpha: return VkBlendFactor.DstAlpha;
                case Blend.InverseDestinationAlpha: return VkBlendFactor.OneMinusDstAlpha;
                case Blend.DestinationColor: return VkBlendFactor.DstColor;
                case Blend.InverseDestinationColor: return VkBlendFactor.OneMinusDstColor;
                case Blend.SourceAlphaSaturate: return VkBlendFactor.SrcAlphaSaturate;
                case Blend.BlendFactor: return VkBlendFactor.Zero;
                case Blend.InverseBlendFactor: return VkBlendFactor.Zero;
                case Blend.Source1Color: return VkBlendFactor.Src1Color;
                case Blend.InverseSource1Color: return VkBlendFactor.OneMinusSrc1Color;
                case Blend.Source1Alpha: return VkBlendFactor.Src1Alpha;
                case Blend.InverseSource1Alpha: return VkBlendFactor.OneMinusSrc1Alpha;
                case Blend.AlphaFactor: return VkBlendFactor.Zero;
                case Blend.InverseAlphaFactor: return VkBlendFactor.Zero;
                default: return VkBlendFactor.Zero;
            }
        }

        private static VkBlendOp TranslateBlendOp(BlendOp blendOp)
        {
            switch (blendOp)
            {
                case BlendOp.Add: return VkBlendOp.Add;
                case BlendOp.Subtract: return VkBlendOp.Subtract;
                case BlendOp.ReverseSubtract: return VkBlendOp.ReverseSubtract;
                case BlendOp.Minimum: return VkBlendOp.Min;
                case BlendOp.Maximum: return VkBlendOp.Max;
                default: return VkBlendOp.Add;
            }
        }

        private static VkCompareOp TranslateComparisonFunc(ComparisonFunc comparisonFunc)
        {
            switch (comparisonFunc)
            {
                case ComparisonFunc.None: return VkCompareOp.Never;
                case ComparisonFunc.Never: return VkCompareOp.Never;
                case ComparisonFunc.Less: return VkCompareOp.Less;
                case ComparisonFunc.Equal: return VkCompareOp.Equal;
                case ComparisonFunc.LessEqual: return VkCompareOp.LessOrEqual;
                case ComparisonFunc.Greater: return VkCompareOp.Greater;
                case ComparisonFunc.NotEqual: return VkCompareOp.NotEqual;
                case ComparisonFunc.GreaterEqual: return VkCompareOp.GreaterOrEqual;
                case ComparisonFunc.Always: return VkCompareOp.Always;
                default: return VkCompareOp.Never;
            }
        }

        private static VkLogicOp TranslateLogicOp(LogicOp logicOp)
        {
            switch (logicOp)
            {
                case LogicOp.Clear: return VkLogicOp.Clear;
                case LogicOp.Set: return VkLogicOp.Set;
                case LogicOp.Copy: return VkLogicOp.Copy;
                case LogicOp.CopyInverted: return VkLogicOp.CopyInverted;
                case LogicOp.NoOp: return VkLogicOp.NoOp;
                case LogicOp.Invert: return VkLogicOp.Invert;
                case LogicOp.And: return VkLogicOp.And;
                case LogicOp.Nand: return VkLogicOp.Nand;
                case LogicOp.Or: return VkLogicOp.Or;
                case LogicOp.Nor: return VkLogicOp.Nor;
                case LogicOp.Xor: return VkLogicOp.Xor;
                case LogicOp.Equivalent: return VkLogicOp.Equivalent;
                case LogicOp.AndReverse: return VkLogicOp.AndReverse;
                case LogicOp.AndInverted: return VkLogicOp.AndInverted;
                case LogicOp.OrReverse: return VkLogicOp.OrReverse;
                case LogicOp.OrInverted: return VkLogicOp.OrInverted;
                default: return VkLogicOp.Clear;
            }
        }

        private static VkStencilOp TranslateStencilOp(StencilOp stencilOp)
        {
            switch (stencilOp)
            {
                case StencilOp.Keep: return VkStencilOp.Keep;
                case StencilOp.Zero: return VkStencilOp.Zero;
                case StencilOp.Replace: return VkStencilOp.Replace;
                case StencilOp.IncrementSaturation: return VkStencilOp.IncrementAndWrap; //wtf
                case StencilOp.DecrementSaturation: return VkStencilOp.DecrementAndWrap; //^^^
                case StencilOp.Invert: return VkStencilOp.Invert;
                case StencilOp.Increment: return VkStencilOp.IncrementAndClamp; //         ^^^
                case StencilOp.Decrement: return VkStencilOp.DecrementAndClamp; //         ^^^
                default: return VkStencilOp.Keep;
            }
        }

        private record struct BindingDesc
        {
            public InputClassification Classification;
            public int Stride;
        }
    }

    internal unsafe struct PipelineHashable
    {
        public fixed byte RenderTargetFormats[8];
        public byte DepthStencilFormat;

        public CombinedByte ViewportStencilCount;

        public override int GetHashCode()
        {
            fixed (PipelineHashable* ptr = &this)
            {
                return new Span<byte>(ptr, sizeof(PipelineHashable)).GetDjb2HashCode();
            }
        }

        public static readonly int DefaultHashCode = new PipelineHashable().GetHashCode();
    }

    internal record struct CombinedByte
    {
        private byte _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CombinedByte(byte value)
        {
            _value = value;
        }

        public byte Lower { get => (byte)((_value >> 4) & 8); set => _value = (byte)((_value & 15) | (value << 4)); }
        public byte Higher { get => (byte)(_value & 8); set => _value = (byte)((_value & 8) | value); }

        public static explicit operator CombinedByte(byte value) => new CombinedByte { _value = value };
        public static implicit operator byte(CombinedByte value) => value._value;
    }

    internal record struct OctoBool
    {
        private byte _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OctoBool(byte value)
        {
            _value = value;
        }

        public unsafe bool First { get => ToBool((byte)(_value & 1)); set => _value = (byte)((_value & ~0b00000001) | ToByte(value)); }
        public unsafe bool Second { get => ToBool((byte)((_value >> 1) & 1)); set => _value = (byte)((_value & ~0b00000010) | (ToByte(value) << 1)); }
        public unsafe bool Third { get => ToBool((byte)((_value >> 2) & 1)); set => _value = (byte)((_value & ~0b00000100) | (ToByte(value) << 2)); }
        public unsafe bool Fourth { get => ToBool((byte)((_value >> 3) & 1)); set => _value = (byte)((_value & ~0b00001000) | (ToByte(value) << 3)); }
        public unsafe bool Fith { get => ToBool((byte)((_value >> 4) & 1)); set => _value = (byte)((_value & ~0b00010000) | (ToByte(value) << 4)); }
        public unsafe bool Sixth { get => ToBool((byte)((_value >> 5) & 1)); set => _value = (byte)((_value & ~0b00100000) | (ToByte(value) << 5)); }
        public unsafe bool Seventh { get => ToBool((byte)((_value >> 6) & 1)); set => _value = (byte)((_value & ~0b01000000) | (ToByte(value) << 6)); }
        public unsafe bool Eighth { get => ToBool((byte)((_value >> 7) & 1)); set => _value = (byte)((_value & ~0b10000000) | (ToByte(value) << 7)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ToBool(byte value) => Unsafe.As<byte, bool>(ref value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ToByte(bool value) => Unsafe.As<bool, byte>(ref value);
    }
}
