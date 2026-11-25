using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PrimaryEditor.Assets.Filesystem
{
    public abstract class BaseFilesystem
    {
        public virtual bool TryGetLocalPath(string fullPath, [NotNullWhen(true)] out string? localPath)
        {
            // full path is already local
            if (fullPath.StartsWith(NamespaceKey))
            {
                localPath = fullPath.Contains('\\') ? fullPath.Replace('\\', '/') : fullPath;
                return true;
            }

            if (!fullPath.StartsWith(WorkingDirectory))
            {
                localPath = null;
                return false;
            }

            if (fullPath.Contains('\\'))
                fullPath = fullPath.Replace('\\', '/');

            localPath = $"{NamespaceKey}/{fullPath[(WorkingDirectory.Length + 1)..]}";
            return true;
        }

        public virtual bool TryGetFullPath(string localPath, [NotNullWhen(true)] out string? fullPath)
        {
            if (Path.IsPathFullyQualified(localPath))
            {
                fullPath = localPath;
                return true;
            }

            if (!localPath.StartsWith(NamespaceKey))
            {
                fullPath = null;
                return false;
            }

            fullPath = Path.Combine(WorkingDirectory, localPath[(NamespaceKey.Length + 1)..]);
            return true;
        }

        public virtual bool IsPathNamespacedTo(string localPath)
        {
            return localPath.StartsWith(NamespaceKey);
        }

        public abstract bool Exists(string localPath);

        public abstract string? ReadAllText(string localPath);
        public abstract byte[]? ReadAllBytes(string localPath);
        public abstract Stream? OpenStream(string localPath);

        public abstract string WorkingDirectory { get; }
        public abstract string NamespaceKey { get; }
    }
}
