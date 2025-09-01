using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.RHI.Direct3D12.Allocators
{
    internal unsafe class TLSF
    {
        private uint _flbitmap;
        private uint[] _slbitmap;

        private BlockHeader*[][] _blocks;

        internal TLSF()
        {
            _flbitmap = 0;
            _slbitmap = new uint[FLIndexCount];

            _blocks = new BlockHeader*[FLIndexCount][];

            fixed (BlockHeader* Null = &BlockNull)
            {
                Null->NextFree = Null;
                Null->PrevFree = Null;

                for (int i = 0; i < FLIndexCount; i++)
                {
                    _slbitmap[i] = 0;
                    _blocks[i] = new BlockHeader*[SLIndexCount];
                    for (int j = 0; j < SLIndexCount; j++)
                    {
                        _blocks[i][j] = Null;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PoolOverhead()
        {
            return 2 * BlockHeaderOverhead;
        }

        public void* AddPool(void* mem, uint bytes)
        {
            BlockHeader* block = null;
            BlockHeader* next;

            uint pool_overhead = PoolOverhead();
            uint pool_bytes = AlignDown(bytes - pool_overhead, AlignSize);

            if (((long)mem & AlignSize) != 0)
            {
                throw new Exception();
            }

            if (pool_bytes < BlockSizeMin || pool_bytes > BlockSizeMax)
            {
                throw new Exception();
            }

            block = OffsetToBlock(mem, unchecked((uint)-(long)BlockHeaderOverhead));
            block->BlockSize = pool_bytes;
            block->IsFree = true;
            block->IsPrevUsed = true;
            BlockInsert(block);

            next = BlockLinkNext(block);
            next->BlockSize = 0;
            next->IsUsed = true;
            block->IsPrevFree = true;

            return mem;
        }

        public void RemovePool(void* pool)
        {
            BlockHeader* block = OffsetToBlock(pool, unchecked((uint)-(int)BlockHeaderOverhead));

            int fl = 0, sl = 0;

            Debug.Assert(block->IsFree);
            Debug.Assert(!BlockNext(block)->IsFree);
            Debug.Assert(BlockNext(block)->BlockSize == 0);

            MappingInsert(block->BlockSize, &fl, &sl);
            RemoveFreeBlock(block, fl, sl);
        }

        public void* Malloc(uint size)
        {
            uint adjust = AdjustRequestSize(size, AlignSize);
            BlockHeader* block = BlockLocateFree(adjust);
            return BlockPrepareUsed(block, adjust);
        }

        public void Free(void* ptr)
        {
            if (ptr != null)
            {
                BlockHeader* block = BlockFromPtr(ptr);
                Debug.Assert(!block->IsFree);
                BlockMarkAsFree(block);
                block = BlockMergePrev(block);
                block = BlockMergeNext(block);
                BlockInsert(block);
            }
        }

        private BlockHeader* SearchSuitableBlock(int* fli, int* sli)
        {
            int fl = *fli;
            int sl = *sli;

            uint sl_map = _slbitmap[fl] & (~0u << sl);
            if (sl_map == 0)
            {
                uint fl_map = _flbitmap & (~0u << (fl + 1));
                if (fl_map == 0)
                {
                    return null;
                }

                fl = FFS(fl_map);
                *fli = fl;
                sl_map = _slbitmap[fl];
            }

            Debug.Assert(sl_map != 0);
            sl = FFS(sl_map);
            *sli = sl;

            return _blocks[fl][sl];
        }

        private void RemoveFreeBlock(BlockHeader* block, int fl, int sl)
        {
            BlockHeader* prev = block->PrevFree;
            BlockHeader* next = block->NextFree;
            Debug.Assert(prev != null);
            Debug.Assert(next != null);
            next->PrevFree = prev;
            prev->NextFree = next;

            if (_blocks[fl][sl] == block)
            {
                _blocks[fl][sl] = next;

                if (next->IsNull)
                {
                    _slbitmap[fl] &= ~(1u << sl);
                    if (_slbitmap[fl] == 0)
                    {
                        _flbitmap &= ~(1u << fl);
                    }
                }
            }
        }

        private void InsertFreeBlock(BlockHeader* block, int fl, int sl)
        {
            BlockHeader* current = _blocks[fl][sl];
            Debug.Assert(current != null);
            Debug.Assert(block != null);
            block->NextFree = current;
            block->PrevFree = BlockNull.NextFree; //should always be null just avoiding the "fixed" semantic
            current->PrevFree = block;

            Debug.Assert(BlockToPtr(block) == AlignPtr(BlockToPtr(block), AlignSize));

            _blocks[fl][sl] = block;
            _flbitmap |= (1u << fl);
            _slbitmap[fl] |= (1u << sl);
        }

        private void BlockRemove(BlockHeader* block)
        {
            int fl, sl;
            MappingInsert(block->BlockSize, &fl, &sl);
            RemoveFreeBlock(block, fl, sl);
        }

        private void BlockInsert(BlockHeader* block)
        {
            int fl, sl;
            MappingInsert(block->BlockSize, &fl, &sl);
            InsertFreeBlock(block, fl, sl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool BlockCanSplit(BlockHeader* block, uint size)
        {
            return block->BlockSize >= sizeof(BlockHeader) + size;
        }

        private BlockHeader* BlockSplit(BlockHeader* block, uint size)
        {
            BlockHeader* remaining = OffsetToBlock(BlockToPtr(block), size - BlockHeaderOverhead);
            uint remain_size = remaining->BlockSize - (size + BlockHeaderOverhead);

            Debug.Assert(BlockToPtr(remaining) == AlignPtr(BlockToPtr(remaining), AlignSize));

            Debug.Assert(block->BlockSize == remain_size + size + BlockHeaderOverhead);
            remaining->BlockSize = remain_size;
            Debug.Assert(remaining->BlockSize >= BlockSizeMin);

            block->BlockSize = size;
            remaining->IsFree = true;

            return remaining;
        }

        private BlockHeader* BlockAbsorb(BlockHeader* prev, BlockHeader* block)
        {
            Debug.Assert(!prev->IsLast);

            prev->Size += block->BlockSize + BlockHeaderOverhead;
            BlockLinkNext(prev);
            return prev;
        }

        private BlockHeader* BlockMergePrev(BlockHeader* block)
        {
            if (block->IsPrevFree)
            {
                BlockHeader* prev = BlockPrev(block);
                Debug.Assert(prev != null);
                Debug.Assert(prev->IsFree);
                BlockRemove(prev);
                block = BlockAbsorb(prev, block);
            }

            return block;
        }

        private BlockHeader* BlockMergeNext(BlockHeader* block)
        {
            BlockHeader* next = BlockNext(block);
            Debug.Assert(next != null);

            if (next->IsFree)
            {
                Debug.Assert(!next->IsLast);
                BlockRemove(next);
                block = BlockAbsorb(block, next);
            }

            return block;
        }

        private void BlockTrimFree(BlockHeader* block, uint size)
        {
            Debug.Assert(block->IsFree);
            if (BlockCanSplit(block, size))
            {
                BlockHeader* remaining_block = BlockSplit(block, size);
                BlockLinkNext(block);
                remaining_block->IsPrevFree = true;
                BlockInsert(remaining_block);
            }
        }

        private void BlockTrimUsed(BlockHeader* block, uint size)
        {
            Debug.Assert(!block->IsFree);
            if (BlockCanSplit(block, size))
            {
                BlockHeader* remaining_block = BlockSplit(block, size);
                remaining_block->IsPrevUsed = true;

                remaining_block = BlockMergeNext(remaining_block);
                BlockInsert(remaining_block);
            }
        }

        private BlockHeader* BlockTrimFreeLeading(BlockHeader* block, uint size)
        {
            BlockHeader* remaining_block = block;
            if (BlockCanSplit(block, size))
            {
                remaining_block = BlockSplit(block, size - BlockHeaderOverhead);
                remaining_block->IsPrevFree = true;

                BlockLinkNext(block);
                BlockInsert(block);
            }

            return remaining_block;
        }

        private BlockHeader* BlockLocateFree(uint size)
        {
            int fl = 0, sl = 0;
            BlockHeader* block = null;

            if (size > 0)
            {
                MappingSearch(size, &fl, &sl);

                if (fl < FLIndexCount)
                {
                    block = SearchSuitableBlock(&fl, &sl);
                }
            }

            if (block != null)
            {
                Debug.Assert(block->BlockSize >= size);
                RemoveFreeBlock(block, fl, sl);
            }

            return block;
        }

        private void* BlockPrepareUsed(BlockHeader* block, uint size)
        {
            void* p = null;
            if (block != null)
            {
                Debug.Assert(size > 0);
                BlockTrimFree(block, size);
                block->IsUsed = true;
                p = BlockToPtr(block);
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FFS(uint word) => BitOperations.TrailingZeroCount(word);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FLS(uint word) => BitOperations.LeadingZeroCount(word);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlockHeader* BlockFromPtr(void* ptr) => (BlockHeader*)((byte*)ptr - BlockStartOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* BlockToPtr(BlockHeader* ptr) => (void*)((byte*)ptr + BlockStartOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlockHeader* OffsetToBlock(void* ptr, uint size) => (BlockHeader*)((byte*)ptr + size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlockHeader* BlockPrev(BlockHeader* block) { Debug.Assert(block->IsPrevFree); return block->PrevPhysBlock; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlockHeader* BlockNext(BlockHeader* block) { BlockHeader* next = OffsetToBlock(BlockToPtr(block), block->BlockSize - BlockHeaderOverhead); Debug.Assert(!block->IsLast); return next; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlockHeader* BlockLinkNext(BlockHeader* block) { BlockHeader* next = BlockNext(block); next->PrevPhysBlock = block; return next; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockMarkAsFree(BlockHeader* block) { BlockHeader* next = BlockLinkNext(block); next->IsPrevFree = true; block->IsFree = true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockMarkAsUsed(BlockHeader* block) { BlockHeader* next = BlockLinkNext(block); next->IsPrevUsed = true; block->IsUsed = true; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AlignUp(uint x, uint align) { Debug.Assert(BitOperations.IsPow2(align)); return (x + (align - 1)) & ~(align - 1); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AlignDown(uint x, uint align) { Debug.Assert(BitOperations.IsPow2(align)); return x - (x & (align - 1)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* AlignPtr(void* ptr, uint align) { long aligned = ((long)ptr + (align - 1)) & ~(align - 1); Debug.Assert(BitOperations.IsPow2(align)); return (void*)align; }

        private static uint AdjustRequestSize(uint size, uint align)
        {
            uint adjust = 0;
            if (size > 0)
            {
                uint aligned = AlignUp(size, align);
                if (aligned < BlockSizeMax)
                {
                    adjust = Math.Max(aligned, BlockSizeMin);
                }
            }

            return adjust;
        }

        private static void MappingInsert(uint size, int* fli, int* sli)
        {
            int fl, sl;
            if (size < SmallBlockSize)
            {
                fl = 0;
                sl = (int)size / (SmallBlockSize / SLIndexCount);
            }
            else
            {
                fl = FLS(size);
                sl = (int)(size >> (fl - SLIndexCount)) ^ (1 << SLIndexCountLog2);
                fl -= FLIndexShift - 1;
            }

            *fli = fl;
            *sli = sl;
        }

        private static void MappingSearch(uint size, int* fli, int* sli)
        {
            if (size >= SmallBlockSize)
            {
                uint round = (uint)((1 << (FLS(size) - SLIndexCountLog2)) - 1);
                size += round;
            }

            MappingInsert(size, fli, sli);
        }

        public const int SLIndexCountLog2 = 5;

        private const int AlignSizeLog2 = 2;
        private const int AlignSize = 1 << AlignSizeLog2;
        private const int FLIndexMax = 30;
        private const int SLIndexCount = 1 << SLIndexCountLog2;
        private const int FLIndexShift = SLIndexCountLog2 - AlignSizeLog2;
        private const int FLIndexCount = FLIndexMax - FLIndexShift + 1;
        private const int SmallBlockSize = 1 << FLIndexShift;

        private const int BlockHeaderOverhead = 8;
        private const int BlockStartOffset = 16;
        private const uint BlockHeaderFreeBit = 1u << 30;
        private const uint BlockHeaderPrevFreeBit = 1u << 31;

        private const uint BlockSizeMin = (uint)(28 - 8);
        private const uint BlockSizeMax = 1u << FLIndexMax;

        private static readonly BlockHeader BlockNull = new BlockHeader
        {
            PrevPhysBlock = null,
            Size = 0,
            NextFree = null,
            PrevFree = null,
        };

        private struct BlockHeader
        {
            public BlockHeader* PrevPhysBlock;

            public uint Size;

            public BlockHeader* NextFree;
            public BlockHeader* PrevFree;

            public uint BlockSize { get => Size & ~(BlockHeaderFreeBit | BlockHeaderPrevFreeBit); set => Size = (value | (Size & (BlockHeaderFreeBit | BlockHeaderPrevFreeBit))); }
            public bool IsLast => BlockSize == 0;
            public bool IsFree { get => (Size & BlockHeaderFreeBit) > 0; set => Size |= BlockHeaderFreeBit; }
            public bool IsUsed { set => Size = Size & ~BlockHeaderFreeBit; }
            public bool IsPrevFree { get => (Size & BlockHeaderPrevFreeBit) > 0; set { Size |= BlockHeaderPrevFreeBit; } }
            public bool IsPrevUsed { get => (Size & BlockHeaderPrevFreeBit) == 0; set { Size &= ~BlockHeaderPrevFreeBit; } }
            public bool IsNull { get { fixed (BlockHeader* @this = &this) { return @this == BlockNull.NextFree; } } }
        }
    }
}
