using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiStyle
    {
        private Vector2[] _v2Styles;
        private Color32[] _colors;

        private Stack<KeyValuePair<ImGuiStyleVar, Vector2>> _v2StyleStack;
        private Stack<KeyValuePair<ImGuiColorIdx, Color32>> _colorStack;

        internal ImGuiStyle()
        {
            _v2Styles = new Vector2[4];
            _colors = new Color32[11];

            _v2StyleStack = new Stack<KeyValuePair<ImGuiStyleVar, Vector2>>();
            _colorStack = new Stack<KeyValuePair<ImGuiColorIdx, Color32>>();

            FramePadding = new Vector2(2.0f);
            WindowPadding = new Vector2(2.0f);
            ItemPadding = new Vector2(2.0f);
            InnerItemPadding = new Vector2(2.0f);

            WindowBg = new Color32(10, 10, 10, 100);
            WindowBorder = new Color32(63, 110, 161);
            TitlebarBg = new Color32(52, 100, 163);
            TitlebarActiveBg = new Color32(41, 121, 227);
            MenubarBg = new Color32(40, 46, 56);
            FrameBg = new Color32(47, 83, 122);
            FrameHoveredBg = new Color32(88, 138, 191);
            FrameActiveBg = new Color32(26, 67, 110);
            Border = new Color32(85, 108, 133);
            ChildBorder = new Color32(120, 120, 120, 120);
            Text = new Color32(255, 255, 255);
        }

        internal void Finish()
        {
            while (_v2StyleStack.Count > 0)
            {
                KeyValuePair<ImGuiStyleVar, Vector2> style = _v2StyleStack.Pop();
                _v2Styles[(int)style.Key] = style.Value;
            }

            while (_colorStack.Count > 0)
            {
                KeyValuePair<ImGuiColorIdx, Color32> color = _colorStack.Pop();
                _colors[(int)color.Key] = color.Value;
            }
        }

        public void PushStyleVar(ImGuiStyleVar var, Vector2 v)
        {
            _v2StyleStack.Push(new KeyValuePair<ImGuiStyleVar, Vector2>(var, _v2Styles[(int)var]));
            _v2Styles[(int)var] = v;
        }

        public void PopStyleVar(int count = 1)
        {
            int num = 0;
            while (num < count && _v2StyleStack.TryPop(out KeyValuePair<ImGuiStyleVar, Vector2> style))
            {
                _v2Styles[(int)style.Key] = style.Value;
            }
        }

        public void PushColor(ImGuiColorIdx col, Color32 v)
        {
            _colorStack.Push(new KeyValuePair<ImGuiColorIdx, Color32>(col, _colors[(int)col]));
            _colors[(int)col] = v;
        }

        public void PopColor(int count = 1)
        {
            int num = 0;
            while (num < count && _colorStack.TryPop(out KeyValuePair<ImGuiColorIdx, Color32> color))
            {
                _colors[(int)color.Key] = color.Value;
            }
        }

        public Vector2 FramePadding { get => _v2Styles[(int)ImGuiStyleVar.FramePadding]; set => _v2Styles[(int)ImGuiStyleVar.FramePadding] = value; }
        public Vector2 WindowPadding { get => _v2Styles[(int)ImGuiStyleVar.WindowPadding]; set => _v2Styles[(int)ImGuiStyleVar.WindowPadding] = value; }
        public Vector2 ItemPadding { get => _v2Styles[(int)ImGuiStyleVar.ItemPadding]; set => _v2Styles[(int)ImGuiStyleVar.ItemPadding] = value; }
        public Vector2 InnerItemPadding { get => _v2Styles[(int)ImGuiStyleVar.InnerItemPadding]; set => _v2Styles[(int)ImGuiStyleVar.InnerItemPadding] = value; }
    
        public Color32 WindowBg { get => _colors[(int)ImGuiColorIdx.WindowBg]; set => _colors[(int)ImGuiColorIdx.WindowBg] = value; }
        public Color32 WindowBorder { get => _colors[(int)ImGuiColorIdx.WindowBorder]; set => _colors[(int)ImGuiColorIdx.WindowBorder] = value; }
        public Color32 TitlebarBg { get => _colors[(int)ImGuiColorIdx.TitlebarBg]; set => _colors[(int)ImGuiColorIdx.TitlebarBg] = value; }
        public Color32 TitlebarActiveBg { get => _colors[(int)ImGuiColorIdx.TitlebarActiveBg]; set => _colors[(int)ImGuiColorIdx.TitlebarActiveBg] = value; }
        public Color32 MenubarBg { get => _colors[(int)ImGuiColorIdx.MenubarBg]; set => _colors[(int)ImGuiColorIdx.MenubarBg] = value; }
        public Color32 FrameBg { get => _colors[(int)ImGuiColorIdx.FrameBg]; set => _colors[(int)ImGuiColorIdx.FrameBg] = value; }
        public Color32 FrameHoveredBg { get => _colors[(int)ImGuiColorIdx.FrameHoveredBg]; set => _colors[(int)ImGuiColorIdx.FrameHoveredBg] = value; }
        public Color32 FrameActiveBg { get => _colors[(int)ImGuiColorIdx.FrameActiveBg]; set => _colors[(int)ImGuiColorIdx.FrameActiveBg] = value; }
        public Color32 Border { get => _colors[(int)ImGuiColorIdx.Border]; set => _colors[(int)ImGuiColorIdx.Border] = value; }
        public Color32 ChildBorder { get => _colors[(int)ImGuiColorIdx.ChildBorder]; set => _colors[(int)ImGuiColorIdx.ChildBorder] = value; }
        public Color32 Text { get => _colors[(int)ImGuiColorIdx.Text]; set => _colors[(int)ImGuiColorIdx.Text] = value; }
    }

    public enum ImGuiStyleVar : byte
    {
        FramePadding = 0,
        WindowPadding,
        ItemPadding,
        InnerItemPadding,
    }

    public enum ImGuiColorIdx : byte
    {
        WindowBg = 0,
        WindowBorder,

        TitlebarBg,
        TitlebarActiveBg,

        MenubarBg,

        FrameBg,
        FrameHoveredBg,
        FrameActiveBg,

        Border,
        ChildBorder,

        Text
    }
}
