using Editor.UI.Designer;
using Editor.Runners;
using Primary;
using Serilog;
using System.Diagnostics;
using Editor.Assets;

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

            if (args.Contains("--suspend-launch"))
                Console.ReadLine();

            if (args.Contains("--asset-watcher"))
            {
                using (StandaloneAssetWatcher assetWatcher = new StandaloneAssetWatcher(Path.GetFullPath(args[0]), args))
                {
                    assetWatcher.Run();
                }

                return;
            }

            if (args.Contains("--edui-designer"))
            {
                using (UIDesignerEngine designer = new UIDesignerEngine(Path.GetFullPath(args[0]), args))
                {
                    designer.Run();
                }

                return;
            }

            using (EditorRuntime editor = new EditorRuntime(Path.GetFullPath(args[0]), args))
            {
                editor.Run();
            }
        }
    }
}
