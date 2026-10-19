using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Diagnostics;
using Primary.Threading;
using Primary.Windowing;
using VoxelizationDemo.Core;
using VoxelizationDemo.Logic;

namespace VoxelizationDemo.Editor
{
    internal sealed class PickingManager
    {
        private readonly Queue<PendingPickingData> _pendingPicks;

        private Int2 _resolution;
        private Vector2 _positionScale;

        private readonly Vector3[] _vectorBuffer;
        private readonly Half[] _rawBuffer;

        private bool _hasPickFinished;

        private ReinterpetPickingData _reinterpetJob;
        private JobHandle _reinterpetJobHandle;

        internal PickingManager()
        {
            _pendingPicks = new Queue<PendingPickingData>();

            _resolution = new Int2(32);

            _vectorBuffer = new Vector3[32 * 32 * 2];
            _rawBuffer = new Half[32 * 32 * 2 * 4];

            _hasPickFinished = false;

            _reinterpetJob = new ReinterpetPickingData();
            _reinterpetJobHandle = JobHandle.Completed;
        }

        internal void FlushFinishedPicks()
        {
            if (_hasPickFinished)
            {
                if (!_reinterpetJobHandle.HasCompleted)
                    _reinterpetJobHandle.WaitForCompletion();
                _hasPickFinished = false;

                if (_pendingPicks.TryDequeue(out PendingPickingData result))
                {
                    result.Callback(result.Hit, new Int2(32), _vectorBuffer.AsSpan(0, 32 * 32), _vectorBuffer.AsSpan(32 * 32, 32 * 32));
                }
            }

            Window primaryWindow = VoxelRuntime.Instance.WindowManager.PrimaryWindow!;
            if (primaryWindow.ClientSize.X > primaryWindow.ClientSize.Y)
            {
                float aspect = ((float)primaryWindow.ClientSize.Y / primaryWindow.ClientSize.X) / (726.0f / 1336.0f);
                _resolution = Int2.Max(new Int2(1336, (int)(726 * aspect)), new Int2(32));
            }
            else
            {
                float aspect = ((float)primaryWindow.ClientSize.X / primaryWindow.ClientSize.Y) / (1336.0f / 726.0f);
                _resolution = Int2.Max(new Int2((int)(1336 * aspect), 726), new Int2(32));
            }

            _positionScale = _resolution.AsVector2() / primaryWindow.ClientSize.AsVector2();

            if (InputSystem.Pointer.IsButtonHeld(MouseButton.Left) && _pendingPicks.Count == 0)
            {
                RequestPick(InputSystem.Pointer.MousePosition, (relativeHit, sampleSize, positions, normals) =>
                {
                    Vector3 pos = positions[relativeHit.X + relativeHit.Y * sampleSize.X];
                    Vector3 norm = normals[relativeHit.X + relativeHit.Y * sampleSize.X];

                    Gizmos.DrawLine(pos, pos + norm, Color.Red);
                });
            }
        }

        public void RequestPick(Vector2 position, PickFinishedDelegate callback) => RequestPick(position.AsInt2(), callback);

        public void RequestPick(Int2 position, PickFinishedDelegate callback)
        {
            Int2 maxAxis = _resolution - Int2.One;

            position = Int2.Clamp((position.AsVector2() * _positionScale).AsInt2(), Int2.Zero, maxAxis);

            Int2 boxMinimum = Int2.Clamp(position - new Int2(32 / 2), Int2.Zero, _resolution - new Int2(32));
            position -= boxMinimum;

            _pendingPicks.Enqueue(new PendingPickingData(_resolution, new FGRect(boxMinimum.X, boxMinimum.Y, boxMinimum.X + 32, boxMinimum.Y + 32), position, callback));
        }

        internal void ConsumePickRequest()
        {
            if (_pendingPicks.Count > 0)
            {
                _hasPickFinished = true;

                if (!_reinterpetJobHandle.HasCompleted)
                    _reinterpetJobHandle.WaitForCompletion();

                _reinterpetJob.InputData = _rawBuffer;
                _reinterpetJob.OutputData = _vectorBuffer;
                _reinterpetJobHandle = JobScheduler.Schedule(_reinterpetJob, JobPriority.Realtime);

                JobScheduler.Flush(_reinterpetJobHandle);
            }
        }

        internal Int2 Resolution => _pendingPicks.TryPeek(out PendingPickingData result) ? result.Resolution : _resolution;
        internal FGRect FocusArea => _pendingPicks.TryPeek(out PendingPickingData result) ? result.FocusArea : new FGRect(0, 0, 32, 32);

        internal Span<byte> ReadbackSpan => MemoryMarshal.Cast<Half, byte>(_rawBuffer.AsSpan());

        internal bool WantsNewPickingData => _pendingPicks.Count > 0 && !_hasPickFinished;

        private sealed class ReinterpetPickingData : IJob
        {
            public Half[]? InputData;
            public Vector3[]? OutputData;

            public void Execute()
            {
                if (InputData != null && OutputData != null)
                {
                    for (int i = 0; i < OutputData.Length; i++)
                    {
                        ref Vector3 output = ref OutputData[i];
                        ref Half inputX = ref InputData[i * 4];

                        bool hasAnyDataAt = Unsafe.Add(ref inputX, 3) == Half.One;
                        output = hasAnyDataAt ? new Vector3((float)inputX, (float)Unsafe.Add(ref inputX, 1), (float)Unsafe.Add(ref inputX, 2)) : Vector3.NegativeInfinity;
                    }
                }

                InputData = null;
                OutputData = null;
            }
        }
    }

    internal readonly record struct PendingPickingData(Int2 Resolution, FGRect FocusArea, Int2 Hit, PickFinishedDelegate Callback);
    internal delegate void PickFinishedDelegate(Int2 relativeHit, Int2 sampleSize, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals);
}
