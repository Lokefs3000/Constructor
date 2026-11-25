using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal interface IJobTemplatePool
    {
        public IJob Take();
        public void Return(IJob value);
    }
}
