using Editor.UI;
using Editor.UI.Elements;
using Editor.UI.Reflection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Editor.UI.Serialization.Custom
{
    internal class SplitPanelRoutine : ISerializationRoutine
    {
        public UIElement? Deserialize(DeserializeContext context, UIElement parentElement, XmlElement xmlElement)
        {
            UISplitPanel? splitPanel = null;
            if (parentElement is not UISplitContainer splitContainer)
            {
                if (parentElement is not UISplitPanel)
                {
                    UIManager.Logger?.Error("[{p}]: Split panel must be parented to a split container or another split panel", xmlElement.Name);
                    return null;
                }

                splitPanel = Unsafe.As<UISplitPanel>(parentElement);

                UISplitPanel? tempPanel = splitPanel;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                while ((splitContainer = tempPanel.Parent as UISplitContainer) == null)
                {
                    tempPanel = (UISplitPanel)tempPanel.Parent;
                }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            }

            if (!context.TryGetElementData(typeof(UISplitPanel), out CachedElementData elementData))
                return null;

            string? splitDirection = xmlElement.GetAttributeNode("Direction")?.Value;
            if (splitDirection == null)
            {
                UIManager.Logger?.Error("[{p}]: Direction must be defined to create a split panel", xmlElement.Name);
                return null;
            }

            if (!Enum.TryParse(splitDirection, out UISplitDirection direction))
            {
                UIManager.Logger?.Error("[{p}]: Failed to parse split direction: {v}", xmlElement.Name, splitDirection);
                return null;
            }

            UISplitPanel panel = splitContainer.AddSplit(splitPanel, direction);

            foreach (XmlAttribute attrib in xmlElement.Attributes)
            {
                if (attrib.Name != "Direction")
                {
                    if (!context.DeserializeProperty(attrib, elementData, panel))
                        return null;
                }
            }

            return panel;
        }
    }
}
