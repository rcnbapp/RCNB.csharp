using System;
using System.Diagnostics;

namespace RCNB
{
    public static class RcnbConvert
    {
        // char
        private const string cr = "rRŔŕŖŗŘřƦȐȑȒȓɌɍ";
        private const string cc = "cCĆćĈĉĊċČčƇƈÇȻȼ";
        private const string cn = "nNŃńŅņŇňƝƞÑǸǹȠȵ";
        private const string cb = "bBƀƁƃƄƅßÞþ";

        // size
        private static readonly int sr = cr.Length; // 15
        private static readonly int sc = cc.Length; // 15
        private static readonly int sn = cn.Length; // 15
        private static readonly int sb = cb.Length; // 10
        private static readonly int src = sr * sc;
        private static readonly int snb = sn * sb;
        private static readonly int scnb = sc * snb;

        private static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        private static void EncodeByte(byte b, Span<char> dest, ref int index)
        {
            int i = b;
            Span<char> result = stackalloc char[2];
            if (i > 0x7F)
            {
                i = i & 0x7F;
                result[0] = cn[i / sb];
                result[1] = cb[i % sb];
            }
            else
            {
                result[0] = cr[i / sc];
                result[1] = cc[i % sc];
            }
            result.CopyTo(dest.Slice(index));
            index += result.Length;
        }

        private static void EncodeShort(int s, Span<char> dest, ref int index)
        {
            Debug.Assert(s <= 0xFFFF);
            var reverse = false;
            if (s > 0x7FFF)
            {
                reverse = true;
                s = s & 0x7FFF;
            }
            Span<int> resultIndexArray = stackalloc[]
            {
                s / scnb,
                (s % scnb) / snb,
                (s % snb) / sb,
                s % sb,
            };
            Span<char> resultArray = stackalloc[]
            {
                cr[resultIndexArray[0]],
                cc[resultIndexArray[1]],
                cn[resultIndexArray[2]],
                cb[resultIndexArray[3]],
            };
            if (reverse)
            {
                Swap(ref resultArray[0], ref resultArray[2]);
                Swap(ref resultArray[1], ref resultArray[3]);
            }
            resultArray.CopyTo(dest.Slice(index));
            index += resultArray.Length;
        }

        private static void EncodeTwoBytes(ReadOnlySpan<byte> inArray, int i, Span<char> dest, ref int index)
            => EncodeShort((inArray[i] << 8) | inArray[i + 1], dest, ref index);

        private static int CalculateLength(ReadOnlySpan<byte> inArray)
        {
            checked
            {
                return inArray.Length * 2;
            }
        }

        private static void EncodeRcnb(Span<char> resultArray, ReadOnlySpan<byte> inArray)
        {
            int resultIndex = 0;
            for (var i = 0; i < inArray.Length >> 1; i++)
            {
                EncodeTwoBytes(inArray, i * 2, resultArray, ref resultIndex);
            }
            if ((inArray.Length & 1) != 0)
            {
                EncodeByte(inArray[inArray.Length - 1], resultArray, ref resultIndex);
            }
            Debug.Assert(resultIndex == resultArray.Length);
        }

        /// <summary>
        /// Encode RCNB.
        /// </summary>
        /// <param name="inArray">Data to encode.</param>
        /// <returns>Encoded RCNB string.</returns>
#if NETSTANDARD1_1 || NETSTANDARD2_0
        public static string ToRcnbString(ReadOnlySpan<byte> inArray)
        {
            int length = CalculateLength(inArray);
            char[] resultArray = new char[length];
            EncodeRcnb(resultArray, inArray);
            return new string(resultArray);
        }
#else
        public static unsafe string ToRcnbString(ReadOnlySpan<byte> inArray)
        {
            int length = CalculateLength(inArray);
            //fixed (byte* data = &inArray.GetPinnableReference())
            fixed (byte* data = inArray) // it seems to work?
            {
                return string.Create(length,
                    new ByteMemoryMedium(data, inArray.Length),
                    (span, a) => EncodeRcnb(span, new ReadOnlySpan<byte>(a.Pointer, a.Length)));
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
        /// <param name="inArray">Content to encode.</param>
        /// <returns>The encoded content.</returns>
        public static string ToRcnbString(ReadOnlyMemory<byte> inArray)
        {
            int length = CalculateLength(inArray.Span);
            return string.Create(length, inArray, (span, a) => EncodeRcnb(span, a.Span));
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

        private static void FromRcnbString(ReadOnlySpan<char> str, Span<byte> dest)
        {
            if (str.Length / 2 != dest.Length)
                throw new ArgumentException("The length of destination is not apt.", nameof(dest));
            if ((str.Length & 1) != 0)
                throw new FormatException("invalid length.");

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
            Debug.Assert(index == dest.Length);
        }

        /// <summary>
        /// Decode RCNB string.
        /// </summary>
        /// <param name="str">RCNB string.</param>
        /// <returns>Decoded data.</returns>
        public static byte[] FromRcnbString(ReadOnlySpan<char> str)
        {
            byte[] result = new byte[str.Length / 2];
            FromRcnbString(str, result);
            return result;
        }
    }
}
