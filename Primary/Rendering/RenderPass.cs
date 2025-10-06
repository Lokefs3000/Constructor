using Microsoft.Extensions.ObjectPool;

namespace Primary.Rendering
{
    public sealed class RenderPass
    {
        private DefaultObjectPool<RasterPassDescription> _rasterDescriptions;

        private List<IPassDescription> _submittedPasses;

        internal RenderPass()
        {
            _rasterDescriptions = new DefaultObjectPool<RasterPassDescription>(new RasterPassDescriptionPolicy());

            _submittedPasses = new List<IPassDescription>();
        }

        public RasterPassDescription CreateRasterPass()
        {
            RasterPassDescription description = _rasterDescriptions.Get();
            description.Reset(this);

            return description;
        }

        internal void SubmitPassForExecution(IPassDescription pass)
        {
            _submittedPasses.Add(pass);
        }

        internal void ClearForNextFrame()
        {
            for (int i = 0; i < _submittedPasses.Count; i++)
            {
                if (_submittedPasses[i] is RasterPassDescription raster)
                    _rasterDescriptions.Return(raster);
            }

            _submittedPasses.Clear();
        }

        internal IReadOnlyList<IPassDescription> Descriptions => _submittedPasses;
        internal bool IsEmpty => _submittedPasses.Count == 0;

        private struct RasterPassDescriptionPolicy : IPooledObjectPolicy<RasterPassDescription>
        {
            public RasterPassDescription Create()
            {
                return new RasterPassDescription();
            }

            public bool Return(RasterPassDescription obj)
            {
                obj.Reset(null);
                return true;
            }
        }
    }
}
