using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using PrimaryEditor.Project;
using PrimaryEditor.Scripting.Physical;
using PrimaryEditor.Scripting.Serialization;

namespace PrimaryEditor.Scripting
{
    public sealed class ScriptPipeline
    {
        // Filesystem
        private readonly SourceFilesystem _sourceFilesystem;
        private readonly SourceFileWatcher _sourceFileWatcher;
        private readonly FileResolver _fileResolver;

        public ScriptPipeline()
        {
            _sourceFilesystem = new SourceFilesystem();
            _sourceFileWatcher = new SourceFileWatcher(string.Empty);
            _fileResolver = new FileResolver();
        }

        public void PerformLaunchCheck()
        {
            List<(string localPath, AsmDefFile file, AssemblyDefinition def)> files = new List<(string localPath, AsmDefFile file, AssemblyDefinition def)>();

            FindAllAssemblies();
            ResolveAllDefinitions();

            void FindAllAssemblies()
            {
                EdLog.Script.Information("Finding all assemblies");

                foreach (string asmDefFile in Directory.EnumerateFiles(ProjectData.Instance!.Paths.SourceFolder, "*.asmdef", SearchOption.AllDirectories))
                {
                    try
                    {
                        string localPath = File.ReadAllText(asmDefFile);
                        AsmDefFile file = JsonSerializer.Deserialize(localPath, SerializerContext.Default.AsmDefFile)!;

                        AssemblyDefinition definition = new AssemblyDefinition(
                            _sourceFilesystem.GetLocalPath(asmDefFile),
                            file.AssemblyName);

                        _fileResolver.SetDefinitionRoot(definition, definition.LocalPath);

                        files.Add((definition.LocalPath, file, definition));
                    }
                    catch (JsonException ex)
                    {
                        EdLog.Script.Error(ex, "Failed to deserialize assembly defintion from file '{n}'", asmDefFile);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        EdLog.Script.Error(ex, "Failed to setup assembly defintion from file '{n}'", asmDefFile);
                        throw;
                    }
                }
            }

            void ResolveAllDefinitions()
            {
                foreach ((string localPath, AsmDefFile file, AssemblyDefinition def) in files)
                {
                    
                }
            }
        }
    }
}
