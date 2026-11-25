using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Primary.Assets;
using Primary.Common;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace NewShaderProcessor.Native
{
#pragma warning disable CA1416 // Validate platform compatibility
    internal unsafe sealed class NativeIncludeHandler : IDisposable
    {
        private readonly ComPtr<IDxcUtils> _utils;

        private readonly GCHandle _handle;
        private readonly NativeObject* _object;

        private readonly Dictionary<string, Ptr<IDxcBlob>> _loadedBlobs;

        private bool _disposedValue;

        internal NativeIncludeHandler(ComPtr<IDxcUtils> utils)
        {
            _utils = utils;
            utils.Get()->AddRef();

            _handle = GCHandle.Alloc(this, GCHandleType.Normal);

            // This is so ass
            _object = NativeHelper.New<NativeObject>();
            _object->Handle = GCHandle.ToIntPtr(_handle);
            _object->Base = new IDxcIncludeHandler
            {
                lpVtbl = (void**)NativeHelper.New<IDxcIncludeHandler.Vtbl<IDxcIncludeHandler>>()
            };

            IDxcIncludeHandler.Vtbl<IDxcIncludeHandler>* vtbl = (IDxcIncludeHandler.Vtbl<IDxcIncludeHandler>*)_object->Base.lpVtbl;
            *vtbl = new IDxcIncludeHandler.Vtbl<IDxcIncludeHandler>
            {
                QueryInterface = &ProxyQueryInterface,
                AddRef = &ProxyAddRef,
                Release = &ProxyRelease,
                LoadSource = &ProxyLoadSource
            };

            _loadedBlobs = new Dictionary<string, Ptr<IDxcBlob>>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (var (_, blob) in _loadedBlobs)
                {
                    blob.Pointer->Release();
                }
                _loadedBlobs.Clear();

                NativeHelper.Free(_object);
                _handle.Free();

                _utils.Get()->Release();

                _disposedValue = true;
            }
        }

        ~NativeIncludeHandler()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public IDxcIncludeHandler* Object => (IDxcIncludeHandler*)_object;

        private HRESULT LoadSource(string filename, out IDxcBlob* includeSource)
        {
            ref Ptr<IDxcBlob> loadedBlob = ref CollectionsMarshal.GetValueRefOrAddDefault(_loadedBlobs, filename, out bool exists);
            if (!exists)
            {
                loadedBlob = Ptr<IDxcBlob>.Null;

                try
                {
                    string? sourceText = AssetFilesystem.ReadString(filename);
                    if (sourceText != null)
                    {
                        IDxcBlobUtf16* blobUtf16 = null;
                        fixed (char* ptr = sourceText)
                        {
                            // We are so fucked if this modifies the data ptr
                            HRESULT hr = _utils.Get()->CreateBlob(ptr, (uint)(sourceText.Length * Unsafe.SizeOf<char>()), DXC.DXC_CP_UTF16, (IDxcBlobEncoding**)&blobUtf16);
                            if (hr.FAILED)
                            {
                                Marshal.ThrowExceptionForHR(hr.Value);
                            }
                        }

                        loadedBlob = (IDxcBlob*)blobUtf16;
                    }
                }
                catch (Exception)
                {
                }
            }

            includeSource = loadedBlob.Pointer;
            return loadedBlob.IsNull ? E_FAIL : S_OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int ProxyQueryInterface(IDxcIncludeHandler* self, Guid* riid, void** ppvObject)
        {
            *ppvObject = null;
            return E_NOINTERFACE; //E_NOINTERFACE
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint ProxyAddRef(IDxcIncludeHandler* self)
        {
            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint ProxyRelease(IDxcIncludeHandler* self)
        {
            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int ProxyLoadSource(IDxcIncludeHandler* self, char* pFilename, IDxcBlob** ppIncludeSource)
        {
            string filename = new string(pFilename);

            GCHandle handle = GCHandle.FromIntPtr(((NativeObject*)self)->Handle);
            int ret = ((NativeIncludeHandler)handle.Target!).LoadSource(filename, out *ppIncludeSource).Value;

            return ret;
        }

        private const int S_OK = unchecked((int)0x00000000);
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_FAIL = unchecked((int)0x80004005);

        private struct NativeObject
        {
            public IDxcIncludeHandler Base;
            public nint Handle;
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}
