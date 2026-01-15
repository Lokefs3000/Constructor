using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Layout
{
    public interface IUILayoutModifier
    {
        public IUILayoutModiferTime Timing { get; }

        public void ModifyElement(IUILayoutModiferTime time);
    }

    public enum IUILayoutModiferTime : byte
    {
        Acending = 1 << 0,
        Descending = 1 << 1,
    }
}
