using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Primary.Assets;
using PrimaryEditor.Inspector.Views.IO;
using Tomlyn;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewManager
    {
        private readonly FrozenDictionary<Type, ViewDescription> _customViews;

        internal ViewManager()
        {
            _customViews = TryFindAllCustomViews();
        }

        private FrozenDictionary<Type, ViewDescription> TryFindAllCustomViews()
        {
            using Stream? stream = AssetFilesystem.OpenStream("Editor/Inspector/CustomViews.toml");
            if (stream == null)
            {
                EdLog.Inspector.Error("Failed to read custom views configuration file");
                return FrozenDictionary<Type, ViewDescription>.Empty;
            }

            Dictionary<string, string> customViews;
            try
            {
                customViews = TomlSerializer.Deserialize<Dictionary<string, string>>(stream)!;
            }
            catch (Exception ex)
            {
                EdLog.Inspector.Error(ex, "Failed to deserialize custom view configurations from file");
                return FrozenDictionary<Type, ViewDescription>.Empty;
            }

            Dictionary<Type, ViewDescription> dict = new Dictionary<Type, ViewDescription>();
            foreach (var (typeName, localPath) in customViews)
            {
                Type? sourceType = Type.GetType(typeName);
                if (sourceType == null)
                {
                    EdLog.Inspector.Warning("Failed to find type '{t}' for custom view", typeName);
                    continue;
                }

                using Stream? viewStream = AssetFilesystem.OpenStream(localPath);
                if (viewStream == null)
                {
                    EdLog.Inspector.Warning("Failed to find open stream for custom view", localPath);
                    continue;
                }

                CustomViewJson[] json = [];
                try
                {
                    json = JsonSerializer.Deserialize(viewStream, CustomViewJsonContext.Default.CustomViewJsonArray)!;
                }
                catch (Exception ex)
                {
                    EdLog.Inspector.Warning(ex, "Invalid json encountered while deserializing custom view from file", localPath);
                    continue;
                }

                try
                {
                    ViewTypeResolver typeResolver = new ViewTypeResolver(sourceType);
                    ViewDescription description = new ViewDescription(ref typeResolver, json);

                    dict.Add(sourceType, description);
                }
                catch (Exception ex)
                {
                    EdLog.Inspector.Warning(ex, "Failed to setup view description from custom view '{}'", localPath);
                    continue;
                }
            }

            return dict.ToFrozenDictionary();
        }

        public bool TryGetCustomView(Type type, [NotNullWhen(true)] out ViewDescription? value)
        {
            return _customViews.TryGetValue(type, out value);
        }
    }
}
