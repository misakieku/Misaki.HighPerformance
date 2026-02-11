using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public static unsafe class ShuffleTableGenerator
{
    public static uint* ComputeShuffleTable512_32Bit()
    {
        const nuint entryCount = 512;
        const int elementCount = 16;

        // Align to 64 bytes for AVX-512 performance
        var table = (uint*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(uint), 64);

        for (var mask = 0u; mask < entryCount; mask++)
        {
            // We are filling 16 integers for this mask
            var pRow = table + (mask * elementCount);
            var outputIndex = 0;
            // 1. Pack the valid indices to the front
            for (var bit = 0; bit < 16; bit++)
            {
                // Check if the i-th bit is set
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (uint)bit; // Write the Source Index
                    outputIndex++;
                }
            }
            // 2. Fill the remaining slots (Pad with 0 or similar)
            // It doesn't strictly matter what these are, as we won't read them,
            // but filling with 0 is clean.
            while (outputIndex < 16)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }

    public static ulong* ComputeShuffleTable512_64Bit()
    {
        const nuint entryCount = 256;
        const int elementCount = 8;

        // Align to 64 bytes for AVX-512 performance
        var table = (ulong*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(ulong), 64);
        for (var mask = 0u; mask < entryCount; mask++)
        {
            // We are filling 8 integers for this mask
            var pRow = table + (mask * elementCount);
            var outputIndex = 0;

            // 1. Pack the valid indices to the front
            for (var bit = 0; bit < 8; bit++)
            {
                // Check if the i-th bit is set
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (ulong)bit; // Write the Source Index
                    outputIndex++;
                }
            }

            // 2. Fill the remaining slots (Pad with 0 or similar)
            while (outputIndex < 8)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }

    public static uint* ComputeShuffleTable256_32Bit()
    {
        const nuint entryCount = 256;
        const nuint elementCount = 8;

        // Align to 32 bytes for AVX performance
        var table = (uint*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(uint), 32);

        for (var mask = 0u; mask < entryCount; mask++)
        {
            // We are filling 8 integers for this mask
            var pRow = table + (mask * elementCount);

            var outputIndex = 0;

            for (var bit = 0; bit < 8; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (uint)bit;
                    outputIndex++;
                }
            }

            while (outputIndex < 8)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }

    public static ulong* ComputeShuffleTable256_64Bit()
    {
        const nuint entryCount = 16;
        const nuint elementCount = 4;

        var table = (ulong*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(ulong), 32);

        for (var mask = 0u; mask < entryCount; mask++)
        {
            var pRow = table + (mask * elementCount);
            var outputIndex = 0;

            // We only check 4 bits because there are only 4 ulongs in a Vector256
            for (var bit = 0; bit < 4; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (ulong)bit;
                    outputIndex++;
                }
            }

            // Fill remaining slots with 0 (or a specific 'clear' index)
            while (outputIndex < 4)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }

    public static uint* ComputeShuffleTable128_32Bit()
    {
        const nuint entryCount = 16;
        const nuint elementCount = 4;

        var table = (uint*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(uint), 16);

        for (var mask = 0u; mask < entryCount; mask++)
        {
            var pRow = table + (mask * elementCount);
            var outputIndex = 0;

            for (var bit = 0; bit < 4; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (uint)bit;
                    outputIndex++;
                }
            }

            while (outputIndex < 4)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }

    public static ulong* ComputeShuffleTable128_64Bit()
    {
        const nuint entryCount = 8;
        const nuint elementCount = 2;

        var table = (ulong*)NativeMemory.AlignedAlloc(entryCount * elementCount * sizeof(ulong), 16);

        for (var mask = 0u; mask < entryCount; mask++)
        {
            var pRow = table + (mask * elementCount);
            var outputIndex = 0;

            for (var bit = 0; bit < 2; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    pRow[outputIndex] = (byte)bit;
                    outputIndex++;
                }
            }

            while (outputIndex < 2)
            {
                pRow[outputIndex] = 0;
                outputIndex++;
            }
        }

        return table;
    }
}
