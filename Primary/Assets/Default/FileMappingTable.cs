using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Primary.Assets.Default
{
    public sealed class FileMappingTable
    {
        private readonly FrozenDictionary<string, string> _fileMappings;
        private readonly FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _fileMappingsSpan;
    
        public FileMappingTable(string projectDirectory)
        {
            string importedFolder = Path.Combine(projectDirectory, "Library/Imported/");

            JsonNode mappingsRootNode = JsonSerializer.Deserialize<JsonNode>(File.ReadAllText(Path.Combine(projectDirectory, "Library/Saved/Remappings.json")))!;

            Dictionary<string, string> mappings = new Dictionary<string, string>();
            foreach (JsonObject mapping in ((JsonArray)mappingsRootNode["remappings"]!)!)
            {
                string source = mapping["source"]!.GetValue<string>();
                string remap = mapping["remap"]!.GetValue<string>();

                mappings.Add(source, Path.Combine(projectDirectory, remap));
            }

            _fileMappings = mappings.ToFrozenDictionary();
            _fileMappingsSpan = _fileMappings.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool TryGetFileMapping(ReadOnlySpan<char> file, [NotNullWhen(true)] out string? mapping)
        {
            return _fileMappingsSpan.TryGetValue(file, out mapping);
        }
    }
}
