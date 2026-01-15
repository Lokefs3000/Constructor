using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Designer
{
    internal sealed class UIDesigner : UIWindow
    {
        public UIDesigner(int uniqueWindowId) : base(uniqueWindowId)
        {
            UIFontAsset font = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

            UIFontStyle regular = font.FindStyle(null)!;

            UISplitContainer primaryContainer = new UISplitContainer() { Parent = RootElement };
            UISplitPanel leftSplit = primaryContainer.AddSplit(null, UISplitDirection.Vertical);

            UISplitPanel hierchySplit = primaryContainer.AddSplit(leftSplit, UISplitDirection.Horizontal);
            {

            }

            UISplitPanel elementsSplit = primaryContainer.AddSplit(leftSplit, UISplitDirection.Horizontal);
            {
                {
                    UIListLayout listLayout = elementsSplit.AddLayoutModifier<UIListLayout>();
                    listLayout.Direction = UIListLayoutDirection.Vertical;
                }

                UILabel basicHeaderLabel = new UILabel()
                {
                    Parent = elementsSplit,
                    FontStyle = regular,
                    Text = "Basic",
                    Size = 32.0f,
                    Alignment = UITextAlignment.Left | UITextAlignment.Middle,
                };

                basicHeaderLabel.Transform.Size = new UIValue2(new UIValue(1.0f), new UIValue((int)basicHeaderLabel.Size));

                {
                    UIButton frameButton = CreateButton("Frame");
                    frameButton.Parent = elementsSplit;
                }

                UIButton CreateButton(string text)
                {
                    UIButton button = new UIButton();
                    {
                        UIFitterLayout fitterLayout = button.AddLayoutModifier<UIFitterLayout>(); ;
                        fitterLayout.Axis = UIFitterAxis.Both;
                        fitterLayout.Margin = new UIValue2(6, 4);
                    }

                    UILabel label = new UILabel
                    {
                        Parent = button,
                        FontStyle = regular,
                        Text = text,
                        Size = 24.0f,
                        AutoSize = UITextAutoSize.FitSizeToText
                    };

                    return button;
                }
            }

            UISplitPanel middleSplit = primaryContainer.AddSplit(null, UISplitDirection.Vertical);
            {
                UIFrame toolbarFrame = new UIFrame() { Parent = middleSplit, CornerRadius = 0.1f };
                {
                    toolbarFrame.Transform.Position = new UIValue2(8, 8);

                    {
                        UIListLayout listLayout = toolbarFrame.AddLayoutModifier<UIListLayout>();
                        listLayout.Direction = UIListLayoutDirection.Vertical;
                        listLayout.Padding = new UIValue2(4, 4);

                        UIFitterLayout fitterLayout = toolbarFrame.AddLayoutModifier<UIFitterLayout>();
                        fitterLayout.Axis = UIFitterAxis.Both;
                        fitterLayout.Margin = new UIValue2(4, 4);
                    }

                    UIButton pointerButton = new UIButton() { Parent = toolbarFrame };
                    {
                        pointerButton.Transform.Size = new UIValue2(32, 32);
                    }

                    UIButton handButton = new UIButton() { Parent = toolbarFrame };
                    {
                        handButton.Transform.Size = new UIValue2(32, 32);
                    }
                }
            }

            UISplitPanel rightSplit = primaryContainer.AddSplit(null, UISplitDirection.Vertical);
            {

            }

            primaryContainer.AutoBalanceSplits(null, true);

            UIFrame testFrame = new UIFrame
            {
                Parent = RootElement,
                FillColor = new UIGradientColor(UIGradientType.Linear, [
                    new UIGradientKey(0.0f, new Color(0.1f)),
                    new UIGradientKey(0.2f, new Color(0.5f)),
                    new UIGradientKey(0.4f, new Color(0.1f)),
                    new UIGradientKey(0.6f, new Color(0.5f)),
                    new UIGradientKey(0.8f, new Color(0.1f)),
                    new UIGradientKey(1.0f, new Color(0.5f)),
                    ]),
            };

            testFrame.Transform.Size = new UIValue2(256, 256);
        }
    }
}
