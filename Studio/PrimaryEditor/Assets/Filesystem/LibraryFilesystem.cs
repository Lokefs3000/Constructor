using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Common;
using PrimaryEditor.Project;

namespace PrimaryEditor.Assets.Filesystem
{
    public sealed class LibraryFilesystem : BaseFilesystem
    {
        internal LibraryFilesystem()
        {

        }

        public override bool Exists(string localPath)
        {
            string fullPath = $"{WorkingDirectory}{localPath.AsSpan()[NamespaceKey.Length..]}";
            return File.Exists(fullPath);
        }

        public override string? ReadAllText(string localPath)
        {
            string fullPath = $"{WorkingDirectory}{localPath.AsSpan()[NamespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.ReadAllText(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        public override byte[]? ReadAllBytes(string localPath)
        {
            string fullPath = $"{WorkingDirectory}{localPath.AsSpan()[NamespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.ReadAllBytes(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        public override Stream? OpenStream(string localPath)
        {
            string fullPath = $"{WorkingDirectory}{localPath.AsSpan()[NamespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.OpenRead(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        public override string WorkingDirectory => ProjectData.Instance.Paths.LibraryFolder;
        public override string NamespaceKey => "Library";
    }
}
