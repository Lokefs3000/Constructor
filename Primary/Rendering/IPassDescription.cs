using Primary.Rendering.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    internal interface IPassDescription
    {
        /// <summary>Not thread-safe</summary>
        internal void ExecuteInternal(RenderPassData passData);
    }

    public enum RenderPassThreadingPolicy : byte
    {
        /// <summary>
        /// Runs on main thread
        /// </summary>
        None = 0,

        /// <summary>
        /// Runs parallel on the thread pool
        /// </summary>
        Parallel,

        /// <summary>
        /// Runs multiple times parallel on the thread pool
        /// <para>
        /// See also: <seealso cref="RasterPassDescription.SetThreadingSplitCount(int)"/>
        /// </para>
        /// </summary>
        SplitParallel
    }
}
