using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;

namespace Editor.Gui.Inspector
{
    internal sealed class ObjectCache
    {
        private Dictionary<Type, IOpaqueObjectTypePool> _pools;

        private List<PropertyElements> _templatePool;
        private int _templatePoolHead;

        internal ObjectCache()
        {
            _pools = new Dictionary<Type, IOpaqueObjectTypePool>();

            _templatePool = new List<PropertyElements>();
            _templatePoolHead = 0;
        }

        internal T GetElement<T>() where T : new()
        {
            Type t = typeof(T);
            if (!_pools.TryGetValue(t, out IOpaqueObjectTypePool? opaquePool))
            {
                Type specified = typeof(ObjectTypePool<>).MakeGenericType(typeof(T));
                opaquePool = new ObjectTypePool<T>();
                _pools.Add(t, opaquePool);
            }

            return ((ObjectTypePool<T>)opaquePool).GetPooledObject();
        }

        internal object GetElement(Type type)
        {
            if (!_pools.TryGetValue(type, out IOpaqueObjectTypePool? opaquePool))
            {
                Type specified = typeof(ObjectTypePool<>).MakeGenericType(type);
                opaquePool = (IOpaqueObjectTypePool)Activator.CreateInstance(specified)!;

                _pools.Add(type, opaquePool);
            }

            return opaquePool.GetOpaquePooledObject();
        }

        internal PropertyElements GetPropertyTemplate()
        {
            if (_templatePoolHead == _templatePool.Count)
            {
                UIElement element = new UIElement();
                UILabel label = new UILabel(element);
                {
                    element.Size = UIValue2.Max;

                    UIFitterLayout fitterLayout = element.AddLayoutModifier<UIFitterLayout>();
                    fitterLayout.Axis = UIFitterAxis.Vertical;
                    fitterLayout.Margin = new UIValue2(4, 4);

                    label.AutoSize = UITextAutoSize.FitBoundsToText;
                }

                PropertyElements elements = new PropertyElements(element, label);
                _templatePool.Add(elements);

                ++_templatePoolHead;
                return elements;
            }

            return _templatePool[_templatePoolHead++];
        }
    }
}
