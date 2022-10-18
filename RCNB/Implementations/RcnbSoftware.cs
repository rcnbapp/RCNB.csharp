using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RCNB.Implementations;

public static class RcnbSoftware
{
    /// <summary>
    /// Returns <c>true</c>.
    /// </summary>
    public static bool IsSupported => true;

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
    internal unsafe static void EncodeRcnb(byte* inData, char* outChars, nint n)
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

    internal unsafe static nint DecodeRcnb(char* inChars, byte* outData, nint charsLen)
    {
        nint index = 0;
        for (var i = 0; i < (charsLen >> 2); i++)
        {
            var l = DecodeShort(inChars + i * 4, outData + index);
            index += l;
        }
        if ((charsLen & 2) != 0)
        {
            var l = DecodeByte(inChars + (charsLen - 2), outData + index);
            index += l;
        }
        return index;
    }

    private unsafe static int DecodeShort(char* source, byte* dest)
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

    private unsafe static int DecodeByte(char* source, byte* dest)
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
    /// Encodes RCNB.
    /// </summary>
    /// <param name="value_in_span"></param>
    /// <param name="value_out_span"></param>
    public static unsafe void EncodeRcnb(Span<byte> value_in_span, Span<char> value_out_span)
    {
        if (value_out_span.Length < (value_in_span.Length << 1))
            throw new ArgumentOutOfRangeException(nameof(value_in_span), "Input is too large to store encoded data to output.");

        fixed (byte* bytesPtr = value_in_span)
        fixed (char* charsPtr = value_out_span)
            EncodeRcnb(bytesPtr, charsPtr, value_in_span.Length);
    }
}