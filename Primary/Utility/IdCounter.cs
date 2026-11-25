using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Utility
{
    public sealed class IdCounter
    {
        private readonly Stack<int> _freeIds;
        private int _idCounter;

        public IdCounter()
        {
            _freeIds = new Stack<int>();
            _idCounter = 0;
        }

        public int Take()
        {
            return _freeIds.TryPop(out int id) ? id : _idCounter++;
        }

        public void Return(int id)
        {
            _freeIds.Push(id);
        }
    }
}
