using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Collections;
using Primary.Timing;

namespace VoxelizationDemo.Editor
{
    internal sealed class HandleAllocator : IDisposable
    {
        private readonly Dictionary<object, HandleInfo> _handles;
        private bool _disposedValue;

        internal HandleAllocator()
        {
            _handles = new Dictionary<object, HandleInfo>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (var (obj, handle) in _handles)
                {
                    handle.Handle.Free();
                }

                _handles.Clear();

                _disposedValue = true;
            }
        }

        ~HandleAllocator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void CleanupPrevious()
        {
            long currentTimestamp = Time.TimestampForActiveFrame;

            using RentedList<object> handlesToRemove = new RentedList<object>();
            foreach (var (obj, handle) in _handles)
            {
                if (handle.LastAccessTime - currentTimestamp > s_maxHandleLifetime)
                {
                    handle.Handle.Free();
                    handlesToRemove.Add(obj);

                    AppLog.Editor.Debug("Freeing handle for object '{obj}'", obj);
                }
            }

            if (!handlesToRemove.IsEmpty)
            {
                foreach (object obj in handlesToRemove)
                {
                    _handles.Remove(obj);
                }
            }
        }

        internal GCHandle GetHandle(object obj)
        {
            ref HandleInfo info = ref CollectionsMarshal.GetValueRefOrAddDefault(_handles, obj, out bool exists);
            if (!exists)
            {
                info = new HandleInfo(GCHandle.Alloc(obj, GCHandleType.Normal), Time.TimestampForActiveFrame);
                return info.Handle;
            }
            else
            {
                info.LastAccessTime = Time.TimestampForActiveFrame;
                return info.Handle;
            }
        }

        internal ImTextureRef GetHandleAsImTextureRef(object obj)
        {
            ref HandleInfo info = ref CollectionsMarshal.GetValueRefOrAddDefault(_handles, obj, out bool exists);
            if (!exists)
            {
                info = new HandleInfo(GCHandle.Alloc(obj, GCHandleType.Normal), Time.TimestampForActiveFrame);
                return new ImTextureRef { TexID = new ImTextureID(GCHandle.ToIntPtr(info.Handle) | IsGCHandleBit) };
            }
            else
            {
                info.LastAccessTime = Time.TimestampForActiveFrame;
                return new ImTextureRef { TexID = new ImTextureID(GCHandle.ToIntPtr(info.Handle) | IsGCHandleBit) };
            }
        }

        private static readonly long s_maxHandleLifetime = (long)(Stopwatch.Frequency * 2.0);

        internal const nint IsGCHandleBit = 1 << 31;

        private record struct HandleInfo(GCHandle Handle, long LastAccessTime);
    }
}
