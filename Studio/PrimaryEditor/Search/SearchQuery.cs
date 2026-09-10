using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Pooling;
using Primary.Utility;

namespace PrimaryEditor.Search
{
    public sealed class SearchQuery
    {
        private readonly List<string> _words;
        private string? _text;

        private readonly HashSet<object> _parameters;

        public SearchQuery()
        {
            _words = new List<string>();
            _text = string.Empty;

            _parameters = new HashSet<object>();
        }

        public void Clear()
        {
            _words.Clear();
            _text = null;

            _parameters.Clear();
        }

        public void ClearWords()
        {
            _words.Clear();
        }

        public bool AddWord(string word) => _words.AddUnique(word);
        public bool RemoveWord(string word) => _words.Remove(word);

        public bool AddParameter(object param) => _parameters.Add(param);
        public bool RemoveParameter(object param) => _parameters.Remove(param);

        public ROList<string> Words => _words;

        public string? Text { get => _text; set => _text = value; }

        public ROHashSet<object> Parameters => _parameters;

        internal record class PoolPolicy() : IObjectPoolPolicy<SearchQuery>
        {
            public SearchQuery Create() => new SearchQuery();
            public bool Return(ref SearchQuery obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
