using Editor.Gui.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui
{
    public class EditorWindow
    {
        private string _windowTitle;
        private DockableElement _rootElement;

        public EditorWindow()
        {
            _windowTitle = GetType().Name;
            _rootElement = new DockableElement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNewDockingContainer(DockingContainer? container)
        {
            _rootElement.Container = container;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvalidateLayout()
        {
            _rootElement.InvalidateLayout();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNewLayoutSpace(Vector2 size)
        {
            _rootElement.Size = size;
        }

        public string Title { get => _windowTitle; protected set => _windowTitle = value; }

        public Element RootElement { get => _rootElement; }
    }
}
