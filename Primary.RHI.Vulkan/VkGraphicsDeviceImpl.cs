using Primary.Common;
using Primary.RHI.Vulkan.Utility;
using Serilog;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkGraphicsDeviceImpl : GraphicsDevice
    {
        private static ILogger? s_int_logger;
        private static ILogger s_logger => NullableUtility.ThrowIfNull(s_int_logger);

        private VkInstance _instance;
        private VkDebugUtilsMessengerEXT _debugMessenger;
        private VkPhysicalDevice _physicalDevice;
        private QueueFamily _queueFamily;
        private VkDevice _device;
        private VkQueue _graphicsQueue;
        private VkQueue _transferQueue;
        private VmaAllocator _allocator;
        private VkCommandPool _transferCommandPool;
        private VkFence _syncFence;
        private VkSemaphore[] _syncSemaphores;

        private SamplerPool _samplerPool;
        private ImageTransitionManager _imageTransitionManager;
        private UploadManager _uploadManager;

        private ConcurrentStack<VkCommandBuffer>[] _commandBufferStacks;

        private List<Action> _actionsForNextFrame;

        private List<SubmittedCommandBuffer> _submittedCommandBuffers;
        private List<SubmittedSwapChain> _submittedSwapChains;

        private bool _disposedValue;

        internal VkGraphicsDeviceImpl(ILogger logger)
        {
            s_int_logger = logger;

            VkResult result = Vk.vkInitialize();
            if (result != VkResult.Success)
            {
                //bad
            }

            EnsureVulkanVersion();
            CreateInstance();
            FindPhysicalDevice();
            CreateLogicalDevice();
            CreateAllocator();
            CreateCommandPools();
            CreateMiscObjects();

            _samplerPool = new SamplerPool(this);
            _imageTransitionManager = new ImageTransitionManager(this);
            _uploadManager = new UploadManager(this);

            _commandBufferStacks = new ConcurrentStack<VkCommandBuffer>[3];

            _commandBufferStacks[0] = new ConcurrentStack<VkCommandBuffer>();
            _commandBufferStacks[1] = new ConcurrentStack<VkCommandBuffer>();
            _commandBufferStacks[2] = new ConcurrentStack<VkCommandBuffer>();

            _actionsForNextFrame = new List<Action>();

            _submittedCommandBuffers = new List<SubmittedCommandBuffer>();
            _submittedSwapChains = new List<SubmittedSwapChain>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _samplerPool.Dispose();

                Vk.vkDeviceWaitIdle(_device);

                foreach (VkSemaphore semaphore in _syncSemaphores)
                    Vk.vkDestroySemaphore(_device, semaphore);
                Vk.vkDestroyFence(_device, _syncFence);
                Vk.vkDestroyCommandPool(_device, _transferCommandPool);
                Vma.vmaDestroyAllocator(_allocator);
                Vk.vkDestroyDevice(_device);
                if (_debugMessenger.IsNotNull)
                    Vk.vkDestroyDebugUtilsMessengerEXT(_instance, _debugMessenger);
                Vk.vkDestroyInstance(_instance);

                _disposedValue = true;
            }
        }

        ~VkGraphicsDeviceImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override void WaitForIdle()
        {
            Vk.vkDeviceWaitIdle(_device).CheckResult();
        }

        internal VkCommandBuffer GetCommandBuffer(CommandBufferType type)
        {
            ConcurrentStack<VkCommandBuffer> commandBuffers = _commandBufferStacks[((int)type - 1)];
            if (commandBuffers.TryPop(out VkCommandBuffer commandBuffer))
            {
                return commandBuffer;
            }

            switch (type)
            {
                case CommandBufferType.Graphics: throw new NotImplementedException();
                case CommandBufferType.Compute: throw new NotImplementedException();
                case CommandBufferType.Transfer: Vk.vkAllocateCommandBuffer(_device, _transferCommandPool, out commandBuffer); break;
                default: throw new NotSupportedException();
            }

            return commandBuffer;
        }

        internal void ReturnCommandBuffer(VkCommandBuffer commandBuffer, CommandBufferType type)
        {
            ConcurrentStack<VkCommandBuffer> commandBuffers = _commandBufferStacks[(int)type];
            commandBuffers.Push(commandBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueCommandBuffer(VkCommandBuffer commandBuffer, CommandBufferType type, int priority)
        {
            lock (_submittedCommandBuffers)
            {
                _submittedCommandBuffers.Add(new SubmittedCommandBuffer
                {
                    CommandBuffer = commandBuffer,
                    Type = type,
                    Priority = priority
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SchedulePresent(VkSwapChainImpl impl, PresentParameters parameters)
        {
            lock (_submittedSwapChains)
            {
                _submittedSwapChains.Add(new SubmittedSwapChain
                {
                    SwapChain = impl,
                    Flags = parameters
                });
            }
        }

        public override void BeginNewFrame()
        {
            Vk.vkWaitForFences(_device, _syncFence, true, 2000).CheckResult();
            Vk.vkResetFences(_device, _syncFence);

            //events
            {
                for (int i = 0; i < _actionsForNextFrame.Count; i++)
                {
                    _actionsForNextFrame[i]();
                }

                _actionsForNextFrame.Clear();
            }
        }

        public override void EndActiveFrame()
        {
            if (_syncSemaphores.Length < (_submittedCommandBuffers.Count + _submittedSwapChains.Count))
            {
                VkSemaphore[] semaphores = new VkSemaphore[BitOperations.RoundUpToPowerOf2((uint)(_submittedCommandBuffers.Count + _submittedSwapChains.Count))];
                Array.Copy(_syncSemaphores, semaphores, _syncSemaphores.Length);

                for (int i = _syncSemaphores.Length; i < semaphores.Length; i++)
                {
                    Vk.vkCreateSemaphore(_device, out semaphores[i]).CheckResult();
                }

                _syncSemaphores = semaphores;
            }

            for (int i = 0; i < _submittedSwapChains.Count; i++)
            {
                _submittedSwapChains[i].SwapChain.AcquireNextImage(_syncSemaphores[i]);
            }

            //wait for idle

            //TODO: implement sync for false condition
            int activeSemaphoreIndex = _submittedSwapChains.Count;
            if (_submittedCommandBuffers.Count > 0)
            {
                using PoolArray<VkCommandBufferSubmitInfo> commandBuffers = ArrayPool<VkCommandBufferSubmitInfo>.Shared.Rent(_submittedCommandBuffers.Count);

                //future me
                //sync between these 3 only when required and batch rest

                //TODO: use vkQueueSubmit2 to allow using numeric (64bit) value instead of binary semaphores
                //TODO: batch all together to only use 1 call to vkQueueSubmit2

                _submittedCommandBuffers.OrderBy(x => -x.Priority);

                CommandBufferType lastType = CommandBufferType.Undefined;
                int batchCount = 0;
                bool isFirstSubmit = true;

                //almost works like a null terminator in a string huh?
                _submittedCommandBuffers.Add(new SubmittedCommandBuffer
                {
                    CommandBuffer = VkCommandBuffer.Null,
                    Type = CommandBufferType.Undefined
                });

                for (int i = 0; i < _submittedCommandBuffers.Count; i++)
                {
                    SubmittedCommandBuffer submitted = _submittedCommandBuffers[i];
                    if (submitted.Type != lastType)
                    {
                        if (batchCount > 0)
                        {
                            VkQueue queue = lastType switch
                            {
                                CommandBufferType.Graphics => throw new NotImplementedException(),
                                CommandBufferType.Compute => throw new NotImplementedException(),
                                CommandBufferType.Transfer => _transferQueue,
                                _ => throw new NotImplementedException(),
                            };

                            if (isFirstSubmit)
                            {
                                VkSemaphore signalSemaphore = _syncSemaphores[activeSemaphoreIndex];

                                using PoolArray<VkSemaphoreSubmitInfo> swapChainWaitSignals = ArrayPool<VkSemaphoreSubmitInfo>.Shared.Rent(_submittedSwapChains.Count);
                                for (int j = 0; j < _submittedSwapChains.Count; j++)
                                {
                                    swapChainWaitSignals[j] = new VkSemaphoreSubmitInfo
                                    {
                                        sType = VkStructureType.SemaphoreSubmitInfo,
                                        pNext = null,
                                        semaphore = _syncSemaphores[j],
                                        value = (ulong)i,
                                        stageMask = VkPipelineStageFlags2.AllCommands,
                                        deviceIndex = 0
                                    };
                                }

                                VkSemaphoreSubmitInfo signalSubmitInfo = new()
                                {
                                    sType = VkStructureType.SemaphoreSubmitInfo,
                                    pNext = null,
                                    semaphore = signalSemaphore,
                                    value = (ulong)i,
                                    stageMask = VkPipelineStageFlags2.AllCommands,
                                    deviceIndex = 0
                                };

                                VkSubmitInfo2 submit2 = new()
                                {
                                    sType = VkStructureType.SubmitInfo2,
                                    pNext = null,
                                    flags = VkSubmitFlags.None,
                                    waitSemaphoreInfoCount = (uint)_submittedSwapChains.Count,
                                    pWaitSemaphoreInfos = (VkSemaphoreSubmitInfo*)Unsafe.AsPointer(ref swapChainWaitSignals[0]),
                                    commandBufferInfoCount = (uint)batchCount,
                                    pCommandBufferInfos = (VkCommandBufferSubmitInfo*)Unsafe.AsPointer(ref commandBuffers[0]),
                                    signalSemaphoreInfoCount = 1,
                                    pSignalSemaphoreInfos = &signalSubmitInfo
                                };

                                Vk.vkQueueSubmit2(queue, 1, &submit2, _submittedCommandBuffers.Count == i + 1 ? _syncFence : VkFence.Null);
                            }
                            else
                            {
                                VkSemaphore waitSemaphore = _syncSemaphores[activeSemaphoreIndex++];
                                VkSemaphore signalSemaphore = _syncSemaphores[activeSemaphoreIndex];

                                VkSemaphoreSubmitInfo waitSubmitInfo = new()
                                {
                                    sType = VkStructureType.SemaphoreSubmitInfo,
                                    pNext = null,
                                    semaphore = waitSemaphore,
                                    value = (ulong)i,
                                    stageMask = VkPipelineStageFlags2.AllCommands,
                                    deviceIndex = 0
                                };

                                VkSemaphoreSubmitInfo signalSubmitInfo = new()
                                {
                                    sType = VkStructureType.SemaphoreSubmitInfo,
                                    pNext = null,
                                    semaphore = signalSemaphore,
                                    value = (ulong)i,
                                    stageMask = VkPipelineStageFlags2.AllCommands,
                                    deviceIndex = 0
                                };

                                VkSubmitInfo2 submit2 = new()
                                {
                                    sType = VkStructureType.SubmitInfo2,
                                    pNext = null,
                                    flags = VkSubmitFlags.None,
                                    waitSemaphoreInfoCount = isFirstSubmit ? 0 : 1u,
                                    pWaitSemaphoreInfos = isFirstSubmit ? null : &waitSubmitInfo,
                                    commandBufferInfoCount = (uint)batchCount,
                                    pCommandBufferInfos = (VkCommandBufferSubmitInfo*)Unsafe.AsPointer(ref commandBuffers[0]),
                                    signalSemaphoreInfoCount = 1,
                                    pSignalSemaphoreInfos = &signalSubmitInfo
                                };

                                Vk.vkQueueSubmit2(queue, 1, &submit2, _submittedCommandBuffers.Count == i + 1 ? _syncFence : VkFence.Null);
                            }

                            isFirstSubmit = false;
                        }

                        batchCount = 0;
                        lastType = _submittedCommandBuffers[i].Type;
                    }

                    commandBuffers[batchCount++] = new VkCommandBufferSubmitInfo
                    {
                        sType = VkStructureType.CommandBufferSubmitInfo,
                        pNext = null,
                        commandBuffer = _submittedCommandBuffers[i].CommandBuffer,
                        deviceMask = 0
                    };
                }

                _submittedCommandBuffers.Clear();
            }

            //swapChains
            for (int i = 0; i < _submittedSwapChains.Count; i++)
            {
                _submittedSwapChains[i].SwapChain.PresentInternalSemaphore(_submittedSwapChains[i].Flags, _syncSemaphores[activeSemaphoreIndex]);
            }

            _submittedSwapChains.Clear();
        }

        public override void Submit(CommandBuffer commandBuffer, int priority)
        {
            VkCommandBufferImpl impl = (VkCommandBufferImpl)commandBuffer;
            if (commandBuffer.IsOpen)
            {
                s_logger.Error("Cannot submit command buffer that is not closed!");
                return;
            }

            impl.SubmitToQueue(priority);
        }

        public override SwapChain CreateSwapChain(Vector2 clientSize, nint windowHandle)
        {
            return new VkSwapChainImpl(this, clientSize, windowHandle);
        }

        public override Buffer CreateBuffer(BufferDescription description, nint bufferData)
        {
            return new VkBufferImpl(this, ref description, bufferData);
        }

        public override PipelineLibrary CreatePipelineLibrary(Span<byte> initialData)
        {
            return new VkPipelineLibraryImpl(this, initialData);
        }

        public override GraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDescription description, GraphicsPipelineBytecode bytecode)
        {
            return new VkGraphicsPipelineImpl(this, ref description, ref bytecode);
        }

        public override CopyCommandBuffer CreateTransferCommandBuffer()
        {
            return new VkTransferCommandBufferImpl(this);
        }

        internal void EnqueueActionForNextFrame(Action action)
        {
            lock (_actionsForNextFrame)
            {
                int index = _actionsForNextFrame.FindIndex((x) => x == action);

                if (index > -1)
                    _actionsForNextFrame[index] = action;
                else
                    _actionsForNextFrame.Add(action);
            }
        }

        private void EnsureVulkanVersion()
        {
            VkVersion maximum = Vk.vkEnumerateInstanceVersion();
            if (maximum < MinimumVulkanVersion)
            {
                //bad
            }
        }

        private void CreateInstance()
        {
            Span<VkUtf8String> validationLayerNames =
                [
                    Vk.VK_LAYER_KHRONOS_VALIDATION_EXTENSION_NAME
                ];

            List<VkUtf8String> enabledLayerNames = new List<VkUtf8String>();

            if (true)
            {
                ReadOnlySpan<VkLayerProperties> layers = Vk.vkEnumerateInstanceLayerProperties();

                for (int i = 0; i < layers.Length; i++)
                {
                    VkLayerProperties layer = layers[i];
                    VkUtf8String name = new VkUtf8String(layer.layerName);

                    int layerIndex = validationLayerNames.IndexOf(name);
                    if (layerIndex > -1)
                        enabledLayerNames.Add(validationLayerNames[layerIndex]);
                }
            }

            Span<VkUtf8String> supportedExtensionNames =
                [
                    Vk.VK_KHR_SURFACE_EXTENSION_NAME,
                    Vk.VK_EXT_DEBUG_UTILS_EXTENSION_NAME,
                    Vk.VK_KHR_WIN32_SURFACE_EXTENSION_NAME,
                ];

            HashSet<string> availableExtensions = QueryAvailableExtensions(supportedExtensionNames);
            HashSet<VkUtf8String> enabledExtenionNames = new HashSet<VkUtf8String>
                {
                    Vk.VK_KHR_SURFACE_EXTENSION_NAME,
                    GetRequiredPlatformExtension()
                };

            ValidateAvailableExtensions(availableExtensions, enabledExtenionNames);

            VkUtf8String applicationName = "Runtime"u8;
            VkUtf8String engineName = "Primary"u8;

            VkApplicationInfo applicationInfo = new()
            {
                sType = VkStructureType.ApplicationInfo,
                pNext = null,

                pApplicationName = applicationName,
                pEngineName = engineName,

                applicationVersion = new VkVersion(1, 0, 0),
                engineVersion = new VkVersion(1, 0, 0),

                apiVersion = MinimumVulkanVersion
            };

            bool debuggingEnabled = true && availableExtensions.Contains(Encoding.UTF8.GetString(Vk.VK_EXT_DEBUG_UTILS_EXTENSION_NAME));
            if (debuggingEnabled)
                enabledExtenionNames.Add(Vk.VK_EXT_DEBUG_UTILS_EXTENSION_NAME);

            using VkStringArray ppEnabledLayerNames = new VkStringArray(enabledLayerNames);
            using VkStringArray ppEnabledExtensionNames = new VkStringArray(enabledExtenionNames);

            VkInstanceCreateInfo createInfo = new()
            {
                sType = VkStructureType.InstanceCreateInfo,
                pNext = null,
                flags = VkInstanceCreateFlags.None,
                pApplicationInfo = &applicationInfo,
                enabledLayerCount = ppEnabledLayerNames.Length,
                ppEnabledLayerNames = ppEnabledLayerNames,
                enabledExtensionCount = ppEnabledExtensionNames.Length,
                ppEnabledExtensionNames = ppEnabledExtensionNames
            };

            Vk.vkCreateInstance(createInfo, null, out _instance).CheckResult();
            Vk.vkLoadInstance(_instance);

            if (debuggingEnabled)
            {
                VkDebugUtilsMessengerCreateInfoEXT debugMessenger = new()
                {
                    sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                    pUserData = null,
                    messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Warning | VkDebugUtilsMessageSeverityFlagsEXT.Error,
                    messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance,
                    pfnUserCallback = &DebugMessageCallback
                };

                Vk.vkCreateDebugUtilsMessengerEXT(_instance, debugMessenger, null, out _debugMessenger).CheckResult();
            }
        }

        private void FindPhysicalDevice()
        {
            ReadOnlySpan<VkPhysicalDevice> devices = Vk.vkEnumeratePhysicalDevices(_instance);

            _physicalDevice = devices[0]; //more logic here.. duh
            _queueFamily = QueryQueueFamilies();
        }

        private void CreateLogicalDevice()
        {
            Span<VkUtf8String> supportedExtensionNames =
                [
                    Vk.VK_KHR_SWAPCHAIN_EXTENSION_NAME,
                    Vk.VK_EXT_MUTABLE_DESCRIPTOR_TYPE_EXTENSION_NAME,
                ];

            HashSet<VkUtf8String> availableExtensions = QueryAvailableDeviceExtensions(_physicalDevice, supportedExtensionNames);
            HashSet<VkUtf8String> enabledExtenionNames = new HashSet<VkUtf8String>
                {
                    Vk.VK_KHR_SWAPCHAIN_EXTENSION_NAME,
                    Vk.VK_EXT_MUTABLE_DESCRIPTOR_TYPE_EXTENSION_NAME,
                };

            ValidateAvailableDeviceExtensions(availableExtensions, enabledExtenionNames);

            float* queuePriorities = stackalloc float[] { 1.0f, 1.0f };
            Span<VkDeviceQueueCreateInfo> queueCreateInfos =
                [
                    new VkDeviceQueueCreateInfo
                    {
                        sType = VkStructureType.DeviceQueueCreateInfo,
                        pNext = null,
                        queueFamilyIndex = _queueFamily.GraphicsFamily!.Value,
                        queueCount = 2,
                        flags = VkDeviceQueueCreateFlags.None,
                        pQueuePriorities = queuePriorities
                    }
                ];

            VkPhysicalDeviceMutableDescriptorTypeFeaturesEXT mutableDescriptorTypeFeatures = new()
            {
                sType = VkStructureType.PhysicalDeviceMutableDescriptorTypeFeaturesEXT,
                pNext = null,
            };

            VkPhysicalDeviceVulkan12Features features12 = new()
            {
                sType = VkStructureType.PhysicalDeviceVulkan12Features,
                pNext = &mutableDescriptorTypeFeatures,
            };

            VkPhysicalDeviceVulkan13Features features13 = new()
            {
                sType = VkStructureType.PhysicalDeviceVulkan13Features,
                pNext = &features12,
                synchronization2 = true
            };

            VkPhysicalDeviceFeatures2 features = new()
            {
                sType = VkStructureType.PhysicalDeviceFeatures2,
                pNext = &features13,
                features = new VkPhysicalDeviceFeatures { }
            };

            Vk.vkGetPhysicalDeviceFeatures2(_physicalDevice, &features);

            using VkStringArray ppEnabledExtensions = new VkStringArray(enabledExtenionNames);

            VkDeviceCreateInfo createInfo = new()
            {
                sType = VkStructureType.DeviceCreateInfo,
                pNext = &features,
                flags = VkDeviceCreateFlags.None,
                pQueueCreateInfos = (VkDeviceQueueCreateInfo*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(queueCreateInfos)),
                queueCreateInfoCount = (uint)queueCreateInfos.Length,
                pEnabledFeatures = null,
                enabledExtensionCount = ppEnabledExtensions.Length,
                ppEnabledExtensionNames = ppEnabledExtensions,
            };

            Vk.vkCreateDevice(_physicalDevice, createInfo, null, out _device).CheckResult();
            Vk.vkLoadDevice(_device);

            Vk.vkGetDeviceQueue(_device, _queueFamily.GraphicsFamily!.Value, 0, out _graphicsQueue);
            Vk.vkGetDeviceQueue(_device, _queueFamily.TransferFamily!.Value, 0, out _transferQueue);
        }

        private void CreateAllocator()
        {
            VmaAllocatorCreateInfo createInfo = new()
            {
                flags = VmaAllocatorCreateFlags.None,
                device = _device,
                pDeviceMemoryCallbacks = null,
                physicalDevice = _physicalDevice,
                instance = _instance,
                pAllocationCallbacks = null,
                pHeapSizeLimit = null,
                preferredLargeHeapBlockSize = 0,
                pTypeExternalMemoryHandleTypes = null,
                pVulkanFunctions = null,
                vulkanApiVersion = MinimumVulkanVersion
            };

            Vma.vmaCreateAllocator(createInfo, out _allocator).CheckResult();
        }

        private void CreateCommandPools()
        {
            VkCommandPoolCreateInfo createInfo = new()
            {
                sType = VkStructureType.CommandPoolCreateInfo,
                pNext = null,
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
                queueFamilyIndex = _queueFamily.TransferFamily!.Value
            };

            Vk.vkCreateCommandPool(_device, createInfo, null, out _transferCommandPool).CheckResult();
        }

        private void CreateMiscObjects()
        {
            Vk.vkCreateFence(_device, out _syncFence);

            _syncSemaphores = Array.Empty<VkSemaphore>();
        }

        private QueueFamily QueryQueueFamilies()
        {
            QueueFamily queueFamily = new QueueFamily();

            ReadOnlySpan<VkQueueFamilyProperties> families = Vk.vkGetPhysicalDeviceQueueFamilyProperties(_physicalDevice);
            for (int i = 0; i < families.Length; i++)
            {
                VkQueueFamilyProperties family = families[i];

                if (!queueFamily.GraphicsFamily.HasValue && family.queueFlags.HasFlag(VkQueueFlags.Graphics))
                {
                    queueFamily.GraphicsFamily = (uint)i;
                }

                if (!queueFamily.TransferFamily.HasValue && family.queueFlags.HasFlag(VkQueueFlags.Transfer))
                {
                    queueFamily.TransferFamily = (uint)i;
                }

                if (queueFamily.IsValid)
                {
                    break;
                }
            }

            return queueFamily;
        }

        public VkInstance VkInstance => _instance;
        public QueueFamily QueueFamily => _queueFamily;
        public VkDevice VkDevice => _device;
        public VkQueue GraphicsQueue => _graphicsQueue;
        public VkQueue TransferQueue => _transferQueue;
        public VmaAllocator VmaAllocator => _allocator;
        public VkCommandPool TransferCommandPool => _transferCommandPool;

        public SamplerPool SamplerPool => _samplerPool;
        public ImageTransitionManager ImageTransitionManager => _imageTransitionManager;
        public UploadManager UploadManager => _uploadManager;

        public readonly object QueueLock = new object();

        public override GraphicsAPI API => GraphicsAPI.Vulkan;

        private static VkUtf8String GetRequiredPlatformExtension()
        {
            if (OperatingSystem.IsWindows())
            {
                return Vk.VK_KHR_WIN32_SURFACE_EXTENSION_NAME;
            }

            return new VkUtf8String();
        }

        private static HashSet<string> QueryAvailableExtensions(Span<VkUtf8String> supported)
        {
            Vk.vkEnumerateInstanceExtensionProperties(out uint propertyCount).CheckResult();
            VkExtensionProperties[] properties = new VkExtensionProperties[propertyCount];
            Vk.vkEnumerateInstanceExtensionProperties(properties).CheckResult();

            HashSet<string> set = new HashSet<string>();
            for (int i = 0; i < properties.Length; i++)
            {
                VkExtensionProperties property = properties[i];
                VkUtf8String name = new VkUtf8String(property.extensionName);

                int index = supported.IndexOf(name);
                if (index > -1)
                    set.Add(Encoding.UTF8.GetString(name.Span));
            }

            return set;
        }

        private static void ValidateAvailableExtensions(HashSet<string> available, HashSet<VkUtf8String> extensions)
        {
            foreach (VkUtf8String required in extensions)
            {
                if (!available.Contains(Encoding.UTF8.GetString(required.Span)))
                {
                    //bad
                }
            }
        }

        private static HashSet<VkUtf8String> QueryAvailableDeviceExtensions(VkPhysicalDevice device, Span<VkUtf8String> supported)
        {
            ReadOnlySpan<VkExtensionProperties> properties = Vk.vkEnumerateDeviceExtensionProperties(device);

            HashSet<VkUtf8String> set = new HashSet<VkUtf8String>();
            for (int i = 0; i < properties.Length; i++)
            {
                VkExtensionProperties property = properties[i];
                VkUtf8String name = new VkUtf8String(property.extensionName);

                int index = supported.IndexOf(name);
                if (index > -1)
                    set.Add(name);
            }

            return set;
        }

        private static void ValidateAvailableDeviceExtensions(HashSet<VkUtf8String> available, HashSet<VkUtf8String> extensions)
        {
            foreach (VkUtf8String required in extensions)
            {
                if (!available.Contains(required))
                {
                    //bad
                }
            }
        }

        [UnmanagedCallersOnly]
        private unsafe static uint DebugMessageCallback(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT types, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* userData)
        {
            VkUtf8String message = new VkUtf8String(pCallbackData->pMessage);
            string str = Encoding.UTF8.GetString(message.Buffer, message.Length);

            switch (severity)
            {
                case VkDebugUtilsMessageSeverityFlagsEXT.Verbose: s_logger.Verbose(str); break;
                case VkDebugUtilsMessageSeverityFlagsEXT.Info: s_logger.Information(str); break;
                case VkDebugUtilsMessageSeverityFlagsEXT.Warning: s_logger.Warning(str); break;
                case VkDebugUtilsMessageSeverityFlagsEXT.Error: s_logger.Error(str); break;
            }

            return Vk.VK_FALSE;
        }

        public static readonly VkVersion MinimumVulkanVersion = VkVersion.Version_1_4;

        private record struct SubmittedCommandBuffer
        {
            public VkCommandBuffer CommandBuffer;
            public CommandBufferType Type;
            public int Priority;
        }

        private record struct SubmittedSwapChain
        {
            public VkSwapChainImpl SwapChain;
            public PresentParameters Flags;
        }
    }

    internal struct QueueFamily
    {
        public uint? GraphicsFamily = null;
        public uint? TransferFamily = null;

        public QueueFamily()
        {
        }

        public bool IsValid => GraphicsFamily.HasValue && TransferFamily.HasValue;
    }
}
