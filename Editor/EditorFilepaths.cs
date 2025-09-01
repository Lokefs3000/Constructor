using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    public static class EditorFilepaths
    {
        public static string ContentPath { get; private set; } = string.Empty;
        public static string SourcePath { get; private set; } = string.Empty;

        public static string LibraryPath { get; private set; } = string.Empty;
        public static string LibraryAssetsPath { get; private set; } = string.Empty;

        internal static void Initialize(string filePath)
        {
            ContentPath = Path.Combine(filePath, "Content");
            SourcePath = Path.Combine(filePath, "Source");

            LibraryPath = Path.Combine(filePath, "Library");
            LibraryAssetsPath = Path.Combine(LibraryPath, "Assets");
        }
    }
}
