using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPass
    {
        private RenderPassErrorReporter _errorReporter;
        private RenderPassBlackboard _blackboard;

        private Dictionary<Type, IPassData> _passDataPool;

        internal RenderPass()
        {
            _errorReporter = new RenderPassErrorReporter();
            _blackboard = new RenderPassBlackboard();

            _passDataPool = new Dictionary<Type, IPassData>();
        }

        internal void ClearInternals()
        {

        }

        public RasterPassDescription SetupRasterPass<T>(string name, out T data) where T : class, IPassData, new()
        {
            if (!_passDataPool.TryGetValue(typeof(T), out IPassData? passData))
            {
                passData = new T();
                _passDataPool[typeof(T)] = passData;
            }

            data = Unsafe.As<T>(passData);
            data.Clear();

            RasterPassDescription desc = new RasterPassDescription(this, name);
            return desc;
        }

        internal void ReportError(RPErrorSource source, RPErrorType type) => _errorReporter.ReportError(source, type);

        public RenderPassBlackboard Blackboard => _blackboard;
    }
}
