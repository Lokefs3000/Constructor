using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering;
using Primary.Timing;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.DearImGui
{
    internal sealed class RHIInspector : RHI.IObjectTracker
    {
        private TextureAsset _noPreview;

        private ObjectType _showTypes;

        private int _objectImageSize;

        private List<TrackedObject> _objects;
        private int[] _objectCounters;

        internal RHIInspector()
        {
            _noPreview = AssetManager.LoadAsset<TextureAsset>("Editor/Textures/NoPreview.png")!;

            _showTypes = ObjectType.All;

            _objectImageSize = 128;

            _objects = new List<TrackedObject>();
            _objectCounters = new int[4];

            RenderingManager.Device.InstallTracker(this);
        }

        internal void Render()
        {
            long timestampLimit = Stopwatch.Frequency * 4;
            long currentTimestamp = Stopwatch.GetTimestamp();

            if (ImGui.Begin("RHI inspector", ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Show"))
                    {
                        if (ImGui.MenuItem($"Buffer ({_objectCounters[0]})", FlagUtility.HasFlag(_showTypes, ObjectType.Buffer)))
                            _showTypes ^= ObjectType.Buffer;

                        if (ImGui.MenuItem($"Texture ({_objectCounters[1]})", FlagUtility.HasFlag(_showTypes, ObjectType.Texture)))
                            _showTypes ^= ObjectType.Texture;

                        if (ImGui.MenuItem($"RenderTarget ({_objectCounters[2]})", FlagUtility.HasFlag(_showTypes, ObjectType.RenderTarget)))
                            _showTypes ^= ObjectType.RenderTarget;

                        if (ImGui.MenuItem($"GraphicsPipeline ({_objectCounters[3]})", FlagUtility.HasFlag(_showTypes, ObjectType.GraphicsPipeline)))
                            _showTypes ^= ObjectType.GraphicsPipeline;

                        ImGui.EndMenu();
                    }

                    int count = _objectImageSize / 32;
                    if (ImGui.SliderInt("##SCALE", ref count, 0, 256 / 32))
                        _objectImageSize = count * 32;

                    ImGui.EndMenuBar();
                }

                if (ImGui.BeginChild("##SUB", ImGuiChildFlags.Borders))
                {
                    Vector2 screenCursor = ImGui.GetCursorScreenPos();
                    Vector2 contentAvail = ImGui.GetContentRegionAvail();

                    ImGuiContextPtr context = ImGui.GetCurrentContext();
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    float paddedSize = _objectImageSize + context.Style.FramePadding.X;

                    Vector2 cursor = screenCursor;

                    if (_objectImageSize > 0)
                    {

                        Span<TrackedObject> objects = _objects.AsSpan();
                        for (int i = 0; i < objects.Length; i++)
                        {
                            ref TrackedObject @object = ref objects[i];
                            if (!FlagUtility.HasEither(@object.Type, _showTypes))
                                continue;

                            DrawIcon(context, drawList, ref cursor, _objectImageSize, paddedSize, ref @object);

                            if (cursor.X + paddedSize > contentAvail.X + screenCursor.X)
                            {
                                cursor.X = screenCursor.X;
                                cursor.Y += paddedSize;
                            }
                            else
                                ImGui.SameLine();
                        }
                    }
                    else
                    {
                        bool colorToggle = false;

                        float indvSize = context.FontSize + context.Style.FramePadding.Y * 2.0f;
                        float sizeDispXPos = 500.0f;

                        Span<TrackedObject> objects = _objects.AsSpan();
                        for (int i = 0; i < objects.Length; i++)
                        {
                            ref TrackedObject @object = ref objects[i];
                            if (!FlagUtility.HasEither(@object.Type, _showTypes))
                                continue;

                            if (colorToggle)
                                drawList.AddRectFilled(cursor, cursor + new Vector2(contentAvail.X, indvSize), 0x10ffffff);

                            drawList.AddText(cursor + context.Style.FramePadding, 0xffffffff, @object.Name);

                            switch (@object.Type)
                            {
                                case ObjectType.Buffer:
                                    {
                                        RHI.Buffer buffer = Unsafe.As<RHI.Buffer>(@object.Resource);
                                        drawList.AddText(cursor + context.Style.FramePadding + new Vector2(sizeDispXPos, 0.0f), 0xffffffff, FileUtility.FormatSize(buffer.Description.ByteWidth, "F4", CultureInfo.InvariantCulture));
                                        break;
                                    }
                                case ObjectType.Texture:
                                    {
                                        RHI.Texture texture = Unsafe.As<RHI.Texture>(@object.Resource);
                                        drawList.AddText(cursor + context.Style.FramePadding + new Vector2(sizeDispXPos, 0.0f), 0xffffffff, FileUtility.FormatSize(RHI.FormatStatistics.Query(texture.Description.Format).CalculateSize(texture.Description.Width, texture.Description.Height, texture.Description.Depth) * (long)texture.Description.MipLevels, "F4", CultureInfo.InvariantCulture));
                                        break;
                                    }
                                case ObjectType.RenderTarget:
                                    break;
                                case ObjectType.GraphicsPipeline:
                                    break;
                                case ObjectType.All:
                                    break;
                                default:
                                    break;
                            }

                            cursor.Y += indvSize;
                            colorToggle = !colorToggle;
                        }

                        ImGui.Dummy(new Vector2(0, indvSize * objects.Length));
                    }
                }
                ImGui.EndChild();
            }
            ImGui.End();

            unsafe void DrawIcon(ImGuiContextPtr context, ImDrawListPtr drawList, ref Vector2 cursor, float size, float paddedSize, ref TrackedObject @object)
            {
                uint id = ImGui.GetID(@object.Resource.Handle.GetHashCode());

                ImRect bb = new ImRect(cursor, cursor + new Vector2(size));

                Vector2 textSize = ImGui.CalcTextSize(@object.Name);

                bool hovered, held;
                if (ImGuiP.ButtonBehavior(bb, id, &hovered, &held))
                {

                }

                ImGuiP.ItemAdd(bb, id);
                ImGuiP.ItemSize(bb);

                if (ImGui.BeginItemTooltip())
                {
                    ImDrawListPtr tooltipDrawList = ImGui.GetWindowDrawList();
                    ImGuiStylePtr style = ImGui.GetStyle();

                    Vector2 screenCursor = ImGui.GetCursorScreenPos();

                    switch (@object.Type)
                    {
                        case ObjectType.Buffer:
                            {
                                RHI.Buffer buffer = Unsafe.As<RHI.Buffer>(@object.Resource);

                                Vector2 sidePosition = screenCursor;
                                float height = context.FontSize + style.FramePadding.Y;

                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Name:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Byte width:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Stride:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Memory:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Usage:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Mode:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "CPU:"u8); sidePosition.Y += height;

                                sidePosition = screenCursor + new Vector2(style.FramePadding.X + ImGui.CalcTextSize("Byte width:"u8).X, 0.0f);

                                string byteWidthFormatted = FileUtility.FormatSize(buffer.Description.ByteWidth, "G", CultureInfo.InvariantCulture);
                                string strideFormatted = FileUtility.FormatSize(buffer.Description.Stride, "G", CultureInfo.InvariantCulture);

                                tooltipDrawList.AddText(sidePosition, 0xffffffff, @object.Name); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, byteWidthFormatted); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, strideFormatted); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, buffer.Description.Memory.ToString()); sidePosition.Y += height;

                                RHI.BufferUsage usage = buffer.Description.Usage;
                                if (usage == RHI.BufferUsage.None)
                                {
                                    tooltipDrawList.AddText(sidePosition, 0xffffffff, "None"u8);
                                    sidePosition.Y += height;
                                }
                                else
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        RHI.BufferUsage val = (RHI.BufferUsage)(1 << i);
                                        if (FlagUtility.HasFlag(usage, val))
                                        {
                                            tooltipDrawList.AddText(sidePosition, 0xffffffff, val.ToString());
                                            sidePosition.Y += height;
                                        }
                                    }
                                }

                                RHI.BufferMode mode = buffer.Description.Mode;
                                if (mode == RHI.BufferMode.None)
                                {
                                    tooltipDrawList.AddText(sidePosition, 0xffffffff, "None"u8);
                                    sidePosition.Y += height;
                                }
                                else
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        RHI.BufferMode val = (RHI.BufferMode)(1 << i);
                                        if (FlagUtility.HasFlag(mode, val))
                                        {
                                            tooltipDrawList.AddText(sidePosition, 0xffffffff, val.ToString());
                                            sidePosition.Y += height;
                                        }
                                    }
                                }

                                switch (buffer.Description.CpuAccessFlags)
                                {
                                    case RHI.CPUAccessFlags.None: tooltipDrawList.AddText(sidePosition, 0xffffffff, "None"u8); sidePosition.Y += height; break;
                                    case RHI.CPUAccessFlags.Write: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Write"u8); sidePosition.Y += height; break;
                                    case RHI.CPUAccessFlags.Read: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Read"u8); sidePosition.Y += height; break;
                                    case (RHI.CPUAccessFlags)3: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Read/Write"u8); sidePosition.Y += height; break;
                                }

                                Vector2 dummySize = new Vector2(sidePosition.X - screenCursor.X + ImGui.CalcTextSize(@object.Name).X, sidePosition.Y - screenCursor.Y);
                                ImGui.Dummy(dummySize);

                                break;
                            }
                        case ObjectType.Texture:
                            {
                                RHI.Texture texture = Unsafe.As<RHI.Texture>(@object.Resource);
                                bool shaderVisible = FlagUtility.HasFlag(texture.Description.Usage, RHI.TextureUsage.ShaderResource);

                                Vector2 imageSize = new Vector2(256.0f);

                                if (shaderVisible)
                                {
                                    uint maxSize = Math.Max(texture.Description.Width, texture.Description.Height);
                                    float downscale = MathF.Min(256.0f / maxSize, 1.0f);

                                    imageSize = new Vector2(texture.Description.Width, texture.Description.Height) * downscale;
                                }

                                tooltipDrawList.AddRect(screenCursor, screenCursor + imageSize + new Vector2(2.0f), new Color32(style.Colors[(int)ImGuiCol.Border]).ARGB);

                                if (shaderVisible)
                                    tooltipDrawList.AddImage(ImGuiUtility.GetTextureRef(texture.Handle), screenCursor + Vector2.One, screenCursor + imageSize + Vector2.Zero);
                                else if (_noPreview.Status == ResourceStatus.Success)
                                    tooltipDrawList.AddImage(ImGuiUtility.GetTextureRef(_noPreview.Handle), screenCursor + Vector2.One, screenCursor + imageSize + Vector2.Zero);

                                Vector2 sidePosition = screenCursor + new Vector2(imageSize.X + 2.0f + style.FramePadding.X, 0.0f);
                                float height = context.FontSize + style.FramePadding.Y;

                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Name:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Size:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Mip levels:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Dimension:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Format:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Memory:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Usage:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "CPU:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Swizzle:"u8); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, "Total memory:"u8); sidePosition.Y += height;

                                sidePosition = screenCursor + new Vector2(imageSize.X + 2.0f + style.FramePadding.X * 2.0f + ImGui.CalcTextSize("Total memory:"u8).X, 0.0f);

                                RHI.FormatInfo fi = RHI.FormatStatistics.Query(texture.Description.Format);
                                string formattedSize = FileUtility.FormatSize(fi.CalculateSize(texture.Description.Width, texture.Description.Height, texture.Description.Depth) * (long)texture.Description.MipLevels, "G", CultureInfo.InvariantCulture);

                                string formatText = $"{texture.Description.Format} (bc:{fi.IsBlockCompressed}, ch:{fi.ChannelCount}, width:{(fi.IsBlockCompressed ? fi.BlockWidth : fi.BytesPerPixel)})";

                                tooltipDrawList.AddText(sidePosition, 0xffffffff, @object.Name); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, $"{texture.Description.Width}x{texture.Description.Height}x{texture.Description.Depth}"); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, texture.Description.MipLevels.ToString()); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, texture.Description.Dimension.ToString()); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, formatText); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, texture.Description.Memory.ToString()); sidePosition.Y += height;

                                RHI.TextureUsage usage = texture.Description.Usage;
                                if (usage == RHI.TextureUsage.None)
                                {
                                    tooltipDrawList.AddText(sidePosition, 0xffffffff, "None"u8);
                                    sidePosition.Y += height;
                                }
                                else
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        RHI.TextureUsage val = (RHI.TextureUsage)(1 << i);
                                        if (FlagUtility.HasFlag(usage, val))
                                        {
                                            tooltipDrawList.AddText(sidePosition, 0xffffffff, val.ToString());
                                            sidePosition.Y += height;
                                        }
                                    }
                                }

                                switch (texture.Description.CpuAccessFlags)
                                {
                                    case RHI.CPUAccessFlags.None: tooltipDrawList.AddText(sidePosition, 0xffffffff, "None"u8); sidePosition.Y += height; break;
                                    case RHI.CPUAccessFlags.Write: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Write"u8); sidePosition.Y += height; break;
                                    case RHI.CPUAccessFlags.Read: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Read"u8); sidePosition.Y += height; break;
                                    case (RHI.CPUAccessFlags)3: tooltipDrawList.AddText(sidePosition, 0xffffffff, "Read/Write"u8); sidePosition.Y += height; break;
                                }

                                tooltipDrawList.AddText(sidePosition, 0xffffffff, $"{texture.Description.Swizzle.R.ToString()},{texture.Description.Swizzle.G.ToString()},{texture.Description.Swizzle.B.ToString()},{texture.Description.Swizzle.A.ToString()}"); sidePosition.Y += height;
                                tooltipDrawList.AddText(sidePosition, 0xffffffff, formattedSize); sidePosition.Y += height;

                                Vector2 dummySize = new Vector2(sidePosition.X - screenCursor.X + ImGui.CalcTextSize(formatText).X, MathF.Max(imageSize.Y + 2.0f, sidePosition.Y - screenCursor.Y));
                                ImGui.Dummy(dummySize);
                                break;
                            }
                        case ObjectType.RenderTarget:
                            break;
                        case ObjectType.GraphicsPipeline:
                            break;
                        case ObjectType.All:
                            break;
                        default:
                            break;
                    }

                    ImGui.EndTooltip();
                }

                drawList.AddRectFilled(bb.Min, bb.Max, 0x10ffffff);
                drawList.AddText(cursor + new Vector2((size - textSize.X) * 0.5f, size - textSize.Y - context.Style.FramePadding.Y), 0xffffffff, @object.Name);

                Vector2 centerIcon = new Vector2(bb.Max.X - 12.0f, bb.Min.Y + 12.0f);
                long dist = Time.TimestampForActiveFrame - @object.CreationTimestamp;
                if (dist < timestampLimit)
                {
                    float timer = 1.0f - (float)(dist / (double)timestampLimit);
                    drawList.AddQuadFilled(centerIcon - new Vector2(4.0f, 0.0f), centerIcon - new Vector2(0.0f, 4.0f), centerIcon + new Vector2(4.0f, 0.0f), centerIcon + new Vector2(0.0f, 4.0f), new Color32(1.0f, 1.0f, 0.0f, timer).ABGR);
                }

                switch (@object.Type)
                {
                    case ObjectType.Texture:
                        {
                            RHI.Texture texture = Unsafe.As<RHI.Texture>(@object.Resource);
                            if (FlagUtility.HasFlag(texture.Description.Usage, RHI.TextureUsage.ShaderResource))
                            {
                                Boundaries availRegion = new Boundaries(
                                    cursor + context.Style.FramePadding,
                                    cursor - context.Style.FramePadding + new Vector2(size, size + -textSize.Y - context.Style.FramePadding.Y));
                                Boundaries drawRegion = Boundaries.Zero;

                                Vector2 center = availRegion.Center;
                                Vector2 sizeIndv = Vector2.Zero;
                                if (texture.Description.Width < texture.Description.Height)
                                {
                                    Vector2 totalSize = availRegion.Size;
                                    sizeIndv = new Vector2(totalSize.Y * (texture.Description.Width / (float)texture.Description.Height), totalSize.Y);
                                }
                                else
                                {
                                    Vector2 totalSize = availRegion.Size;
                                    sizeIndv = new Vector2(totalSize.Y, totalSize.Y * (texture.Description.Height / (float)texture.Description.Width));
                                }

                                Vector2 half = sizeIndv * 0.5f;
                                drawRegion = new Boundaries(center - half, center + half);

                                drawList.AddImage(ImGuiUtility.GetTextureRef(texture.Handle), drawRegion.Minimum, drawRegion.Maximum);
                            }

                            break;
                        }
                    case ObjectType.RenderTarget:
                        break;
                    case ObjectType.GraphicsPipeline:
                        break;
                    case ObjectType.All:
                        break;
                    default:
                        break;
                }

                cursor.X += paddedSize;
            }
        }

        public void ObjectCreated(RHI.Resource resource)
        {
            ObjectType type = ObjectType.None;
            if (resource is RHI.Buffer)
                type = ObjectType.Buffer;
            else if (resource is RHI.Texture)
                type = ObjectType.Texture;
            else if (resource is RHI.RenderTarget)
                type = ObjectType.RenderTarget;
            else if (resource is RHI.GraphicsPipeline)
                type = ObjectType.GraphicsPipeline;
            else
            {
                EdLog.Gui.Warning("RHI tracked resource has unknown type: {res}", resource);
                return;
            }

            _objects.Add(new TrackedObject(type, resource, "null", Time.TimestampForActiveFrame));
            _objectCounters[(int)type - 1]++;
        }

        public void ObjectDestroyed(RHI.Resource resource)
        {
            int idx = _objects.FindIndex((x) => x.Resource == resource);
            if (idx != -1)
            {
                _objectCounters[(int)_objects[idx].Type - 1]--;
                _objects.RemoveAt(idx);
            }
        }

        public void ObjectRenamed(RHI.Resource resource, string newName)
        {
            int idx = _objects.FindIndex((x) => x.Resource == resource);
            if (idx != -1)
            {
                Span<TrackedObject> objects = _objects.AsSpan();
                objects[idx].Name = newName == string.Empty ? $"{resource.GetType().Name}<{resource.Handle}>" : newName;
            }
        }

        private enum ObjectType : byte
        {
            None = 0,

            Buffer = 1 << 0,
            Texture = 1 << 1,
            RenderTarget = 1 << 2,
            GraphicsPipeline = 1 << 3,

            All = Buffer | Texture | RenderTarget | GraphicsPipeline,
        }

        private record struct TrackedObject(ObjectType Type, RHI.Resource Resource, string Name, long CreationTimestamp);
    }
}
