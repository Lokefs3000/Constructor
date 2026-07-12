using System;
using System.Collections.Generic;
using System.Text;
using Editor.Processors.Texture;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Common;
using PrimaryEditor.Assets;
using PrimaryEditor.Core;
using Tomlyn;

namespace PrimaryEditor.Inspector.Contexts.Texture
{
    public sealed class TextureInspectorContext : InspectorContext
    {
        private TextureInspectorConfig _config;

        private readonly List<InspectorGroup> _groups;

        internal TextureInspectorContext(AssetId assetId)
        {
            _config = CreateConfigFromAsset(assetId);

            _groups = [new TextureInspectorGroup(assetId)];

            ((TextureInspectorGroup)_groups[0]).UpdateAll(_config);
        }

        public override void UpdateValues()
        {
            
        }

        public override bool MatchesContext<T>(ref T value)
        {
            return value is AssetId assetId && assetId == _config.TargetAssetId;
        }

        public override ROList<InspectorGroup> Groups => _groups;

        private static TextureInspectorConfig CreateConfigFromAsset(AssetId assetId)
        {
            AssetPipeline pipeline = EditorRuntime.Instance.AssetPipeline;

            if (!pipeline.AssetRegistry.TryGetLocalPathForId(assetId, out string? localPath))
                throw new Exception("Failed to get local path for id");

            localPath = pipeline.Configuration.GetConfigPath(localPath) ?? throw new Exception("Failed to get config path for id");

            using Stream? stream = FileUtility.TryWaitOpen(localPath, FileMode.Open, FileAccess.Read, FileShare.Read) ?? throw new Exception("Failed to open stream for id config path");
            TextureInspectorConfig inspectorConfig = TomlSerializer.Deserialize<TextureInspectorConfig>(stream, TextureInspectorConfigContext.Default)!;

            inspectorConfig.TargetAssetId = assetId;
            return inspectorConfig;
        }
    }
}
