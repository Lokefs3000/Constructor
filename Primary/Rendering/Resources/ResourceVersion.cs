using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Pooling;
using Primary.Rendering.Passes;

namespace Primary.Rendering.Resources
{
    public sealed class ResourceVersion
    {
        private readonly List<WriterPass> _writers;
        private readonly List<RenderPassIndex> _readers;

        private RenderPassIndex? _currentWriter;
        private int _currentReadIndex;

        internal ResourceVersion()
        {
            _writers = new List<WriterPass>();
            _readers = new List<RenderPassIndex>();

            _currentWriter = null;
            _currentReadIndex = 0;
        }

        internal void ClearAll()
        {
            _writers.Clear();
            _readers.Clear();

            _currentWriter = null;
            _currentReadIndex = 0;
        }

        internal void Write(RenderPassIndex pass)
        {
            _writers.Add(new WriterPass(pass, _readers.Count));
            
            _currentWriter = pass;
            _currentReadIndex = _readers.Count;
        }

        internal void Read(RenderPassIndex pass)
        {
            _readers.Add(pass);
        }

        public ROList<WriterPass> Writers => _writers;
        public ROList<RenderPassIndex> Readers => _readers;

        public RenderPassIndex? CurrentWriter => _currentWriter;
        public int CurrentReadIndex => _currentReadIndex;

        internal sealed class GenericPoolPolicy : IObjectPoolPolicy<ResourceVersion>
        {
            public ResourceVersion Create() => new ResourceVersion();

            public bool Return(ref ResourceVersion obj)
            {
                obj.ClearAll();
                return true;
            }

            public static readonly GenericPoolPolicy Default = new GenericPoolPolicy();
        }
    }

    public readonly record struct WriterPass(RenderPassIndex Pass, int ReaderMax);
}
