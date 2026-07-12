using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using PrimaryEditor.Core;

namespace PrimaryEditor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using (EditorRuntime runtime = new EditorRuntime(args))
            {
                runtime.Run();
            }
        }
    }
}
