using System;
using System.Collections.Generic;
using System.Text;
using Primary;
using Primary.Logging;
using PrimaryEditor.Assets;

namespace PrimaryEditor.Core
{
    public sealed class AssetWatcher : IDisposable
    {
        private readonly Logbook _logbook;
        private readonly AssetPipeline _assetPipeline;

        internal AssetWatcher(ReadOnlySpan<string> args)
        {
            AppArguments.Parse(args);
            AppArguments.TryAddValue("detailed-asset-info");

            _logbook = new Logbook();
            _assetPipeline = new AssetPipeline(null);
        }

        public void Dispose()
        {
        }
    }
}
