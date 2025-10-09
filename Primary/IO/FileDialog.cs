using System.Diagnostics;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Primary.IO
{
    public static unsafe class FileDialog
    {
        public static OpenFileDialogResult OpenFile(OpenFileDialogParams @params)
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.OpenFile(@params);

            throw new NotImplementedException();
        }

        public static SaveFileDialogResult SaveFile(SaveFileDialogParams @params)
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.SaveFile(@params);

            throw new NotImplementedException();
        }

        private static class Win32Impl
        {
#pragma warning disable CA1416 // Validate platform compatibility
            private static TRet TrySetupDefaultDialog<TRet, TType>(DefaultDialogParams @params, TRet defaultRet, FileDialogCallback<TRet, TType> callback, FileDialogCancel<TRet> cancel) where TRet : allows ref struct where TType : unmanaged, INativeGuid
            {
                if (@params.Filters.IsEmpty)
                {
                    @params.Filters = s_defaultFilters;
                }

                Guid clsid;
                if (typeof(TType) == typeof(IFileOpenDialog))
                    clsid = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
                else if (typeof(TType) == typeof(IFileSaveDialog))
                    clsid = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
                else
                    throw new NotSupportedException(typeof(TType).Name);

                CoInstancePtr<TType> pfd = null;
                if (CoInstancePtr<TType>.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, ref pfd))
                {
                    IFileDialog* rootDialog = (IFileDialog*)pfd.Pointer;

                    uint dwFlags;
                    if (rootDialog->GetOptions(&dwFlags).SUCCEEDED)
                    {
                        if (rootDialog->SetOptions(dwFlags | 0x40/*FOS_FORCEFILESYSTEM */).SUCCEEDED)
                        {
                            COMDLG_FILTERSPEC* spec = stackalloc COMDLG_FILTERSPEC[@params.Filters.Length];
                            for (int i = 0; i < @params.Filters.Length; i++)
                            {
                                FileFilter filter = @params.Filters[i];
                                spec[i] = new COMDLG_FILTERSPEC
                                {
                                    pszName = CoAllocateString(filter.FriendlyName),
                                    pszSpec = CoAllocateString(filter.Spec)
                                };
                            }

                            if (rootDialog->SetFileTypes((uint)@params.Filters.Length, spec).SUCCEEDED)
                            {
                                if ((uint)@params.DefaultFilterIndex < @params.Filters.Length)
                                    rootDialog->SetFileTypeIndex((uint)@params.DefaultFilterIndex);

                                if (@params.DefaultFileName != null)
                                {
                                    ReadOnlySpan<char> @str = @params.DefaultFileName;
                                    int indexOfDot = str.LastIndexOf('.');

                                    if (indexOfDot != -1)
                                    {
                                        if (indexOfDot > 0 && indexOfDot < str.Length - 1)
                                        {
                                            char* ptr = CoAllocateString(str.Slice(indexOfDot + 1));
                                            rootDialog->SetDefaultExtension(ptr);
                                            CoFreeString(ptr);
                                        }

                                        if (indexOfDot != 0)
                                        {
                                            char* ptr = CoAllocateString(str.Slice(0, indexOfDot));
                                            rootDialog->SetFileName(ptr);
                                            CoFreeString(ptr);
                                        }
                                    }
                                    else
                                    {
                                        char* ptr = CoAllocateString(str);
                                        rootDialog->SetFileName(ptr);
                                        CoFreeString(ptr);
                                    }
                                }

                                if (@params.DefaultDirectory != null)
                                {
                                    string shellString = "shell:" + @params.DefaultDirectory;
                                    char* ptr = CoAllocateString(shellString);

                                    IShellItem* defFolder = null;
                                    if (Windows.SHCreateItemFromParsingName(ptr, null, Windows.__uuidof<IShellItem>(), (void**)&defFolder).SUCCEEDED)
                                    {
                                        rootDialog->SetDefaultFolder(defFolder);
                                        defFolder->Release();
                                    }

                                    CoFreeString(ptr);
                                }

                                HRESULT hr = rootDialog->Show(HWND.NULL);
                                if (hr.SUCCEEDED)
                                {
                                    defaultRet = callback(pfd.Pointer);
                                }
                                else if ((uint)hr.Value == 0x800704C7 /*ERROR.ERROR_CANCELLED*/)
                                {
                                    defaultRet = cancel();
                                }
                            }

                            for (int i = 0; i < @params.Filters.Length; i++)
                            {
                                ref COMDLG_FILTERSPEC filterSpec = ref spec[i];
                                CoFreeString(filterSpec.pszName);
                                CoFreeString(filterSpec.pszSpec);
                            }
                        }
                    }
                }
                pfd.Dispose();

                return defaultRet;
            }

            public static OpenFileDialogResult OpenFile(OpenFileDialogParams @params)
            {
                Debug.Assert(OperatingSystem.IsWindows());

                bool hasMultiSelect = @params.AllowMultiSelect;

                return TrySetupDefaultDialog<OpenFileDialogResult, IFileOpenDialog>(new DefaultDialogParams(@params), new OpenFileDialogResult(FileDialogResult.Error, ReadOnlySpan<string>.Empty), (x) =>
                {
                    ReadOnlySpan<string> mem = ReadOnlySpan<string>.Empty;

                    IShellItemArray* result = null;
                    if (x->GetResults(&result).SUCCEEDED)
                    {
                        if (hasMultiSelect)
                        {
                            uint numItems = 0;
                            //such a weird function
                            if (result->GetCount(&numItems).SUCCEEDED)
                            {
                                string[] arr = new string[numItems];
                                int valid = 0;

                                for (int i = 0; i < numItems; i++)
                                {
                                    IShellItem* psi = null;
                                    if (result->GetItemAt((uint)i, &psi).SUCCEEDED)
                                    {
                                        char* psiResult = null;
                                        if (psi->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &psiResult).SUCCEEDED)
                                        {
                                            arr[valid++] = new string(psiResult);
                                            Windows.CoTaskMemFree(psiResult);
                                        }

                                        psi->Release();
                                    }
                                }

                                mem = new ReadOnlySpan<string>(arr, 0, valid);
                            }
                        }
                        else
                        {
                            IShellItem* psi = null;
                            if (result->GetItemAt(0, &psi).SUCCEEDED)
                            {
                                char* psiResult = null;
                                if (psi->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &psiResult).SUCCEEDED)
                                {
                                    mem = new ReadOnlySpan<string>([new string(psiResult)]);
                                    Windows.CoTaskMemFree(psiResult);
                                }

                                psi->Release();
                            }
                        }

                        result->Release();
                    }

                    return new OpenFileDialogResult(mem.IsEmpty ? FileDialogResult.Error : FileDialogResult.Ok, mem);
                }, () => new OpenFileDialogResult(FileDialogResult.Cancel, ReadOnlySpan<string>.Empty));
            }

            public static SaveFileDialogResult SaveFile(SaveFileDialogParams @params)
            {
                Debug.Assert(OperatingSystem.IsWindows());

                return TrySetupDefaultDialog<SaveFileDialogResult, IFileSaveDialog>(new DefaultDialogParams(@params), new SaveFileDialogResult(FileDialogResult.Error, string.Empty), (x) =>
                {
                    string mem = string.Empty;

                    IShellItem* psi = null;
                    if (x->GetResult(&psi).SUCCEEDED)
                    {
                        char* psiResult = null;
                        if (psi->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &psiResult).SUCCEEDED)
                        {
                            mem = new string(psiResult);
                            Windows.CoTaskMemFree(psiResult);
                        }

                        psi->Release();
                    }

                    return new SaveFileDialogResult(mem == string.Empty ? FileDialogResult.Error : FileDialogResult.Ok, mem);
                }, () => new SaveFileDialogResult(FileDialogResult.Cancel, string.Empty));
            }

            private static char* CoAllocateString(ReadOnlySpan<char> str)
            {
                Debug.Assert(!str.IsEmpty);

                char* ptr = (char*)Windows.CoTaskMemAlloc((nuint)((str.Length + 1) * sizeof(char)));

                str.CopyTo(new Span<char>(ptr, str.Length));
                ptr[str.Length] = '\0';

                return ptr;
            }

            private static void CoFreeString(char* ptr)
            {
                Debug.Assert(ptr != null);
                Windows.CoTaskMemFree(ptr);
            }

            private static readonly FileFilter[] s_defaultFilters = [
                new FileFilter("Any file", "*.*")
                ];

            private delegate TRet FileDialogCallback<TRet, TType>(TType* dialog) where TRet : allows ref struct where TType : unmanaged, INativeGuid;
            private delegate TRet FileDialogCancel<TRet>() where TRet : allows ref struct;

            private struct CoInstancePtr<T> : IDisposable where T : unmanaged, INativeGuid
            {
                private IUnknown* _ptr;

                public CoInstancePtr(T* ptr)
                {
                    _ptr = (IUnknown*)ptr;
                }

                public void Dispose()
                {
                    if (_ptr != null)
                        _ptr->Release();
                }

                public T* Pointer => (T*)_ptr;

                public static bool CoCreateInstance(Guid rclsid, IUnknown* pUnkOuter, CLSCTX dwClsContext, T** ptr)
                {
                    void* raw = null;

                    HRESULT hr = Windows.CoCreateInstance(&rclsid, pUnkOuter, (uint)dwClsContext, Windows.__uuidof<T>(), &raw);
                    if (hr.FAILED)
                    {
                        *ptr = null;
                        return false;
                    }

                    *ptr = (T*)raw;
                    return true;
                }

                public static bool CoCreateInstance(Guid rclsid, IUnknown* pUnkOuter, CLSCTX dwClsContext, ref CoInstancePtr<T> ptr)
                {
                    void* raw = null;

                    HRESULT hr = Windows.CoCreateInstance(&rclsid, pUnkOuter, (uint)dwClsContext, Windows.__uuidof<T>(), &raw);
                    if (hr.FAILED)
                    {
                        ptr._ptr = null;
                        return false;
                    }

                    ptr._ptr = (IUnknown*)raw;
                    return true;
                }


                public static implicit operator CoInstancePtr<T>(T* ptr) => new CoInstancePtr<T>(ptr);
            }

            private ref struct DefaultDialogParams
            {
                public string? DefaultFileName;
                public string? DefaultDirectory;

                public ReadOnlySpan<FileFilter> Filters;
                public int DefaultFilterIndex;

                public DefaultDialogParams(OpenFileDialogParams @params)
                {
                    DefaultFileName = @params.DefaultFileName;
                    DefaultDirectory = @params.DefaultDirectory;

                    Filters = @params.Filters;
                    DefaultFilterIndex = @params.DefaultFilterIndex;
                }

                public DefaultDialogParams(SaveFileDialogParams @params)
                {
                    DefaultFileName = @params.DefaultFileName;
                    DefaultDirectory = @params.DefaultDirectory;

                    Filters = @params.Filters;
                    DefaultFilterIndex = @params.DefaultFilterIndex;
                }
            }

