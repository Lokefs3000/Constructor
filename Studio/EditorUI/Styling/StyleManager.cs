using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using EditorUI.Reflection.Cache;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Utility;
using Primary.Windowing;

namespace EditorUI.Styling
{
    public sealed class StyleManager
    {
        private UIManager _manager;

        private List<StylesheetProvider> _providers;

        private List<StylesheetClass> _classList;
        private Queue<StyleObjectSearch> _styleQueue;
        private Queue<int> _childCountQueue;
        private HashSet<Type> _typeSet;

        private HashSet<object> _pendingStyleUpdates;

        private Dictionary<Type, string[]> _styleClassList;

        internal StyleManager(UIManager manager)
        {
            _manager = manager;

            _providers = new List<StylesheetProvider>();

            _classList = new List<StylesheetClass>();
            _styleQueue = new Queue<StyleObjectSearch>();
            _childCountQueue = new Queue<int>();
            _typeSet = new HashSet<Type>();

            _pendingStyleUpdates = new HashSet<object>();

            _styleClassList = new Dictionary<Type, string[]>();
        }

        public void ReplaceStylesheetFromName(string sourceName, Stylesheet newStylesheet)
        {
            foreach (StylesheetProvider provider in _providers)
            {
                provider.TryReplaceStylesheet(sourceName, newStylesheet);
            }
        }

        internal void RegisterProvider(StylesheetProvider provider)
        {
            _providers.AddUnique(provider);
        }

        internal void UnregisterProvider(StylesheetProvider provider)
        {
            _providers.Remove(provider);
        }

        internal void UpdateStylesForPending()
        {
            if (_pendingStyleUpdates.Count > 0)
            {
                foreach (object obj in _pendingStyleUpdates)
                {
                    if (obj is WidgetWindow widgetWindow)
                    {
                        UpdateStyleOn(widgetWindow.StylesheetProvider, widgetWindow.RootWidget);
                    }
                    else if (obj is ISingleStyledObject styledObject && styledObject.StyledObject != null)
                    {
                        StylesheetProvider provider = styledObject.StylesheetProvider;
                        UpdateStyleOn(provider, styledObject.StyledObject);
                    }
                }

                _pendingStyleUpdates.Clear();
            }
        }

        internal void UpdateWindowStyling(WidgetWindow window)
        {
            _pendingStyleUpdates.Add(window);
        }

        internal void UpdateSingleStyling(ISingleStyledObject singleStyledObject)
        {
            _pendingStyleUpdates.Add(singleStyledObject);
        }

        private void UpdateStyleOn(StylesheetProvider provider, StyledObject root)
        {
            _classList.Clear();
            _styleQueue.Clear();
            _childCountQueue.Clear();
            _typeSet.Clear();

            _styleQueue.Enqueue(new StyleObjectSearch(root, 0));
            _childCountQueue.Enqueue(1);

            int childCount = -1;

            bool forceStyleRedo = provider.HasChangedStylesheets;

            StyledObject? currentParent = null;

            while (_styleQueue.TryDequeue(out StyleObjectSearch result))
            {
                StyledObject styledObject = result.StyledObject;
                if (styledObject.ParentObject != currentParent)
                {
                    currentParent = styledObject.ParentObject;
                    childCount = _childCountQueue.Dequeue();

                    _typeSet.Clear();
                }

                bool descendFurther = false;
                if (forceStyleRedo || styledObject.StateFlags.HasFlags(StateFlags.SelfInvalidStyle))
                {
                    descendFurther = true;

                    _classList.Clear();
                    AssembleClassList(provider, styledObject, result.ChildIndex, childCount);

                    WidgetCachedData cachedData = _manager.ReflectionManager.WidgetPropertyCache.GetCachedData(styledObject.GetType());
                    StylesheetContext context = new StylesheetContext(cachedData, provider, _classList, forceStyleRedo);

                    styledObject.ResolveInvalidProperties(in context);
                }
                else
                    descendFurther = styledObject.StateFlags.HasFlags(StateFlags.InvalidStyle);

                styledObject.RemoveStateFlags(StateFlags.SelfInvalidStyle);

                if (descendFurther)
                {
                    int countBefore = _styleQueue.Count;
                    StyleQueueContext context = new StyleQueueContext(_styleQueue, forceStyleRedo);

                    styledObject.GetUnstyledObjects(ref context);
                    _childCountQueue.Enqueue(_styleQueue.Count - countBefore);
                }
            }

            provider.OnStylesUpdated();

            _classList.Clear();
            _styleQueue.Clear();
            _childCountQueue.Clear();
            _typeSet.Clear();
        }

        private void AssembleClassList(StylesheetProvider provider, StyledObject styledObject, int childIndex, int parentChildCount)
        {
            string[] classNameList = GetClassListForType(styledObject.GetType());
            if (classNameList.Length > 0)
            {
                for (int i = 0; i < classNameList.Length; ++i)
                {
                    string className = classNameList[i];
                    for (int j = 0; j < provider.Stylesheets.Count; ++j)
                    {
                        if (provider.Stylesheets[j].TryGetClass(ClassType.Typed, className, out StylesheetClass? stylesheetClass))
                        {
                            _classList.Add(stylesheetClass);
                        }
                    }
                }
            }

            StyledObject? parentObject = styledObject.ParentObject;
            if (parentObject != null)
            {
                --parentChildCount;

                foreach (string className in parentObject.ClassList)
                {
                    for (int j = 0; j < provider.Stylesheets.Count; ++j)
                    {
                        if (provider.Stylesheets[j].TryGetClass(ClassType.Named, className, out StylesheetClass? stylesheetClass))
                        {
                            if (stylesheetClass.SubClasses.Count > 0)
                            {
                                foreach (StylesheetClass subClass in stylesheetClass.SubClasses)
                                {
                                    switch (subClass.ClassName.PseduoType)
                                    {
                                        case PseduoClassType.FirstChild:
                                            {
                                                if (childIndex == 0)
                                                    _classList.Add(subClass);
                                                break;
                                            }
                                        case PseduoClassType.LastChild:
                                            {
                                                if (childIndex == parentChildCount)
                                                    _classList.Add(subClass);
                                                break;
                                            }
                                        case PseduoClassType.OnlyChild:
                                            {
                                                if (parentChildCount == 0)
                                                    _classList.Add(subClass);
                                                break;
                                            }
                                        case PseduoClassType.FirstOfType:
                                            {
                                                if (_typeSet.Add(styledObject.GetType()))
                                                    _classList.Add(subClass);
                                                break;
                                            }
                                        case PseduoClassType.LastOfType: throw new NotImplementedException();
                                        case PseduoClassType.OnlyOfType: throw new NotImplementedException();
                                        case PseduoClassType.AllChildren:
                                            {
                                                _classList.Add(subClass);
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private string[] GetClassListForType(Type type)
        {
            ref string[]? items = ref CollectionsMarshal.GetValueRefOrAddDefault(_styleClassList, type, out bool exists);
            if (items == null)
            {
                using RentedList<string> itemList = new RentedList<string>();
                do
                {
                    itemList.Add(JsonNamingPolicy.CamelCase.ConvertName(type.Name));
                } while ((type = type.BaseType!) != typeof(Widget).BaseType);

                items = [.. itemList];
            }

            return items;
        }

        internal readonly record struct StyleObjectSearch(StyledObject StyledObject, int ChildIndex);
    }
}
