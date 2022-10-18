using System;

namespace RCNB.Implementations;

internal static class RcnbUtility
{
    internal static int CalculateLength(ReadOnlySpan<byte> bytes)
    {
        checked
        {
            return bytes.Length * 2;
        }
    }
}