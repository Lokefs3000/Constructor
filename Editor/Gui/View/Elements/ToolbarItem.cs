using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.Gui.View.Elements
{
    [UIElementPrettyName("ToolbarItem")]
    [StyleableStates("Normal", "Hovered", "Pressed", "Toggled")]
    public abstract class ToolbarItem : UIButton
    {
        protected ToolbarItemAppearance[] _appearances;
        protected int _currentAppearance;

        protected UIFontAsset? _font;
        protected FontStyle _style;
        protected FontWeight _weight;

        protected Int2 _padding;
        protected int _innerSpacing;

        protected UIColor _textColor;
        protected UIColor _imageColor;
        protected UIColor _splitterColor;

        protected bool _isExpandable;
        protected bool _isToggleable;

        private float _textWidthSinceLastLayout;
        private bool _isToggled;

        public ToolbarItem()
        {
            _appearances = [];
            _currentAppearance = 0;

            _font = null;
            _style = FontStyle.Normal;
            _weight = FontWeight.Normal;

            _padding = Int2.Zero;
            _innerSpacing = 0;

            _textColor = Color.White;
            _imageColor = Color.White;
            _splitterColor = Color.Black;

            _isExpandable = false;
            _isToggleable = false;

            _textWidthSinceLastLayout = 0.0f;
            _isToggled = false;
        }

        protected virtual void Expand()
        {

        }

        protected virtual void Toggled()
        {
            SetState("Toggled", _isToggled);
            OnToggled?.Invoke(_isToggled);
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            base.MeasureSize(context);

            ToolbarItemAppearance appearance = _appearances[_currentAppearance];

            float availableHeight = _currentSize.Y - _padding.Y * 2;
            float currentWidth = _isExpandable ? ExpandableAreaWidth : 0.0f;

            if (appearance.Text != null)
            {
                UIFontTypeData? typeData = _font?.FindStyle(_style, _weight);
                if (typeData != null)
                {
                    float textSize = availableHeight / TextManager.PixelsPerEM;

                    TextVisualInfo visualInfo = new TextVisualInfo(new PaintColor(_textColor.Solid), textSize, typeData);
                    TextWrapInfo wrapInfo = new TextWrapInfo(TextOrigin.Bottom, new Vector2(float.PositiveInfinity, availableHeight), false, visualInfo);

                    TextManager text = UIManager.Instance.TextManager;
                    StringHandle handle = text.GetStringHandle(appearance.Text);

                    ShapedTextData textData = UIManager.Instance.TextManager.ShapeText(wrapInfo, UITextOverflow.Overflow, handle.String, handle.Hash);

                    currentWidth += textData.TotalSize.X + _innerSpacing;
                    _textWidthSinceLastLayout = textData.TotalSize.X;
                }
            }

            if (appearance.Sprite != null)
            {
                currentWidth += availableHeight + _innerSpacing;
            }

            _currentSize = new Vector2(currentWidth + _padding.X * 2, _currentSize.Y);
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            base.DrawVisual(painter);

            ToolbarItemAppearance appearance = _appearances[_currentAppearance];

            float innerHeight = _currentSize.Y - _padding.Y * 2;
            Vector2 minimum = _pixelCoordinates.Minimum + _padding.AsVector2();

            if (appearance.Text != null)
            {
                UIFontTypeData? typeData = _font?.FindStyle(_style, _weight);
                if (typeData != null)
                {
                    float textSize = innerHeight / TextManager.PixelsPerEM;

                    TextBuilder text = new TextBuilder()
                        .SetAllowRichText(false)
                        .SetMaxExtents(new Vector2(float.PositiveInfinity, innerHeight))
                        .SetOrigin(TextOrigin.Bottom);

                    painter.DrawText(new Vector2(minimum.X, _pixelCoordinates.Maximum.Y - _padding.Y), UIPaint.FromColor(_textColor), text, typeData, textSize, appearance.Text);
                    minimum.X += _textWidthSinceLastLayout + _innerSpacing;
                }
            }

            if (appearance.Sprite != null)
            {
                painter.DrawImage(new Boundaries(minimum, minimum + new Vector2(innerHeight)), UIPaint.FromColor(_imageColor), appearance.Sprite);
                minimum.X += innerHeight + _innerSpacing;
            }

            if (_isExpandable)
            {
                painter.DrawLine(new Vector2(minimum.X + 1.0f, minimum.Y), minimum + new Vector2(1.0f, innerHeight), UIPaint.FromColor(_splitterColor));

                DrawArrow(painter, new Vector2(minimum.X + _padding.X * 0.5f + 0.5f, minimum.Y), innerHeight);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawArrow(UIPainterContext painter, Vector2 minimum, float innerHeight)
        {
            float yCenter = innerHeight * 0.5f;

            Span<Vector2> points =
            [
                minimum + new Vector2(3.0f, yCenter - 1.0f),
                minimum + new Vector2((ExpandableAreaWidth - 1) / 2 + 1, yCenter + 4.0f),
                minimum + new Vector2(ExpandableAreaWidth - 2, yCenter - 1.0f),
            ];

            painter.DrawLines(points, UIPaint.FromColor(Color.White), UILineMode.Strip, 1.5f);
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            if (@event.Type == UIEventType.MouseActivate)
            {
                bool areBothActive = _isToggleable && _isExpandable;
                if (_isToggleable || _isExpandable)
                {
                    if (areBothActive)
                    {
                        if (@event.Mouse.Position.X < _pixelCoordinates.Maximum.X - ExpandableAreaWidth)
                        {
                            _isToggled = !_isToggled;
                            Toggled();
                        }
                        else
                        {
                            Expand();
                        }
                    }
                    else
                    {
                        if (_isToggleable)
                        {
                            _isToggled = !_isToggled;
                            Toggled();
                        }
                        else
                        {
                            Expand();
                        }
                    }
                }

                return;
            }

            base.HandleEvent(in @event);
        }

        public void ChangeAppearance(int index)
        {
            index = Math.Clamp(index, 0, _appearances.Length - 1);
            if (_currentAppearance != index)
            {
                _currentAppearance = index;
                AddStateFlags(UIStateFlags.InvalidAll);
            }
        }

        internal virtual void InitializeAppearanceArray(int count) => _appearances = new ToolbarItemAppearance[count];
        internal virtual void SetAppearanceData(int index, string key, ToolbarItemAppearance appearance) => _appearances[index] = appearance;

        internal ToolbarItemAppearance[] Appearances
        {
            get => _appearances;
            set
            {
                _appearances = value;
                _currentAppearance = Math.Min(_currentAppearance, value.Length - 1);

                AddStateFlags(UIStateFlags.InvalidAll);
            }
        }

        public bool IsToggled
        {
            get => _isToggleable;
            set
            {
                if (_isToggled != value)
                {
                    SetState("Toggled", value);
                    _isToggled = value;
                }
            }
        }

        #region Properties
        [EditableProperty(nameof(_isExpandable))] public bool IsExpandable { get => _isExpandable; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isToggleable))] public bool IsToggleable { get => _isToggleable; set => SetEditableProperty(value); }

        [StyleableProperty(nameof(_font), UIStateFlags.InvalidVisual)] public UIFontAsset? Font { get => _font; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_style), UIStateFlags.InvalidVisual)] public FontStyle FontStyle { get => _style; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_weight), UIStateFlags.InvalidVisual)] public FontWeight FontWeight { get => _weight; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_padding), UIStateFlags.InvalidVisual)] public Int2 Padding { get => _padding; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_innerSpacing), UIStateFlags.InvalidVisual)] public int InnerSpacing { get => _innerSpacing; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_textColor), UIStateFlags.InvalidVisual)] public UIColor TextColor { get => _textColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_imageColor), UIStateFlags.InvalidVisual)] public UIColor ImageColor { get => _imageColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_splitterColor), UIStateFlags.InvalidVisual)] public UIColor SplitterColor { get => _splitterColor; set => SetStyleProperty(value); }
        #endregion
        #region Events
        public event Action<bool>? OnToggled;
        #endregion

        private const int ExpandableAreaWidth = 13;
    }

    public readonly record struct ToolbarItemAppearance(string? Text, Sprite? Sprite);
}
