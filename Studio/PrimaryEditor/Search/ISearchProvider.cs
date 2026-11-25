using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections;

namespace PrimaryEditor.Search
{
    public interface ISearchProvider
    {
        public void Query(SearchQuery query, SearchList list);
    }
}
