using CommunityToolkit.Diagnostics;
using Editor.UI.Reflection;
using Primary.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class ClassListCache
    {
        private UIManager _uiManager;

        private ConcurrentDictionary<Type, string[]> _classLists;

        internal ClassListCache(UIManager uiManager)
        {
            _uiManager = uiManager;

            _classLists = new ConcurrentDictionary<Type, string[]>();
        }

        public string[] GetClassList(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)));

            if (_classLists.TryGetValue(type, out string[]? value))
                return value;

            using RentedArray<string> temp = RentedArray<string>.Rent(32, true);
            int count = 0;

            Type? currentType = type;
            do
            {
                Guard.IsLessThan(count, 32);
                if (_uiManager.ReflectionManager.ElementCache.TryGetElementData(currentType, out CachedElementData elementData))
                    temp[count++] = elementData.PrettyName;
                else
                    UIManager.Logger?.Warning("Failed to get element data for: {t}", currentType);
            } while ((currentType = currentType.BaseType) != typeof(StyleBase) && currentType != null);

            temp.Span.Slice(0, count).Reverse();
            string[] arr = temp.ToArray(0, count);

            _classLists.TryAdd(type, arr);
            return arr;
        }
    }
}
