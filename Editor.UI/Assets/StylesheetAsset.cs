using Editor.UI.Styling;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Assets
{
    public sealed class StylesheetAsset : BaseAssetDefinition<StylesheetAsset, StylesheetAssetData>
    {
        public StylesheetAsset(StylesheetAssetData assetData) : base(assetData)
        {

        }

        public StylesheetClass AddClass(string className) => AssetData.AddClass(className, out _);
        public StylesheetClass AddClass(string className, out bool alreadyExists) => AssetData.AddClass(className, out alreadyExists);

        public void ReplaceClass(string className, StylesheetClass value) => AssetData.ReplaceClass(className, value);

        public void RemoveClass(string className) => AssetData.RemoveClass(className);

        public StylesheetClass GetClass(string className) => AssetData.GetClass(className);
        public bool TryGetClass(string className, [NotNullWhen(true)] out StylesheetClass? value) => AssetData.TryGetClass(className, out value);

        public bool HasClass(string className) => AssetData.HasClass(className);

        public StylesheetClass? this[string className]
        {
            get
            {
                TryGetClass(className, out StylesheetClass? value);
                return value;
            }
            set
            {
                if (value == null)
                    RemoveClass(className);
                else
                    ReplaceClass(className, value);
            }
        }

        public IReadOnlyDictionary<string, StylesheetClass> Classes => AssetData.Classes;
    }

    public sealed class StylesheetAssetData : BaseInternalAssetData<StylesheetAsset>
    {
        private Dictionary<string, StylesheetClass> _classes;

        public StylesheetAssetData(AssetId id) : base(id)
        {
            _classes = new Dictionary<string, StylesheetClass>();
        }

        public override void Dispose()
        {
            _classes.Clear();
            _classes.TrimExcess();

            base.Dispose();
        }

        public void UpdateAssetData(StylesheetAsset asset, Dictionary<string, StylesheetClass> classes)
        {
            base.UpdateAssetData(asset);

            _classes = classes;
        }

        public StylesheetClass AddClass(string className, out bool alreadyExits)
        {
            alreadyExits = _classes.TryGetValue(className, out StylesheetClass? value);

            if (!alreadyExits)
            {
                value = new StylesheetClass();
                _classes.Add(className, value);
            }

            return value!;
        }

        public void ReplaceClass(string className, StylesheetClass value)
        {
            _classes[className] = value;
        }

        public void RemoveClass(string className)
        {
            _classes.Remove(className);
        }

        public StylesheetClass GetClass(string className)
        {
            return _classes[className];
        }

        public bool TryGetClass(string className, [NotNullWhen(true)] out StylesheetClass? value)
        {
            return _classes.TryGetValue(className, out value);
        }

        public bool HasClass(string className) => _classes.ContainsKey(className);

        internal Dictionary<string, StylesheetClass> Classes => _classes;
    }
}
