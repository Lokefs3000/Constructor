using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Search
{
    public abstract class SearchItem
    {
        protected Type? _sourceType;

        public SearchItem()
        {
            _sourceType = null;
        }

        protected internal virtual void ClearForPoolReturn()
        {
            _sourceType = null;
        }

        public Type SourceType => _sourceType!;
    }
}
