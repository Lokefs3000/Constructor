using Editor.Runners;
using Serilog;
using System.Diagnostics;

namespace Editor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string runnerName = args[0];
                switch (runnerName)
                {
                    case "texture": new TextureRunner().Execute(args.AsSpan(1)); return;
                    case "model": new ModelRunner().Execute(args.AsSpan(1)); return;
                    case "shader": new ShaderRunner().Execute(args.AsSpan(1)); return;
                    case "bundle": new BundleRunner().Execute(args.AsSpan(1)); return;
                }
            }

            Stopwatch sw = Stopwatch.StartNew();
            using (Editor editor = new Editor(Path.GetFullPath(args[0])))
            {
                sw.Stop();
                Log.Information("Editor startup took: {secs}s", sw.Elapsed.TotalSeconds);

                editor.Run();
            }
        }
    }
}