#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    public readonly record struct FileFilter(string FriendlyName, string Spec);

    public ref struct OpenFileDialogParams
    {
        public string? DefaultFileName;
        public string? DefaultDirectory;

        public ReadOnlySpan<FileFilter> Filters;
        public int DefaultFilterIndex;

        public bool AllowMultiSelect;

        public OpenFileDialogParams()
        {
            DefaultFileName = null;
            DefaultDirectory = null;

            Filters = ReadOnlySpan<FileFilter>.Empty;
            DefaultFilterIndex = 0;

            AllowMultiSelect = false;
        }
    }

    public ref struct SaveFileDialogParams
    {
        public string? DefaultFileName;
        public string? DefaultDirectory;

        public ReadOnlySpan<FileFilter> Filters;
        public int DefaultFilterIndex;

        public SaveFileDialogParams()
        {
            DefaultFileName = null;
            DefaultDirectory = null;

            Filters = ReadOnlySpan<FileFilter>.Empty;
            DefaultFilterIndex = 0;
        }
    }

    public readonly ref struct OpenFileDialogResult
    {
        public readonly FileDialogResult Result;
        public readonly ReadOnlySpan<string> Files;

        internal OpenFileDialogResult(FileDialogResult result, ReadOnlySpan<string> files)
        {
            Result = result;
            Files = files;
        }
    }

    public readonly ref struct SaveFileDialogResult
    {
        public readonly FileDialogResult Result;
        public readonly string File;

        internal SaveFileDialogResult(FileDialogResult result, string files)
        {
            Result = result;
            File = files;
        }
    }

    public enum FileDialogResult : byte
    {
        Error = 0,

        Ok,
        Cancel,
    }
}
