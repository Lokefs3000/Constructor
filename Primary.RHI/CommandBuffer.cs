using Primary.Common;

namespace Primary.RHI
{
    public abstract class CommandBuffer : IDisposable
    {
        public abstract string Name { set; }

        public abstract bool IsOpen { get; }
        public abstract bool IsReady { get; }

        public abstract CommandBufferType Type { get; }

        public abstract void Dispose();

        public abstract bool Begin();
        public abstract void End();

        public abstract nint Map(Buffer buffer, MapIntent intent, ulong writeSize = 0, ulong writeOffset = 0);
        public abstract nint Map(Texture texture, MapIntent intent, TextureLocation location, uint subresource = 0, uint rowPitch = 0);
        public abstract void Unmap(Resource resource);

        public abstract bool CopyBufferRegion(Buffer src, uint srcOffset, Buffer dst, uint dstOffset, uint size);
        public abstract bool CopyTextureRegion(Resource src, TextureLocation srcLoc, uint srcSubRes, Resource dst, TextureLocation dstLoc, uint dstSubRes);

        public abstract void BeginEvent(Color32 color, ReadOnlySpan<char> name);
        public abstract void EndEvent();
        public abstract void SetMarker(Color32 color, ReadOnlySpan<char> name);
    }

    public enum CommandBufferType : byte
    {
        Undefined = 0,
        Graphics,
        Compute,
        Copy
    }

    public struct TextureLocation
    {
        public int X;
        public int Y;
        public int Z;

        public uint Width;
        public uint Height;
        public uint Depth;
    }

    public struct MapRange
    {
        public uint Begin;
        public uint End;

        public MapRange(uint begin, uint end)
        {
            Begin = begin;
            End = end;
        }
    }

    public enum MapIntent : byte
    {
        Read = 0,
        Write,
        ReadWrite
    }

    public record struct CommandBufferEventScope : IDisposable
    {
        private CommandBuffer _commandBuffer;
        private bool _disposed;

        public CommandBufferEventScope(CommandBuffer commandBuffer, Color32 color, ReadOnlySpan<char> name)
        {
            _commandBuffer = commandBuffer;

            commandBuffer.BeginEvent(color, name);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _commandBuffer.EndEvent();
                _disposed = true;
            }
        }
    }
}
