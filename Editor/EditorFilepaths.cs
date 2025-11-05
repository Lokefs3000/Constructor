namespace Editor
{
    public static class EditorFilepaths
    {
        public static string ContentPath { get; private set; } = string.Empty;
        public static string SourcePath { get; private set; } = string.Empty;

        public static string LibraryPath { get; private set; } = string.Empty;
        public static string LibraryImportedPath { get; private set; } = string.Empty;
        public static string LibraryIntermediatePath { get; private set; } = string.Empty;
        public static string LibraryAssetsPath { get; private set; } = string.Empty;

        public static string EnginePath { get; private set; } = string.Empty;
        public static string EditorPath { get; private set; } = string.Empty;

        internal static void Initialize(string filePath)
        {
            ContentPath = Path.Combine(filePath, "Content");
            SourcePath = Path.Combine(filePath, "Source");

            LibraryPath = Path.Combine(filePath, "Library");
            LibraryImportedPath = Path.Combine(LibraryPath, "Imported");
            LibraryIntermediatePath = Path.Combine(LibraryPath, "Intermediate");
            LibraryAssetsPath = Path.Combine(LibraryPath, "Assets");

            EnginePath = @"D:/source/repos/Constructor/Source/Engine";
            EditorPath = @"D:/source/repos/Constructor/Source/Editor";
        }
    }
}
