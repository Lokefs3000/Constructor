using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Memory;

namespace Primary.Rendering.Memory
{
    internal sealed class VirtualAllocator
    {
        private readonly List<VirtualMemoryBlock> _memoryBlocks;
        private int _allocatedMemory;

        internal VirtualAllocator()
        {
            _memoryBlocks = new List<VirtualMemoryBlock>();
            _allocatedMemory = 0;
        }

        internal void ResetAllocator()
        {
            _memoryBlocks.Clear();
            _allocatedMemory = 0;
        }

        public VirtualAllocation Allocate(int memoryAlignment, int memorySize)
        {
            Span<VirtualMemoryBlock> blocks = _memoryBlocks.AsSpan();
            for (int i = 0; i < blocks.Length; ++i)
            {
                ref VirtualMemoryBlock block = ref blocks[i];

                int currentOffset = block.BlockOffset;
                int alignedOffset = MemoryHelper.AlignUp(block.BlockOffset, memoryAlignment);
                int actualMemorySize = (alignedOffset - block.BlockOffset) + memorySize;

                if (actualMemorySize <= block.BlockSize)
                {
                    if (actualMemorySize == block.BlockSize)
                        _memoryBlocks.RemoveAt(i);
                    else
                        block = new VirtualMemoryBlock(block.BlockOffset + actualMemorySize, block.BlockSize - actualMemorySize);

                    return new VirtualAllocation(currentOffset, alignedOffset, actualMemorySize);
                }
            }

            int newBlockSize = Math.Max(_allocatedMemory, Math.Max(ushort.MaxValue, MemoryHelper.AlignUp(memorySize, 2048)));
            int offsetWithAlignment = MemoryHelper.AlignUp(_allocatedMemory, memoryAlignment);

            VirtualAllocation allocation = new VirtualAllocation(_allocatedMemory, offsetWithAlignment, (offsetWithAlignment - _allocatedMemory) + memorySize);
            if (newBlockSize > allocation.MemorySize)
                _memoryBlocks.Add(new VirtualMemoryBlock(offsetWithAlignment + allocation.MemorySize, newBlockSize - allocation.MemorySize));

            _allocatedMemory += newBlockSize;

            return allocation;
        }

        public void Free(VirtualAllocation allocation)
        {
            int insertPosition = -1;
            int endOffset = allocation.MemoryOffset + allocation.MemorySize;

            Span<VirtualMemoryBlock> blocks = _memoryBlocks.AsSpan();
            for (int i = 0; i < blocks.Length; ++i)
            {
                ref VirtualMemoryBlock block = ref blocks[i];
                if (insertPosition == -1 && block.BlockOffset > allocation.RealMemoryOffset)
                {
                    insertPosition = i;
                }

                if (block.BlockOffset + block.BlockSize == allocation.RealMemoryOffset)
                {
                    block = new VirtualMemoryBlock(block.BlockOffset, block.BlockSize + (allocation.MemoryOffset - allocation.RealMemoryOffset) + allocation.MemorySize);

                    VirtualMemoryBlock prevBlock = block;

                    ++i;
                    while (i < blocks.Length)
                    {
                        block = ref blocks[i];

                        if (prevBlock.BlockOffset + prevBlock.BlockSize == block.BlockOffset)
                        {
                            block = new VirtualMemoryBlock(prevBlock.BlockOffset, prevBlock.BlockSize + block.BlockSize);

                            _memoryBlocks.RemoveAt(i--);
                            blocks = _memoryBlocks.AsSpan();
                        }
                        else
                        {
                            break;
                        }
                    }

                    return;
                }
                else if (block.BlockOffset == endOffset)
                {
                    block = new VirtualMemoryBlock(allocation.RealMemoryOffset, block.BlockSize + (allocation.MemoryOffset - allocation.RealMemoryOffset) + allocation.MemorySize);

                    VirtualMemoryBlock prevBlock = block;

                    --i;
                    while (i > 0)
                    {
                        block = ref blocks[i];

                        if (block.BlockOffset + block.BlockSize == prevBlock.BlockOffset)
                        {
                            block = new VirtualMemoryBlock(block.BlockOffset, prevBlock.BlockSize + block.BlockSize);

                            _memoryBlocks.RemoveAt(i++);
                            blocks = _memoryBlocks.AsSpan();
                        }
                        else
                        {
                            break;
                        }
                    }

                    return;
                }
            }

            if (insertPosition == -1)
                _memoryBlocks.Add(new VirtualMemoryBlock(allocation.RealMemoryOffset, (allocation.MemoryOffset - allocation.RealMemoryOffset) + allocation.MemorySize));
            else
                _memoryBlocks.Insert(insertPosition, new VirtualMemoryBlock(allocation.RealMemoryOffset, (allocation.MemoryOffset - allocation.RealMemoryOffset) + allocation.MemorySize));
        }

        public int AllocatedMemory => _allocatedMemory;

        private readonly record struct VirtualMemoryBlock(int BlockOffset, int BlockSize);
    }

    internal readonly record struct VirtualAllocation(int RealMemoryOffset, int MemoryOffset, int MemorySize);
}
