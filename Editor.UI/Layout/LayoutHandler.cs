using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Pooling;
using Primary.Profiling;
using Primary.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Editor.UI.Layout
{
    public sealed class LayoutHandler
    {
        private readonly UILayoutManager _layoutManager;

        private List<ElementData> _elements;
        private UIMeasurements _measurements;

        internal LayoutHandler(UILayoutManager layoutManager)
        {
            _layoutManager = layoutManager;

            _elements = new List<ElementData>();
            _measurements = new UIMeasurements();
        }

        internal void Handle(UIElement root)
        {
            using (new ProfilingScope($"Layout-{root.WindowOwner!.ToString()}"))
            {
                RecalculateLayout(root);
                //PerformMeasurePass(root);
                //PerformLayoutPass(root);

                //ClearInternals();

                ThreadHelper.ExecuteOnMainThread(root.WindowOwner!.OnLayoutRecalculatedCallback);
            }
        }

        private void RecalculateLayout(UIElement root)
        {
            CollectChildren(root, 0);
            _elements.Sort(ElementData.Comparer.Default);

            for (int i = 0; i < _elements.Count; ++i)
            {
                UIElement element = _elements[i].Element;

                Vector2 localRegion = element.Parent == null ? root.WindowOwner!.ClientSize.AsVector2() : element.Parent.ViewSize;

                UIMeasureContext context = new UIMeasureContext(_layoutManager, localRegion);

                element.ClearPreviousLayoutData();

                element.MeasureSize(context);
                element.ExecuteMeasureMods(context);

                element.ViewSize += element.CurrentSize;
            }

            for (int i = _elements.Count - 1; i >= 0; --i)
            {
                UIElement element = _elements[i].Element;
                Vector2 localRegion = element.Parent == null ? root.WindowOwner!.ClientSize.AsVector2() : element.Parent.ViewSize;

                _measurements.SetupNewElement(element);
                element.ChildExtents = Vector2.Max(element.ChildExtents, _measurements.TreeSize);

                UILayoutContext context = new UILayoutContext(this, _measurements, localRegion);

                element.RecalculateLayout(context);
                _measurements.GetSourceChildExtents();

                element.ExecuteLayoutMods(context);
                element.ChildExtents = _measurements.ChildExtents;

                if (!element.Anchor.Equals(Vector2.Zero))
                    element.RelativeOffset -= element.CurrentSize * element.Anchor;
            }

            CalculateBounds(root, Vector2.Zero);

            _elements.Clear();
            _measurements.Clear();

            void CollectChildren(UIElement parent, int depth)
            {
                _elements.Add(new ElementData(parent, depth));

                ++depth;
                foreach (UIElement child in parent.Children)
                {
                    if (Flags.HasFlag(child.StateFlags, UIStateFlags.InvalidLayout))
                    {
                        CollectChildren(child, depth);
                    }
                }
            }

            void CalculateBounds(UIElement parent, Vector2 offset)
            {
                Vector2 baseOffset = offset + parent.RelativeOffset;
                parent.PixelCoordinates = new Boundaries(baseOffset, baseOffset + parent.CurrentSize);
                parent.ViewCoordinates = Boundaries.Clip(Boundaries.Offset(new Boundaries(parent.PixelCoordinates.Minimum, parent.PixelCoordinates.Maximum + parent.ViewSize), parent.ViewOffset), parent.PixelCoordinates);

                parent.FinalizeLayout();

                Boundaries treeBounds = parent.PixelCoordinates;
                //Vector2 childExtents = parent.ChildExtents;

                foreach (UIElement child in parent.Children)
                {
                    if (Flags.HasFlag(child.StateFlags, UIStateFlags.InvalidLayout))
                    {
                        CalculateBounds(child, baseOffset);
                    }

                    treeBounds = Boundaries.Union(treeBounds, child.ElementTreeBounds);
                    //childExtents = Vector2.Max(childExtents, child.RelativeOffset + child.CurrentSize);
                }

                parent.ElementTreeBounds = treeBounds;
                parent.ScrollPosition = Vector2.Clamp(parent.ScrollPosition, Vector2.Zero, Vector2.Max(parent.ChildExtents - parent.ViewSize, Vector2.Zero));
                //parent.ChildExtents = childExtents;

                parent.RemoveStateFlags(UIStateFlags.InvalidLayout);
            }
        }

        internal readonly record struct Policy(UILayoutManager LayoutManager) : IObjectPoolPolicy<LayoutHandler>
        {
            public LayoutHandler Create() => new LayoutHandler(LayoutManager);
            public bool Return(ref LayoutHandler obj) => true;
        }

        private readonly record struct ElementData(UIElement Element, int Depth)
        {
            public sealed class Comparer : IComparer<ElementData>
            {
                public int Compare(ElementData x, ElementData y) => x.Depth.CompareTo(y.Depth);

                public static readonly Comparer Default = new Comparer();
            }
        }
    }

    public readonly record struct UIElementMeasurements(Vector2 RealSize, Vector2 TreeSize);

    public sealed class UIMeasurements
    {
        private UIElement? _element;

        private Vector2 _sourceChildExtents;

        private Vector2 _treeSize;
        private Vector2 _childExtents;

        private bool _needsRefresh;
        private bool _isOutdated;

        internal UIMeasurements()
        {
            
        }

        internal void SetupNewElement(UIElement element)
        {
            _element = element;

            _sourceChildExtents = Vector2.Zero;

            _treeSize = Vector2.Zero;
            _childExtents = Vector2.Zero;

            _needsRefresh = true;
            _isOutdated = true;
        }

        internal void Clear()
        {
            _element = null;
        }

        internal void GetSourceChildExtents()
        {
            _sourceChildExtents = _element!.ChildExtents;

            _isOutdated = true;
            _needsRefresh = true;
        }

        internal void RefreshIfRequired()
        {
            _needsRefresh = _isOutdated || _needsRefresh;
        }

        private void RecalculateTreeSize()
        {
            Vector2 treeSize = Vector2.Zero;
            foreach (UIElement child in _element!.Children)
            {
                if (child.IsEnabled)
                    treeSize = Vector2.Max(treeSize, child.RelativeOffset + child.CurrentSize);
            }

            _treeSize = treeSize;
            _childExtents = Vector2.Max(_sourceChildExtents, treeSize);

            _needsRefresh = false;
            _isOutdated = false;
        }

        public void MarkAsOutdated()
        {
            _isOutdated = true;
        }

        public Vector2 TreeSize
        {
            get
            {
                if (_needsRefresh)
                    RecalculateTreeSize();
                return _treeSize;
            }
        }

        public Vector2 ChildExtents
        {
            get
            {
                if (_needsRefresh)
                    RecalculateTreeSize();
                return _childExtents;
            }
        }
    }
}
