using Primary.Assets;
using Primary.Assets.Types;
using Primary.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Primary.Threading
{
    internal sealed class AssetLoadScheduler
    {
        private readonly AssetManager _manager;
        private readonly AssetLoadTimings _timings;

        private ConcurrentDictionary<AssetId, Task> _running;

        internal AssetLoadScheduler(AssetManager manager, AssetLoadTimings timings)
        {
            _manager = manager;
            _timings = timings;

            _running = new ConcurrentDictionary<AssetId, Task>();
        }

        internal ValueTask Schedule(IAssetLoader loader, AssetId id, IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, bool isReloading)
        {
            Task task = _running.GetOrAdd(id, CreateTask, new CreateTaskArgs(_timings, loader, id, asset, assetData, sourcePath, localPath, isReloading));
            if (task.IsCompleted)
            {
                _running.TryRemove(id, out _);
            }

            return new ValueTask(task);
        }

        internal bool TryGetRunningTask(AssetId id, [NotNullWhen(true)] out Task? task)
        {
            return _running.TryGetValue(id, out task);
        }

        private Task CreateTask(AssetId id, CreateTaskArgs args)
        {
            Action action = () =>
            {
                try
                {
                    using (args.Timings.StartTiming(args.Id))
                    {
                        args.Loader.FactoryLoad(args.Asset, args.AssetData, args.SourcePath, args.LocalPath, null);
                    }

                    _running.TryRemove(id, out _);
                    _manager.RegisterAssetLoad(id, args.Asset, args.IsReloading);
                }
                catch (Exception ex)
                {
                    EngLog.Assets.Error(ex, "Error occured loading asset '{path}'", args.SourcePath);
                    args.AssetData.UpdateAssetFailed(args.Asset);
                }
            };

            // TODO: FIX SO IT DOES NOT CRASH THE GPU WHEN LOADING SHADERS
            if (args.Asset is not ShaderAsset and not ComputeShaderAsset)
            {
                Task task = Task.Factory.StartNew(action, TaskCreationOptions.PreferFairness);
                return task;
            }
            else
            {
                //Task task = Task.Factory.StartNew(action);
                action();
                return Task.CompletedTask;
            }
        }

        private readonly record struct CreateTaskArgs(AssetLoadTimings Timings, IAssetLoader Loader, AssetId Id, IAssetDefinition Asset, IInternalAssetData AssetData, string SourcePath, string LocalPath, bool IsReloading);
    }
}
