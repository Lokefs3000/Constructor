using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class StyleManager
    {
        private ClassListCache _classListCache;

        internal StyleManager(UIManager uiManager)
        {
            _classListCache = new ClassListCache(uiManager);
        }

        internal void RefreshStyles()
        {
            using (new ProfilingScope("RefreshStyles"))
            {
                foreach (var kvp in UIManager.Instance.WindowManager.Active)
                {
                    if (kvp.Value.StyleUpdater.HasInvalidStyleBases)
                    {
                        kvp.Value.StyleUpdater.UpdateAll(kvp.Value.WindowTitle.Length == 0 ? null : kvp.Value.WindowTitle);
                    }
                }
            }
        }

        public ClassListCache ClassListCache => _classListCache;
    }
}
