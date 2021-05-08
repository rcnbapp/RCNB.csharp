namespace RCNB.Acceleration
{
    using System;
#if NETCOREAPP3_1_OR_GREATER
    using System.Runtime.InteropServices;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;

    public static partial class RcnbAvx2
    {
        /// <summary>
        /// Returns whether CPU support AVX2;
        /// </summary>
        public static bool IsSupported => Avx2.IsSupported;

        private static readonly byte[] s_swizzle = new byte[16]
        {1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14};

        private static readonly uint[] s_permuted = new uint[8]
        {0, 4, 1, 5, 2, 6, 3, 7};

        private static readonly byte[] s_shuffler = new byte[16]
        {0, 1, 4, 5, 2, 3, 6, 7, 8, 9, 12, 13, 10, 11, 14, 15};

        private static readonly byte[,] s_rc_lo = new byte[2, 16]
        {
        {114, 82, 84, 85, 86, 87, 88, 89, 166, 16, 17, 18, 19, 76, 77, 0},
        {99, 67, 6, 7, 8, 9, 10, 11, 12, 13, 135, 136, 199, 59, 60, 0}
        };

        private static readonly byte[,] s_rc_hi = new byte[2, 16]
        {
        {0, 0, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 0},
        {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 2, 2, 0}
        };

        private static readonly byte[,] s_nb_lo = new byte[2, 16]
        {
        {110, 78, 67, 68, 69, 70, 71, 72, 157, 158, 209, 248, 249, 32, 53, 0},
        {98, 66, 128, 129, 131, 132, 133, 223, 222, 254, 0, 0, 0, 0, 0, 0}
        };

        private static readonly byte[,] s_nb_hi = new byte[2, 16]
        {
        {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 2, 2, 0},
        {0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        };

        internal static unsafe void EncodeRcnb(byte* value_in, char* value_out, nint n)
        {
            nint encoded = EncodeRcnbInternal(value_in, (byte*)value_out, n);
            value_in += encoded;
            value_out += encoded << 1;
            RcnbConvert.EncodeRcnb(value_in, value_out, (int)(n - encoded));
        }

        private static unsafe nint EncodeRcnbInternal(byte* value_in, byte* value_out, nint n)
        {
            var batch = n >> 5;
            if (batch == 0)
                return 0;
            unchecked
            {
                fixed (byte* swizzle = s_swizzle)
                fixed (uint* permuted = s_permuted)
                fixed (byte* shuffler = s_shuffler)
                fixed (byte* rc_lo = s_rc_lo)
                fixed (byte* rc_hi = s_rc_hi)
                fixed (byte* nb_lo = s_nb_lo)
                fixed (byte* nb_hi = s_nb_hi)
                {
                    // __m256i r_swizzle = _mm256_broadcastsi128_si256(*(__m128i *) &swizzle);
                    var r_swizzle = Avx2.BroadcastVector128ToVector256(swizzle);
                    // __m256i r_permute = *(__m256i *) &permuted;
                    var r_permute = *(Vector256<uint>*)permuted;
                    // __m256i r_shuffler = _mm256_broadcastsi128_si256(*(__m128i *) &shuffler);
                    var r_shuffler = Avx2.BroadcastVector128ToVector256(shuffler);
                    // for (size_t i = 0; i < n; ++i) {
                    for (nint i = 0; i < batch; i++)
                    {
                        // __m256i input = _mm256_loadu_si256((__m256i *) value_in);
                        var input = Avx.LoadVector256(value_in);
                        // value_in += 32;
                        value_in += 32;
                        // input = _mm256_shuffle_epi8(input, r_swizzle);
                        input = Avx2.Shuffle(input, r_swizzle);
                        // // 0xffff for neg, 0x0000 for pos
                        // __m256i sign = _mm256_srai_epi16(input, 15);
                        var sign = Avx2.ShiftRightArithmetic(input.AsInt16(), 15);
                        // input = _mm256_and_si256(input, _mm256_set1_epi16(0x7fff));
                        input = Avx2.And(input, Vector256.Create((short)0x7fff).AsByte());

                        // __m256i idx_r = _mm256_srli_epi16(_mm256_mulhi_epu16(input, _mm256_set1_epi16(-5883)), 11);
                        var idx_r = Avx2.ShiftRightLogical(Avx2.MultiplyHigh(input.AsUInt16(), Vector256.Create((ushort)-5883)), 11);
                        // __m256i r_mul_2250 = _mm256_mullo_epi16(idx_r, _mm256_set1_epi16(2250));
                        var r_mul_2250 = Avx2.MultiplyLow(idx_r, Vector256.Create((ushort)2250));
                        // __m256i i_mod_2250 = _mm256_sub_epi16(input, r_mul_2250);
                        var i_mod_2250 = Avx2.Subtract(input.AsUInt16(), r_mul_2250);
                        // __m256i idx_c = _mm256_srli_epi16(_mm256_mulhi_epu16(i_mod_2250, _mm256_set1_epi16(-9611)), 7);
                        var idx_c = Avx2.ShiftRightLogical(Avx2.MultiplyHigh(i_mod_2250, Vector256.Create((ushort)-9611)), 7);
                        // __m256i c_mul_150 = _mm256_add_epi16(r_mul_2250, _mm256_mullo_epi16(idx_c, _mm256_set1_epi16(150)));
                        var c_mul_150 = Avx2.Add(r_mul_2250, Avx2.MultiplyLow(idx_c, Vector256.Create((ushort)150)));
                        // __m256i i_mod_150 = _mm256_sub_epi16(input, c_mul_150);
                        var i_mod_150 = Avx2.Subtract(input.AsUInt16(), c_mul_150);
                        // __m256i idx_n = _mm256_srli_epi16(_mm256_mulhi_epu16(i_mod_150, _mm256_set1_epi16(-13107)), 3);
                        var idx_n = Avx2.ShiftRightLogical(Avx2.MultiplyHigh(i_mod_150, Vector256.Create((ushort)-13107)), 3);
                        // __m256i n_mul_10 = _mm256_add_epi16(c_mul_150, _mm256_mullo_epi16(idx_n, _mm256_set1_epi16(10)));
                        var n_mul_10 = Avx2.Add(c_mul_150, Avx2.MultiplyLow(idx_n, Vector256.Create((ushort)10)));
                        // __m256i idx_b = _mm256_sub_epi16(input, n_mul_10);
                        var idx_b = Avx2.Subtract(input.AsUInt16(), n_mul_10);

                        // __m256i idx_rc = _mm256_packus_epi16(idx_r, idx_c);
                        var idx_rc = Avx2.PackUnsignedSaturate(idx_r.AsInt16(), idx_c.AsInt16());
                        // __m256i idx_nb = _mm256_packus_epi16(idx_n, idx_b);
                        var idx_nb = Avx2.PackUnsignedSaturate(idx_n.AsInt16(), idx_b.AsInt16());
                        // idx_rc = _mm256_permute4x64_epi64(idx_rc, 0xd8);
                        idx_rc = Avx2.Permute4x64(idx_rc.AsInt64(), 0xd8).AsByte();
                        // idx_nb = _mm256_permute4x64_epi64(idx_nb, 0xd8);
                        idx_nb = Avx2.Permute4x64(idx_nb.AsInt64(), 0xd8).AsByte();

                        // __m256i rc_l = _mm256_shuffle_epi8(*(__m256i*)&rc_lo, idx_rc);
                        var rc_l = Avx2.Shuffle(*(Vector256<byte>*)rc_lo, idx_rc);
                        // __m256i rc_h = _mm256_shuffle_epi8(*(__m256i*)&rc_hi, idx_rc);
                        var rc_h = Avx2.Shuffle(*(Vector256<byte>*)rc_hi, idx_rc);
                        // __m256i nb_l = _mm256_shuffle_epi8(*(__m256i*)&nb_lo, idx_nb);
                        var nb_l = Avx2.Shuffle(*(Vector256<byte>*)nb_lo, idx_nb);
                        // __m256i nb_h = _mm256_shuffle_epi8(*(__m256i*)&nb_hi, idx_nb);
                        var nb_h = Avx2.Shuffle(*(Vector256<byte>*)nb_hi, idx_nb);

                        // __m256i r1c1_t = _mm256_unpacklo_epi8(rc_l, rc_h);
                        var r1c1_t = Avx2.UnpackLow(rc_l, rc_h);
                        // __m256i r2c2_t = _mm256_unpackhi_epi8(rc_l, rc_h);
                        var r2c2_t = Avx2.UnpackHigh(rc_l, rc_h);
                        // __m256i n1b1_t = _mm256_unpacklo_epi8(nb_l, nb_h);
                        var n1b1_t = Avx2.UnpackLow(nb_l, nb_h);
                        // __m256i n2b2_t = _mm256_unpackhi_epi8(nb_l, nb_h);
                        var n2b2_t = Avx2.UnpackHigh(nb_l, nb_h);

                        // __m256i sign1 = _mm256_permute4x64_epi64(sign, 0b01000100);
                        var sign1 = Avx2.Permute4x64(sign.AsInt64(), 0b01000100).AsByte();
                        // __m256i sign2 = _mm256_permute4x64_epi64(sign, 0b11101110);
                        var sign2 = Avx2.Permute4x64(sign.AsInt64(), 0b11101110).AsByte();


                        // __m256i r1c1 = _mm256_blendv_epi8(r1c1_t, n1b1_t, sign1);
                        var r1c1 = Avx2.BlendVariable(r1c1_t, n1b1_t, sign1);
                        // __m256i r2c2 = _mm256_blendv_epi8(r2c2_t, n2b2_t, sign2);
                        var r2c2 = Avx2.BlendVariable(r2c2_t, n2b2_t, sign2);
                        // __m256i n1b1 = _mm256_blendv_epi8(n1b1_t, r1c1_t, sign1);
                        var n1b1 = Avx2.BlendVariable(n1b1_t, r1c1_t, sign1);
                        // __m256i n2b2 = _mm256_blendv_epi8(n2b2_t, r2c2_t, sign2);
                        var n2b2 = Avx2.BlendVariable(n2b2_t, r2c2_t, sign2);

                        // __m256i rn1cb1 = _mm256_unpacklo_epi16(r1c1, n1b1);
                        var rn1cb1 = Avx2.UnpackLow(r1c1.AsInt16(), n1b1.AsInt16());
                        // __m256i rn2cb2 = _mm256_unpackhi_epi16(r1c1, n1b1);
                        var rn2cb2 = Avx2.UnpackHigh(r1c1.AsInt16(), n1b1.AsInt16());
                        // __m256i rn3cb3 = _mm256_unpacklo_epi16(r2c2, n2b2);
                        var rn3cb3 = Avx2.UnpackLow(r2c2.AsInt16(), n2b2.AsInt16());
                        // __m256i rn4cb4 = _mm256_unpackhi_epi16(r2c2, n2b2);
                        var rn4cb4 = Avx2.UnpackHigh(r2c2.AsInt16(), n2b2.AsInt16());

                        // __m256i rncb1 = _mm256_permutevar8x32_epi32(rn1cb1, r_permute);
                        var rncb1 = Avx2.PermuteVar8x32(rn1cb1.AsInt32(), r_permute.AsInt32());
                        // __m256i rncb2 = _mm256_permutevar8x32_epi32(rn2cb2, r_permute);
                        var rncb2 = Avx2.PermuteVar8x32(rn2cb2.AsInt32(), r_permute.AsInt32());
                        // __m256i rncb3 = _mm256_permutevar8x32_epi32(rn3cb3, r_permute);
                        var rncb3 = Avx2.PermuteVar8x32(rn3cb3.AsInt32(), r_permute.AsInt32());
                        // __m256i rncb4 = _mm256_permutevar8x32_epi32(rn4cb4, r_permute);
                        var rncb4 = Avx2.PermuteVar8x32(rn4cb4.AsInt32(), r_permute.AsInt32());

                        // __m256i rcnb1 = _mm256_shuffle_epi8(rncb1, r_shuffler);
                        var rcnb1 = Avx2.Shuffle(rncb1.AsByte(), r_shuffler);
                        // __m256i rcnb2 = _mm256_shuffle_epi8(rncb2, r_shuffler);
                        var rcnb2 = Avx2.Shuffle(rncb2.AsByte(), r_shuffler);
                        // __m256i rcnb3 = _mm256_shuffle_epi8(rncb3, r_shuffler);
                        var rcnb3 = Avx2.Shuffle(rncb3.AsByte(), r_shuffler);
                        // __m256i rcnb4 = _mm256_shuffle_epi8(rncb4, r_shuffler);
                        var rcnb4 = Avx2.Shuffle(rncb4.AsByte(), r_shuffler);

                        // if (sizeof(wchar_t) == 2)
                        // {
                        //     _mm256_storeu_si256((__m256i*)(value_out), rcnb1);
                        Avx.Store(value_out, rcnb1);
                        //     value_out += 32;
                        value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), rcnb2);
                        Avx.Store(value_out, rcnb2);
                        //     value_out += 32;
                        value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), rcnb3);
                        Avx.Store(value_out, rcnb3);
                        //     value_out += 32;
                        value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), rcnb4);
                        Avx.Store(value_out, rcnb4);
                        //     value_out += 32;
                        value_out += 32;
                        // }
                        // else if (sizeof(wchar_t) == 4)
                        // {
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb1, 0)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb1, 1)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb2, 0)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb2, 1)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb3, 0)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb3, 1)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb4, 0)));
                        //     value_out += 32;
                        //     _mm256_storeu_si256((__m256i*)(value_out), _mm256_cvtepi16_epi32(_mm256_extracti128_si256(rcnb4, 1)));
                        //     value_out += 32;
                        // }
                    }
                }
            }
            return batch << 5;
        }
    }
#else
    public static partial class RcnbAvx2
    {
        /// <summary>
        /// False because AVX2 needs .NET Core 3.1 or above.
        /// </summary>
        public static bool IsSupported => false;
        internal static unsafe void EncodeRcnb(byte* value_in, char* value_out, nint n)
        {
            throw new PlatformNotSupportedException("To use acceleration, you must use .NET Core 3.1 or above.");
        }
    }
#endif
    /// <summary>
    /// AVX2 accelerated encoding class.
    /// </summary>
    public static partial class RcnbAvx2
    {
        /// <summary>
        /// Encodes RCNB.
        /// </summary>
        /// <param name="value_in_span"></param>
        /// <param name="value_out_span"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static unsafe void EncodeRcnb(Span<byte> value_in_span, Span<char> value_out_span)
        {
            if (value_out_span.Length < (value_in_span.Length << 1))
                throw new ArgumentOutOfRangeException(nameof(value_in_span), "Input is too large to store encoded data to output.");

            fixed (byte* bytesPtr = value_in_span)
            fixed (char* charsPtr = value_out_span)
                EncodeRcnb(bytesPtr, charsPtr, value_in_span.Length);
        }
    }
}