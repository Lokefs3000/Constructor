using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Rendering.Structures;
using Primary.Windowing;
using VoxelizationDemo.Core;
using VoxelizationDemo.Logic;

namespace VoxelizationDemo.Editor
{
    internal sealed class PickingManager
    {
        private readonly Queue<PendingPickingData> _pendingPicks;

        private Int2 _resolution;

        private readonly Vector3[] _dataBuffer;

        private bool _hasPickFinished;

        internal PickingManager()
        {
            _pendingPicks = new Queue<PendingPickingData>();

            _resolution = Int2.Zero;

            _dataBuffer = new Vector3[32 * 32 * 2];

            _hasPickFinished = false;
        }

        internal void FlushFinishedPicks()
        {
            if (_hasPickFinished)
            {
                if (_pendingPicks.TryDequeue(out PendingPickingData result))
                {
                    result.Callback(result.Hit, new Int2(32), _dataBuffer.AsSpan(0, 32 * 32), _dataBuffer.AsSpan(32 * 32, 32 * 32));
                }

                _hasPickFinished = false;
            }

            Window primaryWindow = VoxelRuntime.Instance.WindowManager.PrimaryWindow!;
            if (primaryWindow.ClientSize.X > primaryWindow.ClientSize.Y)
            {
                float aspect = (float)primaryWindow.ClientSize.Y / primaryWindow.ClientSize.X;
                _resolution = Int2.Max(new Int2(1336, (int)(726 * aspect)), new Int2(32));
            }
            else
            {
                float aspect = (float)primaryWindow.ClientSize.X / primaryWindow.ClientSize.Y;
                _resolution = Int2.Max(new Int2((int)(1336 * aspect), 726), new Int2(32));
            }

            if (InputSystem.Pointer.IsButtonPressed(MouseButton.Left))
            {
                RequestPick(InputSystem.Pointer.MousePosition, (relativeHit, sampleSize, positions, normals) =>
                {
                    AppLog.Editor.Information("{x}", positions[relativeHit.X + relativeHit.Y * sampleSize.X]);
                });
            }
        }

        public void RequestPick(Vector2 position, PickFinishedDelegate callback) => RequestPick(position.AsInt2(), callback);

        public void RequestPick(Int2 position, PickFinishedDelegate callback)
        {
            Int2 boxMinimum = Int2.Clamp(position - new Int2(32 / 2), Int2.Zero, _resolution - new Int2(32));
            position -= boxMinimum;

            _pendingPicks.Enqueue(new PendingPickingData(_resolution, new FGRect(boxMinimum.X, boxMinimum.Y, boxMinimum.X + 32, boxMinimum.Y + 32), position, callback));
        }

        internal Int2 Resolution => _pendingPicks.TryPeek(out PendingPickingData result) ? result.Resolution : _resolution;
        internal FGRect FocusArea => _pendingPicks.TryPeek(out PendingPickingData result) ? result.FocusArea : new FGRect(0, 0, 32, 32);

        internal Span<byte> ReadbackSpan => MemoryMarshal.Cast<Vector3, byte>(_dataBuffer.AsSpan());

        internal bool WantsNewPickingData => _pendingPicks.Count > 0 && !_hasPickFinished;
    }

    internal readonly record struct PendingPickingData(Int2 Resolution, FGRect FocusArea, Int2 Hit, PickFinishedDelegate Callback);
    internal delegate void PickFinishedDelegate(Int2 relativeHit, Int2 sampleSize, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals);
}
