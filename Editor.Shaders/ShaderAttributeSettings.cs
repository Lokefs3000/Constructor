using Editor.Shaders.Attributes;
using System.Collections.Frozen;

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

        public static readonly ShaderAttributeSettings Graphics = new ShaderAttributeSettings(
            new AttributeVertex(),
            new AttributePixel(),
            new AttributeConstants(),
            new AttributeIALayout(),
            new AttributeBindGroup(),
            new AttributeProperty(),
            new AttributeDisplay(),
            new AttributeGlobal(),
            new AttributeSampled());

        public static readonly ShaderAttributeSettings Compute = new ShaderAttributeSettings(
            new AttributeKernel(),
            new AttributeConstants(),
            new AttributeProperty(),
            new AttributeDisplay(),
            new AttributeGlobal(),
            new AttributeSampled(),
            new AttributeNumThreads());
    }
}
