using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Threading;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackDevice : IPlaybackDevice
    {
        private readonly D3D12RHIDevice _device;
        private readonly PlaybackExecutor _executor;

        private JobHandle _currentExecutorJob;

        private bool _disposedValue;

        internal PlaybackDevice(RHIDevice device)
        {
            _device = (D3D12RHIDevice)device;
            _executor = new PlaybackExecutor(this);

            _currentExecutorJob = JobHandle.Completed;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _currentExecutorJob.WaitForCompletion();
                    _executor.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ReturnFinishedExecutor(PlaybackExecutor executor)
        {
        }

        public JobHandle ExecuteRenderPlan(CompiledRenderPlan renderPlan)
        {
            using (new ProfilingScope("StartPlayback"))
            {
                if (!_currentExecutorJob.HasCompleted)
                {
                    using (new ProfilingScope("WaitOnPreviousFrame"))
                    {
                        _currentExecutorJob.WaitForCompletion();
                    }
                }

                /*
                 
                bool doesAnySwapChainNeedAResize = false;
                foreach (D3D12RHISwapChain swapChain in renderPlan.SwapChains)
                {
                    if (swapChain.HasPendingResize)
                    {
                        doesAnySwapChainNeedAResize = true;
                        break;
                    }
                }

                if (doesAnySwapChainNeedAResize)
                {
                    _executor.ForceWaitOnFences();

                    using (new ProfilingScope("ResizeSwapChains"))
                    {
                        foreach (D3D12RHISwapChain swapChain in renderPlan.SwapChains)
                        {
                            if (swapChain.HasPendingResize)
                            {
                                swapChain.ResizeBuffersToNewSize();
                            }
                        }
                    }
                }
                 
                 */

                JobHandle handle = JobScheduler.Schedule(_executor, JobPriority.Realtime);
                _executor.GiveWorkData(renderPlan, handle);

                JobScheduler.Flush(handle);
                return handle;
            }
        }

        public ResourceAlignmentInfo QueryForAlignment(in FGBufferDesc bufferDesc)
        {
            int bufferWidth = bufferDesc.ElementCount * bufferDesc.Stride;
            if (bufferDesc.Target.HasFlags(FGBufferTarget.Constant))
                bufferWidth = MemoryHelper.AlignUp(bufferWidth, 256);

            ResourceDesc1 resourceDesc = new ResourceDesc1
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = (ulong)bufferWidth,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                Layout = TextureLayout.LayoutRowMajor,
                Flags = ResourceFlags.AllowUnorderedAccess,
                SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
            };

            if (_device.Setup.UseTightAlignment)
                resourceDesc.Flags |= ResourceFlags.UseTightAlignment;

            ResourceAllocationInfo1 allocationInfo1;
            _device.Device.GetResourceAllocationInfo3(0, 1, &resourceDesc, (uint*)null, null, &allocationInfo1);

            return new ResourceAlignmentInfo(bufferDesc.Target.HasFlags(FGBufferTarget.Constant) ? 256 : (int)allocationInfo1.Alignment, (int)allocationInfo1.SizeInBytes);
        }

        public ResourceAlignmentInfo QueryForAlignment(in FGTextureDesc textureDesc)
        {
            ResourceDesc1 resourceDesc = new ResourceDesc1
            {
                Dimension = textureDesc.Dimension switch
                {
                    FGTextureDimension.Tex2D => ResourceDimension.Texture2D,
                    FGTextureDimension.Tex3D => ResourceDimension.Texture3D,
                    FGTextureDimension.Cube => ResourceDimension.Texture2D,
                    FGTextureDimension.Tex2DArray => ResourceDimension.Texture2D,
                    FGTextureDimension.CubeArray => ResourceDimension.Texture2D,
                    _ => throw new NotImplementedException(),
                },
                Alignment = 0,
                Width = (ulong)textureDesc.Width,
                Height = (uint)textureDesc.Height,
                DepthOrArraySize = (ushort)((textureDesc.Dimension == FGTextureDimension.Cube || textureDesc.Dimension == FGTextureDimension.CubeArray) ? 6 * textureDesc.DepthOrArraySize : textureDesc.DepthOrArraySize),
                MipLevels = (ushort)textureDesc.MipCount,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                Layout = TextureLayout.LayoutUnknown,
                Flags = ResourceFlags.None,
                SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
            };

            if (textureDesc.Target.HasFlags(FGTextureTarget.RenderTarget))
            {
                resourceDesc.Format = ConversionHelper.ToRenderTargetFormat(textureDesc.Format);
                resourceDesc.Flags |= ResourceFlags.AllowRenderTarget;
            }
            else if (textureDesc.Target.HasFlags(FGTextureTarget.DepthStencil))
            {
                resourceDesc.Format = ConversionHelper.ToDepthStencilFormat(textureDesc.Format);
                resourceDesc.Flags |= ResourceFlags.AllowDepthStencil;
            }

            if (textureDesc.Target.HasFlags(FGTextureTarget.RandomReadWrite))
                resourceDesc.Flags |= ResourceFlags.AllowUnorderedAccess;

            if (_device.Setup.UseTightAlignment)
                resourceDesc.Flags |= ResourceFlags.UseTightAlignment;

            ResourceAllocationInfo1 allocationInfo1;
            _device.Device.GetResourceAllocationInfo3(0, 1, &resourceDesc, (uint*)null, null, &allocationInfo1);

            return new ResourceAlignmentInfo((int)allocationInfo1.Alignment, (int)allocationInfo1.SizeInBytes);
        }

        internal D3D12RHIDevice NativeDevice => _device;
    }
}
