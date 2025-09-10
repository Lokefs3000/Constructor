using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.DearImGui
{
    internal sealed class RHIInspector : RHI.IObjectTracker
    {
        private ObjectType _showTypes;

        private int _objectImageSize;

        private List<TrackedObject> _objects;
        private int[] _objectCounters;

        internal RHIInspector()
        {
            _showTypes = ObjectType.All;

            _objectImageSize = 128;

            _objects = new List<TrackedObject>();
            _objectCounters = new int[4];

            RenderingManager.Device.InstallTracker(this);
        }

        internal void Render()
        {
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

            static unsafe void DrawIcon(ImGuiContextPtr context, ImDrawListPtr drawList, ref Vector2 cursor, float size, float paddedSize, ref TrackedObject @object)
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
                    switch (@object.Type)
                    {
                        case ObjectType.Buffer:
                            {
                                RHI.Buffer buffer = Unsafe.As<RHI.Buffer>(@object.Resource);

                                ImGui.TextUnformatted(@object.Name);
                                ImGui.TextUnformatted(FileUtility.FormatSize(buffer.Description.Stride, "F4", CultureInfo.InvariantCulture));
                                ImGui.TextUnformatted(FileUtility.FormatSize(buffer.Description.ByteWidth, "F4", CultureInfo.InvariantCulture));
                                break;
                            }
                        case ObjectType.Texture:
                            {
                                RHI.Texture texture = Unsafe.As<RHI.Texture>(@object.Resource);
                                if (FlagUtility.HasFlag(texture.Description.Usage, RHI.TextureUsage.ShaderResource))
                                {
                                    uint maxSize = Math.Max(texture.Description.Width, texture.Description.Height);
                                    float downscale = MathF.Min(256.0f / maxSize, 1.0f);

                                    ImGui.Image(ImGuiUtility.GetTextureRef(texture.Handle), new Vector2(texture.Description.Width, texture.Description.Height) * downscale);
                                }

                                ImGui.TextUnformatted(@object.Name);
                                ImGui.TextUnformatted($"{texture.Description.Width}x{texture.Description.Height}x{texture.Description.Depth}");
                                ImGui.TextUnformatted($"{texture.Description.Format}");
                                ImGui.TextUnformatted($"{texture.Description.MipLevels}");
                                ImGui.TextUnformatted(FileUtility.FormatSize(RHI.FormatStatistics.Query(texture.Description.Format).CalculateSize(texture.Description.Width, texture.Description.Height, texture.Description.Depth) * (long)texture.Description.MipLevels, "F4", CultureInfo.InvariantCulture));

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

            _objects.Add(new TrackedObject(type, resource, "null"));
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

        private record struct TrackedObject(ObjectType Type, RHI.Resource Resource, string Name);
    }
}
