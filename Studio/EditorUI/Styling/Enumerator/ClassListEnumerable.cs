using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using TerraFX.Interop.Windows;

namespace EditorUI.Styling.Enumerator
{
    public readonly record struct ClassListEnumerable(ROList<string> User, ROList<StylesheetClass> Default, StylesheetProvider Stylesheets) : IEnumerable<StylesheetClass>
    {
        public Enumerator GetEnumerator() => new Enumerator(User, Default, Stylesheets);

        IEnumerator<StylesheetClass> IEnumerable<StylesheetClass>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public record struct Enumerator : IEnumerator<StylesheetClass>
        {
            private readonly ROList<string> _user;
            private readonly ROList<StylesheetClass> _default;
            private readonly StylesheetProvider _stylesheets;

            private bool _useDefault;
            private int _index;

            private ROList<Stylesheet> _classList;
            private int _classListIndex;

            private StylesheetClass? _value;

            public Enumerator(ROList<string> user, ROList<StylesheetClass> @default, StylesheetProvider stylesheets)
            {
                _user = user;
                _default = @default;
                _stylesheets = stylesheets;

                _useDefault = user.Count == 0;
                _index = _useDefault ? @default.Count - 1 : user.Count - 1;

                _classList = ROList<Stylesheet>.Empty;
                _classListIndex = -1;

                _value = null;
            }

            public void Dispose()
            {
                _useDefault = false;
                _index = -1;

                _classList = ROList<Stylesheet>.Empty;
                _classListIndex = -1;

                _value = null;
            }

            public bool MoveNext()
            {
            RetryMoveNext:
                if (_index == -1)
                {
                    _value = null;
                    return false;
                }

                if (_classList.Count > 0)
                {
                    string className = _user[_index];
                    bool foundClassWithName = false;

                    for (_classListIndex = 0; _classListIndex < _classList.Count; ++_classListIndex)
                    {
                        Stylesheet stylesheet = _classList[_classListIndex];
                        if (stylesheet.TryGetClass(ClassType.Named, className, out _value))
                        {
                            // --_classListIndex;
                            foundClassWithName = true;
                            break;
                        }
                    }

                    if (--_index < 0)
                    {
                        _classList = ROList<Stylesheet>.Empty;
                        _index = _default.Count == 0 ? -1 : _default.Count - 1;
                        _useDefault = true;
                    }

                    if (!foundClassWithName)
                        goto RetryMoveNext;

                    return true;
                }

                if (_useDefault)
                {
                    _value = _default[_index--];
                }
                else
                {
                    _classList = _stylesheets.Stylesheets;
                    _classListIndex = 0;
                    
                    if (_classList.Count == 0)
                    {
                        _useDefault = true;
                        _index = _default.Count == 0 ? -1 : _default.Count - 1;
                    }

                    goto RetryMoveNext;
                }

                return true;
            }

            public void Reset()
            {
                _useDefault = _user.Count == 0;
                _index = _useDefault ? _default.Count - 1 : _user.Count - 1;

                _classList = ROList<Stylesheet>.Empty;
                _classListIndex = -1;

                _value = null;
            }

            public readonly StylesheetClass Current => _value!;
            readonly object IEnumerator.Current => Current;
        }
    }
}
