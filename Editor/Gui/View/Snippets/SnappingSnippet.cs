using Editor.Interaction;
using Editor.UI;
using Editor.UI.Elements;
using Editor.UI.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.View.Snippets
{
    internal class SnappingSnippet : LayoutSnippet
    {
        private UINumberField? _snapScaleField;

        public SnappingSnippet(string? snippetFile) : base(snippetFile)
        {
        }

        protected override void SetupSelf()
        {
            if (RootElement != null)
            {
                _snapScaleField = RootElement.FindElementWithId<UINumberField>("snap-scale");

                if (_snapScaleField != null)
                {
                    _snapScaleField.Value = ToolManager.SnapScale;
                    _snapScaleField.OnValueChanged += OnSnapScaleValueChanged;

                    ToolManager.OnSnapScaleChanged += OnSnapScaleChanged;
                }
            }
        }

        protected override void CleanupSelf()
        {
            _snapScaleField?.OnValueChanged -= OnSnapScaleValueChanged;

            _snapScaleField = null;

            ToolManager.OnSnapScaleChanged -= OnSnapScaleChanged;
        }

        private void OnSnapScaleChanged(float newScale)
        {
            _snapScaleField?.Value = newScale;
        }

        private void OnSnapScaleValueChanged(double newScale)
        {
            ToolManager.SnapScale = (float)newScale;
        }
    }
}
