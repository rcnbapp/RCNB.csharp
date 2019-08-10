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
        private static readonly int sr = cr.Length;
        private static readonly int sc = cc.Length;
        private static readonly int sn = cn.Length;
        private static readonly int sb = cb.Length;
        private static readonly int src = sr * sc;
        private static readonly int snb = sn * sb;
        private static readonly int scnb = sc * snb;

        private static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        private static void EncodeByte(byte b, char[] dest, ref int index)
        {
            int i = b;
            char[] result;
            if (i > 0x7F)
            {
                i = i & 0x7F;
                result = new[] { cn[i / sb], cb[i % sb] };
            }
            else
            {
                result = new[] { cr[i / sc], cc[i % sc] };
            }
            Array.Copy(result, 0, dest, index, result.Length);
            index += result.Length;
        }

        private static void EncodeShort(int s, char[] dest, ref int index)
        {
            var reverse = false;
            if (s > 0x7FFF)
            {
                reverse = true;
                s = s & 0x7FFF;
            }
            var resultIndexArray = new[]
            {
                s / scnb,
                (s % scnb) / snb,
                (s % snb) / sb,
                s % sb,
            };
            var resultArray = new[]
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
            Array.Copy(resultArray, 0, dest, index, resultArray.Length);
            index += resultArray.Length;
        }

        private static void EncodeTwoBytes(byte[] inArray, int i, char[] dest, ref int index)
            => EncodeShort((inArray[i] << 8) | inArray[i + 1], dest, ref index);

        private static int CalculateLength(byte[] inArray)
        {
            checked
            {
                return inArray.Length * 2;
            }
        }

        public static string ToRcnbString(byte[] inArray)
        {
            char[] resultArray = new char[CalculateLength(inArray)];
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
            return new string(resultArray);
        }
    }
}
