using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RCNB.Implementations;
using static RCNB.Implementations.RcnbUtility;

namespace RCNB
{
    /// <summary>
    /// Encodes and decodes RCNB.
    /// </summary>
    public static class RcnbConvert
    {
        /// <summary>
        /// Encode RCNB, storing results to given location.
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="outChars"></param>
        /// <param name="n">Bytes to encode.</param>
        internal unsafe static void EncodeRcnb(byte* inData, char* outChars, nint n)
        {
            if (RcnbAvx2.IsSupported)
            {
                RcnbAvx2.EncodeRcnb(inData, outChars, n);
            }
            else
            {
                RcnbSoftware.EncodeRcnb(inData, outChars, n);
            }
        }

        /// <summary>
        /// Encodes RCNB. Stores encoding result to <c>outChars</c>;
        /// </summary>
        /// <param name="inData">Data.</param>
        /// <param name="outChars">Results.</param>
        /// <exception cref="ArgumentOutOfRangeException"><c>outChars</c> does not have enough space to store the results.</exception>
        public unsafe static void EncodeRcnb(ReadOnlySpan<byte> inData, Span<char> outChars)
        {
            if (CalculateLength(inData) > outChars.Length)
                throw new ArgumentOutOfRangeException(nameof(inData), "rcnb overflow, data is too long or dest is too short.");
            fixed (byte* bytesPtr = inData)
            fixed (char* charsPtr = outChars)
            {
                EncodeRcnb(bytesPtr, charsPtr, inData.Length);
            }
        }

        /// <summary>
        /// Encode RCNB.
        /// </summary>
        /// <param name="inData">Data to encode.</param>
        /// <returns>Encoded RCNB string.</returns>
#if NETSTANDARD1_1 || NETSTANDARD2_0
        public static unsafe string ToRcnbString(ReadOnlySpan<byte> inData)
        {
            int resultLength = CalculateLength(inData);
            char[] resultArray = new char[resultLength];
            fixed (byte* bytesPtr = inData)
            fixed (char* charsPtr = resultArray)
            {
                EncodeRcnb(bytesPtr, charsPtr, inData.Length);
            }
            return new string(resultArray);
        }
#else
        public static unsafe string ToRcnbString(ReadOnlySpan<byte> inData)
        {
            int length = CalculateLength(inData);
            //fixed (byte* data = &inArray.GetPinnableReference())
            fixed (byte* bytesPtr = inData) // it seems to work? --Yes! It is documented.
            {
                return string.Create(length,
                    new ByteMemoryMedium(bytesPtr, inData.Length),
                    (outChars, dataMedium) =>
                    {
                        fixed (char* charsPtr = outChars)
                            EncodeRcnb(dataMedium.Pointer, charsPtr, dataMedium.Length);
                    });
            }
        }

        private unsafe readonly struct ByteMemoryMedium
        {
            public ByteMemoryMedium(byte* pointer, int length)
            {
                Pointer = pointer;
                Length = length;
            }

            public byte* Pointer { get; }
            public int Length { get; }
        }

        /// <summary>
        /// Encode content to RCNB.
        /// </summary>
        /// <param name="inData">Content to encode.</param>
        /// <returns>The encoded content.</returns>
        public unsafe static string ToRcnbString(ReadOnlyMemory<byte> inData)
        {
            int length = CalculateLength(inData.Span);
            return string.Create(length, inData, (outChars, bytes) =>
            {
                fixed (byte* bytesPtr = bytes.Span)
                fixed (char* charsPtr = outChars)
                    EncodeRcnb(bytesPtr, charsPtr, bytes.Length);
            });
        }

        /// <summary>
        /// Encode content to RCNB.
        /// </summary>
        /// <param name="inArray">Content to encode.</param>
        /// <returns>The encoded content.</returns>
        public static string ToRcnbString(byte[] inArray) => ToRcnbString(inArray.AsMemory());
#endif

        internal unsafe static nint DecodeRcnb(char* inChars, byte* outData, nint charsLen)
        {
            return RcnbSoftware.DecodeRcnb(inChars, outData, charsLen);
        }

        /// <summary>
        /// Decode RCNB char span, saving result to given span.
        /// </summary>
        /// <param name="str">RCNB char span.</param>
        /// <param name="dest">Where to store result.</param>
        /// <returns>Decoded count of bytes.</returns>
        public static int FromRcnbString(ReadOnlySpan<char> str, Span<byte> dest)
        {
            if (str.Length / 2 > dest.Length)
                throw new ArgumentException("The length of destination is not enough.", nameof(dest));
            if ((str.Length & 1) != 0)
                throw new FormatException("The length of RCNB string is not valid.");

            unsafe
            {
                fixed (char* inChars = str)
                fixed (byte* outData = dest)
                {
                    var encodedLength = DecodeRcnb(inChars, outData, str.Length);
                    checked
                    {
                        return (int)encodedLength;
                    }
                }
            }
        }

        /// <summary>
        /// Decode RCNB string.
        /// </summary>
        /// <param name="str">RCNB string.</param>
        /// <returns>Decoded data.</returns>
        public static byte[] FromRcnbString(ReadOnlySpan<char> str)
        {
            byte[] result = new byte[str.Length / 2];
            var decodedLength = FromRcnbString(str, result);
            Debug.Assert(result.Length == decodedLength);
            return result;
        }

#if NETSTANDARD1_1 || NETSTANDARD2_0
        /// <summary>
        /// Decode RCNB string.
        /// </summary>
        /// <param name="str">RCNB string.</param>
        /// <returns>Decoded data.</returns>
        public static byte[] FromRcnbString(string str)
            => FromRcnbString(str.AsSpan());
#endif
    }
}
