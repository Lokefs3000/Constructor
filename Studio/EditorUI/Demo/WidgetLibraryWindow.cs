using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Serialization;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Common;

namespace EditorUI.Demo
{
    public sealed class WidgetLibraryWindow : WidgetWindow
    {
        private Stylesheet _stylesheet;

        public WidgetLibraryWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _stylesheet = new Stylesheet("WidgetLibrary");

            CreateLeftSide();
            CreateRightSide();

            StylesheetProvider.AddStylesheet(_stylesheet);
        }

        private void CreateLeftSide()
        {
            // https://www.realtimecolors.com/?colors=e7f2ec-11151c-262c39-465167-194aad&fonts=Inter-Inter

            // setup widgets
            {
                LayoutFrame layoutFrame = new LayoutFrame { Parent = RootWidget };
                layoutFrame.TryAddClass("wl-left-frame");

                Button basicButton = new Button { Parent = layoutFrame };
                basicButton.TryAddClass("wl-category-button");

                Label basicButtonLabel = new Label { Parent = basicButton, Text = "Basic" };
            }

            // setup stylesheet
            {
                StylesheetClass typedButton = _stylesheet.CreateClass(ClassType.Typed, nameof(Button));
                typedButton.SetStyleValue(new StyleKey(nameof(Widget.BackgroundColor)), "#262c39");
                typedButton.SetStyleValue(new StyleKey(nameof(Widget.StrokeColor)), "#465167");
                typedButton.SetStyleValue(new StyleKey(nameof(Widget.StrokeWidth)), "2");

                typedButton.SetStyleValue(new StyleKey(nameof(Widget.BackgroundColor)), "#465167");
                typedButton.SetStyleValue(new StyleKey(nameof(Widget.StrokeColor)), "#194aad");

                StylesheetClass typedLabel = _stylesheet.CreateClass(ClassType.Typed, nameof(Label));
                typedLabel.SetStyleValue(new StyleKey(nameof(Label.FontFamily)), "url(Editor/Fonts/Inter.uifont)");
                typedLabel.SetStyleValue(new StyleKey(nameof(Label.FontSize)), "16");
                typedLabel.SetStyleValue(new StyleKey(nameof(Label.TextColor)), "#e7f2ec");

                StylesheetClass wlLeftFrame = _stylesheet.CreateClass(ClassType.Named, ".wl-left-frame");
                wlLeftFrame.SetStyleValue(new StyleKey(nameof(Widget.Position)), "0.0 0 0.0 0");
                wlLeftFrame.SetStyleValue(new StyleKey(nameof(Widget.Size)), "0.2 0 1.0 0");
                wlLeftFrame.SetStyleValue(new StyleKey(nameof(Widget.BackgroundColor)), "#11151c");
                wlLeftFrame.SetStyleValue(new StyleKey(nameof(Widget.Padding)), "8.0 8.0 8.0 8.0");

                StylesheetClass wlLeftFrameButtons = wlLeftFrame.CreateSubClass(PseduoClassType.AllChildren);
                wlLeftFrameButtons.SetStyleValue(new StyleKey(nameof(Widget.Size)), "1.0 0 0.0 64");
                wlLeftFrameButtons.SetStyleValue(new StyleKey(nameof(Widget.CornerRadius)), "8.0 8.0 8.0 8.0");

                StylesheetClass wlCategoryButton = _stylesheet.CreateClass(ClassType.Named, ".wl-category-button");

                StylesheetClass wlCategoryButtonFirstChild = wlCategoryButton.CreateSubClass(PseduoClassType.FirstChild);
                wlCategoryButtonFirstChild.SetStyleValue(new StyleKey(nameof(Label.Size)), "1.0 0 1.0 0");
                wlCategoryButtonFirstChild.SetStyleValue(new StyleKey(nameof(Label.FontWeight)), "Bold");
                wlCategoryButtonFirstChild.SetStyleValue(new StyleKey(nameof(Label.FontSize)), "24");
                wlCategoryButtonFirstChild.SetStyleValue(new StyleKey(nameof(Label.Alignment)), "CenterMiddle");
            }
        }

        private void CreateRightSide()
        {
            // setup widgets
            {
                Widget widget = new Widget()
                {
                    Position = new UIValue2(0.2f, 0, 0.0f, 0),
                    Size = new UIValue2(0.8f, 0, 1.0f, 0),

                    Parent = RootWidget,

                    BackgroundColor = Color.FromHex("0c1017")
                };

                CreateBasicTab(widget);
            }

            // setup stylesheet
            {
                StylesheetClass wlHeader = _stylesheet.CreateClass(ClassType.Named, ".wl-header");
                wlHeader.SetStyleValue(new StyleKey(nameof(Label.Size)), "1.0 0 0.0 32");
                wlHeader.SetStyleValue(new StyleKey(nameof(Label.FontStyle)), "Italic");
                wlHeader.SetStyleValue(new StyleKey(nameof(Label.FontWeight)), "Bold");
                wlHeader.SetStyleValue(new StyleKey(nameof(Label.FontSize)), "32");

                StylesheetClass wlTitle = _stylesheet.CreateClass(ClassType.Named, ".wl-title");
                wlTitle.SetStyleValue(new StyleKey(nameof(Label.Size)), "1.0 0 0.0 20");
                wlTitle.SetStyleValue(new StyleKey(nameof(Label.FontWeight)), "Semibold");
                wlTitle.SetStyleValue(new StyleKey(nameof(Label.FontSize)), "20");
            }
        }

