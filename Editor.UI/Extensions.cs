using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI
{
    public static class Extensions
    {
        public static T? FindElementWithId<T>(this UIElement element, string id) where T : UIElement
        {
            foreach (UIElement child in element.Children)
            {
                if (child is T && child.Id == id)
                    return Unsafe.As<T>(child);
                else
                {
                    T? ret = child.FindElementWithId<T>(id);
                    if (ret != null)
                        return ret;
                }
            }

            return null;
        }

        public static T? Raycast<T>(this UIElement element, Vector2 position) where T : UIElement
        {
            if (!element.PixelCoordinates.IsWithin(position))
                return null;

            foreach (UIElement child in element.Children)
            {
                if (child.PixelCoordinates.IsWithin(position))
                {
                    T? ret = Recursive(child, position);
                    if (ret != null)
                        return ret;
                }
                
            }

            return element as T;

            static T? Recursive(UIElement element, Vector2 position)
            {
                foreach (UIElement child in element.Children)
                {
                    if (child.PixelCoordinates.IsWithin(position))
                    {
                        T? ret = Recursive(child, position) ?? (child as T);
                        if (ret != null)
                            return ret;
                    }

                }

                return element as T;
            }
        }
    }
}
