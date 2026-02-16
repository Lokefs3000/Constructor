using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Visual;
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
            WindowTitle = "UI designer";

            UIFontAsset font = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

            UIFontStyle regular = font.FindStyle(null)!;

            UISplitContainer primaryContainer = new UISplitContainer() { Parent = RootElement };

            {
                primaryContainer.Transform.Size = UIValue2.Max;
            }

            UISplitPanel leftSplit = primaryContainer.AddSplit(null, UISplitDirection.Vertical);

            UISplitPanel hierchySplit = primaryContainer.AddSplit(leftSplit, UISplitDirection.Horizontal);
            {
                {
                    hierchySplit.FillColor = new Color(25, 25, 25);
                }
            }

            UISplitPanel elementsSplit = primaryContainer.AddSplit(leftSplit, UISplitDirection.Horizontal);
            {
                {
                    elementsSplit.FillColor = new Color(25, 25, 25);
                }

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

                    UIButton labelButton = CreateButton("Label");
                    labelButton.Parent = elementsSplit;
                }

                UIButton CreateButton(string text)
                {
                    UIButton button = new UIButton();
                    {
                        UIFitterLayout fitterLayout = button.AddLayoutModifier<UIFitterLayout>();
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
                {
                    middleSplit.FillColor = new Color(25, 25, 25);
                }

                UICanvas canvas = new UICanvas { Parent = middleSplit };
                {
                    canvas.Transform.Size = UIValue2.Max;

                    canvas.ClientOffset = Vector2.Zero;
                    canvas.ClientSize = new Vector2(200.0f);
                }

                UIFrame toolbarFrame = new UIFrame() { Parent = middleSplit, CornerRadius = 4.0f };
                {
                    {
                        toolbarFrame.Transform.Position = new UIValue2(8, 8);

                        toolbarFrame.FillColor = new Color(40, 40, 40);

                        toolbarFrame.StrokeColor = new Color(247, 152, 27);
                        toolbarFrame.StrokePosition = UIStrokePosition.Outside;
                        toolbarFrame.StrokeWeight = 1.0f;
                    }

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

                        pointerButton.FillColor = new Color(100, 100, 100);
                        pointerButton.CornerRadius = 3.0f;

                        UIImage icon = new UIImage() { Parent = pointerButton };
                        {
                            icon.Transform.Position = new UIValue2(2, 2);
                            icon.Transform.Size = new UIValue2(-4, 1.0f, -4, 1.0f);

                            icon.Image = AssetManager.LoadAsset<TextureAsset>("Editor/Textures/UIDesigner/TlPointer.png");
                        }
                    }

                    UIButton handButton = new UIButton() { Parent = toolbarFrame };
                    {
                        handButton.Transform.Size = new UIValue2(32, 32);
                    }
                }
            }

            UISplitPanel rightSplit = primaryContainer.AddSplit(null, UISplitDirection.Vertical);
            {
                {
                    rightSplit.FillColor = new Color(25, 25, 25);
                }
            }

            primaryContainer.AutoBalanceSplits(null, true);
        }
    }
}
