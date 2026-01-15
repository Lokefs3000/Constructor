using System.Diagnostics;
using System.Runtime.CompilerServices;

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
