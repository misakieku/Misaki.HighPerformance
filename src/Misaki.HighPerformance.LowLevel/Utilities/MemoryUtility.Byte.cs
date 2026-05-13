using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.LowLevel.Utilities;

public static unsafe partial class MemoryUtility
{
    [DoesNotReturn]
    private static void ThrowMustBeNullTerminatedString()
    {
        throw new ArgumentException("Arg_MustBeNullTerminatedString");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> LoadVector128(ref byte start, nuint offset)
    {
        return Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> LoadVector256(ref byte start, nuint offset)
    {
        return Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint GetByteVector128SpanLength(nuint offset, int length)
    {
        return (uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint GetByteVector256SpanLength(nuint offset, int length)
    {
        return (uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint GetByteVector512SpanLength(nuint offset, int length)
    {
        return (uint)((length - (int)offset) & ~(Vector512<byte>.Count - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe nuint UnalignedCountVector128(byte* searchSpace)
    {
        var unaligned = (nint)searchSpace & (Vector128<byte>.Count - 1);
        return (uint)((Vector128<byte>.Count - unaligned) & (Vector128<byte>.Count - 1));
    }

    /// <summary>
    /// Searches for the first occurrence of a null byte (0x00) in a given byte array.
    /// </summary>
    /// <param name="searchSpace">A pointer to the byte array where the search will be performed.</param>
    /// <returns>Returns the index of the first null byte found in the array..</returns>
    /// <exception cref="ArgumentException">Thrown if the byte array is not null-terminated.</exception>"
    public static int IndexOfNullByte(byte* searchSpace)
    {
        const int Length = int.MaxValue;
        const uint uValue = 0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
        nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
        var lengthToExamine = (nuint)(uint)Length;

        if (Vector128.IsHardwareAccelerated)
        {
            // Avx2 branch also operates on Sse2 sizes, so check is combined.
            lengthToExamine = UnalignedCountVector128(searchSpace);
        }

    SequentialScan:
        while (lengthToExamine >= 8)
        {
            lengthToExamine -= 8;

            if (uValue == searchSpace[offset])
            {
                goto Found;
            }

            if (uValue == searchSpace[offset + 1])
            {
                goto Found1;
            }

            if (uValue == searchSpace[offset + 2])
            {
                goto Found2;
            }

            if (uValue == searchSpace[offset + 3])
            {
                goto Found3;
            }

            if (uValue == searchSpace[offset + 4])
            {
                goto Found4;
            }

            if (uValue == searchSpace[offset + 5])
            {
                goto Found5;
            }

            if (uValue == searchSpace[offset + 6])
            {
                goto Found6;
            }

            if (uValue == searchSpace[offset + 7])
            {
                goto Found7;
            }

            offset += 8;
        }

        if (lengthToExamine >= 4)
        {
            lengthToExamine -= 4;

            if (uValue == searchSpace[offset])
            {
                goto Found;
            }

            if (uValue == searchSpace[offset + 1])
            {
                goto Found1;
            }

            if (uValue == searchSpace[offset + 2])
            {
                goto Found2;
            }

            if (uValue == searchSpace[offset + 3])
            {
                goto Found3;
            }

            offset += 4;
        }

        while (lengthToExamine > 0)
        {
            lengthToExamine -= 1;

            if (uValue == searchSpace[offset])
            {
                goto Found;
            }

            offset += 1;
        }

        // We get past SequentialScan only if IsHardwareAccelerated is true; and remain length is greater than Vector length.
        // However, we still have the redundant check to allow the JIT to see that the code is unreachable and eliminate it when the platform does not
        // have hardware accelerated. After processing Vector lengths we return to SequentialScan to finish any remaining.
        if (Vector512.IsHardwareAccelerated)
        {
            if (offset < Length)
            {
                if ((((uint)searchSpace + offset) & (nuint)(Vector256<byte>.Count - 1)) != 0)
                {
                    // Invert currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                    // with no upper bound e.g. String.strlen.
                    // Start with a check on Vector128 to align to Vector256, before moving to processing Vector256.
                    // This ensures we do not fault across memory pages while searching for an end of string.
                    var search = Vector128.Load(searchSpace + offset);

                    // Same method as below
                    var matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }

                if ((((uint)searchSpace + offset) & (nuint)(Vector512<byte>.Count - 1)) != 0)
                {
                    // Invert currently aligned to Vector512 (is aligned to Vector256); this can cause a problem for searches
                    // with no upper bound e.g. String.strlen.
                    // Start with a check on Vector256 to align to Vector512, before moving to processing Vector256.
                    // This ensures we do not fault across memory pages while searching for an end of string.
                    var search = Vector256.Load(searchSpace + offset);

                    // Same method as below
                    var matches = Vector256.Equals(Vector256<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector256<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }
                lengthToExamine = GetByteVector512SpanLength(offset, Length);
                if (lengthToExamine > offset)
                {
                    do
                    {
                        var search = Vector512.Load(searchSpace + offset);
                        var matches = Vector512.Equals(Vector512<byte>.Zero, search).ExtractMostSignificantBits();
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector512<byte>.Count;
                            continue;
                        }

                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    } while (lengthToExamine > offset);
                }

                lengthToExamine = GetByteVector256SpanLength(offset, Length);
                if (lengthToExamine > offset)
                {
                    var search = Vector256.Load(searchSpace + offset);

                    // Same method as above
                    var matches = Vector256.Equals(Vector256<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector256<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }

                lengthToExamine = GetByteVector128SpanLength(offset, Length);
                if (lengthToExamine > offset)
                {
                    var search = Vector128.Load(searchSpace + offset);

                    // Same method as above
                    var matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }

                if (offset < Length)
                {
                    lengthToExamine = (Length - offset);
                    goto SequentialScan;
                }
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            if (offset < Length)
            {
                if ((((uint)searchSpace + offset) & (nuint)(Vector256<byte>.Count - 1)) != 0)
                {
                    // Invert currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                    // with no upper bound e.g. String.strlen.
                    // Start with a check on Vector128 to align to Vector256, before moving to processing Vector256.
                    // This ensures we do not fault across memory pages while searching for an end of string.
                    var search = Vector128.Load(searchSpace + offset);

                    // Same method as below
                    var matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }

                lengthToExamine = GetByteVector256SpanLength(offset, Length);
                if (lengthToExamine > offset)
                {
                    do
                    {
                        var search = Vector256.Load(searchSpace + offset);
                        var matches = Vector256.Equals(Vector256<byte>.Zero, search).ExtractMostSignificantBits();
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector256<byte>.Count;
                            continue;
                        }

                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    } while (lengthToExamine > offset);
                }

                lengthToExamine = GetByteVector128SpanLength(offset, Length);
                if (lengthToExamine > offset)
                {
                    var search = Vector128.Load(searchSpace + offset);

                    // Same method as above
                    var matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                    if (matches == 0)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                    }
                    else
                    {
                        // Find bitflag offset of first match and add to current offset
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }
                }

                if (offset < Length)
                {
                    lengthToExamine = (Length - offset);
                    goto SequentialScan;
                }
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            if (offset < Length)
            {
                lengthToExamine = GetByteVector128SpanLength(offset, Length);

                while (lengthToExamine > offset)
                {
                    var search = Vector128.Load(searchSpace + offset);

                    // Same method as above
                    var compareResult = Vector128.Equals(Vector128<byte>.Zero, search);
                    if (compareResult == Vector128<byte>.Zero)
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                        continue;
                    }

                    // Find bitflag offset of first match and add to current offset
                    var matches = compareResult.ExtractMostSignificantBits();
                    return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                }

                if (offset < Length)
                {
                    lengthToExamine = (Length - offset);
                    goto SequentialScan;
                }
            }
        }

        ThrowMustBeNullTerminatedString();
    Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
        return (int)offset;
    Found1:
        return (int)(offset + 1);
    Found2:
        return (int)(offset + 2);
    Found3:
        return (int)(offset + 3);
    Found4:
        return (int)(offset + 4);
    Found5:
        return (int)(offset + 5);
    Found6:
        return (int)(offset + 6);
    Found7:
        return (int)(offset + 7);
    }

    public static void ReplaceIfZeros(Span<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Spans must be the same size.");
        }

        var i = 0;
        if (Vector.IsHardwareAccelerated && a.Length >= Vector<byte>.Count)
        {
            ref var ptrA = ref MemoryMarshal.GetReference(a);
            ref var ptrB = ref MemoryMarshal.GetReference(b);

            var limit = a.Length - Vector<byte>.Count;
            for (; i <= limit; i += Vector<byte>.Count)
            {
                var vecA = Vector.LoadUnsafe(ref ptrA, (nuint)i);
                var vecB = Vector.LoadUnsafe(ref ptrB, (nuint)i);

                var mask = Vector.Equals(vecA, Vector<byte>.Zero);

                var result = Vector.ConditionalSelect(mask, vecB, vecA);
                result.StoreUnsafe(ref ptrA, (nuint)i);
            }
        }

        // Fallback standard loop for the remaining "tail" bytes (e.g., the last 15 bytes)
        for (; i < a.Length; i++)
        {
            if (a[i] == 0)
            {
                a[i] = b[i];
            }
        }
    }

    public static void ReplaceIfZeros(void* a, void* b, nuint length)
    {
        var ptrA = (byte*)a;
        var ptrB = (byte*)b;

        nuint i = 0u;

        if (Vector.IsHardwareAccelerated && length >= (nuint)Vector<byte>.Count)
        {
            var vectorSize = (nuint)Vector<byte>.Count;
            var limit = length - vectorSize;
            for (; i <= limit; i += vectorSize)
            {
                var vecA = Vector.Load(ptrA + i);
                var vecB = Vector.Load(ptrB + i);

                var mask = Vector.Equals(vecA, Vector<byte>.Zero);

                var result = Vector.ConditionalSelect(mask, vecB, vecA);
                result.Store(ptrA + i);
            }
        }

        // Fallback standard loop for the remaining "tail" bytes (e.g., the last 15 bytes)
        for (; i < length; i++)
        {
            if (ptrA[i] == 0)
            {
                ptrA[i] = ptrB[i];
            }
        }
    }
}