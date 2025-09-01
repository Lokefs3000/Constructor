using Arch.Core;
using Schedulers;

namespace Primary.Systems
{
    public interface ISystem
    {
        public void Schedule(World world, JobScheduler scheduler);

        public ref readonly QueryDescription Description { get; }
        public bool SystemNeedsFullExecutionTime { get; }
    }
}
