using System;

namespace RCNB
{
    /// <summary>
    /// Encodes and decodes rcnb.
    /// </summary>
    public interface IRcnb
    {
        /// <summary>
        /// Tries to convert the 8-bit unsigned integers inside the specified read-only
        /// span into their equivalent string representation that is encoded with RCNB
        /// characters.
        /// </summary>
        /// <param name="inBytes">A read-only span of 8-bit unsigned integers.</param>
        /// <param name="outChars">
        /// When this method returns <c>true</c>, a span containing the string
        /// representation in RCNB of the elements in <c>bytes</c>. If the length of
        /// <c>bytes</c> is 0, or when this method returns <c>false</c>, nothing is written
        /// into this parameter.
        /// </param>
        /// <param name="charsWritten">When this method returns, the total number of
        /// characters written into <c>chars</c>.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        bool TryToRcnbChars(ReadOnlySpan<byte> inBytes, Span<char> outChars, out int charsWritten);

        /// <summary>
        /// Tries to convert the specified span containing a string representation
        /// that is encoded with RCNB characters into a span of 8-bit unsigned integers.
        /// </summary>
        /// <param name="inChars">A span containing the string representation that is
        /// encoded with RCNB characters.</param>
        /// <param name="outBytes">
        /// When this method returns <c>true</c>, the converted 8-bit
        /// unsigned integers. When this method returns <c>false</c>, either the span remains
        /// unmodified or contains an incomplete conversion of <c>chars</c>, up to the last
        /// valid character.
        /// </param>
        /// <param name="bytesWritten">
        /// When this method returns, the number of bytes that were written in <c>bytes.</c>
        /// </param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        bool TryFromRcnbChars(ReadOnlySpan<char> inChars, Span<byte> outBytes, out int bytesWritten);
    }
}
