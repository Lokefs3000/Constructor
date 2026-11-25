using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

namespace Primary.Rendering.Partitioning
{
    public record struct RenderableChunk : IDisposable
    {
        private readonly ArrayPool<Renderable> _arrayPool;

        private readonly Renderable[] _entities;
        private int _count;

        public RenderableChunk(ArrayPool<Renderable> arrayPool)
        {
            _arrayPool = arrayPool;

            _entities = arrayPool.Rent(16384 / Unsafe.SizeOf<Renderable>());
            _count = 0;
        }

        public void Dispose()
        {
            if (_count > 0)
                Array.Clear(_entities, 0, _count);

            _arrayPool.Return(_entities);
            _count = 0;
        }

        public ref Renderable Create(out int index)
        {
            Guard.IsLessThan(_count, _entities.Length, "Chunk is full");

            index = _count++;
            return ref _entities[index];
        }

        public void Destroy(int index, out int movedId)
        {
            Guard.IsGreaterThan(_count, 0, "Chunk is empty");

            --_count;
            if (_count == index)
            {
                _entities[index] = default;
                movedId = -1;
            }
            else
            {
                ref Renderable toMove = ref _entities[_count];
                movedId = toMove.Id;

                _entities[index] = toMove;
                toMove = default;
            }
        }

        public readonly void DestroyAndTake(int index, ref RenderableChunk chunkToTakeFrom, out int movedId)
        {
            Guard.IsGreaterThan(_count, 0, "Chunk is empty");
            Guard.IsGreaterThan(chunkToTakeFrom._count, 0, "Chunk to take from is empty");

            ref Renderable toMove = ref chunkToTakeFrom._entities[--chunkToTakeFrom._count];
            movedId = toMove.Id;

            _entities[index] = toMove;
            toMove = default;
        }

        public readonly ref Renderable this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _entities.Length);
                return ref _entities.DangerousGetReferenceAt(index);
            }
        }

        public readonly Span<Renderable> Renderables => _entities.AsSpan(0, _count);
        public readonly int Count => _count;

        public readonly bool IsEmpty => _count == 0;
        public readonly bool IsFull => _count >= _entities.Length;
    }
}
