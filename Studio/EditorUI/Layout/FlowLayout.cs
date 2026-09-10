using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Widgets;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Layout
{
    internal readonly record struct FlowLayout
    {
        public readonly void Layout(ref WidgetTreeData treeData, List<WidgetTreeData> widgetTree, LayoutManager manager)
        {
            if (!treeData.HasLayoutData)
                manager.QueryLayoutDataFor(ref treeData);

            Widget widget = treeData.Widget;

            ItemAlignment itemAlignment = widget.AlignItems;

            using RentedList<WidgetMetricsForLayout> lineIndices = new RentedList<WidgetMetricsForLayout>();
            using RentedArray<Rect> newLayouts = RentedArray<Rect>.Rent(widget.Children.Count);

            Int2 currentLineOffset = Int2.Zero;
            int tallestWidgetInLine = 0;
            int itemsWithoutWidth = 0;

            int actualLineWidth = treeData.Layout.InteriorSize.X;
            if (actualLineWidth <= 0)
            {
                actualLineWidth = manager.TryFindActualInteriorWidth(ref treeData);
            }

            int childIndex = 0;

            Span<WidgetTreeData> treeRecursion = widgetTree.AsSpan();
            for (int i = treeData.TreeRange.Start; i < treeData.TreeRange.End; ++i)
            {
                ref WidgetTreeData childTreeData = ref treeRecursion[i];
                if (!childTreeData.HasLayoutData)
                    manager.QueryLayoutDataFor(ref childTreeData);

                if (childTreeData.Widget.Position == PositionMode.Relative)
                {
                    bool hasBlockLayout = childTreeData.Widget.DisplayOutside == DisplayOutside.Block;

                    int nextLineOffsetX = currentLineOffset.X + childTreeData.Layout.BoxSize.X;
                    if (currentLineOffset.X > 0 && (hasBlockLayout || nextLineOffsetX > actualLineWidth))
                    {
                        lineIndices.Add(new WidgetMetricsForLayout(i, currentLineOffset.X, tallestWidgetInLine, itemsWithoutWidth));

                        currentLineOffset = new Int2(0, currentLineOffset.Y + tallestWidgetInLine);
                        tallestWidgetInLine = 0;
                        itemsWithoutWidth = 0;

                        nextLineOffsetX = childTreeData.Layout.BoxSize.X;
                    }

                    WidgetLayout sourceLayout = childTreeData.Layout;
                    if (sourceLayout.HasWidth)
                        ++itemsWithoutWidth;

                    ref Rect childLayout = ref newLayouts[childIndex++];

                    childLayout = new Rect(currentLineOffset, sourceLayout.BoxSize);
                    tallestWidgetInLine = Math.Max(tallestWidgetInLine, childTreeData.Layout.BoxSize.Y);

                    // Force a line break by assigning (hopefully) impossible values
                    if (hasBlockLayout)
                        currentLineOffset.X = int.MaxValue;
                }

                // Skip any potential children as we aren't trying to lay them out
                i = childTreeData.TreeRange.End;
            }

            lineIndices.Add(new WidgetMetricsForLayout(treeData.TreeRange.End, currentLineOffset.X, tallestWidgetInLine, itemsWithoutWidth));

            if (itemAlignment != ItemAlignment.Start)
            {
                childIndex = 0;

                int lastLineStart = treeData.TreeRange.Start;
                for (int lineI = 0; lineI < lineIndices.Count; ++lineI)
                {
                    WidgetMetricsForLayout lineMetrics = lineIndices[lineI];

                    int boxOffsetInLine = 0;
                    int boxWidthInLine = 0;

                    switch (itemAlignment)
                    {
                        case ItemAlignment.Center: boxOffsetInLine = (actualLineWidth - lineMetrics.LineWidth) / 2; break;
                        case ItemAlignment.End: boxOffsetInLine = actualLineWidth - lineMetrics.LineWidth; break;
                        case ItemAlignment.Stretch: boxWidthInLine = (actualLineWidth - lineMetrics.LineWidth) / lineMetrics.ItemsWithoutWidth; break;
                    }

                    for (int i = lastLineStart; i < lineMetrics.LineEndIndex; ++i)
                    {
                        ref WidgetTreeData childTreeData = ref treeRecursion[i];
                        ref Rect childLayout = ref newLayouts[childIndex++];

                        switch (itemAlignment)
                        {
                            case ItemAlignment.Center:
                            case ItemAlignment.End: childLayout.X += boxOffsetInLine; break;
                            case ItemAlignment.Baseline: childLayout.Y += lineMetrics.LineHeight - childLayout.Size.Y; break;
                            case ItemAlignment.Stretch:
                                {
                                    childLayout.X = boxOffsetInLine;
                                    if (!childTreeData.Layout.HasWidth)
                                        childLayout.Width = boxWidthInLine;

                                    boxOffsetInLine += childLayout.Size.X;
                                    break;
                                }
                        }

                        if (!childTreeData.Layout.HasBoxChanged(childLayout))
                        {
                            childTreeData.Layout = new WidgetLayout(childTreeData.Layout) { Box = childLayout };
                            childTreeData.Widget.AddStateFlags(StateFlags.SelfInvalidLayout);
                        }

                        i = childTreeData.TreeRange.End;
                    }

                    lastLineStart = lineMetrics.LineEndIndex;
                }
            }
        }
    }
}
