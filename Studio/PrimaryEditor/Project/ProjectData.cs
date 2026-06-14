using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Core;
using PrimaryEditor.Startup;

namespace PrimaryEditor.Project
{
    public sealed class ProjectData
    {
        private ProjectPaths _paths;

        internal ProjectData()
        {
            _paths = new ProjectPaths();
        }

        internal void SetupData(string projectPath, StartupSplash splash)
        {
            splash.ActionName = "Setting up project data..";
            _paths.LoadPaths(projectPath);
        }

        public static ProjectData Instance => EditorRuntime.Instance.ProjectData;
    }
}
