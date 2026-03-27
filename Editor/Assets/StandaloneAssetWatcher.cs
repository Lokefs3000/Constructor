using Editor.Storage;
using Primary;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Assets
{
    internal sealed class StandaloneAssetWatcher : Editor
    {
        private CancellationTokenSource _cts;

        public StandaloneAssetWatcher(string baseProjectPath, string[] args) : base(baseProjectPath, args, true)
        {
            _projectSubFilesystem = new ProjectSubFilesystem(EditorFilepaths.ContentPath);

            _engineFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Engine");
            _editorFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Editor");

            _assetDatabase = new AssetDatabase();
            _assetPipeline = new AssetPipeline(null);

            _cts = new CancellationTokenSource();

            base.Initialize(_assetPipeline.Identifier);
        }

        public override void Dispose()
        {
            _cts.Dispose();

            _projectSubFilesystem.Dispose();
            _engineFilesystem.Dispose();
            _editorFilesystem.Dispose();
            _assetPipeline.Dispose();
        }

        public override void Run()
        {
            Thread thread = new Thread(AssetWatcherThread);
            thread.Start();

            while (true)
            {
                string? read = Console.ReadLine();
                if (read == "exit")
                    break;
            }

            _cts.Cancel();
            thread.Join();
        }

        private void AssetWatcherThread()
        {
            while (!_cts.IsCancellationRequested)
            {
                _assetPipeline.PollRemainingEvents();

                Thread.Sleep(75);
            }
        }
    }
}
