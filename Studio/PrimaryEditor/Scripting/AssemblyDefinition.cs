using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Scripting.Physical;

namespace PrimaryEditor.Scripting
{
    public sealed class AssemblyDefinition
    {
        private string _localPath;

        private string _assemblyName;
        private readonly List<string> _directories;
        private readonly List<string> _symbols;
        private readonly List<AssemblyReference> _references;

        private readonly HashSet<string> _files;

        private bool _needsRecompile;

        internal AssemblyDefinition(string localPath, string assemblyName)
        {
            _localPath = localPath;

            _assemblyName = assemblyName;
            _directories = new List<string>();
            _symbols = new List<string>();
            _references = new List<AssemblyReference>();

            _files = new HashSet<string>();

            _needsRecompile = false;
        }

        public void Recompile()
        {
            _needsRecompile = true;
        }

        public string LocalPath { get => _localPath; internal set => _localPath = value; }

        public string AssemblyName => _assemblyName;
        public ROList<string> Directories => _directories;
        public ROList<string> Symbols => _symbols;
        public ROList<AssemblyReference> References => _references;

        public ROHashSet<string> Files => _files;

        public bool NeedsRecompile => _needsRecompile;
    }
}
