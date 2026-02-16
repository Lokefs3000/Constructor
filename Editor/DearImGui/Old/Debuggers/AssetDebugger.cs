using CircularBuffer;
using Editor.Assets;
using Hexa.NET.ImGui;

namespace Editor.DearImGui.Debuggers
{
    internal sealed class AssetDebugger : IVisualDebugger
    {
        private CircularBuffer<EventStorage> _eventBuffer;

        internal AssetDebugger()
        {
            _eventBuffer = new CircularBuffer<EventStorage>(32);

            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
            pipeline.NewFilesystemEvent += (fs, @ev) =>
            {
                _eventBuffer.PushFront(new EventStorage(ev.LocalPath, ev.NewLocalPath, ev.Type, DateTime.Now, fs.Namespace));
            };
        }

        public void Render()
        {
            if (ImGui.CollapsingHeader("Events"u8))
            {
                ImGui.BeginTable("EV"u8, 5, ImGuiTableFlags.Borders);

                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Local path"u8);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("New local path"u8);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextUnformatted("Event type"u8);

                    ImGui.TableSetColumnIndex(3);
                    ImGui.TextUnformatted("Date"u8);

                    ImGui.TableSetColumnIndex(4);
                    ImGui.TextUnformatted("Namespace"u8);
                }

                foreach (EventStorage storage in _eventBuffer)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(storage.LocalPath);

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted(storage.LocalPath);
                        ImGui.EndTooltip();
                    }

                    ImGui.TableSetColumnIndex(1);
                    if (storage.NewLocalPath == null)
                        ImGui.TextUnformatted("null"u8);
                    else
                    {
                        ImGui.TextUnformatted(storage.NewLocalPath);

                        if (ImGui.BeginItemTooltip())
                        {
                            ImGui.TextUnformatted(storage.NewLocalPath);
                            ImGui.EndTooltip();
                        }
                    }

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextUnformatted(storage.EventType.ToString());

                    ImGui.TableSetColumnIndex(3);
                    ImGui.TextUnformatted(storage.Time.ToString());

                    ImGui.TableSetColumnIndex(4);
                    ImGui.TextUnformatted(storage.FilesystemName);
                }

                ImGui.EndTable();
            }
        }

        public VisualDebuggerType DebuggerType => VisualDebuggerType.Editor;
        public ReadOnlySpan<byte> DebuggerName => "Assets"u8;

        private readonly record struct EventStorage(string LocalPath, string? NewLocalPath, FilesystemEventType EventType, DateTime Time, string FilesystemName);
    }
}
