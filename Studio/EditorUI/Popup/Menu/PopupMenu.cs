using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using Primary.Windowing;

namespace EditorUI.Popup.Menu
{
    public abstract class PopupMenu : PopupHost
    {
        internal abstract void BeginItemHover(PopupMenuItem item);
        internal abstract void EndItemHover(PopupMenuItem item);
        internal abstract void HandleItemPress(PopupMenuItem item);

        internal abstract void Lock();
        internal abstract void Unlock();

        protected internal void ThrowIfLocked()
        {
            if (IsMenuLocked)
                throw new InvalidOperationException("No operations can be performed on this menu because it is currently locked");
        }

        public abstract bool IsMenuLocked { get; }

        #region Serializable
        public abstract IAssetProvider<FontFamily>? FontFamily { get; set; }
        public abstract FontStyle FontStyle { get; set; }
        public abstract FontWeight FontWeight { get; set; }

        public abstract float FontSize { get; set; }

        public abstract Vector4 ItemPadding { get; set; }
        public abstract float MaxWidth { get; set; }
        #endregion
    }
}
