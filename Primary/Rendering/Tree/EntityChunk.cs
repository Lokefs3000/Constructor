using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

namespace Primary.Rendering.Tree
{
    public record struct EntityChunk : IDisposable
    {
        private readonly ArrayPool<RenderableEntity> _arrayPool;

        private readonly RenderableEntity[] _entities;
        private int _count;

        public EntityChunk(ArrayPool<RenderableEntity> arrayPool)
        {
            _arrayPool = arrayPool;

            _entities = arrayPool.Rent(16384 / Unsafe.SizeOf<RenderableEntity>());
            _count = 0;
        }

        public void Dispose()
        {
            if (_count > 0)
                Array.Clear(_entities, 0, _count);

            _arrayPool.Return(_entities);
            _count = 0;
        }

        public ref RenderableEntity Create(out int index)
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
                _entities[index] = RenderableEntity.Null;
                movedId = -1;
            }
            else
            {
                ref RenderableEntity toMove = ref _entities[_count];
                movedId = toMove.Id;

                _entities[index] = toMove;
                toMove = RenderableEntity.Null;
            }
        }

        public readonly void DestroyAndTake(int index, ref EntityChunk chunkToTakeFrom, out int movedId)
        {
            Guard.IsGreaterThan(_count, 0, "Chunk is empty");
            Guard.IsGreaterThan(chunkToTakeFrom._count, 0, "Chunk to take from is empty");

            ref RenderableEntity toMove = ref chunkToTakeFrom._entities[--chunkToTakeFrom._count];
            movedId = toMove.Id;

            _entities[index] = toMove;
            toMove = RenderableEntity.Null;
        }

        public readonly ref RenderableEntity this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _entities.Length);
                return ref _entities.DangerousGetReferenceAt(index);
            }
        }

        public readonly Span<RenderableEntity> Entities => _entities.AsSpan();
        public readonly int Count => _count;

        public readonly bool IsEmpty => _count == 0;
        public readonly bool IsFull => _count >= _entities.Length;
    }
}
