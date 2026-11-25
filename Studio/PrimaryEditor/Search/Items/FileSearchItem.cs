using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Search.Providers;

namespace PrimaryEditor.Search.Items
{
    public sealed class FileSearchItem : SearchItem
    {
        private string? _filePath;

        public FileSearchItem()
        {
            _filePath = null;
        }

        internal void SetupItem(string filePath)
        {
           // _sourceType = typeof(FileSearchProvider);
            _filePath = filePath;
        }

        protected internal override void ClearForPoolReturn()
        {
            base.ClearForPoolReturn();
            _filePath = null;
        }

        public string FilePath => _filePath!;
    }
}
