using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui
{
    public sealed class UIDragManager
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private object? _activeDrag = null;
        private Dictionary<object, DragInitialData> _pendingDrags;

        internal UIDragManager()
        {
            _pendingDrags = new Dictionary<object, DragInitialData>();

            s_instance.Target = this;
        }

        private Stack<object> _removalQueue = new Stack<object>();
        internal void Update()
        {
            if (_pendingDrags.Count == 0 && _activeDrag == null)
                return;

            Vector2 global = UIEventManager.Instance.GlobalMousePosition;
            if (_activeDrag != null)
            {
                ref DragInitialData initialData = ref CollectionsMarshal.GetValueRefOrNullRef(_pendingDrags, _activeDrag);
                if (!Unsafe.IsNullRef(ref initialData))
                {
                    Vector2 delta = global - initialData.MousePosition;
                    UIDragData dragData = new UIDragData(delta);

                    try
                    {
                        initialData.Callback(dragData);
                    }
                    catch (Exception ex)
                    {
                        //bad

                        _pendingDrags.Remove(_activeDrag);
                        _activeDrag = null;
                    }
                }
                else
                {
                    _pendingDrags.Remove(_activeDrag);
                    _activeDrag = null;
                }
            }
            else
            {
                foreach (var kvp in _pendingDrags)
                {
                    Vector2 dist = Vector2.Abs(kvp.Value.MousePosition - global);
                    if (MathF.Max(dist.X, dist.Y) >= kvp.Value.Threshold)
                    {
                        _activeDrag = kvp.Key;
                        break;
                    }
                }

                if (_activeDrag != null)
                {
                    _removalQueue.Clear();
                    foreach (var kvp in _pendingDrags.Keys)
                    {
                        if (kvp != _activeDrag)
                            _removalQueue.Push(kvp);
                    }

                    while (_removalQueue.TryPop(out object? result))
                        _pendingDrags.Remove(result);
                }
            }
        }

        public static void SetupDrag(object context, Action<UIDragData> callback, float threshold = 5.0f)
        {
            UIDragManager @this = NullableUtility.ThrowIfNull((UIDragManager?)s_instance.Target);
            @this._pendingDrags[context] = new DragInitialData(UIEventManager.Instance.GlobalMousePosition, callback, threshold);
        }

        public static void CancelDrag(object context)
        {
            UIDragManager @this = NullableUtility.ThrowIfNull((UIDragManager?)s_instance.Target);
            @this._pendingDrags.Remove(context);

            if (@this._activeDrag == context)
                @this._activeDrag = null;
        }

        public static object? CurrentDragContext => NullableUtility.ThrowIfNull((UIDragManager?)s_instance.Target)._activeDrag;

        private record struct DragInitialData(Vector2 MousePosition, Action<UIDragData> Callback, float Threshold);
    }

    public record struct UIDragData(Vector2 MouseOffset);
}
