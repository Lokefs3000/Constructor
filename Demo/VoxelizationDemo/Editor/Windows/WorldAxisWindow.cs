using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Extensions;
using Primary.Input;
using Primary.Mathematics;
using Primary.Windowing;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class WorldAxisWindow : IEditorWindow
    {
        public bool UpdateAndRender()
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;

            {
                const float Padding = 8.0f;

                Vector2 screenSize = runtime.WindowManager.PrimaryWindow!.ClientSize.AsVector2();
                Vector2 widgetSize = new Vector2(100.0f);

                ImGui.SetNextWindowPos(new Vector2(screenSize.X - widgetSize.X - Padding, Padding));
                ImGui.SetNextWindowSize(widgetSize);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            bool drawWindowInternal = ImGui.Begin("##AXIS"u8, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing);
            ImGui.PopStyleVar();

            if (drawWindowInternal)
            {
                ImGuiWindowPtr window = ImGuiP.GetCurrentWindow();
                ImDrawListPtr drawList = window.DrawList;

                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                Vector2 cursorPos = window.DC.CursorPos + contentAvail * 0.5f;

                Matrix4x4 mat = Matrix4x4.CreateLookTo(Vector3.Zero, Vector3.Transform(Vector3.UnitZ, runtime.CameraManager.ViewQuaternion), Vector3.Transform(Vector3.UnitY, runtime.CameraManager.ViewQuaternion));
                Vector3 posAxisX = Vector3.Transform(Vector3.UnitX, mat);
                Vector3 posAxisY = Vector3.Transform(Vector3.UnitY, mat);
                Vector3 posAxisZ = Vector3.Transform(Vector3.UnitZ, mat);

                Vector3 negAxisX = Vector3.Transform(-Vector3.UnitX, mat);
                Vector3 negAxisY = Vector3.Transform(-Vector3.UnitY, mat);
                Vector3 negAxisZ = Vector3.Transform(-Vector3.UnitZ, mat);

                Vector4 contentAvailVec4 = Vector4.Shuffle(contentAvail.AsVector4Unsafe(), 0, 1, 0, 1) * 0.5f - new Vector4(9.0f);
                Vector4 cursorPosVec4 = Vector4.Shuffle(cursorPos.AsVector4Unsafe(), 0, 1, 0, 1);

                Vector4 axisXEnds = new Vector4(posAxisX.X, -posAxisX.Y, negAxisX.X, -negAxisX.Y) * contentAvailVec4 + cursorPosVec4;
                Vector4 axisYEnds = new Vector4(posAxisY.X, -posAxisY.Y, negAxisY.X, -negAxisY.Y) * contentAvailVec4 + cursorPosVec4;
                Vector4 axisZEnds = new Vector4(posAxisZ.X, -posAxisZ.Y, negAxisZ.X, -negAxisZ.Y) * contentAvailVec4 + cursorPosVec4;

                {
                    s_axisOrder[0] = SelectorAxis.XPositive;
                    s_axisOrder[1] = SelectorAxis.XNegative;
                    s_axisOrder[2] = SelectorAxis.YPositive;
                    s_axisOrder[3] = SelectorAxis.YNegative;
                    s_axisOrder[4] = SelectorAxis.ZPositive;
                    s_axisOrder[5] = SelectorAxis.ZNegative;

                    s_axisDepths[0] = posAxisX.Z;
                    s_axisDepths[1] = negAxisX.Z;
                    s_axisDepths[2] = posAxisY.Z;
                    s_axisDepths[3] = negAxisY.Z;
                    s_axisDepths[4] = posAxisZ.Z;
                    s_axisDepths[5] = negAxisZ.Z;

                    s_axisDepths.Sort(s_axisOrder);
                }

                if (ImGui.IsWindowHovered() && Vector2.Distance(InputSystem.Pointer.MousePosition, cursorPos) < contentAvailVec4.X + 8.0f)
                {
                    drawList.AddCircleFilled(cursorPos, contentAvailVec4.X + 8.0f, 0x40ffffff);
                }

                for (int i = 0; i < 6; ++i)
                {
                    SelectorAxis axis = s_axisOrder[i];
                    Vector2 endPosition = axis switch
                    {
                        SelectorAxis.XPositive => axisXEnds.GetLower(),
                        SelectorAxis.XNegative => axisXEnds.GetUpper(),
                        SelectorAxis.YPositive => axisYEnds.GetLower(),
                        SelectorAxis.YNegative => axisYEnds.GetUpper(),
                        SelectorAxis.ZPositive => axisZEnds.GetLower(),
                        SelectorAxis.ZNegative => axisZEnds.GetUpper(),
                        _ => Vector2.Zero
                    };

                    uint color = (SelectorAxis)((int)axis / 2 * 2) switch
                    {
                        SelectorAxis.XPositive => 0xff443ce6,
                        SelectorAxis.YPositive => 0xff3ce683,
                        SelectorAxis.ZPositive => 0xffe6553c,
                        _ => 0xffff00ff
                    };

                    uint id = ImGui.GetID((int)axis);
                    ImRect bb = new ImRect(endPosition - new Vector2(8.0f), endPosition + new Vector2(8.0f));

                    bool isHovered = false;
                    bool isHeld = false;
                    if (ImGuiP.ButtonBehavior(bb, id, ref isHovered, ref isHeld))
                    {
                        
                    }

                    uint outlineColor = 0;
                    if (((int)axis) % 2 == 0)
                    {
                        drawList.AddLine(cursorPos, endPosition, color);
                        drawList.AddCircleFilled(endPosition, 8.0f, color);

                        const float HalfThickness = 1.0f;

                        switch (axis)
                        {
                            case SelectorAxis.XPositive:
                                {
                                    Vector2 tl = endPosition - new Vector2(3.0f, 4.0f);
                                    Vector2 br = endPosition + new Vector2(3.0f, 4.0f);

                                    drawList.AddQuadFilled(
                                        tl.WithElement(0, tl.X - HalfThickness), tl.WithElement(0, tl.X + HalfThickness),
                                        br.WithElement(0, br.X + HalfThickness), br.WithElement(0, br.X - HalfThickness),
                                        0xff000000);

                                    drawList.AddQuadFilled(
                                        tl.WithElement(0, br.X - HalfThickness), tl.WithElement(0, br.X + HalfThickness),
                                        br.WithElement(0, tl.X + HalfThickness), br.WithElement(0, tl.X - HalfThickness),
                                        0xff000000);

                                    break;
                                }
                            case SelectorAxis.YPositive:
                                {
                                    Vector2 tl = endPosition - new Vector2(3.0f, 4.0f);
                                    Vector2 br = endPosition + new Vector2(3.0f, 4.0f);
                                    Vector2 joint = endPosition.WithElement(1, endPosition.Y + 1.0f);

                                    drawList.AddQuadFilled(
                                        tl.WithElement(0, tl.X - HalfThickness), tl.WithElement(0, tl.X + HalfThickness),
                                        joint.WithElement(0, joint.X + HalfThickness), joint.WithElement(0, joint.X - HalfThickness),
                                        0xff000000);

                                    drawList.AddQuadFilled(
                                        tl.WithElement(0, br.X - HalfThickness), tl.WithElement(0, br.X + HalfThickness),
                                        joint.WithElement(0, joint.X + HalfThickness), joint.WithElement(0, joint.X - HalfThickness),
                                        0xff000000);

                                    drawList.AddRectFilled(joint.WithElement(0, joint.X - HalfThickness), br.WithElement(0, joint.X + HalfThickness), 0xff000000);

                                    break;
                                }
                            case SelectorAxis.ZPositive:
                                {
                                    Vector2 tl = endPosition - new Vector2(3.0f, 4.0f);
                                    Vector2 br = endPosition + new Vector2(3.0f, 4.0f);

                                    drawList.AddRectFilled(tl.WithElement(0, tl.X - HalfThickness), new Vector2(br.X + HalfThickness, tl.Y + HalfThickness * 2.0f), 0xff000000);
                                    drawList.AddRectFilled(new Vector2(tl.X - HalfThickness, br.Y - HalfThickness * 2.0f), br.WithElement(0, br.X + HalfThickness), 0xff000000);

                                    tl.Y += HalfThickness * 2.0f;
                                    br.Y -= HalfThickness * 2.0f;

                                    drawList.AddQuadFilled(
                                        tl.WithElement(0, br.X - HalfThickness), tl.WithElement(0, br.X + HalfThickness),
                                        br.WithElement(0, tl.X + HalfThickness), br.WithElement(0, tl.X - HalfThickness),
                                        0xff000000);

                                    break;
                                }
                        }
                    }
                    else
                    {
                        outlineColor = color;
                        drawList.AddCircleFilled(endPosition, 8.0f, (color & 0x00ffffff) | 0x40000000);
                    }

                    if (isHovered || isHeld)
                    {
                        const byte BrightnessIncrease = 50;

                        Color32 c32 = new Color32(color, true);
                        if (c32.R > c32.G && c32.R > c32.B)
                        {
                            c32.G += BrightnessIncrease;
                            c32.B += BrightnessIncrease;
                        }
                        else if (c32.G > c32.B)
                        {
                            c32.R += BrightnessIncrease;
                            c32.B += BrightnessIncrease;
                        }
                        else
                        {
                            c32.R += BrightnessIncrease;
                            c32.G += BrightnessIncrease;
                        }

                        outlineColor = 0xffffffff;
                    }

                    if (outlineColor != 0)
                        drawList.AddCircle(endPosition, 8.0f, outlineColor);
                }
            }

            ImGui.End();
            return true;
        }

        // Stackalloc is kinda annoying with hot reload which i heavily depend on for iteration speed
        private static readonly SelectorAxis[] s_axisOrder = new SelectorAxis[6];
        private static readonly float[] s_axisDepths = new float[6];

        private enum SelectorAxis : byte
        {
            XPositive = 0,
            XNegative,
            YPositive,
            YNegative,
            ZPositive,
            ZNegative
        }
    }
}
