using Arch.Core;
using Primary.Assets;
using Primary.Profiling;
using System.Text.Json;

namespace Primary.Systems
{
    public sealed class SystemManager
    {
        private static JsonSerializerOptions s_options = new JsonSerializerOptions
        {

        };

        private RunnableSystem[] _systems;

        internal SystemManager()
        {
            _systems = Array.Empty<RunnableSystem>();

            ResolveSystemDependencies();
        }

        private void ResolveSystemDependencies()
        {
            string sourceString = AssetFilesystem.ReadString("Content/Systems.json") ?? "[]";
            string[] systemNameTypes = JsonSerializer.Deserialize<string[]>(sourceString) ?? Array.Empty<string>();

            if (systemNameTypes.Length > 0)
            {
                List<RunnableSystem> systems = new List<RunnableSystem>();
                for (int i = 0; i < systemNameTypes.Length; i++)
                {
                    string typeName = systemNameTypes[i];
                    Type? type = Type.GetType(typeName, false);

                    if (type == null)
                    {
                        EngLog.Systems.Error("Failed to find type for system: {sys}", typeName);
                    }
                    else
                    {
                        ISystem? system = (ISystem?)Activator.CreateInstance(type, false);
                        if (system == null)
                        {
                            EngLog.Systems.Error("Failed to create instance for system: {sys}", typeName);
                            continue;
                        }

                        systems.Add(new RunnableSystem
                        {
                            Runner = system
                        });
                    }

                    _systems = systems.ToArray(); //TODO: implement dependency logic!
                }
            }
        }

        public void RunSystems()
        {
            using (new ProfilingScope("RunSystems"))
            {
                World world = Engine.GlobalSingleton.SceneManager.World;

                for (int i = 0; i < _systems.Length; i++)
                {
                    ref RunnableSystem system = ref _systems[i];
                    system.Runner.Schedule(world, null);
                }
            }
        }

        private record struct RunnableSystem
        {
            public ISystem Runner;
        }
    }
}
