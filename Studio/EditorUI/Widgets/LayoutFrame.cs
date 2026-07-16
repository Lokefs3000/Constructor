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

        protected internal override MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        {
            MeasureStatus status = base.MeasureSelf(in context);

            _positionLockAxis = _layoutDirection == LayoutDirection.Vertical ? LayoutLockAxis.AxisY : LayoutLockAxis.AxisX;
            _sizeLockAxis = LayoutLockAxis.None;

            if (_layoutBalance == LayoutBalance.Fit)
            {
                switch (_layoutDirection)
                {
                    case LayoutDirection.Vertical:
                        {
                            if (_children != null && _children.Count > 0)
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * (_children.Count - 1);
                                float childSize = (_layoutState.IdealSize.Y - _padding.Y - _padding.W) / _children.Count - itemPadding;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealSize = new Vector2(child.IdealSize.X, childSize);
                                    child.AddStateFlags(StateFlags.SelfInvalidLayout);
                                }
                            }

                            _sizeLockAxis = LayoutLockAxis.AxisY;
                            break;
                        }
                    case LayoutDirection.Horizontal:
                        {
                            if (_children != null && _children.Count > 0)
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * (_children.Count - 1);
                                float childSize = (_layoutState.IdealSize.X - _padding.X - _padding.Z) / _children.Count - itemPadding;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealSize = new Vector2(childSize, child.IdealSize.Y);
                                    child.AddStateFlags(StateFlags.SelfInvalidLayout);
                                }
                            }

                            _sizeLockAxis = LayoutLockAxis.AxisX;
                            break;
                        }
                }
            }

            return status;
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
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
                                    totalSpace += child.IdealSize.Y + (child.Margin.Y + child.Margin.W);
                                }

                                totalSpace += _itemPadding * (_children.Count - 1);

                                if (_layoutBalance == LayoutBalance.Space)
                                {
                                    float positionIncrement = (_layoutState.IdealSize.Y - _padding.Y - _padding.W - _children[^1].IdealSize.Y) / (_children.Count - 1);
                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        float marginSize = child.Margin.Y + child.Margin.W;
                                        float contentOffset = child.ContentPosition.Y - child.IdealPosition.Y;

                                        child.IdealPosition = new Vector2(child.IdealPosition.X, positionIncrement * i + marginSize);
                                        child.ContentPosition = new Vector2(child.ContentPosition.X, child.IdealPosition.Y + contentOffset);
                                    }
                                }
                                else
                                {
                                    float startPosition = _layoutAlignment switch
                                    {
                                        LayoutAlignment.Start => 0.0f,
                                        LayoutAlignment.Middle => (_layoutState.IdealSize.Y - totalSpace) * 0.5f,
                                        LayoutAlignment.End => _layoutState.IdealSize.Y - totalSpace,
                                        _ => 0.0f
                                    };

                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        float marginSize = child.Margin.Y + child.Margin.W;
                                        float contentOffset = child.ContentPosition.Y - child.IdealPosition.Y;

                                        startPosition += marginSize;

                                        child.IdealPosition = new Vector2(child.IdealPosition.X, startPosition);
                                        child.ContentPosition = new Vector2(child.ContentPosition.X, child.IdealPosition.Y + contentOffset);
                                        startPosition += child.IdealSize.Y + _itemPadding;
                                    }
                                }
                            }
                            else
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * _children.Count;
                                float positionIncrement = (_layoutState.IdealSize.Y - _padding.Y - _padding.W + itemPadding) / _children.Count;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    float marginSize = child.Margin.Y + child.Margin.W;
                                    float contentOffset = child.ContentPosition.Y - child.IdealPosition.Y;

                                    child.IdealPosition = new Vector2(child.IdealPosition.X, positionIncrement * i + marginSize);
                                    child.ContentPosition = new Vector2(child.ContentPosition.X, child.IdealPosition.Y + contentOffset);
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
                                    totalSpace += child.IdealSize.X + (child.Margin.X + child.Margin.Z);
                                }

                                totalSpace += _itemPadding * (_children.Count - 1);

                                if (_layoutBalance == LayoutBalance.Space)
                                {
                                    float positionIncrement = (_layoutState.IdealSize.X - _padding.X - _padding.Z - _children[^1].IdealSize.X) / (_children.Count - 1);
                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        float marginSize = child.Margin.X + child.Margin.Z;
                                        float contentOffset = child.ContentPosition.X - child.IdealPosition.X;

                                        child.IdealPosition = new Vector2(positionIncrement * i + marginSize, child.IdealPosition.Y);
                                        child.ContentPosition = new Vector2(child.IdealPosition.X + contentOffset, child.ContentPosition.Y);
                                    }
                                }
                                else
                                {
                                    float startPosition = _layoutAlignment switch
                                    {
                                        LayoutAlignment.Start => 0.0f,
                                        LayoutAlignment.Middle => (_layoutState.IdealSize.X - totalSpace) * 0.5f,
                                        LayoutAlignment.End => _layoutState.IdealSize.X - totalSpace,
                                        _ => 0.0f
                                    };

                                    for (int i = 0; i < _children.Count; ++i)
                                    {
                                        Widget child = _children[i];
                                        float marginSize = child.Margin.X + child.Margin.Z;
                                        float contentOffset = child.ContentPosition.X - child.IdealPosition.X;

                                        startPosition += marginSize;

                                        child.IdealPosition = new Vector2(startPosition, child.IdealPosition.Y);
                                        child.ContentPosition = new Vector2(child.IdealPosition.X + contentOffset, child.ContentPosition.Y);

                                        startPosition += child.IdealSize.X + _itemPadding;
                                    }
                                }
                            }
                            else
                            {
                                float itemPadding = _itemPadding == 0.0f ? 0.0f : _itemPadding * 0.5f * _children.Count;
                                float positionIncrement = (_layoutState.IdealSize.X - _padding.X - _padding.Z + itemPadding) / _children.Count;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    float marginSize = child.Margin.X + child.Margin.Z;
                                    float contentOffset = child.ContentPosition.X - child.IdealPosition.X;

                                    child.IdealPosition = new Vector2(positionIncrement * i + marginSize, child.IdealPosition.Y);
                                    child.ContentPosition = new Vector2(child.IdealPosition.X + contentOffset, child.ContentPosition.Y);
                                }
                            }
                        }

                        break;
                    }
            }

            return base.LayoutSelf(in context);
        }

        public override LayoutBehaviour LayoutBehaviour => new LayoutBehaviour(base.LayoutBehaviour.AsGroup, true);

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
