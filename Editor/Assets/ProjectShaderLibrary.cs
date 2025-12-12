using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.Assets
{
    internal class ProjectShaderLibrary : IShaderSubLibrary
    {
        private string _absolutePath;

        private bool _mappingsModified;

        private ConcurrentDictionary<string, string> _libraryMap;

        public ProjectShaderLibrary(string filepath)
        {
            _absolutePath = Path.GetFullPath(filepath);

            _mappingsModified = false;

            _libraryMap = new ConcurrentDictionary<string, string>();

            string mappingFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ShaderMappings.dat");
            if (File.Exists(mappingFile))
            {
                ReadFileMappings(mappingFile);
            }

            //foreach (string file in Directory.GetFiles(_absolutePath, "*.sbc"))
            //{
            //    if (VerifyAndGetPath(file, out string? path))
            //    {
            //        if (!_libraryMap.TryAdd(path, Path.GetFullPath(file)))
            //        {
            //            //bad
            //        }
            //    }
            //}
        }

        public void Dispose()
        {
            FlushFileMappings();
        }

        private void ReadFileMappings(string mappingsFilePath)
        {
            string source = File.ReadAllText(mappingsFilePath);

            int i = 0;
            while (i < source.Length)
            {
                int find = source.IndexOf(';', i);
                if (find == -1)
                {
                    i = source.IndexOf('\n', i + 1);
                    if (i == -1)
                        break;
                }

                string shaderPath = source.Substring(i, find - i);

                int j = find + 1;
                while (j < source.Length && !char.IsControl(source[j]))
                    j++;

                string filePath = source.Substring(find + 1, j - 1 - find);

                string fullFilePath = Path.Combine(Editor.GlobalSingleton.ProjectPath, filePath);
                if (File.Exists(fullFilePath) && VerifyAndGetPath(fullFilePath, out string? valShaderPath))
                {
                    if (valShaderPath == shaderPath)
                    {
                        ExceptionUtility.Assert(_libraryMap.TryAdd(shaderPath, filePath));
                    }
                }

                i = source.IndexOf('\n', i + 1);
                if (i == -1)
                    break;
                i++;
            }
        }

        internal void AddFileToMapping(string shaderPath, string projectPath)
        {
            projectPath = projectPath.Replace('\\', '/');

            _libraryMap.AddOrUpdate(shaderPath, projectPath, (_, _) => projectPath);
            _mappingsModified = true;
        }

        internal void FlushFileMappings()
        {
            string outputFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ShaderMappings.dat");

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _libraryMap)
            {
                sb.Append(kvp.Key);
                sb.Append(';');
                sb.AppendLine(kvp.Value);
            }

            File.WriteAllText(outputFile, sb.ToString());
        }

        private void NewFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("new " + e.FullPath);
        }

        private void OldFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("del " + e.FullPath);
        }

        private void OldFileWritten(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("mod " + e.FullPath);
        }

        public byte[]? ReadFromLibrary(string path)
        {
            if (_libraryMap.TryGetValue(path, out string? filePath))
            {
                try
                {
                    string fullPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, filePath);
                    if (!File.Exists(fullPath))
                    {
                        _libraryMap.Remove(fullPath, out string? _);
                        _mappingsModified = true;
                        return null;
                    }

                    return File.ReadAllBytes(fullPath);
                }
                catch (Exception)
                {
                    //bad
                }
            }

            using Stream? stream = AssetFilesystem.OpenStream(path);
            if (stream != null)
            {
                byte[] data = new byte[stream.Length];
                stream.ReadExactly(data);

                return data;
            }

            return null;
        }

        private static bool VerifyAndGetPath(string path, [MaybeNullWhen(false)] out string shaderPath)
        {
            try
            {
                using FileStream? stream = File.Open(path, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(stream);

                ExceptionUtility.Assert(br.ReadUInt32() == ShaderLibrary.HeaderId, "Id mismatch!");
                ExceptionUtility.Assert(br.ReadUInt32() == ShaderLibrary.HeaderVersion, "Version mismatch!");

                shaderPath = br.ReadString();
                return true;
            }
            catch (Exception)
            {
                //bad
            }

            shaderPath = null;
            return false;
        }
    }
}