        private void CreateBasicTab(Widget parent)
        {
            // setup widgets
            {
                LayoutFrame layoutFrame = new LayoutFrame { Parent = parent, Size = UIValue2.Max, Padding = new Vector4(8.0f), ItemPadding = 12.0f };

                Label headerLabel = new Label { Parent = layoutFrame, Text = "Label:" };
                headerLabel.TryAddClass("wl-header");

                // labels
                {
                    LayoutFrame wrapModeFrame = new LayoutFrame
                    {
                        Parent = layoutFrame,
                        Size = UIValue2.MaxX,
                        AutoResize = AutoResizeMode.ResizeY,

                        LayoutDirection = LayoutDirection.Horizontal,
                        LayoutBalance = LayoutBalance.Fit,

                        ItemPadding = 8.0f,

                        Padding = new Vector4(2.0f),

                        StrokeWidth = 1,
                        StrokeColor = Color.FromHex("262c39")
                    };

                    wrapModeFrame.TryAddClass("wl-lblwrap-frame");

                    Label overflowLabel = new Label
                    {
                        Parent = wrapModeFrame,
                        Text = "I overflow because and eventually get clipped because there is too much text!"
                    };

                    Label ellipsisLabel = new Label
                    {
                        Parent = wrapModeFrame,
                        WrapMode = TextWrapMode.Ellipsis,
                        Text = "I overflow because get turned into a ellipsis instead of get clipped!"
                    };

                    Label wrapLabel = new Label
                    {
                        Parent = wrapModeFrame,
                        WrapMode = TextWrapMode.Wrap,
                        Text = "I wrap around the box and at each word so the text won't overflow! It will still clip at the bottom since it still goes out of bounds with no way to wrap."
                    };

                    GridFrame alignmentFrame = new GridFrame
                    {
                        Parent = layoutFrame,
                        Size = UIValue2.MaxX,
                        AutoResize = AutoResizeMode.ResizeY,

                        LayoutDirection = LayoutDirection.Horizontal,
                        // LayoutBalance = LayoutBalance.Fit,

                        ItemPadding = new Vector2(8.0f),
                        MaxRowsOrColumns = 3,
                        
                        HorizontalBalance = LayoutBalance.Fit,

                        Padding = new Vector4(2.0f),

                        StrokeWidth = 1,
                        StrokeColor = Color.FromHex("262c39")
                    };

                    alignmentFrame.TryAddClass("wl-align-frame");

                    Label tlLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align top-left!", Alignment = TextAlignment.TopLeft };
                    Label tmLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align top-middle!", Alignment = TextAlignment.TopMiddle };
                    Label trLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align top-right!", Alignment = TextAlignment.TopRight };
                    Label clLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align center-left!", Alignment = TextAlignment.CenterLeft };
                    Label cmLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align center-middle!", Alignment = TextAlignment.CenterMiddle };
                    Label crLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align center-right!", Alignment = TextAlignment.CenterRight };
                    Label blLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align bottom-left!", Alignment = TextAlignment.BottomLeft };
                    Label bmLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align bottom-middle!", Alignment = TextAlignment.BottomMiddle };
                    Label brLabel = new Label { Parent = alignmentFrame, Size = new UIValue2(0, 64), Text = "I align bottom-right!", Alignment = TextAlignment.BottomRight };
                }
            }

            // setup stylesheets
            {
                StylesheetClass wlLblWrapFrame = _stylesheet.CreateClass(ClassType.Named, ".wl-lblwrap-frame");

                StylesheetClass wlLblWrapFrameAllChildren = wlLblWrapFrame.CreateSubClass(PseduoClassType.AllChildren);
                wlLblWrapFrameAllChildren.SetStyleValue(new StyleKey("BackgroundColor"), "#465167");

                StylesheetClass wlAlignFrame = _stylesheet.CreateClass(ClassType.Named, ".wl-align-frame");

                StylesheetClass wlAlignFrameAllChildren = wlAlignFrame.CreateSubClass(PseduoClassType.AllChildren);
                wlAlignFrameAllChildren.SetStyleValue(new StyleKey("BackgroundColor"), "#465167");
            }
        }
    }
}
