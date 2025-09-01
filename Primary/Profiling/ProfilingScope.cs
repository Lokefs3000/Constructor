using CommunityToolkit.HighPerformance;
using System.Runtime.CompilerServices;

namespace Primary.Profiling
{
    public struct ProfilingScope : IDisposable
    {
        private int _hash;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfilingScope(string name)
        {
            _hash = name.GetDjb2HashCode();
            ProfilingManager.Instance.BeginProfiling(name, _hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            ProfilingManager.Instance.EndProfiling(_hash);
        }
    }
}
