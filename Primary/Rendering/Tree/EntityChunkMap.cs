using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel.Jagged;
using CommunityToolkit.HighPerformance;
using Primary.Collections.Extensions;
using Primary.Scenes;

namespace Primary.Rendering.Tree
{
    internal sealed class EntityChunkMap
    {
        private readonly ArrayPool<RenderableEntity> _arrayPool;
        private readonly JaggedArray<RenderableEntitySlot> _slots;
        private readonly List<EntityChunk> _chunks;

        private readonly Stack<int> _recycledIds;
        private int _entityCounter;

        private int _aliveEntityCount;

        private int _firstFreeChunkIndex;

        internal EntityChunkMap(ArrayPool<RenderableEntity> arrayPool)
        {
            _arrayPool = arrayPool;
            _slots = new JaggedArray<RenderableEntitySlot>(2048 / Unsafe.SizeOf<RenderableEntitySlot>(), RenderableEntitySlot.None);
            _chunks = new List<EntityChunk>();

            _recycledIds = new Stack<int>();
            _entityCounter = 0;

            _aliveEntityCount = 0;

            _firstFreeChunkIndex = -1;
        }

        internal ref RenderableEntity CreateRenderableEntity(out int id)
        {
            ++_aliveEntityCount;

            if (_firstFreeChunkIndex == -1)
            {
                _firstFreeChunkIndex = _chunks.Count;
                AddChunkToList();
            }

            ref EntityChunk chunkToAddTo = ref _chunks.GetRefAtIndex(_firstFreeChunkIndex);
            ref RenderableEntity entity = ref chunkToAddTo.Create(out int index);

            if (!_recycledIds.TryPop(out entity.Id))
                entity.Id = _entityCounter++;

            _slots.EnsureCapacity(_aliveEntityCount);
            _slots.Add(entity.Id, new RenderableEntitySlot(_firstFreeChunkIndex, index));

            if (chunkToAddTo.IsFull)
            {
                FindFirstFreeChunk();   
            }

            id = entity.Id;
            return ref entity;
        }

        internal void DestroyRenderableEntity(int id)
        {
            --_aliveEntityCount;

            ref RenderableEntitySlot entitySlot = ref _slots[id];
            Debug.Assert(entitySlot.Chunk != -1 && entitySlot.IndexInChunk != -1);

            ref EntityChunk chunk = ref _chunks.GetRefAtIndex(entitySlot.Chunk);

            // Is this the last chunk?
            if (_chunks.Count == entitySlot.Chunk + 1)
            {
                if (chunk.Count == 1)
                {
                    chunk.Dispose();
                    _chunks.RemoveAt(entitySlot.Chunk);
                }
                else
                {
                    chunk.Destroy(entitySlot.IndexInChunk, out int movedId);

                    if (movedId != -1)
                    {
                        // Move the entity slot as well
                        _slots[movedId] = entitySlot;
                    }
                }
            }
            else
            {
                ref EntityChunk lastChunk = ref _chunks.GetRefAtIndex(_chunks.Count - 1);
                chunk.DestroyAndTake(entitySlot.IndexInChunk, ref lastChunk, out int movedId);

                if (movedId != -1)
                {
                    // Move the entity slot as well
                    _slots[movedId] = entitySlot;
                }

                if (lastChunk.IsEmpty)
                {
                    lastChunk.Dispose();
                    _chunks.RemoveAt(_chunks.Count - 1);
                }
            }

            _slots.Remove(id);
        }

        internal ref RenderableEntitySlot GetSlot(int id)
        {
            return ref _slots[id];
        }

        internal ref RenderableEntity GetEntity(int id)
        {
            ref RenderableEntitySlot slot = ref _slots[id];
            return ref _chunks.GetRefAtIndex(slot.Chunk)[slot.IndexInChunk];
        }

        internal ref RenderableEntity GetEntity(RenderableEntitySlot slot)
        {
            return ref _chunks.GetRefAtIndex(slot.Chunk)[slot.IndexInChunk];
        }

        private void AddChunkToList()
        {
            EntityChunk chunk = new EntityChunk(_arrayPool);
            _chunks.Add(chunk);
        }

        private void FindFirstFreeChunk()
        {
            int i = _firstFreeChunkIndex + 1;
            _firstFreeChunkIndex = -1;

            for (; i < _chunks.Count; ++i)
            {
                if (!_chunks[i].IsFull)
                {
                    _firstFreeChunkIndex = i;
                    break;
                }
            }
        }

        public ArrayPool<RenderableEntity> ArrayPool => _arrayPool;

        public Span<EntityChunk> Chunks => _chunks.AsSpan();
    }
}
