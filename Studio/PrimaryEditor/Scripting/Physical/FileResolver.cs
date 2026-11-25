using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Scripting.Physical
{
    public sealed class FileResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _definitionRoots;
        private readonly Dictionary<AssemblyDefinition, string> _folderDefinitions;

        private readonly Dictionary<string, AssemblyDefinition>.AlternateLookup<ReadOnlySpan<char>> _definitionRootsAlt;

        internal FileResolver()
        {
            _definitionRoots = new Dictionary<string, AssemblyDefinition>();
            _folderDefinitions = new Dictionary<AssemblyDefinition, string>();

            _definitionRootsAlt = _definitionRoots.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        internal bool SetDefinitionRoot(AssemblyDefinition definition, string rootDirectory)
        {
            if (rootDirectory.EndsWith('/'))
            {
                EdLog.Script.Warning("Root directory should not end with a slash '{p}'", rootDirectory);
                rootDirectory = rootDirectory[..^1];
            }

            if (!_definitionRoots.TryAdd(rootDirectory, definition))
            {
                if (_definitionRoots.TryGetValue(rootDirectory, out AssemblyDefinition? assemblyAt) && assemblyAt == definition)
                    return true;

                return false;
            }

            if (_folderDefinitions.TryGetValue(definition, out string? prevRootDir))
            {
                AssemblyDefinition? prevParentDef = GetDefinitionForPath(rootDirectory);
                if (prevParentDef != null)
                    TransferOwnershipOfAll(definition, prevParentDef);

                _definitionRoots.Remove(prevRootDir);
                _folderDefinitions[definition] = rootDirectory;
            }
            else
            {
                _folderDefinitions.Add(definition, rootDirectory);
            }


            AssemblyDefinition? parentDef = GetDefinitionForPath(rootDirectory);
            if (parentDef != null)
            {
                TransferOwnershipOfScoped(parentDef, definition, rootDirectory);
            }

            TakeOwnershipOfNewFiles(definition, rootDirectory);
            definition.LocalPath = rootDirectory;

            return true;
        }

        public AssemblyDefinition? GetDefinitionForPath(string sourcePath)
        {
            ReadOnlySpan<char> sourcePathAsSpan = sourcePath;

            AssemblyDefinition? definition = null;
            while (!_definitionRootsAlt.TryGetValue(sourcePathAsSpan, out definition))
            {
                int indexOfParent = sourcePathAsSpan.LastIndexOf("/");
                if (indexOfParent == -1)
                {
                    break;
                }

                sourcePathAsSpan = sourcePathAsSpan[..indexOfParent];
            }

            return definition;
        }

        private void TransferOwnershipOfAll(AssemblyDefinition definition, AssemblyDefinition toTakeOwnership)
        {
            foreach (string file in definition.Files)
            {

            }
        }

        private void TransferOwnershipOfScoped(AssemblyDefinition definition, AssemblyDefinition toTakeOwnership, string scope)
        {

        }

        private void TakeOwnershipOfNewFiles(AssemblyDefinition definition, string directory)
        {

        }
    }
}
