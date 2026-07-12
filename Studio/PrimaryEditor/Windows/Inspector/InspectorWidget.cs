using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Text;
using EditorUI.Widgets;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector
{
    internal abstract class InspectorWidget : IDisposable
    {
        protected int _uniqueHash;

        protected bool _disposedValue;

        internal InspectorWidget()
        {
            _uniqueHash = -1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal abstract void AddValueSource(IInspectorValue inspectorValue);

        internal abstract Widget RootWidget { get; }
    }
}
