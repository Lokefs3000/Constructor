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
            using (new ProfilingScope($"Layout-{root.WindowOwner!.WindowTitle}"))
            {
                RecalculateLayout(root);
                //PerformMeasurePass(root);
                //PerformLayoutPass(root);

                //ClearInternals();

                ThreadHelper.ExecuteOnMainThread(root.WindowOwner!.Invoke_LayoutRecalculated);
            }
        }

        private void RecalculateLayout(UIElement root)
        {
            CollectChildren(root, 0);
            _elements.Sort(ElementData.Comparer.Default);

            for (int i = 0; i < _elements.Count; ++i)
            {
                UIElement element = _elements[i].Element;

                Vector2 localRegion = element.Parent?.CurrentSize ?? root.WindowOwner!.ClientSize.AsVector2();

                UIMeasureContext context = new UIMeasureContext(_layoutManager, localRegion);

                element.MeasureSize(context);
                element.ExecuteMeasureMods(context);
            }

            for (int i = _elements.Count - 1; i >= 0; --i)
            {
                UIElement element = _elements[i].Element;
                Vector2 localRegion = element.Parent?.CurrentSize ?? root.WindowOwner!.ClientSize.AsVector2();

                _measurements.SetupNewElement(element);

                UILayoutContext context = new UILayoutContext(this, _measurements, localRegion);

                element.RecalculateLayout(context);
                element.ExecuteLayoutMods(context);
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

                Boundaries treeBounds = parent.PixelCoordinates;
                foreach (UIElement child in parent.Children)
                {
                    if (Flags.HasFlag(child.StateFlags, UIStateFlags.InvalidLayout))
                    {
                        CalculateBounds(child, baseOffset);
                    }

                    treeBounds = Boundaries.Union(treeBounds, child.PixelCoordinates);
                }

                parent.ElementTreeBounds = treeBounds;

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

        private Vector2 _treeSize;

        private bool _needsRefresh;
        private bool _isOutdated;

        internal UIMeasurements()
        {
            
        }

        internal void SetupNewElement(UIElement element)
        {
            _element = element;

            _treeSize = Vector2.Zero;

            _needsRefresh = true;
            _isOutdated = true;
        }

        internal void Clear()
        {
            _element = null;
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
                treeSize = Vector2.Max(treeSize, child.RelativeOffset + child.CurrentSize);
            }

            _treeSize = treeSize;

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
    }
}
