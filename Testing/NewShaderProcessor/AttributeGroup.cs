using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes;

namespace NewShaderProcessor
{
    public sealed class AttributeGroup
    {
        private readonly FrozenDictionary<string, AttributeSignature> _signatures;
        private readonly FrozenDictionary<string, AttributeSignature>.AlternateLookup<ReadOnlySpan<char>> _signaturesAlt;

        public AttributeGroup(params AttributeSignature[] attributes)
        {
            Dictionary<string, AttributeSignature> dict = new Dictionary<string, AttributeSignature>();
            foreach (AttributeSignature signature in attributes)
            {
                dict.Add(signature.GetType().Name.ToLower(), signature);
            }

            _signatures = dict.ToFrozenDictionary();
            _signaturesAlt = _signatures.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool TryGetSignature(string name, out AttributeSignature? signature) => _signatures.TryGetValue(name, out signature);
        public bool TryGetSignature(ReadOnlySpan<char> name, out AttributeSignature? signature) => _signaturesAlt.TryGetValue(name, out signature);

        public static readonly AttributeGroup Graphics = new AttributeGroup(
            new Vertex(),
            new Pixel(),
            new Constants(),
            new IALayout(),
            new Property(),
            new Display(),
            new Global(),
            new Sampled());

        public static readonly AttributeGroup Compute = new AttributeGroup(
            new Kernel(),
            new NumThreads(),
            new Constants(),
            new Property(),
            new Display(),
            new Global(),
            new Sampled());
    }
}
