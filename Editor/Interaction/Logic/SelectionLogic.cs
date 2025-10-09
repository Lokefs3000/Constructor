using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Interaction.Logic
{
    internal interface GenericSelectionLogic
    {
        internal void RunGeneric(SelectedBase @base);
    }

    internal abstract class SelectionLogic<T> : GenericSelectionLogic where T : SelectedBase
    {
        void GenericSelectionLogic.RunGeneric(SelectedBase @base)
        {
            Debug.Assert(@base is T);
            Run(Unsafe.As<T>(@base));
        }

        public abstract void Run(T current);
    }
}
