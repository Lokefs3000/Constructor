using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.IO
{
    public sealed class EditorClipboard
    {
        private object? _data;

        internal EditorClipboard()
        {
            _data = null;
        }

        public static void Set(object data)
        {
            EditorClipboard clipboard = EditorRuntime.GlobalSingleton.EditorClipboard;
            clipboard._data = data;
        }

        public static T? Get<T>() where T : class
        {
            EditorClipboard clipboard = EditorRuntime.GlobalSingleton.EditorClipboard;
            return clipboard._data as T;
        }

        public static bool Is<T>() where T : class
        {
            EditorClipboard clipboard = EditorRuntime.GlobalSingleton.EditorClipboard;
            return clipboard._data is T and not null;
        }
    }
}
