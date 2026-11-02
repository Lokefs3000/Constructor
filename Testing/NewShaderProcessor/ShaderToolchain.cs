using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NewShaderProcessor.Native;
using NewShaderProcessor.Preprocessor;
using Primary.Common;
using Primary.Memory.Native;
using Serilog;
using SharpGen.Runtime;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace NewShaderProcessor
{
#pragma warning disable CA1416 // Validate platform compatibility
    public unsafe sealed class ShaderToolchain : IDisposable
    {
        private readonly ComPtr<IDxcCompiler3> _compiler;
        private readonly ComPtr<IDxcUtils> _utils;

        private readonly NativeIncludeHandler _includeHandler;

        private readonly NativeStringArray _preprocessorArgs;

        private bool _disposedValue;

        public ShaderToolchain()
        {
            {
                Guid clsid = CLSID.CLSID_DxcCompiler;

                IDxcCompiler3* compiler = null;
                HRESULT hr = DirectX.DxcCreateInstance(&clsid, Windows.__uuidof<IDxcCompiler3>(), (void**)&compiler);
                if (hr.FAILED)
                {
                    Marshal.ThrowExceptionForHR(hr.Value);
                }

                _compiler = compiler;
            }

            {
                Guid clsid = CLSID.CLSID_DxcUtils;

                IDxcUtils* utils = null;
                HRESULT hr = DirectX.DxcCreateInstance(&clsid, Windows.__uuidof<IDxcUtils>(), (void**)&utils);
                if (hr.FAILED)
                {
                    Marshal.ThrowExceptionForHR(hr.Value);
                }

                _utils = utils;
            }

            _includeHandler = new NativeIncludeHandler(_utils);

            _preprocessorArgs = new NativeStringArray(
                "-HV", "2021",
                "-WX",
                "-P");
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _includeHandler.Dispose();

                _preprocessorArgs.Dispose();

                _utils.Get()->Release();
                _compiler.Get()->Release();

                _disposedValue = true;
            }
        }

        ~ShaderToolchain()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void CompileForGraphics(in CompileOptions options)
        {
            ShaderData data = new ShaderData();

            using RentedArray<char> initialPreprocess = RunDxcPreprocessor(options.SourceText);

            {
                SourcePreprocessor preprocessor = new SourcePreprocessor(
                    options.Logger,
                    data,
                    initialPreprocess.Span,
                    options.SourceName);

                preprocessor.Execute();
            }

            
        }

        private RentedArray<char> RunDxcPreprocessor(string sourceText)
        {
            fixed (char* ptr = sourceText)
            {
                IDxcResult* result = null;
                IDxcBlobUtf16* hlslBlob = null;
                try
                {
                    DxcBuffer buffer = new DxcBuffer
                    {
                        Encoding = DXC.DXC_CP_UTF16,
                        Ptr = ptr,
                        Size = (nuint)(Unsafe.SizeOf<char>() * sourceText.Length)
                    };

                    HRESULT hr = hr = _compiler.Get()->Compile(
                        &buffer,
                        _preprocessorArgs.Array,
                        (uint)_preprocessorArgs.Length,
                        _includeHandler.Object,
                        Windows.__uuidof<IDxcResult>(),
                        (void**)&result);

                    if (hr.FAILED)
                    {
                        string? errorBuffer = GetErrorsAsString(result);
                        if (errorBuffer != null)
                        {
                            throw new CompilerException(errorBuffer);
                        }

                        Marshal.ThrowExceptionForHR(hr.Value);
                    }

                    hr = result->GetOutput(DXC_OUT_KIND.DXC_OUT_HLSL, Windows.__uuidof<IDxcBlobUtf16>(), (void**)&hlslBlob, null);
                    if (hr.FAILED)
                    {
                        Marshal.ThrowExceptionForHR(hr.Value);
                    }

                    RentedArray<char> array = new RentedArray<char>((int)hlslBlob->GetStringLength());
                    new ReadOnlySpan<char>(hlslBlob->GetStringPointer(), array.Count).CopyTo(array.Span);

                    return array;
                }
                finally
                {
                    if (result != null)
                        result->Release();
                    if (hlslBlob != null)
                        hlslBlob->Release();
                }
            }
        }

        private static string? GetErrorsAsString(IDxcResult* result)
        {
            IDxcBlobUtf16* outBlob = null;
            HRESULT hr = result->GetOutput(DXC_OUT_KIND.DXC_OUT_ERRORS, Windows.__uuidof<IDxcBlobUtf16>(), (void**)&outBlob, null);

            if (hr.FAILED || outBlob == null)
            {
                return null;
            }

            try
            {
                return new string(outBlob->GetStringPointer(), 0, (int)outBlob->GetStringLength());
            }
            finally
            {
                outBlob->Release();
            }
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility

    public readonly record struct CompileOptions(string SourceText, string SourceName, ILogger? Logger);
}
