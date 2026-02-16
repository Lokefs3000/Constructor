using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Primary.Common;
using Primary.Pooling;
using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Layout
{
    internal sealed class LayoutHandler
    {
        private readonly UILayoutManager _layoutManager;

        private Dictionary<UIElement, UIElementMeasurements> _measurements;

        internal LayoutHandler(UILayoutManager layoutManager)
        {
            _layoutManager = layoutManager;

            _measurements = new Dictionary<UIElement, UIElementMeasurements>();
        }

        internal void Handle(UIElement root)
        {
            using (new ProfilingScope($"Layout-{root.WindowOwner!.WindowTitle}"))
            {
                PerformMeasurePass(root);
                PerformLayoutPass(root);

                ClearInternals();
            }
        }

        private void ClearInternals()
        {
            _measurements.Clear();
        }

        private void PerformMeasurePass(UIElement root)
        {
            using (new ProfilingScope("Measure"))
            {
                RecursiveMeasure(root);

                void RecursiveMeasure(UIElement element)
                {
                    element.Transform.ResetCalculatedData();
                    if (element.Parent == null)
                        element.Transform.RealSize = element.WindowOwner!.ClientSize;

                    element.MeasureSize(_layoutManager);

                    Vector2 treeSize = Vector2.Zero;
                    foreach (UIElement child in element.Children)
                    {
                        if (Flags.HasFlag(child.InvalidFlags, UIInvalidationFlags.Layout))
                        {
                            RecursiveMeasure(child);
                        }

                        if (_measurements.TryGetValue(child, out UIElementMeasurements measurements))
                            treeSize = Vector2.Max(treeSize, measurements.TreeSize);
                        else
                            treeSize = Vector2.Max(treeSize, child.Transform.RealSize);
                    }

                    element.ExecuteMeasureModifiers(treeSize);
                    treeSize = Vector2.Max(treeSize, element.Transform.RealSize);

                    _measurements.Add(element, new UIElementMeasurements(element.Transform.RealSize, treeSize));
                }
            }
        }

        private void PerformLayoutPass(UIElement root)
        {
            using (new ProfilingScope("Layout"))
            {
                RecursiveLayout(root);

                void RecursiveLayout(UIElement element)
                {
                    ref UIElementMeasurements measurements = ref CollectionsMarshal.GetValueRefOrNullRef(_measurements, element);
                    Debug.Assert(!Unsafe.IsNullRef(ref measurements));

                    {
                        UIMeasurements passed = new UIMeasurements(element);

                        element.RecalculateLayout(_layoutManager, ref passed);
                        element.ComputeBoundaries();

                        if (passed.HasBeenModified)
                        {
                            foreach (UIElement child in element.Children)
                            {
                                if (Flags.HasFlag(child.InvalidFlags, UIInvalidationFlags.Layout))
                                {
                                    measurements = ref CollectionsMarshal.GetValueRefOrNullRef(_measurements, child);

                                    Vector2 treeSize = child.Transform.RealSize;
                                    foreach (UIElement child2 in child.Children)
                                    {
                                        treeSize = Vector2.Max(treeSize, child2.Transform.RelativePosition + child2.Transform.RealSize);
                                    }

                                    measurements = new UIElementMeasurements(measurements.RealSize, treeSize);
                                }
                            }
                        }
                    }

                    Boundaries boundaries = element.Transform.RenderCoordinates;
                    foreach (UIElement child in element.Children)
                    {
                        if (Flags.HasFlag(child.InvalidFlags, UIInvalidationFlags.Layout))
                        {
                            RecursiveLayout(child);
                        }

                        boundaries = Boundaries.Combine(boundaries, child.Transform.RenderCoordinates);
                    }

                    element.SetTreeBounds(boundaries);
                    if (Flags.HasFlag(element.InvalidFlags, UIInvalidationFlags.Visual))
                        element.InvalidateSelf(UIInvalidationFlags.Visual);
                }
            }
        }

        internal readonly record struct Policy(UILayoutManager LayoutManager) : IObjectPoolPolicy<LayoutHandler>
        {
            public LayoutHandler Create() => new LayoutHandler(LayoutManager);
            public bool Return(ref LayoutHandler obj) => true;
        }
    }

    public readonly record struct UIElementMeasurements(Vector2 RealSize, Vector2 TreeSize);

    public ref struct UIMeasurements
    {
        private UIElement _element;
        private bool _hasBeenModified;

        private Vector2 _treeSize;
        private bool _isOutdated;

        internal UIMeasurements(UIElement element)
        {
            _element = element;
            _hasBeenModified = false;

            _treeSize = new Vector2(-1.0f);
            _isOutdated = true;
        }

        internal void CheckIfTreeIsValid()
        {
            if (_isOutdated)
            {
                Vector2 treeSize = _element.Transform.RealSize;
                foreach (UIElement child in _element.Children)
                {
                    treeSize = Vector2.Max(treeSize, child.Transform.RelativePosition + child.Transform.RealSize);
                }

                _treeSize = treeSize;
                _isOutdated = false;
            }
        }

        public void MarkAsOutdated()
        {
            _hasBeenModified = true;
            _isOutdated = true;
        }

        public Vector2 TreeSize => _treeSize;
        internal bool HasBeenModified => _hasBeenModified;
    }
}
