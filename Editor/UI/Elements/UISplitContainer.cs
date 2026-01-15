using Editor.UI.Datatypes;
using Primary.Common;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.WinRT;

namespace Editor.UI.Elements
{
    public class UISplitContainer : UIElement
    {
        private List<UISplitPanel> _splitPanels;

        public UISplitContainer()
        {
            _splitPanels = new List<UISplitPanel>();
        }

        public UISplitPanel AddSplit(UISplitPanel? panelToSplit, UISplitDirection direction)
        {
            if (panelToSplit == null)
            {
                if (Children.Count > 0 && Unsafe.As<UISplitPanel>(Children[0]).Direction != direction)
                    throw new Exception("Invalid split direction");
            }
            else
            {
                if (panelToSplit.OwnedSplits.Count > 0 && Unsafe.As<UISplitPanel>(panelToSplit.OwnedSplits[0]).Direction != direction)
                    throw new Exception("Invalid split direction");
            }

            UISplitPanel panel = new UISplitPanel(direction) { Parent = this };
            _splitPanels.Add(panel);

            if (panelToSplit != null)
                panel.SetSplitOwner(panelToSplit);

            EnsureMinimumSizes(panelToSplit);
            return panel;
        }

        private void EnsureMinimumSizes(UISplitPanel? panel)
        {
            IReadOnlyList<UIElement> panels = panel?.OwnedSplits ?? Children;
            int count = panels.Count((x) => Unsafe.As<UISplitPanel>(x).SplitOwner == panel);

            Vector2 size = (panel?.GetParentRenderBoundaries() ?? GetParentRenderBoundaries()).Size;
            if (size.X == 0.0f || size.Y == 0.0f)
                return;

            size = Vector2.Max(size, new Vector2(MaxPanelGraceArea * count + 1));

            float minGrace = MaxPanelGraceArea / size.X;
            float maxPosition = (size.X - MaxPanelGraceArea * count) / size.X;

            //Ensure minimum
            for (int i = panels.Count - 1; i >= 0; --i)
            {
                UISplitPanel child = Unsafe.As<UISplitPanel>(panels[i]);
                if (child.SplitOwner == panel)
                {
                    bool hasMoved = false;
                    float minLocalPosition = TryFindPrevious(i, out UISplitPanel? found) ? found.Position + minGrace : 0.0f;

                    if (child.Position < minLocalPosition)
                    {
                        child.Position = minLocalPosition;
                        hasMoved = true;
                    }

                    if (TryFindNext(i, out UISplitPanel? next))
                    {
                        float grace = next.Position - minGrace;

                        if (child.Position >= grace)
                        {
                            if (hasMoved)
                                next.Position += minGrace;

                            child.Position = grace;
                        }
                    }
                }
            }

            //Ensure maximum
            for (int i = 0; i < panels.Count - 1; ++i)
            {
                UISplitPanel child = Unsafe.As<UISplitPanel>(panels[i]);
                if (child.SplitOwner == panel)
                {
                    bool hasMoved = false;
                    float maxLocalPosition = TryFindNext(i, out UISplitPanel? found) ? found.Position + minGrace : 0.0f;

                    if (child.Position > maxLocalPosition)
                    {
                        child.Position = maxLocalPosition;
                        hasMoved = true;
                    }

                    if (TryFindPrevious(i, out UISplitPanel? prev))
                    {
                        float grace = prev.Position - minGrace;

                        if (child.Position < grace)
                        {
                            if (hasMoved)
                                prev.Position -= minGrace;

                            child.Position = grace;
                        }
                    }
                }
            }

            bool TryFindPrevious(int i, [NotNullWhen(true)] out UISplitPanel? found)
            {
                --i;
                for (; i > 0; --i)
                {
                    UISplitPanel check = Unsafe.As<UISplitPanel>(panels[i]);
                    if (check.SplitOwner == panel)
                    {
                        found = check;
                        return true;
                    }    
                }

                found = null;
                return false;
            }

            bool TryFindNext(int i, [NotNullWhen(true)] out UISplitPanel? found)
            {
                ++i;
                for (; i < panels.Count; ++i)
                {
                    UISplitPanel check = Unsafe.As<UISplitPanel>(panels[i]);
                    if (check.SplitOwner == panel)
                    {
                        found = check;
                        return true;
                    }
                }

                found = null;
                return false;
            }
        }

