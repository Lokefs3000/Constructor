using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Layout;
using Primary.Common;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class LayoutFrame : ScrollView
    {
        protected LayoutDirection _layoutDirection;
        protected LayoutAlignment _layoutAlignment;
        protected LayoutBalance _layoutBalance;

        protected float _itemPadding;

        public LayoutFrame()
        {
            _layoutDirection = LayoutDirection.Vertical;
            _layoutAlignment = LayoutAlignment.Start;
            _layoutBalance = LayoutBalance.None;

            _itemPadding = 0.0f;
        }

        protected internal override MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            LayoutLockAxis lockAxis = LayoutLockAxis.None;
            if (_layoutBalance == LayoutBalance.Fit)
            {
                switch (_layoutDirection)
                {
                    case LayoutDirection.Vertical:
                        {
                            if (_children != null && _children.Count > 0)
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * (_children.Count - 1);
                                float childSize = (_idealSize.Y - _padding.Y - _padding.W) / _children.Count - itemPadding;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealSize = new Vector2(0.0f, childSize);
                                }
                            }

                            lockAxis = LayoutLockAxis.AxisY;
                            break;
                        }
                    case LayoutDirection.Horizontal:
                        {
                            if (_children != null && _children.Count > 0)
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * (_children.Count - 1);
                                float childSize = (_idealSize.X - _padding.X - _padding.Z) / _children.Count - itemPadding;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealSize = new Vector2(childSize, 0.0f);
                                }
                            }

                            lockAxis = LayoutLockAxis.AxisX;
                            break;
                        }
                }
            }

            return new MeasureReturnData(MeasureStatus.Success, lockAxis);
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            base.LayoutSelf(in context);

            LayoutLockAxis lockAxis = LayoutLockAxis.None;
            switch (_layoutDirection)
            {
                case LayoutDirection.Vertical:
                    {
                        lockAxis = LayoutLockAxis.AxisY;
                        break;
                    }
                case LayoutDirection.Horizontal:
                    {
                        lockAxis = LayoutLockAxis.AxisX;
                        break;
                    }
            }

            return new LayoutReturnData(lockAxis, true);
        }

        protected internal override void PostLayoutSelf(ref readonly LayoutContext context)
        {
            switch (_layoutDirection)
            {
                case LayoutDirection.Vertical:
                    {
                        if (_children != null && _children.Count > 0)
                        {
                            if (_layoutBalance != LayoutBalance.Fit)
                            {
                                float totalSpace = 0.0f;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    totalSpace += child.IdealSize.Y;
                                }

                                totalSpace += _itemPadding * (_children.Count - 1);

                                if (_layoutBalance == LayoutBalance.Space)
                                {
                                    float positionIncrement = (_idealSize.Y - _padding.Y - _padding.W - _children[^1].IdealSize.Y) / (_children.Count - 1);
                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        child.IdealPosition = new Vector2(child.IdealPosition.X, positionIncrement * i);
                                    }
                                }
                                else
                                {
                                    float startPosition = _layoutAlignment switch
                                    {
                                        LayoutAlignment.Start => 0.0f,
                                        LayoutAlignment.Middle => (_idealSize.Y - totalSpace) * 0.5f,
                                        LayoutAlignment.End => _idealSize.Y - totalSpace,
                                        _ => 0.0f
                                    };

                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        child.IdealPosition = new Vector2(child.IdealPosition.X, startPosition);
                                        startPosition += child.IdealSize.Y + _itemPadding;
                                    }
                                }
                            }
                            else
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * _children.Count;
                                float positionIncrement = (_idealSize.Y - _padding.Y - _padding.W + itemPadding) / _children.Count;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealPosition = new Vector2(child.IdealPosition.X, positionIncrement * i);
                                }
                            }
                        }

                        break;
                    }
                case LayoutDirection.Horizontal:
                    {
                        if (_children != null && _children.Count > 0)
                        {
                            if (_layoutBalance != LayoutBalance.Fit)
                            {
                                float totalSpace = 0.0f;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    totalSpace += child.IdealSize.X;
                                }

                                totalSpace += _itemPadding * (_children.Count - 1);

                                if (_layoutBalance == LayoutBalance.Space)
                                {
                                    float positionIncrement = (_idealSize.X - _padding.X - _padding.Z - _children[^1].IdealSize.X) / (_children.Count - 1);
                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        child.IdealPosition = new Vector2(positionIncrement * i, child.IdealPosition.Y);
                                    }
                                }
                                else
                                {
                                    float startPosition = _layoutAlignment switch
                                    {
                                        LayoutAlignment.Start => 0.0f,
                                        LayoutAlignment.Middle => (_idealSize.X - totalSpace) * 0.5f,
                                        LayoutAlignment.End => _idealSize.X - totalSpace,
                                        _ => 0.0f
                                    };

                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        child.IdealPosition = new Vector2(startPosition, child.IdealPosition.Y);
                                        startPosition += child.IdealSize.X + _itemPadding;
                                    }
                                }
                            }
                            else
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * _children.Count;
                                float positionIncrement = (_idealSize.X - _padding.X - _padding.Z + itemPadding) / _children.Count;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealPosition = new Vector2(positionIncrement * i, child.IdealPosition.Y);
                                }
                            }
                        }

                        break;
                    }
            }
        }

        public override void AddStateFlags(StateFlags flags)
        {
            if (flags.HasFlags(StateFlags.InvalidLayout) && _children != null)
            {
                for (int i = 0; i < _children.Count; ++i)
                {
                    if (_children[i].StateFlags.HasFlags(StateFlags.ThisLayout))
                    {
                        flags |= StateFlags.ThisLayout;
                        break;
                    }
                }
            }

            base.AddStateFlags(flags);
        }

        #region Serializable
        [Styled(nameof(_layoutDirection), StateFlags.SelfInvalidLayout)] public LayoutDirection LayoutDirection { get => _layoutDirection; set => SetStyledField(value); }
        [Styled(nameof(_layoutAlignment), StateFlags.SelfInvalidLayout)] public LayoutAlignment LayoutAlignment { get => _layoutAlignment; set => SetStyledField(value); }
        [Styled(nameof(_layoutBalance), StateFlags.SelfInvalidLayout)] public LayoutBalance LayoutBalance { get => _layoutBalance; set => SetStyledField(value); }

        [Styled(nameof(_itemPadding), StateFlags.SelfInvalidLayout)] public float ItemPadding { get => _itemPadding; set => SetStyledField(value); }
        #endregion
    }

    public enum LayoutDirection : byte
    {
        Vertical = 0,
        Horizontal,
    }

    public enum LayoutAlignment : byte
    {
        Start = 0,
        Middle,
        End
    }

    public enum LayoutBalance : byte
    {
        None = 0,
        Space,
        Fit
    }
}
