using Editor.Shaders.Attributes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders
{
    public sealed class ShaderAttributeSettings
    {
        private readonly FrozenDictionary<string, AttributeSignature> _signatures;

        public ShaderAttributeSettings(params AttributeSignature[] attributes)
        {
            Dictionary<string, AttributeSignature> dict = new Dictionary<string, AttributeSignature>();
            foreach (AttributeSignature signature in attributes)
            {
                dict.Add(signature.Name, signature);
            }

            _signatures = dict.ToFrozenDictionary();
        }

        public bool TryGetSignature(string name, out AttributeSignature? signature) => _signatures.TryGetValue(name, out signature);

        public static readonly ShaderAttributeSettings Default = new ShaderAttributeSettings(
            new AttributeVertex(),
            new AttributePixel(),
            new AttributeConstants(),
            new AttributeIALayout(),
            new AttributeBindGroup(),
            new AttributeProperty(),
            new AttributeDisplay(),
            new AttributeGlobal());
    }
}
