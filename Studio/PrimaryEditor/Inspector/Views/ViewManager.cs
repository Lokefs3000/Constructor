using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Primary.Assets;
using Primary.Collections;
using PrimaryEditor.Inspector.Setup;
using PrimaryEditor.Inspector.Views.IO;
using Tomlyn;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewManager
    {
        private readonly Dictionary<Type, ViewDescription> _customViews;

        internal ViewManager()
        {
            _customViews = TryFindAllCustomViews();
        }

        private Dictionary<Type, ViewDescription> TryFindAllCustomViews()
        {
            using Stream? stream = AssetFilesystem.OpenStream("Editor/Inspector/CustomViews.toml");
            if (stream == null)
            {
                EdLog.Inspector.Error("Failed to read custom views configuration file");
                return [];
            }

            Dictionary<string, string> customViews;
            try
            {
                customViews = TomlSerializer.Deserialize<Dictionary<string, string>>(stream)!;
            }
            catch (Exception ex)
            {
                EdLog.Inspector.Error(ex, "Failed to deserialize custom view configurations from file");
                return [];
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

            return dict;
        }

        private ViewDescription GenerateViewFromReflection(InspectorDescription description)
        {
            ViewTypeResolver typeResolver = new ViewTypeResolver(description.Type);

            RentedList<ViewObject> viewObjects = new RentedList<ViewObject>();
            RecursiveGenerate(ref viewObjects, ref typeResolver, description, 0.0f, description.Type.FullName!.Length);

            ViewDescription view = new ViewDescription(viewObjects.AsSpan());
            viewObjects.Dispose();

            return view;

            static void RecursiveGenerate(ref RentedList<ViewObject> viewObjects, ref ViewTypeResolver typeResolver, InspectorDescription desc, float currentIndent, int sourceTypeNameLength)
            {
                foreach (DescriptionValue descriptionValue in desc.Values)
                {
                    ViewObject viewObject;
                    if (descriptionValue.Description != null)
                    {
                        RentedList<ViewObject> subViewObjects = new RentedList<ViewObject>();
                        RecursiveGenerate(ref subViewObjects, ref typeResolver, descriptionValue.Description, currentIndent + 1.0f, sourceTypeNameLength);

                        viewObject = new ViewGroup(subViewObjects.AsSpan(), 0.0f, currentIndent);
                        subViewObjects.Dispose();
                    }
                    else
                    {
                        viewObject = new ViewField(ref typeResolver, descriptionValue.ValueSource.TargetName[(sourceTypeNameLength + 1)..], descriptionValue.Name, 0.0f, currentIndent);
                    }

                    viewObjects.Add(viewObject);
                }
            }
        }

        public bool TryGetCustomView(Type type, [NotNullWhen(true)] out ViewDescription? value)
        {
            return _customViews.TryGetValue(type, out value);
        }

        public ViewDescription GetViewForInspector(InspectorDescription description)
        {
            ref ViewDescription? view = ref CollectionsMarshal.GetValueRefOrAddDefault(_customViews, description.Type, out bool exists);
            if (view == null || !exists)
            {
                view = GenerateViewFromReflection(description);
            }

            return view;
        }
    }
}
