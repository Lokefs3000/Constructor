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

        internal Time()
        {
            s_instance = this;

            _isFirstFrame = true;
            _lastFrameTimestamp = 0;

            _deltaTimeDouble = 0;
            _deltaTime = 0;

            _frameIndex = 0;
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
                _deltaTimeDouble = (timestampThisFrame - _lastFrameTimestamp) / (double)Stopwatch.Frequency;
                _deltaTime = (float)_deltaTimeDouble;
            }

            _lastFrameTimestamp = timestampThisFrame;

            _frameIndex++;
        }

        public static double DeltaTimeDouble => Instance._deltaTimeDouble;
        public static float DeltaTime => Instance._deltaTime;

        public static long TimestampForActiveFrame => Instance._lastFrameTimestamp;

        public static int FrameIndex => Instance._frameIndex;
    }
}
