using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Interop.PhysX;
using Primary.Common;

namespace Primary.Physics
{
    public unsafe sealed class PhysicsManager : IDisposable
    {
        private readonly PhysicsConfig _config;

        private readonly PxAllocatorCallback* _allocatorCallback;
        private readonly PxErrorCallback* _errorCallback;

        private readonly bool _ownsObjects;
        private readonly PxFoundation* _foundation;
        private readonly PxPhysics* _physics;

        private readonly PxMaterial* _defaultMat;

        private bool _disposedValue;

        internal PhysicsManager()
        {
            if (OperatingSystem.IsWindows())
            {
                // stupid ass windows can't find 'PhysXCommon_64.dll' bs
                NativeLibrary.Load("runtimes/win-x64/native/PhysXCommon_64.dll");
            }

            _config = new PhysicsConfig();

            _allocatorCallback = PhysXInterop.CreateAllocatorCallback((delegate* unmanaged[Cdecl]<ulong, sbyte*, sbyte*, int, void*>)&AllocateFunc, (delegate* unmanaged[Cdecl]<void*, void>)&DeallocateFunc);
            _errorCallback = PhysXInterop.CreateErrorCallback((delegate* unmanaged[Cdecl]<PxErrorCode, sbyte*, sbyte*, int, void>)&ErrorFunc);

            PxFoundation* currentFoundation = PhysXInterop.PxGetFoundation();
            PxPhysics* currentPhysics = PhysXInterop.PxGetPhysics();
            if ((currentFoundation == null) != (currentPhysics == null))
            {
                throw new InvalidOperationException("PhysX must either be fully initialized or not at all!");
            }
            else if (currentFoundation != null)
            {
                EngLog.Physics.Information("Using unowned PhysX objects");

                _ownsObjects = false;

                _foundation = currentFoundation;
                _physics = currentPhysics;
            }
            else
            {
                _ownsObjects = true;

                _foundation = PhysXInterop.PxCreateFoundation(_allocatorCallback, _errorCallback);
                _physics = PhysXInterop.PxCreatePhysics(_foundation, new PxTolerancesScale { length = 1.0f, speed = 1.0f }, Engine.IsDebugBuild, null);
            }

            _defaultMat = _physics->createMaterial(0.5f, 0.5f, 0.1f);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _defaultMat->release();

                    if (_ownsObjects)
                    {
                        _physics->release();
                        _foundation->release();
                    }

                    PhysXInterop.DestroyErrorCallback(_errorCallback);
                    PhysXInterop.DestroyAllocatorCallback(_allocatorCallback);
                }

                _disposedValue = true;
            }
        }

        ~PhysicsManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Function pointers
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void* AllocateFunc(ulong numBytes, sbyte* typeName, sbyte* filename, int line)
        {
            return NativeMemory.AlignedAlloc((nuint)numBytes, 16);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void DeallocateFunc(void* ptr)
        {
            NativeMemory.Free(ptr);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void ErrorFunc(PxErrorCode code, sbyte* message, sbyte* filename, int line)
        {
            string messageStr = new string(message);
            switch (code)
            {
                case PxErrorCode.eDEBUG_INFO: EngLog.Physics.Information("[{code}]: " + messageStr, code); break;
                case PxErrorCode.eDEBUG_WARNING: EngLog.Physics.Warning("[{code}]: " + messageStr, code); break;
                default: EngLog.Physics.Error("[{code}]: " + messageStr, code); break;
            }
        }
        #endregion

        public PhysicsConfig Config => _config;

        internal PxFoundation* PhysXFoundation => _foundation;
        internal PxPhysics* PhysXPhysics => _physics;

        internal PxMaterial* PhysXDefaultMat => _defaultMat;
    }
}
