using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI
{
    public interface IObjectTracker
    {
        public void ObjectCreated(Resource resource);
        public void ObjectDestroyed(Resource resource);

        public void ObjectRenamed(Resource resource, string newName);
    }
}
