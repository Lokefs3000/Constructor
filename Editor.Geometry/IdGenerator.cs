using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.Geometry
{
    internal sealed class IdGenerator
    {
        private HashSet<int> _ids;

        internal IdGenerator()
        {
            _ids = new HashSet<int>();
        }

        /// <summary>Not thread-safe</summary>
        internal int GetId()
        {
            while (true)
            {
                int id = (int)Stopwatch.GetTimestamp();
                if (_ids.Add(id))
                    return id;
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void ReturnId(int id)
        {
            _ids.Remove(id);
        }

        /// <summary>Not thread-safe</summary>
        internal void AddId(int id)
        {
            Guard.IsTrue(_ids.Add(id));
        }
    }
}
