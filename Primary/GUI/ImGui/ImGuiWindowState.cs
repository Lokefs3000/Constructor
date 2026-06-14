using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiWindowState
    {
        public ImGuiWindowFlags WindowFlags { get; set; }

        public int Id { get; set; }
        public int Parent { get; set; }

        public string Title { get; set; }

        public int Layer { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }

        public bool IsNew { get; set; }
        public bool IsSameLine { get; set; }
        public ImGuiFlowDirection FlowDirection { get; set; }

        public Vector2 PrevCursorPos { get; set; }
        public Vector2 CursorPos { get; set; }
        public Vector2 MaxCursorPos { get; set; }

        public int IndentLevel { get; set; }

        public Vector2 Scroll { get; set; }

        public int LastItemId { get; set; }
        public Boundaries LastItemRect { get; set; }
        public ImGuiItemStateFlags LastItemFlags { get; set; }

        public ImGuiDrawList DrawList { get; set; }

        public ImGuiWindowState()
        {
            WindowFlags = ImGuiWindowFlags.None;

            Id = ImGuiStateController.InvalidId;
            Parent = ImGuiStateController.InvalidId;

            Title = string.Empty;

            Layer = 1;

            Position = new Vector2(100.0f);
            Size = new Vector2(100.0f);

            IsNew = true;
            IsSameLine = false;

            FlowDirection = ImGuiFlowDirection.Vertical;

            PrevCursorPos = Vector2.Zero;
            CursorPos = Vector2.Zero;
            MaxCursorPos = Vector2.Zero;

            IndentLevel = 0;

            Scroll = Vector2.Zero;

            LastItemId = 0;
            LastItemRect = Boundaries.Zero;
            LastItemFlags = ImGuiItemStateFlags.None;

            DrawList = null!;
        }

        internal void ResetState()
        {
            PrevCursorPos = Vector2.Zero;
            CursorPos = Vector2.Zero;
            MaxCursorPos = Vector2.Zero;

            IndentLevel = 0;

            LastItemId = 0;
            LastItemRect = Boundaries.Zero;
            LastItemFlags = ImGuiItemStateFlags.None;

            DrawList = null!;
        }
    }

    public enum ImGuiWindowFlags : ushort
    {
        None = 0,

        AlwaysOnTop = 1 << 1,
        AlwaysResize = 1 << 2,

        DontBringToFrontOnFocus = 1 << 3,

        NoTitlebar = 1 << 4,
        NoBackground = 1 << 5,

        NoResize = 1 << 6,
        NoMove = 1 << 7,

        Tooltip = (1 << 0) | AlwaysOnTop | AlwaysResize,
        Child = 1 << 8
    }

    public enum ImGuiItemStateFlags : byte
    {
        None = 0,

        Hovered = 1 << 0,
        Held = 1 << 1
    }

    public enum ImGuiFlowDirection : byte
    {
        Vertical = 0,
        Horizontal
    }

    public enum ImGuiChildFlags : byte
    {
        None = 0,

        Borders = 1 << 1,
        AlwaysResize = 1 << 2,
    }
}
