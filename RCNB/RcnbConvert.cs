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

#if NETSTANDARD1_1
        public static string ToRcnbString(ReadOnlySpan<byte> inArray)
        {
            int length = CalculateLength(inArray);
            char[] resultArray = new char[length];
            EncodeRcnb(resultArray, inArray);
            return new string(resultArray);
        }
#else
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
#endif
    }
}
