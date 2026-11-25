using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Assets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AssetImporterAttribute : Attribute
    {
        private readonly string[] _filetypes;

        public AssetImporterAttribute(params string[] filetypes)
        {
            _filetypes = filetypes;
        }

        public string[] Filetypes => _filetypes;
    }
}
