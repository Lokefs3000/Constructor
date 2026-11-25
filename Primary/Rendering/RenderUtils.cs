using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering
{
    public static class RenderUtils
    {
        public static uint CalcSubresourceIndex(uint mipSlice, uint arraySlice, uint planeSlice, uint mipLevels, uint arraySize)
        {
            return mipSlice + (arraySlice * mipLevels) + (planeSlice * mipLevels * arraySize);
        }

        public static uint CalcSubresourceIndex(uint mipSlice, uint arraySlice, uint mipLevels)
        {
            return mipSlice + (mipLevels * arraySlice);
        }
    }
}