        public void AutoBalanceSplits(UISplitPanel? panel, bool balanceSubSplits = false)
        {
            IReadOnlyList<UIElement> panels = panel?.OwnedSplits ?? Children;
            int count = panels.Count((x) => Unsafe.As<UISplitPanel>(x).SplitOwner == panel);

            for (int i = 0, j = 0; i < panels.Count; i++)
            {
                UISplitPanel child = Unsafe.As<UISplitPanel>(panels[i]);
                if (child.SplitOwner == panel)
                {
                    child.Position = (j++ + 1) / (float)count;

                    if (child.OwnedSplits.Count > 0 && balanceSubSplits)
                    {
                        AutoBalanceSplits(child, balanceSubSplits);
                    }
                }
            }
        }

        public override UIRecalcLayoutStatus RecalculateLayout(UILayoutManager manager, UIRecalcType type)
        {
            base.RecalculateLayout(manager, type);

            if (type == UIRecalcType.Descending && Children.Count > 0)
            {
                Boundaries parentBounds = GetParentRenderBoundaries();
                Vector2 parentSize = parentBounds.Size;

                EnsureMinimumSizes(null);

                UISplitPanel first = Unsafe.As<UISplitPanel>(Children.First());
                IteratePanels(first.Direction, Children, null, parentBounds.Minimum, parentSize);
            }

            return UIRecalcLayoutStatus.Finished;
        }

        private void IteratePanels(UISplitDirection direction, IReadOnlyList<UIElement> panels, UISplitPanel? splitOwner, Vector2 parentOffset, Vector2 parentSize)
        {
            int panelCount = panels.Count((x) => x is UISplitPanel panel && panel.Direction == direction && panel.SplitOwner == splitOwner);
            int currentPosition = 0;

            switch (direction)
            {
                case UISplitDirection.Horizontal:
                    {
                        for (int i = 0; i < panels.Count; ++i)
                        {
                            UISplitPanel panel = Unsafe.As<UISplitPanel>(panels[i]);
                            if (panel.Direction != direction || (panel.SplitOwner != splitOwner && panel != splitOwner))
                                continue;

                            UISplitPanel? nextPanel = null;
                            for (int j = i + 1; j < panels.Count; ++j)
                            {
                                UISplitPanel check = Unsafe.As<UISplitPanel>(panels[j]);
                                if (check.Direction == direction && check.SplitOwner == splitOwner)
                                {
                                    nextPanel = check;
                                    break;
                                }
                            }

                            UITransform transform = panel.Transform;

                            int offset = currentPosition;
                            int height = nextPanel == null ? (int)(parentSize.Y - offset) : (int)(panel.Position * parentSize.Y - currentPosition);

                            currentPosition = (int)(panel.Position * parentSize.Y);

                            transform.Position = new UIValue2((int)parentOffset.X, (int)(offset + parentOffset.Y));
                            transform.Size = new UIValue2((int)parentSize.X, height);

                            if (panel.OwnedSplits.Count > 0)
                            {
                                UISplitPanel first = panel.OwnedSplits.First();
                                EnsureMinimumSizes(panel);

                                IteratePanels(first.Direction, panel.OwnedSplits, panel, new Vector2((int)parentOffset.X, (int)(offset + parentOffset.Y)), new Vector2(parentSize.X, height));
                            }
                        }

                        break;
                    }
                case UISplitDirection.Vertical:
                    {
                        for (int i = 0; i < panels.Count; ++i)
                        {
                            UISplitPanel panel = Unsafe.As<UISplitPanel>(panels[i]);
                            if (panel.Direction != direction || panel.SplitOwner != splitOwner)
                                continue;

                            UISplitPanel? nextPanel = null;
                            for (int j = i + 1; j < panels.Count; ++j)
                            {
                                UISplitPanel check = Unsafe.As<UISplitPanel>(panels[j]);
                                if (check.Direction == direction && check.SplitOwner == splitOwner)
                                {
                                    nextPanel = check;
                                    break;
                                }
                            }

                            UITransform transform = panel.Transform;

                            int offset = currentPosition;
                            int width = nextPanel == null ? (int)(parentSize.X - offset) : (int)(panel.Position * parentSize.X - currentPosition);

                            currentPosition = (int)(panel.Position * parentSize.X);

                            transform.Position = new UIValue2((int)(offset + parentOffset.X), (int)parentOffset.Y);
                            transform.Size = new UIValue2(width, (int)parentSize.Y);

                            if (panel.OwnedSplits.Count > 0)
                            {
                                UISplitPanel first = panel.OwnedSplits.First();
                                EnsureMinimumSizes(panel);

                                IteratePanels(first.Direction, panel.OwnedSplits, panel, new Vector2((int)(offset + parentOffset.X), (int)parentOffset.Y), new Vector2(width, parentSize.Y));
                            }
                        }

                        break;
                    }
            }
        }

        public const int MaxPanelGraceArea = 14;
    }
}
