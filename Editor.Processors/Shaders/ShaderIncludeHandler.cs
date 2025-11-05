// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using Primary.Common;
using Serilog;
using SharpGen.Runtime;
using System.Runtime.InteropServices;
using Vortice.Dxc;

namespace Editor.Processors.Shaders;

public class ShaderIncludeHandler : CallbackBase, IDxcIncludeHandler
{
    private readonly string[] _includeDirectories;
    private readonly Dictionary<string, SourceCodeBlob> _sourceFiles = new Dictionary<string, SourceCodeBlob>();

    public ShaderIncludeHandler(params string[] includeDirectories)
    {
        _includeDirectories = includeDirectories;
    }

    protected override void DisposeCore(bool disposing)
    {
        foreach (var pinnedObject in _sourceFiles.Values)
            pinnedObject?.Dispose();

        _sourceFiles.Clear();
    }

    public Result LoadSource(string fileName, out IDxcBlob? includeSource)
    {
        if (fileName.StartsWith("./") || fileName.StartsWith(".\\"))
            fileName = fileName.Substring(2);

        var includeFile = GetFilePath(fileName);

        if (string.IsNullOrEmpty(includeFile))
        {
            includeSource = default;

            return Result.Fail;
        }

        if (!_sourceFiles.TryGetValue(includeFile, out SourceCodeBlob? sourceCodeBlob))
        {
            byte[] data = NewMethod(includeFile);

            sourceCodeBlob = new SourceCodeBlob(data);
            _sourceFiles.Add(includeFile, sourceCodeBlob);
        }

        includeSource = sourceCodeBlob.Blob;

        return Result.Ok;
    }

    private static byte[] NewMethod(string includeFile)
    {
        try
        {
            using (FileStream stream = FileUtility.TryWaitOpen(includeFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[stream.Length];
                stream.ReadExactly(buffer);

                return buffer;
            }
        }
        catch (Exception)
        {
            Log.Information("failed to open shader include: \"{ff}\"", includeFile);
            throw;
        }
    }

    private string? GetFilePath(string fileName)
    {
        for (int i = 0; i < _includeDirectories.Length; i++)
        {
            var filePath = _includeDirectories[i].Length == 0 ? fileName : Path.GetFullPath(Path.Combine(_includeDirectories[i], fileName));

            if (File.Exists(filePath))
                return filePath;
        }

        return null;
    }

    internal string[] ReadFiles => _sourceFiles.Keys.ToArray();

    private class SourceCodeBlob : IDisposable
    {
        private byte[] _data;
        private GCHandle _dataPointer;
        private IDxcBlobEncoding? _blob;

        internal IDxcBlob? Blob { get => _blob; }

        public SourceCodeBlob(byte[] data)
        {
            _data = data;

            _dataPointer = GCHandle.Alloc(data, GCHandleType.Pinned);

            _blob = DxcCompiler.Utils.CreateBlob(_dataPointer.AddrOfPinnedObject(), (uint)data.Length, Dxc.DXC_CP_UTF8);
        }

        public void Dispose()
        {
            //_blob?.Dispose();
            _blob = null;

            if (_dataPointer.IsAllocated)
                _dataPointer.Free();
            _dataPointer = default;
        }
    }
}