using Primary.Pooling;

namespace Primary.Rendering2.Recording
{
    public sealed class FrameGraphState
    {
        private ObjectPool<RenderPassStateData> _stateDataPool;
        private Dictionary<int, RenderPassStateData> _stateDict;

        internal FrameGraphState()
        {
            _stateDataPool = new ObjectPool<RenderPassStateData>(new StateDataPolicy());
            _stateDict = new Dictionary<int, RenderPassStateData>();
        }

        internal void ClearForFrame()
        {
            foreach (var kvp in _stateDict)
            {
                _stateDataPool.Return(kvp.Value);
            }

            _stateDict.Clear();
        }

        internal RenderPassStateData GetStateData(int passIndex)
        {
            if (_stateDict.TryGetValue(passIndex, out RenderPassStateData? stateData))
                return stateData;

            stateData = _stateDataPool.Get();
            _stateDict[passIndex] = stateData;

            return stateData;
        }

        private readonly record struct StateDataPolicy : IObjectPoolPolicy<RenderPassStateData>
        {
            public RenderPassStateData Create() => new RenderPassStateData();

            public bool Return(ref RenderPassStateData obj)
            {
                obj.Reset();
                return true;
            }
        }
    }
}
