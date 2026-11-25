using Editor.UI.Serialization;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI
{
    public sealed class LayoutSnippetManager
    {
        private readonly UIManager _manager;

        private HashSet<LayoutSnippet> _snippets;

        internal LayoutSnippetManager(UIManager manager)
        {
            _manager = manager;

            _snippets = new HashSet<LayoutSnippet>();
        }

        internal T LoadNewSnippet<T>(string snippetFile, object?[]? arguments) where T : LayoutSnippet
        {
            T snippet = (T)Activator.CreateInstance(typeof(T), arguments != null ? [snippetFile, .. arguments] : [snippetFile])!;

            _manager.SerializationManager.DeserializeSnippet(snippet, snippetFile);
            _snippets.Add(snippet);

            snippet.Invoke_SetupSelf();
            return snippet;
        }

        internal void RemoveSnippet(LayoutSnippet snippet, bool destroyElement)
        {
            if (_snippets.Remove(snippet))
            {
                snippet.Invoke_OnCleanup(destroyElement);
                if (destroyElement)
                    snippet.RootElement?.Destroy();
            }
        }

        internal void ReloadAll(string snippetFile)
        {
            foreach (LayoutSnippet snippet in _snippets)
            {
                if (snippet.SnippetFile == snippetFile)
                {
                    snippet.Invoke_OnCleanup(true);
                    snippet.ClearData();

                    DateTime startTime = DateTime.Now;
                    _manager.SerializationManager.DeserializeSnippet(snippet, snippetFile);

                    UIManager.Logger?.Debug("Loading snippet file {f} took {t:f2}s", snippetFile, (DateTime.Now - startTime).TotalSeconds);

                    snippet.Invoke_SetupSelf();
                    snippet.Invoke_OnReloaded();
                }    
            }
        }
    }
}
