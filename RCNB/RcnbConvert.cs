using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RCNB
{
    /// <summary>
    /// Encodes and decodes RCNB.
    /// </summary>
    public static class RcnbConvert
    {
        // char
        private const string cr = "rRŔŕŖŗŘřƦȐȑȒȓɌɍ";
        private const string cc = "cCĆćĈĉĊċČčƇƈÇȻȼ";
        private const string cn = "nNŃńŅņŇňƝƞÑǸǹȠȵ";
        private const string cb = "bBƀƁƃƄƅßÞþ";

        // size
        private const int sr = 15; // cr.Length;
        private const int sc = 15; // cc.Length;
        private const int sn = 15; // cn.Length;
        private const int sb = 10; // cb.Length;
        private const int src = sr * sc;
        private const int snb = sn * sb;
        private const int scnb = sc * snb;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        private unsafe static void EncodeFinalByte(byte inByte, char* outChars)
        {
            fixed (char* pr = cr)
            fixed (char* pc = cc)
            fixed (char* pn = cn)
            fixed (char* pb = cb)
            {
                int i = inByte;
                if (i > 0x7F)
                {
                    i &= 0x7F;
                    outChars[0] = pn[i / sb];
                    outChars[1] = pb[i % sb];
                }
                else
                {
                    outChars[0] = pr[i / sc];
                    outChars[1] = pc[i % sc];
                }
            }
        }

        /// <summary>
        /// Encode RCNB, storing results to given location.
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="outChars"></param>
        /// <param name="n">Bytes to encode.</param>
        internal unsafe static void EncodeRcnb(byte* inData, char* outChars, int n)
        {
            // avoid unnecessary range checking
            fixed (char* pr = cr)
            fixed (char* pc = cc)
            fixed (char* pn = cn)
            fixed (char* pb = cb)
            {
                Span<int> resultIndexArray = stackalloc int[4];
                for (int i = 0; i < n >> 1; i++)
                {
                    int s = (inData[0] << 8) | inData[1];

                    Debug.Assert(s <= 0xFFFF);
                    var reverse = false;
                    if (s > 0x7FFF)
                    {
                        reverse = true;
                        s &= 0x7FFF;
                    }
#if NETSTANDARD1_1
                    resultIndexArray[0] = s / scnb;
                    resultIndexArray[1] = (s % scnb) / snb;
                    resultIndexArray[2] = (s % snb) / sb;
                    resultIndexArray[3] = s % sb;
#else
                    int temp;
                    resultIndexArray[0] = Math.DivRem(s, scnb, out temp);
                    resultIndexArray[1] = Math.DivRem(temp, snb, out temp);
                    resultIndexArray[2] = Math.DivRem(temp, sb, out temp);
                    resultIndexArray[3] = temp;
#endif
                    outChars[0] = pr[resultIndexArray[0]];
                    outChars[1] = pc[resultIndexArray[1]];
                    outChars[2] = pn[resultIndexArray[2]];
                    outChars[3] = pb[resultIndexArray[3]];
                    if (reverse)
                    {
                        Swap(ref outChars[0], ref outChars[2]);
                        Swap(ref outChars[1], ref outChars[3]);
                    }

                    inData += 2;
                    outChars += 4;
                }
                if ((n & 1) != 0)
                {
                    EncodeFinalByte(*inData, outChars);
                }
            }
        }

        private static int CalculateLength(ReadOnlySpan<byte> bytes)
        {
            checked
            {
                return bytes.Length * 2;
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

        private static int DecodeShort(ReadOnlySpan<char> source, Span<byte> dest)
        {
            var reverse = cr.IndexOf(source[0]) < 0;
            Span<int> idx = !reverse
                ? stackalloc int[] { cr.IndexOf(source[0]), cc.IndexOf(source[1]), cn.IndexOf(source[2]), cb.IndexOf(source[3]) }
                : stackalloc int[] { cr.IndexOf(source[2]), cc.IndexOf(source[3]), cn.IndexOf(source[0]), cb.IndexOf(source[1]) };
            if (idx[0] < 0 || idx[1] < 0 || idx[2] < 0 || idx[3] < 0)
                throw new FormatException("not rcnb");
            var result = idx[0] * scnb + idx[1] * snb + idx[2] * sb + idx[3];
            if (result > 0x7FFF)
                throw new FormatException("rcnb overflow");
            result = reverse ? result | 0x8000 : result;
            dest[0] = (byte)(result >> 8);
            dest[1] = (byte)(result & 0xff);
            return 2;
        }

        private static int DecodeByte(ReadOnlySpan<char> source, Span<byte> dest)
        {
            var nb = false;
            Span<int> idx = stackalloc int[] { cr.IndexOf(source[0]), cc.IndexOf(source[1]) };
            if (idx[0] < 0 || idx[1] < 0)
            {
                idx[0] = cn.IndexOf(source[0]);
                idx[1] = cb.IndexOf(source[1]);
                nb = true;
            }
            if (idx[0] < 0 || idx[1] < 0)
                throw new FormatException("not rc/nb");
            var result = nb ? idx[0] * sb + idx[1] : idx[0] * sc + idx[1];
            if (result > 0x7F)
                throw new FormatException("rc/nb overflow");
            result = nb ? result | 0x80 : result;
            dest[0] = (byte)result;
            return 1;
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

            var index = 0;
            for (var i = 0; i < (str.Length >> 2); i++)
            {
                var l = DecodeShort(str.Slice(i * 4, 4), dest.Slice(index));
                index += l;
            }
            if ((str.Length & 2) != 0)
            {
                var l = DecodeByte(str.Slice(str.Length - 2, 2), dest.Slice(index));
                index += l;
            }
            return index;
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
