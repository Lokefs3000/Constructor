using Tomlyn;
using Tomlyn.Model;

namespace Editor.Project
{
    public sealed class ProjectConfiguration
    {
        public string AuthorName;
        public string ProjectName;
        public string Version;

        internal ProjectConfiguration(string projectFile)
        {
            TomlTable table = Toml.ToModel<TomlTable>(File.ReadAllText(projectFile));

            {
                TomlTable project = (TomlTable)table["general"];

                AuthorName = (string)project["author_name"];
                ProjectName = (string)project["project_name"];
                Version = (string)project["version"];
            }
        }
    }
}
