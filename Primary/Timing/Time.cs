using Primary.Common;
using System.Diagnostics;

namespace Primary.Timing
{
    public sealed class Time
    {
        private static Time? s_instance = null;
        private static Time Instance => NullableUtility.ThrowIfNull(s_instance);

        private bool _isFirstFrame;
        private long _lastFrameTimestamp;

        private double _deltaTimeDouble;
        private float _deltaTime;

        private int _frameIndex;

        private double _maxDeltaTime;

        internal Time()
        {
            s_instance = this;

            _isFirstFrame = true;
            _lastFrameTimestamp = 0;

            _deltaTimeDouble = 0;
            _deltaTime = 0;

            _frameIndex = -1;

            _maxDeltaTime = 1.0 / 3.0;
        }

        public void BeginNewFrame()
        {
            long timestampThisFrame = Stopwatch.GetTimestamp();

            if (_isFirstFrame)
            {
                _deltaTimeDouble = 0.0;
                _deltaTime = 0.0f;

                _isFirstFrame = false;
            }
            else
            {
                _deltaTimeDouble = Math.Min((timestampThisFrame - _lastFrameTimestamp) / (double)Stopwatch.Frequency, _maxDeltaTime);
                _deltaTime = (float)_deltaTimeDouble;
            }

            _lastFrameTimestamp = timestampThisFrame;

            ++_frameIndex;
        }

        public static int GetFrameDifference(int a, int b) => (int)(a > b ? (uint)a - (uint)b : (uint)b - (uint)a);

        public static double DeltaTimeDouble => Instance._deltaTimeDouble;
        public static float DeltaTime => Instance._deltaTime;

        public static long TimestampForActiveFrame => Instance._lastFrameTimestamp;

        public static int FrameIndex => Instance._frameIndex;
    }
}
