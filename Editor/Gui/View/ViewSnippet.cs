using Editor.Gui.Windows;
using Editor.UI.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.View
{
    public class ViewSnippet : LayoutSnippet
    {
        protected readonly EditorViewWindow _editorView;

        public ViewSnippet(string? snippetFile, EditorViewWindow window) : base(snippetFile)
        {
            _editorView = window;
        }

        public virtual void Update() { }
    }
}
