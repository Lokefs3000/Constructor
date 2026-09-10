using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using PrimaryEditor.Core;
using PrimaryEditor.Startup;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Project
{
    public sealed class ProjectData
    {
        private readonly ProjectPaths _paths;

        public ProjectData()
        {
            s_instance.Target = this;

            _paths = new ProjectPaths();
        }

        public void SetupData(string projectPath, StartupSplash? splash)
        {
            splash?.ActionName = "Setting up project data..";

            projectPath = projectPath.Replace('\\', '/');

            _paths.SetupPaths(projectPath);
        }

        public ProjectPaths Paths => _paths;

        private static readonly WeakReference s_instance = new WeakReference(null);
        public static ProjectData? Instance => Unsafe.As<ProjectData>(s_instance.Target);
    }
}
