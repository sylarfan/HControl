using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HalconControl
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SelectBucketIndex(int bufferSize)
        {
            uint num1 = (uint)(bufferSize - 1) >> 4;
            int num2 = 0;
            if (num1 > (uint)ushort.MaxValue)
            {
                num1 >>= 16;
                num2 = 16;
            }
            if (num1 > (uint)byte.MaxValue)
            {
                num1 >>= 8;
                num2 += 8;
            }
            if (num1 > 15U)
            {
                num1 >>= 4;
                num2 += 4;
            }
            if (num1 > 3U)
            {
                num1 >>= 2;
                num2 += 2;
            }
            if (num1 > 1U)
            {
                num1 >>= 1;
                ++num2;
            }
            return num2 + (int)num1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaxSizeForBucket(int binIndex)
        {
            return 16 << binIndex;
        }
    }
}
