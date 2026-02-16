using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal interface IElementInspector
    {
        public void Inspect(UIElement element);

        public static bool InputUIValue2(ReadOnlySpan<byte> fmt, ref UIValue2 v)
        {
            bool r = false;

            ImGui.Text(fmt);

            ImGui.Indent();
            ImGui.PushID(fmt);

            Vector2 avail = ImGui.GetContentRegionAvail();
            float singleWidth = avail.X * 0.3f;

            ImGui.SetNextItemWidth(singleWidth);
            r = ImGui.DragInt("##XA"u8, ref v.X.Absolute) || r;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(singleWidth);
            r = ImGui.DragFloat("##XR"u8, ref v.X.Relative) || r;
            ImGui.SameLine();
            ImGui.Text("X"u8);

            ImGui.SetNextItemWidth(singleWidth);
            r = ImGui.DragInt("##YA"u8, ref v.Y.Absolute) || r;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(singleWidth);
            r = ImGui.DragFloat("##YR"u8, ref v.Y.Relative) || r;
            ImGui.SameLine();
            ImGui.Text("Y"u8);

            ImGui.PopID();
            ImGui.Unindent();

            return r;
        }

        public static bool InputUIColor(ReadOnlySpan<byte> fmt, ref UIColor v)
        {
            bool r = false;

            ImGui.PushID(fmt);

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Vector2 dummySize = new Vector2(avail.X * 0.5f, context.FontSize);

            ImGui.Dummy(dummySize);
            ImGui.OpenPopupOnItemClick("Edit color"u8, ImGuiPopupFlags.MouseButtonLeft);

            ImGui.SameLine();
            ImGui.Text(fmt);

            if (v.Type == UIColorType.Solid)
            {
                drawList.AddRectFilled(pos + Vector2.One, pos + dummySize - Vector2.One, new Color32(v.Solid.AsVector4()).ABGR);
                drawList.AddRect(pos, pos + dummySize, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR);
            }
            else
            {
                Vector2 from = pos + Vector2.One;
                Vector2 to = pos + dummySize - Vector2.One;

                UIGradientColor gradient = v.Gradient;
                for (int i = 0; i < gradient.Keys.Length - 1; ++i)
                {
                    UIGradientKey keyNow = gradient.Keys[i];
                    UIGradientKey keyNext = gradient.Keys[i + 1];

                    uint color1 = new Color32(keyNow.Color.AsVector4()).ABGR;
                    uint color2 = new Color32(keyNext.Color.AsVector4()).ABGR;

                    drawList.AddRectFilledMultiColor(new Vector2(float.Lerp(from.X, to.X, keyNow.Time), from.Y), new Vector2(float.Lerp(from.X, to.X, keyNext.Time), to.Y), color1, color2, color2, color1);
                }

                drawList.AddRect(pos, pos + dummySize, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR);

                for (int i = 0; i < gradient.Keys.Length; ++i)
                {
                    UIGradientKey key = gradient.Keys[i];
                    Vector2 markerPos = new Vector2(float.Lerp(from.X, to.X, key.Time), to.Y);

                    ImGui.SetCursorScreenPos(markerPos);
                    ImGui.Dummy(new Vector2(4.0f, 8.0f));

                    drawList.AddTriangleFilled(markerPos + new Vector2(-2.0f, 4.0f), markerPos + new Vector2(2.0f, 4.0f), new Vector2(markerPos.X, markerPos.Y), 0xffffffff);
                }
            }

            if (ImGui.BeginPopup("Edit color"u8, ImGuiWindowFlags.NoSavedSettings))
            {
                if (ImGui.BeginCombo("Type"u8, v.Type == UIColorType.Solid ? "Solid"u8 : "Gradient"u8))
                {
                    if (ImGui.Selectable("Solid"u8, v.Type == UIColorType.Solid))
                    {
                        r = v.Type != UIColorType.Solid;
                        if (r)
                            v = v.Gradient.Keys.FirstOrDefault(new UIGradientKey(0.0f, Color.White)).Color;

                        v.Type = UIColorType.Solid;
                    }
                    else if (ImGui.Selectable("Gradient"u8, v.Type == UIColorType.Gradient))
                    {
                        r = v.Type != UIColorType.Gradient;
                        if (r)
                            v = new UIGradientColor(UIGradientType.Linear, v.Solid);

                        v.Type = UIColorType.Gradient;
                    }

                    ImGui.EndCombo();
                }

                if (v.Type == UIColorType.Solid)
                {
                    r = ImGui.ColorPicker4("#ED"u8, ref Unsafe.As<Color, float>(ref v.Solid)) || r;
                }
                else
                {
                    pos = ImGui.GetCursorScreenPos();
                    avail = ImGui.GetContentRegionAvail();

                    ImGui.Dummy(new Vector2(avail.X, 20.0f));

                    drawList = ImGui.GetWindowDrawList();
                    drawList.AddRect(pos, pos + new Vector2(avail.X, 20.0f), new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR);

                    Vector2 from = pos + Vector2.One;
                    Vector2 to = pos + new Vector2(avail.X - 1.0f, 19.0f);

                    ref UIGradientColor gradient = ref v.Gradient;
                    for (int i = 0; i < gradient.Keys.Length - 1; ++i)
                    {
                        UIGradientKey keyNow = gradient.Keys[i];
                        UIGradientKey keyNext = gradient.Keys[i + 1];

                        uint color1 = new Color32(keyNow.Color.AsVector4()).ABGR;
                        uint color2 = new Color32(keyNext.Color.AsVector4()).ABGR;

                        drawList.AddRectFilledMultiColor(new Vector2(float.Lerp(from.X, to.X, keyNow.Time), from.Y), new Vector2(float.Lerp(from.X, to.X, keyNext.Time), to.Y), color1, color2, color2, color1);
                    }

                    if (ImGui.BeginChild("##KS", ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        for (int i = 0; i < gradient.Keys.Length; i++)
                        {
                            ref UIGradientKey key = ref gradient.Keys[i];

                            ImGui.PushID(i);
                            if (ImGui.BeginChild("##CH"u8, ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                            {
                                r = ImGui.DragFloat("##TM"u8, ref key.Time, 0.01f, 0.0f, 1.0f) || r;
                                r = ImGui.ColorPicker4("#ED"u8, ref Unsafe.As<Color, float>(ref key.Color)) || r;

                                ImGui.NewLine();

                                if (gradient.Keys.Length > 2 && ImGui.Button("Remove"u8))
                                {
                                    List<UIGradientKey> keyList = [.. gradient.Keys];
                                    keyList.RemoveAt(i);

                                    gradient.Keys = keyList.ToArray();
                                    --i;
                                    r = true;
                                }

                                ImGui.SameLine();

                                if (ImGui.Button("Add<"u8))
                                {
                                    List<UIGradientKey> keyList = [.. gradient.Keys];

                                    float time = i > 0 ? float.Lerp(gradient.Keys[i - 1].Time, key.Time, 0.5f) : float.Lerp(0.0f, key.Time, 0.5f);
                                    keyList.Insert(i, new UIGradientKey(time, gradient.Sample(time)));

                                    gradient.Keys = keyList.ToArray();
                                    r = true;
                                }

                                ImGui.SameLine();

                                if (ImGui.Button("Add>"u8))
                                {
                                    List<UIGradientKey> keyList = [.. gradient.Keys];

                                    float time = i < gradient.Keys.Length - 1 ? float.Lerp(key.Time, gradient.Keys[i + 1].Time, 0.5f) : float.Lerp(key.Time, 1.0f, 0.5f);
                                    keyList.Insert(i + 1, new UIGradientKey(time, gradient.Sample(time)));

                                    gradient.Keys = keyList.ToArray();
                                    r = true;
                                }
                            }
                            ImGui.EndChild();
                            ImGui.PopID();

                            ImGui.SameLine();
                        }
                    }
                    ImGui.EndChild();
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
           
            return r;
        }

        public static bool ComboBox(ReadOnlySpan<byte> fmt, ref int v, string[] values)
        {
            bool r = false;

            if (ImGui.BeginCombo(fmt, values[v]))
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    if (ImGui.Selectable(values[i], i == v))
                    {
                        r = i != v;
                        v = i;
                    }
                }

                ImGui.EndCombo();
            }

            return r;
        }
    }
}
