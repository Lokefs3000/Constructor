using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Text;
using Primary.Common;
using Primary.Input;
using Primary.Rendering.Debuggable;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Debugging
{
    public static class UIDebugRenderer
    {
        private static Queue<UIElement> s_elementQueue = new Queue<UIElement>();

        public static void Draw(IDebugRenderer renderer, UIDockHost host)
        {
            UIWindow? activeWindow = host.ActiveWindow;
            if (activeWindow != null)
            {
                renderer.PushMatrix(
                    Matrix4x4.CreateTranslation(new Vector3(-host.ClientSize * 0.5f, 0.0f)) *
                    Matrix4x4.CreateScale(1.0f, -1.0f, 1.0f) *
                    Matrix4x4.CreateOrthographic(host.ClientSize.X, host.ClientSize.Y, -1.0f, 1.0f), false);

                s_elementQueue.Clear();
                s_elementQueue.Enqueue(activeWindow.RootElement);

                while (s_elementQueue.TryDequeue(out UIElement? element))
                {
                    UITransform transform = element.Transform;

                    renderer.DrawWireRect(transform.RenderCoordinates.Minimum, transform.RenderCoordinates.Maximum, transform.RenderCoordinates.IsWithin(InputSystem.Pointer.MousePosition) ? Color.Red : Color.Yellow);
                    //renderer.DrawSolidRect(transform.RenderCoordinates.Minimum, transform.RenderCoordinates.Maximum, new Color(transform.RenderCoordinates.IsWithin(InputSystem.Pointer.MousePosition) ? Color.Red : Color.Yellow) { A = 0.1f });

                    if (FlagUtility.HasFlag(element.InvalidFlags, UIInvalidationFlags.Visual))
                    {
                        renderer.DrawWireBoundaries(element.InvalidVisualRegion, Color.Green);
                    }

                    if (element is UISplitPanel splitPanel && splitPanel.SplitOwner != null)
                    {
                        DrawArrow(renderer, splitPanel.SplitOwner.Transform.RenderCoordinates.Center, splitPanel.Transform.RenderCoordinates.Center, Color.Blue);
                    }
                    else if (element is UILabel label && label.FontStyle != null)
                    {
                        float lineHeight = label.LineHeight.GetValueOrDefault(label.FontStyle.LineHeight) * label.Size;
                        foreach (UITextShapingLine line in label.ShapingData.Lines)
                        {
                            Vector2 min = transform.RenderCoordinates.Minimum + new Vector2(0.0f, line.LineIndex * lineHeight);
                            renderer.DrawWireRect(min, min + line.LineSize, Color.Blue);
                        }
                    }

                    foreach (UIElement child in element.Children)
                    {
                        s_elementQueue.Enqueue(child);
                    }
                }

                renderer.PopMatrix();
            }
        }

        private static void DrawArrow(IDebugRenderer renderer, Vector2 from, Vector2 to, Color color)
        {
            Vector2 d = Vector2.Normalize(to - from);
            float angle = MathF.Atan2(d.Y, d.X);

            Vector2 forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 30.0f;
            Vector2 side = new Vector2(-d.Y, d.X) * 30.0f;

            renderer.DrawLine(from.AsVector3(), to.AsVector3(), color);
            renderer.DrawLine(to.AsVector3(), (to + side - forward).AsVector3(), color);
            renderer.DrawLine(to.AsVector3(), (to - side - forward).AsVector3(), color);
        }
    }
}
