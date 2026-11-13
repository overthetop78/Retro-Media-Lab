using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Mp3FrameDecoder
{
    private void bs_init(CPointer<bs_t> bs, CPointer<byte> data, int bytes)
    {
        unchecked
        {
            bs[0].buf = data;
            bs[0].pos = 0;
            bs[0].limit = (bytes * 8);
        }
    }
    private uint get_bits(CPointer<bs_t> bs, int n)
    {
        unchecked
        {
            uint next = default;
            uint cache = unchecked((uint)(0));
            uint s = unchecked((uint)((bs[0].pos & 7)));
            int shl = unchecked((int)((unchecked((uint)(n)) + s)));
            CPointer<byte> p = (bs[0].buf + (bs[0].pos >> 3));
            if (((bs[0].pos += n) > bs[0].limit)) return unchecked((uint)(0));
            next = unchecked((uint)((unchecked((int)((p++)[0])) & (255 >> unchecked((int)(s))))));
            while (((shl -= 8) > 0)) {
            cache |= (next << shl);
            next = unchecked((uint)((p++)[0]));
            }
            return (cache | (next >> (-shl)));
        }
    }
    private int hdr_valid(CPointer<byte> h)
    {
        unchecked
        {
            return ((((((unchecked((int)(h[(int)(0)])) == 255) && (((unchecked((int)(h[(int)(1)])) & 240) == 240) || ((unchecked((int)(h[(int)(1)])) & 254) == 226))) && (((unchecked((int)(h[(int)(1)])) >> 1) & 3) != 0)) && ((unchecked((int)(h[(int)(2)])) >> 4) != 15)) && (((unchecked((int)(h[(int)(2)])) >> 2) & 3) != 3)) ? (int)1 : (int)0);
        }
    }
    private int hdr_compare(CPointer<byte> h1, CPointer<byte> h2)
    {
        unchecked
        {
            return (((((hdr_valid(h2) != 0) && (((unchecked((int)(h1[(int)(1)])) ^ unchecked((int)(h2[(int)(1)]))) & 254) == 0)) && (((unchecked((int)(h1[(int)(2)])) ^ unchecked((int)(h2[(int)(2)]))) & 12) == 0)) && (!(((((unchecked((int)(h1[(int)(2)])) & 240) == 0) ? (int)1 : (int)0) ^ (((unchecked((int)(h2[(int)(2)])) & 240) == 0) ? (int)1 : (int)0)) != 0))) ? (int)1 : (int)0);
        }
    }
    private uint hdr_bitrate_kbps(CPointer<byte> h)
    {
        unchecked
        {
            return (uint)(2 * halfrate[((h[1] & 8) != 0 ? 45 : 0) + (((h[1] >> 1) & 3) - 1) * 15 + (h[2] >> 4)]);
        }
    }
    private uint hdr_sample_rate_hz(CPointer<byte> h)
    {
        unchecked
        {
            return ((g_hz[(int)(((unchecked((int)(h[(int)(2)])) >> 2) & 3))] >> ((!((unchecked((int)(h[(int)(1)])) & 8) != 0)) ? (int)1 : (int)0)) >> ((!((unchecked((int)(h[(int)(1)])) & 16) != 0)) ? (int)1 : (int)0));
        }
    }
    private uint hdr_frame_samples(CPointer<byte> h)
    {
        unchecked
        {
            return unchecked((uint)((((unchecked((int)(h[(int)(1)])) & 6) == 6) ? 384 : (1152 >> (((unchecked((int)(h[(int)(1)])) & 14) == 2) ? (int)1 : (int)0)))));
        }
    }
    private int hdr_frame_bytes(CPointer<byte> h, int free_format_size)
    {
        unchecked
        {
            int frame_bytes = unchecked((int)((((hdr_frame_samples(h) * hdr_bitrate_kbps(h)) * unchecked((uint)(125))) / hdr_sample_rate_hz(h))));
            if (((unchecked((int)(h[(int)(1)])) & 6) == 6)) {
            frame_bytes &= (~3);
            }
            return ((frame_bytes != 0) ? frame_bytes : free_format_size);
        }
    }
    private int hdr_padding(CPointer<byte> h)
    {
        unchecked
        {
            return (((unchecked((int)(h[(int)(2)])) & 2) != 0) ? (((unchecked((int)(h[(int)(1)])) & 6) == 6) ? 4 : 1) : 0);
        }
    }
    private CPointer<L12_subband_alloc_t> L12_subband_alloc_table(CPointer<byte> hdr, CPointer<L12_scale_info> sci)
    {
        unchecked
        {
            CPointer<L12_subband_alloc_t> alloc = default;
            int mode = ((unchecked((int)(hdr[(int)(3)])) >> 6) & 3);
            int nbands = default;
            int stereo_bands = ((mode == 3) ? 0 : ((mode == 1) ? ((((unchecked((int)(hdr[(int)(3)])) >> 4) & 3) << 2) + 4) : 32));
            if (((unchecked((int)(hdr[(int)(1)])) & 6) == 6)) {
            alloc = new CPointer<L12_subband_alloc_t>(g_alloc_L1);
            nbands = 32;
            } else if ((!((unchecked((int)(hdr[(int)(1)])) & 8) != 0))) {
            alloc = new CPointer<L12_subband_alloc_t>(g_alloc_L2M2);
            nbands = 30;
            } else {
            int sample_rate_idx = ((unchecked((int)(hdr[(int)(2)])) >> 2) & 3);
            uint kbps = (hdr_bitrate_kbps(hdr) >> ((mode != 3) ? (int)1 : (int)0));
            if ((!(kbps != 0))) {
            kbps = unchecked((uint)(192));
            }
            alloc = new CPointer<L12_subband_alloc_t>(g_alloc_L2M1);
            nbands = 27;
            if ((kbps < unchecked((uint)(56)))) {
            alloc = new CPointer<L12_subband_alloc_t>(g_alloc_L2M1_lowrate);
            nbands = ((sample_rate_idx == 2) ? 12 : 8);
            } else if (((kbps >= unchecked((uint)(96))) && (sample_rate_idx != 1))) {
            nbands = 30;
            }
            }
            sci[0].total_bands = unchecked((byte)(nbands));
            sci[0].stereo_bands = unchecked((byte)(((stereo_bands > nbands) ? nbands : stereo_bands)));
            return alloc;
        }
    }
    private void L12_read_scalefactors(CPointer<bs_t> bs, CPointer<byte> pba, CPointer<byte> scfcod, int bands, CPointer<float> scf)
    {
        unchecked
        {
            int i = default;
            int m = default;
            for (i = 0; (i < bands); i++) {
            float s = unchecked((float)(0));
            int ba = unchecked((int)((pba++)[0]));
            int mask = ((ba != 0) ? (4 + ((19 >> unchecked((int)(scfcod[(int)(i)]))) & 3)) : 0);
            for (m = 4; (m != 0); m >>= 1) {
            if (((mask & m) != 0)) {
            int b = unchecked((int)(get_bits(bs, 6)));
            s = (g_deq_L12[(int)((((ba * 3) - 6) + (b % 3)))] * unchecked((float)(((1 << 21) >> (b / 3)))));
            }
            (scf++)[0] = s;
            }
            }
        }
    }
    private void L12_read_scale_info(CPointer<byte> hdr, CPointer<bs_t> bs, CPointer<L12_scale_info> sci)
    {
        unchecked
        {
            CPointer<L12_subband_alloc_t> subband_alloc = L12_subband_alloc_table(hdr, sci);
            int i = default;
            int k = 0;
            int ba_bits = 0;
            CPointer<byte> ba_code_tab = new CPointer<byte>(g_bitalloc_code_tab);
            for (i = 0; (i < unchecked((int)(sci[0].total_bands))); i++) {
            byte ba = default;
            if ((i == k)) {
            k += unchecked((int)(subband_alloc[0].band_count));
            ba_bits = unchecked((int)(subband_alloc[0].code_tab_width));
            ba_code_tab = (new CPointer<byte>(g_bitalloc_code_tab) + unchecked((int)(subband_alloc[0].tab_offset)));
            subband_alloc++;
            }
            ba = ba_code_tab[(int)(get_bits(bs, ba_bits))];
            sci[0].bitalloc[(int)((2 * i))] = ba;
            if ((i < unchecked((int)(sci[0].stereo_bands)))) {
            ba = ba_code_tab[(int)(get_bits(bs, ba_bits))];
            }
            sci[0].bitalloc[(int)(((2 * i) + 1))] = unchecked((byte)(((sci[0].stereo_bands != 0) ? unchecked((int)(ba)) : 0)));
            }
            for (i = 0; (i < (2 * unchecked((int)(sci[0].total_bands)))); i++) {
            sci[0].scfcod[(int)(i)] = unchecked((byte)(((sci[0].bitalloc[(int)(i)] != 0) ? (((unchecked((int)(hdr[(int)(1)])) & 6) == 6) ? unchecked((uint)(2)) : get_bits(bs, 2)) : unchecked((uint)(6)))));
            }
            L12_read_scalefactors(bs, new CPointer<byte>(sci[0].bitalloc), new CPointer<byte>(sci[0].scfcod), (unchecked((int)(sci[0].total_bands)) * 2), new CPointer<float>(sci[0].scf));
            for (i = unchecked((int)(sci[0].stereo_bands)); (i < unchecked((int)(sci[0].total_bands))); i++) {
            sci[0].bitalloc[(int)(((2 * i) + 1))] = unchecked((byte)(0));
            }
        }
    }
    private int L12_dequantize_granule(CPointer<float> grbuf, CPointer<bs_t> bs, CPointer<L12_scale_info> sci, int group_size)
    {
        unchecked
        {
            int i = default;
            int j = default;
            int k = default;
            int choff = 576;
            for (j = 0; (j < 4); j++) {
            CPointer<float> dst = (grbuf + (group_size * j));
            for (i = 0; (i < (2 * unchecked((int)(sci[0].total_bands)))); i++) {
            int ba = unchecked((int)(sci[0].bitalloc[(int)(i)]));
            if ((ba != 0)) {
            if ((ba < 17)) {
            int half = ((1 << (ba - 1)) - 1);
            for (k = 0; (k < group_size); k++) {
            dst[(int)(k)] = unchecked((float)((unchecked((int)(get_bits(bs, ba))) - half)));
            }
            } else {
            uint mod = unchecked((uint)(((2 << (ba - 17)) + 1)));
            uint code = get_bits(bs, unchecked((int)(((mod + unchecked((uint)(2))) - (mod >> 3)))));
            for (k = 0; (k < group_size); k++, code /= mod) {
            dst[(int)(k)] = unchecked((float)(unchecked((int)(((code % mod) - (mod / unchecked((uint)(2))))))));
            }
            }
            }
            dst += choff;
            choff = (18 - choff);
            }
            }
            return (group_size * 4);
        }
    }
    private void L12_apply_scf_384(CPointer<L12_scale_info> sci, CPointer<float> scf, CPointer<float> dst)
    {
        unchecked
        {
            int i = default;
            int k = default;
            CopyFloats(((dst + 576) + (unchecked((int)(sci[0].stereo_bands)) * 18)), (dst + (unchecked((int)(sci[0].stereo_bands)) * 18)), ((unchecked((int)(sci[0].total_bands)) - unchecked((int)(sci[0].stereo_bands))) * 18));
            for (i = 0; (i < unchecked((int)(sci[0].total_bands))); i++, dst += 18, scf += 6) {
            for (k = 0; (k < 12); k++) {
            dst[(int)((k + 0))] *= scf[(int)(0)];
            dst[(int)((k + 576))] *= scf[(int)(3)];
            }
            }
        }
    }
    private int L3_read_side_info(CPointer<bs_t> bs, CPointer<L3_gr_info_t> gr, CPointer<byte> hdr)
    {
        unchecked
        {
            uint tables = default;
            uint scfsi = unchecked((uint)(0));
            int main_data_begin = default;
            int part_23_sum = 0;
            int sr_idx = (((unchecked((int)(hdr[(int)(2)])) >> 2) & 3) + ((((unchecked((int)(hdr[(int)(1)])) >> 3) & 1) + ((unchecked((int)(hdr[(int)(1)])) >> 4) & 1)) * 3));
            sr_idx -= ((sr_idx != 0) ? (int)1 : (int)0);
            int gr_count = (((unchecked((int)(hdr[(int)(3)])) & 192) == 192) ? 1 : 2);
            if (((unchecked((int)(hdr[(int)(1)])) & 8) != 0)) {
            gr_count *= 2;
            main_data_begin = unchecked((int)(get_bits(bs, 9)));
            scfsi = get_bits(bs, (7 + gr_count));
            } else {
            main_data_begin = unchecked((int)((get_bits(bs, (8 + gr_count)) >> gr_count)));
            }
            do {
            if (((unchecked((int)(hdr[(int)(3)])) & 192) == 192)) {
            scfsi <<= 4;
            }
            gr[0].part_23_length = unchecked((ushort)(get_bits(bs, 12)));
            part_23_sum += unchecked((int)(gr[0].part_23_length));
            gr[0].big_values = unchecked((ushort)(get_bits(bs, 9)));
            if ((unchecked((int)(gr[0].big_values)) > 288)) {
            return (-1);
            }
            gr[0].global_gain = unchecked((byte)(get_bits(bs, 8)));
            gr[0].scalefac_compress = unchecked((ushort)(get_bits(bs, (((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? 4 : 9))));
            gr[0].sfbtab = new CPointer<byte>(g_scf_long, (int)(sr_idx) * 23);
            gr[0].n_long_sfb = unchecked((byte)(22));
            gr[0].n_short_sfb = unchecked((byte)(0));
            if ((get_bits(bs, 1) != 0)) {
            gr[0].block_type = unchecked((byte)(get_bits(bs, 2)));
            if ((!(gr[0].block_type != 0))) {
            return (-1);
            }
            gr[0].mixed_block_flag = unchecked((byte)(get_bits(bs, 1)));
            gr[0].region_count[(int)(0)] = unchecked((byte)(7));
            gr[0].region_count[(int)(1)] = unchecked((byte)(255));
            if ((unchecked((int)(gr[0].block_type)) == 2)) {
            scfsi &= unchecked((uint)(3855));
            if ((!(gr[0].mixed_block_flag != 0))) {
            gr[0].region_count[(int)(0)] = unchecked((byte)(8));
            gr[0].sfbtab = new CPointer<byte>(g_scf_short, (int)(sr_idx) * 40);
            gr[0].n_long_sfb = unchecked((byte)(0));
            gr[0].n_short_sfb = unchecked((byte)(39));
            } else {
            gr[0].sfbtab = new CPointer<byte>(g_scf_mixed, (int)(sr_idx) * 40);
            gr[0].n_long_sfb = unchecked((byte)((((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? 8 : 6)));
            gr[0].n_short_sfb = unchecked((byte)(30));
            }
            }
            tables = get_bits(bs, 10);
            tables <<= 5;
            gr[0].subblock_gain[(int)(0)] = unchecked((byte)(get_bits(bs, 3)));
            gr[0].subblock_gain[(int)(1)] = unchecked((byte)(get_bits(bs, 3)));
            gr[0].subblock_gain[(int)(2)] = unchecked((byte)(get_bits(bs, 3)));
            } else {
            gr[0].block_type = unchecked((byte)(0));
            gr[0].mixed_block_flag = unchecked((byte)(0));
            tables = get_bits(bs, 15);
            gr[0].region_count[(int)(0)] = unchecked((byte)(get_bits(bs, 4)));
            gr[0].region_count[(int)(1)] = unchecked((byte)(get_bits(bs, 3)));
            gr[0].region_count[(int)(2)] = unchecked((byte)(255));
            }
            gr[0].table_select[(int)(0)] = unchecked((byte)((tables >> 10)));
            gr[0].table_select[(int)(1)] = unchecked((byte)(((tables >> 5) & unchecked((uint)(31)))));
            gr[0].table_select[(int)(2)] = unchecked((byte)((tables & unchecked((uint)(31)))));
            gr[0].preflag = unchecked((byte)((((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? get_bits(bs, 1) : ((unchecked((int)(gr[0].scalefac_compress)) >= 500) ? (uint)1 : (uint)0))));
            gr[0].scalefac_scale = unchecked((byte)(get_bits(bs, 1)));
            gr[0].count1_table = unchecked((byte)(get_bits(bs, 1)));
            gr[0].scfsi = unchecked((byte)(((scfsi >> 12) & unchecked((uint)(15)))));
            scfsi <<= 4;
            gr++;
            } while (((--gr_count) != 0));
            if (((part_23_sum + bs[0].pos) > (bs[0].limit + (main_data_begin * 8)))) {
            return (-1);
            }
            return main_data_begin;
        }
    }
    private void L3_read_scalefactors(CPointer<byte> scf, CPointer<byte> ist_pos, CPointer<byte> scf_size, CPointer<byte> scf_count, CPointer<bs_t> bitbuf, int scfsi)
    {
        unchecked
        {
            int i = default;
            int k = default;
            for (i = 0; ((i < 4) && (scf_count[(int)(i)] != 0)); i++, scfsi *= 2) {
            int cnt = unchecked((int)(scf_count[(int)(i)]));
            if (((scfsi & 8) != 0)) {
            CopyBytes(scf, ist_pos, cnt);
            } else {
            int bits = unchecked((int)(scf_size[(int)(i)]));
            if ((!(bits != 0))) {
            FillBytes(scf, 0, cnt);
            FillBytes(ist_pos, 0, cnt);
            } else {
            int max_scf = ((scfsi < 0) ? ((1 << bits) - 1) : (-1));
            for (k = 0; (k < cnt); k++) {
            int s = unchecked((int)(get_bits(bitbuf, bits)));
            ist_pos[(int)(k)] = unchecked((byte)(((s == max_scf) ? (-1) : s)));
            scf[(int)(k)] = unchecked((byte)(s));
            }
            }
            }
            ist_pos += cnt;
            scf += cnt;
            }
            scf[(int)(0)] = (scf[(int)(1)] = (scf[(int)(2)] = unchecked((byte)(0))));
        }
    }
    private float L3_ldexp_q2(float y, int exp_q2)
    {
        unchecked
        {
            int e = default;
            do {
            e = (((30 * 4) > exp_q2) ? exp_q2 : (30 * 4));
            y *= (g_expfrac[(int)((e & 3))] * unchecked((float)(((1 << 30) >> (e >> 2)))));
            } while (((exp_q2 -= e) > 0));
            return y;
        }
    }
    private void L3_decode_scalefactors(CPointer<byte> hdr, CPointer<byte> ist_pos, CPointer<bs_t> bs, CPointer<L3_gr_info_t> gr, CPointer<float> scf, int ch)
    {
        unchecked
        {
            CPointer<byte> scf_partition = new CPointer<byte>(g_scf_partitions, (int)((((!(!(gr[0].n_short_sfb != 0))) ? (int)1 : (int)0) + ((!(gr[0].n_long_sfb != 0)) ? (int)1 : (int)0))) * 28);
            CPointer<byte> scf_size = default;
            Span<byte> iscf = stackalloc byte[(int)(40)];
            int i = default;
            int scf_shift = (unchecked((int)(gr[0].scalefac_scale)) + 1);
            int gain_exp = default;
            int scfsi = unchecked((int)(gr[0].scfsi));
            float gain = default;
            if (((unchecked((int)(hdr[(int)(1)])) & 8) != 0)) {
            int part = unchecked((int)(g_scfc_decode[(int)(gr[0].scalefac_compress)]));
            scf_size[(int)(1)] = (scf_size[(int)(0)] = unchecked((byte)((part >> 2))));
            scf_size[(int)(3)] = (scf_size[(int)(2)] = unchecked((byte)((part & 3))));
            } else {
            int k = default;
            int modprod = default;
            int sfc = default;
            int ist = ((((unchecked((int)(hdr[(int)(3)])) & 16) != 0) && (ch != 0)) ? (int)1 : (int)0);
            sfc = (unchecked((int)(gr[0].scalefac_compress)) >> ist);
            for (k = ((ist * 3) * 4); (sfc >= 0); sfc -= modprod, k += 4) {
            modprod = 1;
            for (i = 3; (i >= 0); i--) {
            scf_size[(int)(i)] = unchecked((byte)(((sfc / modprod) % unchecked((int)(g_mod[(int)((k + i))])))));
            modprod *= unchecked((int)(g_mod[(int)((k + i))]));
            }
            }
            scf_partition += k;
            scfsi = (-16);
            }
            L3_read_scalefactors(new CPointer<byte>(iscf), ist_pos, scf_size, scf_partition, bs, scfsi);
            if ((gr[0].n_short_sfb != 0)) {
            int sh = (3 - scf_shift);
            for (i = 0; (i < unchecked((int)(gr[0].n_short_sfb))); i++) {
            iscf[(int)((unchecked((int)(gr[0].n_long_sfb)) + i))] += unchecked((byte)((unchecked((int)(gr[0].subblock_gain[(int)((i & 3))])) << sh)));
            }
            } else if ((gr[0].preflag != 0)) {
            for (i = 0; (i < 10); i++) {
            iscf[(int)((11 + i))] += g_preamp[(int)(i)];
            }
            }
            gain_exp = (((unchecked((int)(gr[0].global_gain)) + ((-1) * 4)) - 210) - (((unchecked((int)(hdr[(int)(3)])) & 224) == 96) ? 2 : 0));
            gain = L3_ldexp_q2(unchecked((float)((1 << (((((255 + ((-1) * 4)) - 210) + 3) & (~3)) / 4)))), (((((255 + ((-1) * 4)) - 210) + 3) & (~3)) - gain_exp));
            for (i = 0; (i < (unchecked((int)(gr[0].n_long_sfb)) + unchecked((int)(gr[0].n_short_sfb)))); i++) {
            scf[(int)(i)] = L3_ldexp_q2(gain, (unchecked((int)(iscf[(int)(i)])) << scf_shift));
            }
        }
    }
    private float L3_pow_43(int x)
    {
        unchecked
        {
            float frac = default;
            int sign = default;
            int mult = 256;
            if ((x < 129)) {
            return g_pow43[(int)((16 + x))];
            }
            if ((x < 1024)) {
            mult = 16;
            x <<= 3;
            }
            sign = ((2 * x) & 64);
            frac = (unchecked((float)(((x & 63) - sign))) / unchecked((float)(((x & (~63)) + sign))));
            return ((g_pow43[(int)((16 + ((x + sign) >> 6)))] * (1.0f + (frac * ((4.0f / unchecked((float)(3))) + (frac * (2.0f / unchecked((float)(9)))))))) * unchecked((float)(mult)));
        }
    }
    private void L3_huffman(CPointer<float> dst, CPointer<bs_t> bs, CPointer<L3_gr_info_t> gr_info, CPointer<float> scf, int layer3gr_limit)
    {
        unchecked
        {
            float one = 0.0f;
            int ireg = 0;
            int big_val_cnt = unchecked((int)(gr_info[0].big_values));
            CPointer<byte> sfb = gr_info[0].sfbtab;
            CPointer<byte> bs_next_ptr = (bs[0].buf + (bs[0].pos / 8));
            uint bs_cache = (((((((unchecked((uint)(bs_next_ptr[(int)(0)])) * 256u) + unchecked((uint)(bs_next_ptr[(int)(1)]))) * 256u) + unchecked((uint)(bs_next_ptr[(int)(2)]))) * 256u) + unchecked((uint)(bs_next_ptr[(int)(3)]))) << (bs[0].pos & 7));
            int pairs_to_decode = default;
            int np = default;
            int bs_sh = ((bs[0].pos & 7) - 8);
            bs_next_ptr += 4;
            while ((big_val_cnt > 0)) {
            int tab_num = unchecked((int)(gr_info[0].table_select[(int)(ireg)]));
            int sfb_cnt = unchecked((int)(gr_info[0].region_count[(int)((ireg++))]));
            CPointer<short> codebook = (new CPointer<short>(tabs) + unchecked((int)(tabindex[(int)(tab_num)])));
            int linbits = unchecked((int)(g_linbits[(int)(tab_num)]));
            if ((linbits != 0)) {
            do {
            np = (unchecked((int)((sfb++)[0])) / 2);
            pairs_to_decode = ((big_val_cnt > np) ? np : big_val_cnt);
            one = (scf++)[0];
            do {
            int j = default;
            int w = 5;
            int leaf = unchecked((int)(codebook[(int)((bs_cache >> (32 - w)))]));
            while ((leaf < 0)) {
            {
            bs_cache <<= w;
            bs_sh += w;
            }
            ;
            w = (leaf & 7);
            leaf = unchecked((int)(codebook[(int)(((bs_cache >> (32 - w)) - unchecked((uint)((leaf >> 3)))))]));
            }
            {
            bs_cache <<= (leaf >> 8);
            bs_sh += (leaf >> 8);
            }
            ;
            for (j = 0; (j < 2); j++, dst++, leaf >>= 4) {
            int lsb = (leaf & 15);
            if ((lsb == 15)) {
            lsb += unchecked((int)((bs_cache >> (32 - linbits))));
            {
            bs_cache <<= linbits;
            bs_sh += linbits;
            }
            ;
            while ((bs_sh >= 0)) {
            bs_cache |= (unchecked((uint)((bs_next_ptr++)[0])) << bs_sh);
            bs_sh -= 8;
            }
            ;
            dst[0] = ((one * L3_pow_43(lsb)) * unchecked((float)(((unchecked((int)(bs_cache)) < 0) ? (-1) : 1))));
            } else {
            dst[0] = (g_pow43[(int)((unchecked((uint)((16 + lsb))) - (unchecked((uint)(16)) * (bs_cache >> 31))))] * one);
            }
            {
            bs_cache <<= ((lsb != 0) ? 1 : 0);
            bs_sh += ((lsb != 0) ? 1 : 0);
            }
            ;
            }
            while ((bs_sh >= 0)) {
            bs_cache |= (unchecked((uint)((bs_next_ptr++)[0])) << bs_sh);
            bs_sh -= 8;
            }
            ;
            } while (((--pairs_to_decode) != 0));
            } while ((((big_val_cnt -= np) > 0) && ((--sfb_cnt) >= 0)));
            } else {
            do {
            np = (unchecked((int)((sfb++)[0])) / 2);
            pairs_to_decode = ((big_val_cnt > np) ? np : big_val_cnt);
            one = (scf++)[0];
            do {
            int j = default;
            int w = 5;
            int leaf = unchecked((int)(codebook[(int)((bs_cache >> (32 - w)))]));
            while ((leaf < 0)) {
            {
            bs_cache <<= w;
            bs_sh += w;
            }
            ;
            w = (leaf & 7);
            leaf = unchecked((int)(codebook[(int)(((bs_cache >> (32 - w)) - unchecked((uint)((leaf >> 3)))))]));
            }
            {
            bs_cache <<= (leaf >> 8);
            bs_sh += (leaf >> 8);
            }
            ;
            for (j = 0; (j < 2); j++, dst++, leaf >>= 4) {
            int lsb = (leaf & 15);
            dst[0] = (g_pow43[(int)((unchecked((uint)((16 + lsb))) - (unchecked((uint)(16)) * (bs_cache >> 31))))] * one);
            {
            bs_cache <<= ((lsb != 0) ? 1 : 0);
            bs_sh += ((lsb != 0) ? 1 : 0);
            }
            ;
            }
            while ((bs_sh >= 0)) {
            bs_cache |= (unchecked((uint)((bs_next_ptr++)[0])) << bs_sh);
            bs_sh -= 8;
            }
            ;
            } while (((--pairs_to_decode) != 0));
            } while ((((big_val_cnt -= np) > 0) && ((--sfb_cnt) >= 0)));
            }
            }
            for (np = (1 - big_val_cnt); true; dst += 4) {
            CPointer<byte> codebook_count1 = ((gr_info[0].count1_table != 0) ? new CPointer<byte>(tab33) : new CPointer<byte>(tab32));
            int leaf = unchecked((int)(codebook_count1[(int)((bs_cache >> (32 - 4)))]));
            if ((!((leaf & 8) != 0))) {
            leaf = unchecked((int)(codebook_count1[(int)((unchecked((uint)((leaf >> 3))) + ((bs_cache << 4) >> (32 - (leaf & 3)))))]));
            }
            {
            bs_cache <<= (leaf & 7);
            bs_sh += (leaf & 7);
            }
            ;
            if ((((((bs_next_ptr - bs[0].buf) * 8) - 24) + bs_sh) > layer3gr_limit)) {
            break;
            }
            if ((!((--np) != 0))) {
            np = (unchecked((int)((sfb++)[0])) / 2);
            if ((!(np != 0))) break;
            one = (scf++)[0];
            }
            ;
            if (((leaf & (128 >> 0)) != 0)) {
            dst[(int)(0)] = ((unchecked((int)(bs_cache)) < 0) ? (-one) : one);
            {
            bs_cache <<= 1;
            bs_sh += 1;
            }
            }
            ;
            if (((leaf & (128 >> 1)) != 0)) {
            dst[(int)(1)] = ((unchecked((int)(bs_cache)) < 0) ? (-one) : one);
            {
            bs_cache <<= 1;
            bs_sh += 1;
            }
            }
            ;
            if ((!((--np) != 0))) {
            np = (unchecked((int)((sfb++)[0])) / 2);
            if ((!(np != 0))) break;
            one = (scf++)[0];
            }
            ;
            if (((leaf & (128 >> 2)) != 0)) {
            dst[(int)(2)] = ((unchecked((int)(bs_cache)) < 0) ? (-one) : one);
            {
            bs_cache <<= 1;
            bs_sh += 1;
            }
            }
            ;
            if (((leaf & (128 >> 3)) != 0)) {
            dst[(int)(3)] = ((unchecked((int)(bs_cache)) < 0) ? (-one) : one);
            {
            bs_cache <<= 1;
            bs_sh += 1;
            }
            }
            ;
            while ((bs_sh >= 0)) {
            bs_cache |= (unchecked((uint)((bs_next_ptr++)[0])) << bs_sh);
            bs_sh -= 8;
            }
            ;
            }
            bs[0].pos = layer3gr_limit;
        }
    }
    private void L3_midside_stereo(CPointer<float> left, int n)
    {
        unchecked
        {
            int i = 0;
            CPointer<float> right = (left + 576);
            for (; (i < n); i++) {
            float a = left[(int)(i)];
            float b = right[(int)(i)];
            left[(int)(i)] = (a + b);
            right[(int)(i)] = (a - b);
            }
        }
    }
    private void L3_intensity_stereo_band(CPointer<float> left, int n, float kl, float kr)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < n); i++) {
            left[(int)((i + 576))] = (left[(int)(i)] * kr);
            left[(int)(i)] = (left[(int)(i)] * kl);
            }
        }
    }
    private void L3_stereo_top_band(CPointer<float> right, CPointer<byte> sfb, int nbands, CPointer<int> max_band)
    {
        unchecked
        {
            int i = default;
            int k = default;
            max_band[(int)(0)] = (max_band[(int)(1)] = (max_band[(int)(2)] = (-1)));
            for (i = 0; (i < nbands); i++) {
            for (k = 0; (k < unchecked((int)(sfb[(int)(i)]))); k += 2) {
            if (((right[(int)(k)] != unchecked((float)(0))) || (right[(int)((k + 1))] != unchecked((float)(0))))) {
            max_band[(int)((i % 3))] = i;
            break;
            }
            }
            right += unchecked((int)(sfb[(int)(i)]));
            }
        }
    }
    private void L3_stereo_process(CPointer<float> left, CPointer<byte> ist_pos, CPointer<byte> sfb, CPointer<byte> hdr, CPointer<int> max_band, int mpeg2_sh)
    {
        unchecked
        {
            uint i = default;
            uint max_pos = unchecked((uint)((((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? 7 : 64)));
            for (i = unchecked((uint)(0)); (sfb[(int)(i)] != 0); i++) {
            uint ipos = unchecked((uint)(ist_pos[(int)(i)]));
            if (((unchecked((int)(i)) > max_band[(int)((i % unchecked((uint)(3))))]) && (ipos < max_pos))) {
            float kl = default;
            float kr = default;
            float s = (((unchecked((int)(hdr[(int)(3)])) & 32) != 0) ? 1.41421356f : unchecked((float)(1)));
            if (((unchecked((int)(hdr[(int)(1)])) & 8) != 0)) {
            kl = g_pan[(int)((unchecked((uint)(2)) * ipos))];
            kr = g_pan[(int)(((unchecked((uint)(2)) * ipos) + unchecked((uint)(1))))];
            } else {
            kl = unchecked((float)(1));
            kr = L3_ldexp_q2(unchecked((float)(1)), unchecked((int)((((ipos + unchecked((uint)(1))) >> 1) << mpeg2_sh))));
            if (((ipos & unchecked((uint)(1))) != 0)) {
            kl = kr;
            kr = unchecked((float)(1));
            }
            }
            L3_intensity_stereo_band(left, unchecked((int)(sfb[(int)(i)])), (kl * s), (kr * s));
            } else if (((unchecked((int)(hdr[(int)(3)])) & 32) != 0)) {
            L3_midside_stereo(left, unchecked((int)(sfb[(int)(i)])));
            }
            left += unchecked((int)(sfb[(int)(i)]));
            }
        }
    }
    private void L3_intensity_stereo(CPointer<float> left, CPointer<byte> ist_pos, CPointer<L3_gr_info_t> gr, CPointer<byte> hdr)
    {
        unchecked
        {
            CPointer<int> max_band = default;
            int n_sfb = (unchecked((int)(gr[0].n_long_sfb)) + unchecked((int)(gr[0].n_short_sfb)));
            int i = default;
            int max_blocks = ((gr[0].n_short_sfb != 0) ? 3 : 1);
            L3_stereo_top_band((left + 576), gr[0].sfbtab, n_sfb, max_band);
            if ((gr[0].n_long_sfb != 0)) {
            max_band[(int)(0)] = (max_band[(int)(1)] = (max_band[(int)(2)] = ((((max_band[(int)(0)] < max_band[(int)(1)]) ? max_band[(int)(1)] : max_band[(int)(0)]) < max_band[(int)(2)]) ? max_band[(int)(2)] : ((max_band[(int)(0)] < max_band[(int)(1)]) ? max_band[(int)(1)] : max_band[(int)(0)]))));
            }
            for (i = 0; (i < max_blocks); i++) {
            int default_pos = (((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? 3 : 0);
            int itop = ((n_sfb - max_blocks) + i);
            int prev = (itop - max_blocks);
            ist_pos[(int)(itop)] = unchecked((byte)(((max_band[(int)(i)] >= prev) ? default_pos : unchecked((int)(ist_pos[(int)(prev)])))));
            }
            L3_stereo_process(left, ist_pos, gr[0].sfbtab, hdr, max_band, (unchecked((int)(gr[(int)(1)].scalefac_compress)) & 1));
        }
    }
    private void L3_reorder(CPointer<float> grbuf, CPointer<float> scratch, CPointer<byte> sfb)
    {
        unchecked
        {
            int i = default;
            int len = default;
            CPointer<float> src = grbuf;
            CPointer<float> dst = scratch;
            for (; (0 != (len = unchecked((int)(sfb[0])))); sfb += 3, src += (2 * len)) {
            for (i = 0; (i < len); i++, src++) {
            (dst++)[0] = src[(int)((0 * len))];
            (dst++)[0] = src[(int)((1 * len))];
            (dst++)[0] = src[(int)((2 * len))];
            }
            }
            CopyFloats(grbuf, scratch, (dst - scratch));
        }
    }
    private void L3_antialias(CPointer<float> grbuf, int nbands)
    {
        unchecked
        {
            for (; (nbands > 0); nbands--, grbuf += 18) {
            int i = 0;
            for (; (i < 8); i++) {
            float u = grbuf[(int)((18 + i))];
            float d = grbuf[(int)((17 - i))];
            grbuf[(int)((18 + i))] = ((u * new CPointer<float>(g_aa, (int)(0) * 8)[(int)(i)]) - (d * new CPointer<float>(g_aa, (int)(1) * 8)[(int)(i)]));
            grbuf[(int)((17 - i))] = ((u * new CPointer<float>(g_aa, (int)(1) * 8)[(int)(i)]) + (d * new CPointer<float>(g_aa, (int)(0) * 8)[(int)(i)]));
            }
            }
        }
    }
    private void L3_dct3_9(CPointer<float> y)
    {
        unchecked
        {
            float s0 = default;
            float s1 = default;
            float s2 = default;
            float s3 = default;
            float s4 = default;
            float s5 = default;
            float s6 = default;
            float s7 = default;
            float s8 = default;
            float t0 = default;
            float t2 = default;
            float t4 = default;
            s0 = y[(int)(0)];
            s2 = y[(int)(2)];
            s4 = y[(int)(4)];
            s6 = y[(int)(6)];
            s8 = y[(int)(8)];
            t0 = (s0 + (s6 * 0.5f));
            s0 -= s6;
            t4 = ((s4 + s2) * 0.93969262f);
            t2 = ((s8 + s2) * 0.76604444f);
            s6 = ((s4 - s8) * 0.17364818f);
            s4 += (s8 - s2);
            s2 = (s0 - (s4 * 0.5f));
            y[(int)(4)] = (s4 + s0);
            s8 = ((t0 - t2) + s6);
            s0 = ((t0 - t4) + t2);
            s4 = ((t0 + t4) - s6);
            s1 = y[(int)(1)];
            s3 = y[(int)(3)];
            s5 = y[(int)(5)];
            s7 = y[(int)(7)];
            s3 *= 0.86602540f;
            t0 = ((s5 + s1) * 0.98480775f);
            t4 = ((s5 - s7) * 0.34202014f);
            t2 = ((s1 + s7) * 0.64278761f);
            s1 = (((s1 - s5) - s7) * 0.86602540f);
            s5 = ((t0 - s3) - t2);
            s7 = ((t4 - s3) - t0);
            s3 = ((t4 + s3) - t2);
            y[(int)(0)] = (s4 - s7);
            y[(int)(1)] = (s2 + s1);
            y[(int)(2)] = (s0 - s3);
            y[(int)(3)] = (s8 + s5);
            y[(int)(5)] = (s8 - s5);
            y[(int)(6)] = (s0 + s3);
            y[(int)(7)] = (s2 - s1);
            y[(int)(8)] = (s4 + s7);
        }
    }
    private void L3_imdct36(CPointer<float> grbuf, CPointer<float> overlap, CPointer<float> window, int nbands)
    {
        unchecked
        {
            int i = default;
            int j = default;
            for (j = 0; (j < nbands); j++, grbuf += 18, overlap += 9) {
            CPointer<float> co = default;
            Span<float> si = stackalloc float[(int)(9)];
            co[(int)(0)] = (-grbuf[(int)(0)]);
            si[(int)(0)] = grbuf[(int)(17)];
            for (i = 0; (i < 4); i++) {
            si[(int)((8 - (2 * i)))] = (grbuf[(int)(((4 * i) + 1))] - grbuf[(int)(((4 * i) + 2))]);
            co[(int)((1 + (2 * i)))] = (grbuf[(int)(((4 * i) + 1))] + grbuf[(int)(((4 * i) + 2))]);
            si[(int)((7 - (2 * i)))] = (grbuf[(int)(((4 * i) + 4))] - grbuf[(int)(((4 * i) + 3))]);
            co[(int)((2 + (2 * i)))] = (-(grbuf[(int)(((4 * i) + 3))] + grbuf[(int)(((4 * i) + 4))]));
            }
            L3_dct3_9(co);
            L3_dct3_9(new CPointer<float>(si));
            si[(int)(1)] = (-si[(int)(1)]);
            si[(int)(3)] = (-si[(int)(3)]);
            si[(int)(5)] = (-si[(int)(5)]);
            si[(int)(7)] = (-si[(int)(7)]);
            i = 0;
            for (; (i < 9); i++) {
            float ovl = overlap[(int)(i)];
            float sum = ((co[(int)(i)] * g_twid9[(int)((9 + i))]) + (si[(int)(i)] * g_twid9[(int)((0 + i))]));
            overlap[(int)(i)] = ((co[(int)(i)] * g_twid9[(int)((0 + i))]) - (si[(int)(i)] * g_twid9[(int)((9 + i))]));
            grbuf[(int)(i)] = ((ovl * window[(int)((0 + i))]) - (sum * window[(int)((9 + i))]));
            grbuf[(int)((17 - i))] = ((ovl * window[(int)((9 + i))]) + (sum * window[(int)((0 + i))]));
            }
            }
        }
    }
    private void L3_idct3(float x0, float x1, float x2, CPointer<float> dst)
    {
        unchecked
        {
            float m1 = (x1 * 0.86602540f);
            float a1 = (x0 - (x2 * 0.5f));
            dst[(int)(1)] = (x0 + x2);
            dst[(int)(0)] = (a1 + m1);
            dst[(int)(2)] = (a1 - m1);
        }
    }
    private void L3_imdct12(CPointer<float> x, CPointer<float> dst, CPointer<float> overlap)
    {
        unchecked
        {
            CPointer<float> co = default;
            Span<float> si = stackalloc float[(int)(3)];
            int i = default;
            L3_idct3((-x[(int)(0)]), (x[(int)(6)] + x[(int)(3)]), (x[(int)(12)] + x[(int)(9)]), co);
            L3_idct3(x[(int)(15)], (x[(int)(12)] - x[(int)(9)]), (x[(int)(6)] - x[(int)(3)]), new CPointer<float>(si));
            si[(int)(1)] = (-si[(int)(1)]);
            for (i = 0; (i < 3); i++) {
            float ovl = overlap[(int)(i)];
            float sum = ((co[(int)(i)] * g_twid3[(int)((3 + i))]) + (si[(int)(i)] * g_twid3[(int)((0 + i))]));
            overlap[(int)(i)] = ((co[(int)(i)] * g_twid3[(int)((0 + i))]) - (si[(int)(i)] * g_twid3[(int)((3 + i))]));
            dst[(int)(i)] = ((ovl * g_twid3[(int)((2 - i))]) - (sum * g_twid3[(int)((5 - i))]));
            dst[(int)((5 - i))] = ((ovl * g_twid3[(int)((5 - i))]) + (sum * g_twid3[(int)((2 - i))]));
            }
        }
    }
    private void L3_imdct_short(CPointer<float> grbuf, CPointer<float> overlap, int nbands)
    {
        unchecked
        {
            for (; (nbands > 0); nbands--, overlap += 9, grbuf += 18) {
            Span<float> tmp = stackalloc float[(int)(18)];
            CopyFloats(new CPointer<float>(tmp), grbuf, 18);
            CopyFloats(grbuf, overlap, 6);
            L3_imdct12(new CPointer<float>(tmp), (grbuf + 6), (overlap + 6));
            L3_imdct12((new CPointer<float>(tmp) + 1), (grbuf + 12), (overlap + 6));
            L3_imdct12((new CPointer<float>(tmp) + 2), overlap, (overlap + 6));
            }
        }
    }
    private void L3_change_sign(CPointer<float> grbuf)
    {
        unchecked
        {
            int b = default;
            int i = default;
            b = 0;
            grbuf += 18;
            for (; (b < 32); b += 2, grbuf += 36) for (i = 1; (i < 18); i += 2) grbuf[(int)(i)] = (-grbuf[(int)(i)]);
        }
    }
    private void L3_imdct_gr(CPointer<float> grbuf, CPointer<float> overlap, uint block_type, uint n_long_bands)
    {
        unchecked
        {
            if ((n_long_bands != 0)) {
            L3_imdct36(grbuf, overlap, new CPointer<float>(g_mdct_window, (int)(0) * 18), unchecked((int)(n_long_bands)));
            grbuf += unchecked((int)((unchecked((uint)(18)) * n_long_bands)));
            overlap += unchecked((int)((unchecked((uint)(9)) * n_long_bands)));
            }
            if ((block_type == unchecked((uint)(2)))) L3_imdct_short(grbuf, overlap, unchecked((int)((unchecked((uint)(32)) - n_long_bands)))); else L3_imdct36(grbuf, overlap, new CPointer<float>(g_mdct_window, (int)((block_type == unchecked((uint)(3))) ? 1 : 0) * 18), unchecked((int)((unchecked((uint)(32)) - n_long_bands))));
        }
    }
    private void L3_save_reservoir(CPointer<mp3dec_t> h, CPointer<mp3dec_scratch_t> s)
    {
        unchecked
        {
            int pos = unchecked((int)((unchecked((uint)((s[0].bs.pos + 7))) / 8u)));
            int remains = unchecked((int)(((unchecked((uint)(s[0].bs.limit)) / 8u) - unchecked((uint)(pos)))));
            if ((remains > 511)) {
            pos += (remains - 511);
            remains = 511;
            }
            if ((remains > 0)) {
            CopyBytes(new CPointer<byte>(h[0].reserv_buf), (new CPointer<byte>(s[0].maindata) + pos), remains);
            }
            h[0].reserv = remains;
        }
    }
    private int L3_restore_reservoir(CPointer<mp3dec_t> h, CPointer<bs_t> bs, CPointer<mp3dec_scratch_t> s, int main_data_begin)
    {
        unchecked
        {
            int frame_bytes = ((bs[0].limit - bs[0].pos) / 8);
            int bytes_have = ((h[0].reserv > main_data_begin) ? main_data_begin : h[0].reserv);
            CopyBytes(new CPointer<byte>(s[0].maindata), (new CPointer<byte>(h[0].reserv_buf) + ((0 < (h[0].reserv - main_data_begin)) ? (h[0].reserv - main_data_begin) : 0)), ((h[0].reserv > main_data_begin) ? main_data_begin : h[0].reserv));
            CopyBytes((new CPointer<byte>(s[0].maindata) + bytes_have), (bs[0].buf + (bs[0].pos / 8)), frame_bytes);
            bs_init(ScratchBits(s), new CPointer<byte>(s[0].maindata), (bytes_have + frame_bytes));
            return ((h[0].reserv >= main_data_begin) ? (int)1 : (int)0);
        }
    }
    private void L3_decode(CPointer<mp3dec_t> h, CPointer<mp3dec_scratch_t> s, CPointer<L3_gr_info_t> gr_info, int nch)
    {
        unchecked
        {
            int ch = default;
            for (ch = 0; (ch < nch); ch++) {
            int layer3gr_limit = (s[0].bs.pos + unchecked((int)(gr_info[(int)(ch)].part_23_length)));
            L3_decode_scalefactors(new CPointer<byte>(h[0].header), new CPointer<byte>(s[0].ist_pos, (int)(ch) * 39), ScratchBits(s), (gr_info + ch), new CPointer<float>(s[0].scf), ch);
            L3_huffman(new CPointer<float>(s[0].grbuf, (int)(ch) * 576), ScratchBits(s), (gr_info + ch), new CPointer<float>(s[0].scf), layer3gr_limit);
            }
            if (((unchecked((int)(h[0].header[(int)(3)])) & 16) != 0)) {
            L3_intensity_stereo(new CPointer<float>(s[0].grbuf, (int)(0) * 576), new CPointer<byte>(s[0].ist_pos, (int)(1) * 39), gr_info, new CPointer<byte>(h[0].header));
            } else if (((unchecked((int)(h[0].header[(int)(3)])) & 224) == 96)) {
            L3_midside_stereo(new CPointer<float>(s[0].grbuf, (int)(0) * 576), 576);
            }
            for (ch = 0; (ch < nch); ch++, gr_info++) {
            int aa_bands = 31;
            int n_long_bands = (((gr_info[0].mixed_block_flag != 0) ? 2 : 0) << (((((unchecked((int)(h[0].header[(int)(2)])) >> 2) & 3) + ((((unchecked((int)(h[0].header[(int)(1)])) >> 3) & 1) + ((unchecked((int)(h[0].header[(int)(1)])) >> 4) & 1)) * 3)) == 2) ? (int)1 : (int)0));
            if ((gr_info[0].n_short_sfb != 0)) {
            aa_bands = (n_long_bands - 1);
            L3_reorder((new CPointer<float>(s[0].grbuf, (int)(ch) * 576) + (n_long_bands * 18)), new CPointer<float>(s[0].syn, (int)(0) * 64), (gr_info[0].sfbtab + unchecked((int)(gr_info[0].n_long_sfb))));
            }
            L3_antialias(new CPointer<float>(s[0].grbuf, (int)(ch) * 576), aa_bands);
            L3_imdct_gr(new CPointer<float>(s[0].grbuf, (int)(ch) * 576), new CPointer<float>(h[0].mdct_overlap, (int)(ch) * 288), unchecked((uint)(gr_info[0].block_type)), unchecked((uint)(n_long_bands)));
            L3_change_sign(new CPointer<float>(s[0].grbuf, (int)(ch) * 576));
            }
        }
    }
    private void mp3d_DCT_II(CPointer<float> grbuf, int n)
    {
        unchecked
        {
            int i = default;
            int k = 0;
            for (; (k < n); k++) {
            Span<float> t = stackalloc float[(int)(32)];
            scoped CPointer<float> x = default;
            CPointer<float> y = (grbuf + k);
            x = new CPointer<float>(t);
            i = 0;
            for (; (i < 8); i++, x++) {
            float x0 = y[(int)((i * 18))];
            float x1 = y[(int)(((15 - i) * 18))];
            float x2 = y[(int)(((16 + i) * 18))];
            float x3 = y[(int)(((31 - i) * 18))];
            float t0 = (x0 + x3);
            float t1 = (x1 + x2);
            float t2 = ((x1 - x2) * g_sec[(int)(((3 * i) + 0))]);
            float t3 = ((x0 - x3) * g_sec[(int)(((3 * i) + 1))]);
            x[(int)(0)] = (t0 + t1);
            x[(int)(8)] = ((t0 - t1) * g_sec[(int)(((3 * i) + 2))]);
            x[(int)(16)] = (t3 + t2);
            x[(int)(24)] = ((t3 - t2) * g_sec[(int)(((3 * i) + 2))]);
            }
            x = new CPointer<float>(t);
            i = 0;
            for (; (i < 4); i++, x += 8) {
            float x0 = x[(int)(0)];
            float x1 = x[(int)(1)];
            float x2 = x[(int)(2)];
            float x3 = x[(int)(3)];
            float x4 = x[(int)(4)];
            float x5 = x[(int)(5)];
            float x6 = x[(int)(6)];
            float x7 = x[(int)(7)];
            float xt = default;
            xt = (x0 - x7);
            x0 += x7;
            x7 = (x1 - x6);
            x1 += x6;
            x6 = (x2 - x5);
            x2 += x5;
            x5 = (x3 - x4);
            x3 += x4;
            x4 = (x0 - x3);
            x0 += x3;
            x3 = (x1 - x2);
            x1 += x2;
            x[(int)(0)] = (x0 + x1);
            x[(int)(4)] = ((x0 - x1) * 0.70710677f);
            x5 = (x5 + x6);
            x6 = ((x6 + x7) * 0.70710677f);
            x7 = (x7 + xt);
            x3 = ((x3 + x4) * 0.70710677f);
            x5 -= (x7 * 0.198912367f);
            x7 += (x5 * 0.382683432f);
            x5 -= (x7 * 0.198912367f);
            x0 = (xt - x6);
            xt += x6;
            x[(int)(1)] = ((xt + x7) * 0.50979561f);
            x[(int)(2)] = ((x4 + x3) * 0.54119611f);
            x[(int)(3)] = ((x0 - x5) * 0.60134488f);
            x[(int)(5)] = ((x0 + x5) * 0.89997619f);
            x[(int)(6)] = ((x4 - x3) * 1.30656302f);
            x[(int)(7)] = ((xt - x7) * 2.56291556f);
            }
            for (i = 0; (i < 7); i++, y += (4 * 18)) {
            y[(int)((0 * 18))] = t[(int)(((0 * 8) + i))];
            y[(int)((1 * 18))] = ((t[(int)(((2 * 8) + i))] + t[(int)(((3 * 8) + i))]) + t[(int)(((3 * 8) + (i + 1)))]);
            y[(int)((2 * 18))] = (t[(int)(((1 * 8) + i))] + t[(int)(((1 * 8) + (i + 1)))]);
            y[(int)((3 * 18))] = ((t[(int)(((2 * 8) + (i + 1)))] + t[(int)(((3 * 8) + i))]) + t[(int)(((3 * 8) + (i + 1)))]);
            }
            y[(int)((0 * 18))] = t[(int)(((0 * 8) + 7))];
            y[(int)((1 * 18))] = (t[(int)(((2 * 8) + 7))] + t[(int)(((3 * 8) + 7))]);
            y[(int)((2 * 18))] = t[(int)(((1 * 8) + 7))];
            y[(int)((3 * 18))] = t[(int)(((3 * 8) + 7))];
            }
        }
    }
    private short mp3d_scale_pcm(float sample)
    {
        unchecked
        {
            if ((unchecked((double)(sample)) >= 32766.5)) return unchecked((short)(32767));
            if ((unchecked((double)(sample)) <= (-32767.5))) return unchecked((short)((-32768)));
            short s = unchecked((short)((sample + .5f)));
            s -= ((unchecked((int)(s)) < 0) ? (short)1 : (short)0);
            return s;
        }
    }
    private void mp3d_synth_pair(CPointer<short> pcm, int nch, CPointer<float> z)
    {
        unchecked
        {
            float a = default;
            a = ((z[(int)((14 * 64))] - z[(int)(0)]) * unchecked((float)(29)));
            a += ((z[(int)((1 * 64))] + z[(int)((13 * 64))]) * unchecked((float)(213)));
            a += ((z[(int)((12 * 64))] - z[(int)((2 * 64))]) * unchecked((float)(459)));
            a += ((z[(int)((3 * 64))] + z[(int)((11 * 64))]) * unchecked((float)(2037)));
            a += ((z[(int)((10 * 64))] - z[(int)((4 * 64))]) * unchecked((float)(5153)));
            a += ((z[(int)((5 * 64))] + z[(int)((9 * 64))]) * unchecked((float)(6574)));
            a += ((z[(int)((8 * 64))] - z[(int)((6 * 64))]) * unchecked((float)(37489)));
            a += (z[(int)((7 * 64))] * unchecked((float)(75038)));
            pcm[(int)(0)] = mp3d_scale_pcm(a);
            z += 2;
            a = (z[(int)((14 * 64))] * unchecked((float)(104)));
            a += (z[(int)((12 * 64))] * unchecked((float)(1567)));
            a += (z[(int)((10 * 64))] * unchecked((float)(9727)));
            a += (z[(int)((8 * 64))] * unchecked((float)(64019)));
            a += (z[(int)((6 * 64))] * unchecked((float)((-9975))));
            a += (z[(int)((4 * 64))] * unchecked((float)((-45))));
            a += (z[(int)((2 * 64))] * unchecked((float)(146)));
            a += (z[(int)((0 * 64))] * unchecked((float)((-5))));
            pcm[(int)((16 * nch))] = mp3d_scale_pcm(a);
        }
    }
    private void mp3d_synth(CPointer<float> xl, CPointer<short> dstl, int nch, CPointer<float> lins)
    {
        unchecked
        {
            int i = default;
            CPointer<float> xr = (xl + (576 * (nch - 1)));
            CPointer<short> dstr = (dstl + (nch - 1));
            CPointer<float> zlin = (lins + (15 * 64));
            CPointer<float> w = new CPointer<float>(g_win);
            zlin[(int)((4 * 15))] = xl[(int)((18 * 16))];
            zlin[(int)(((4 * 15) + 1))] = xr[(int)((18 * 16))];
            zlin[(int)(((4 * 15) + 2))] = xl[(int)(0)];
            zlin[(int)(((4 * 15) + 3))] = xr[(int)(0)];
            zlin[(int)((4 * 31))] = xl[(int)((1 + (18 * 16)))];
            zlin[(int)(((4 * 31) + 1))] = xr[(int)((1 + (18 * 16)))];
            zlin[(int)(((4 * 31) + 2))] = xl[(int)(1)];
            zlin[(int)(((4 * 31) + 3))] = xr[(int)(1)];
            mp3d_synth_pair(dstr, nch, ((lins + (4 * 15)) + 1));
            mp3d_synth_pair((dstr + (32 * nch)), nch, (((lins + (4 * 15)) + 64) + 1));
            mp3d_synth_pair(dstl, nch, (lins + (4 * 15)));
            mp3d_synth_pair((dstl + (32 * nch)), nch, ((lins + (4 * 15)) + 64));
            for (i = 14; (i >= 0); i--) {
            CPointer<float> a = default;
            Span<float> b = stackalloc float[(int)(4)];
            zlin[(int)((4 * i))] = xl[(int)((18 * (31 - i)))];
            zlin[(int)(((4 * i) + 1))] = xr[(int)((18 * (31 - i)))];
            zlin[(int)(((4 * i) + 2))] = xl[(int)((1 + (18 * (31 - i))))];
            zlin[(int)(((4 * i) + 3))] = xr[(int)((1 + (18 * (31 - i))))];
            zlin[(int)((4 * (i + 16)))] = xl[(int)((1 + (18 * (1 + i))))];
            zlin[(int)(((4 * (i + 16)) + 1))] = xr[(int)((1 + (18 * (1 + i))))];
            zlin[(int)(((4 * (i - 16)) + 2))] = xl[(int)((18 * (1 + i)))];
            zlin[(int)(((4 * (i - 16)) + 3))] = xr[(int)((18 * (1 + i)))];
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (0 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 0) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] = ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] = ((vz[(int)(j)] * w0) - (vy[(int)(j)] * w1));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (1 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 1) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vy[(int)(j)] * w1) - (vz[(int)(j)] * w0));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (2 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 2) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vz[(int)(j)] * w0) - (vy[(int)(j)] * w1));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (3 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 3) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vy[(int)(j)] * w1) - (vz[(int)(j)] * w0));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (4 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 4) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vz[(int)(j)] * w0) - (vy[(int)(j)] * w1));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (5 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 5) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vy[(int)(j)] * w1) - (vz[(int)(j)] * w0));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (6 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 6) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vz[(int)(j)] * w0) - (vy[(int)(j)] * w1));
            }
            }
            {
            int j = default;
            float w0 = (w++)[0];
            float w1 = (w++)[0];
            CPointer<float> vz = (zlin + (int)(((4 * i) - (7 * 64))));
            CPointer<float> vy = (zlin + (int)(((4 * i) - ((15 - 7) * 64))));
            ;
            for (j = 0; (j < 4); j++) {
            b[(int)(j)] += ((vz[(int)(j)] * w1) + (vy[(int)(j)] * w0));
            a[(int)(j)] += ((vy[(int)(j)] * w1) - (vz[(int)(j)] * w0));
            }
            }
            dstr[(int)(((15 - i) * nch))] = mp3d_scale_pcm(a[(int)(1)]);
            dstr[(int)(((17 + i) * nch))] = mp3d_scale_pcm(b[(int)(1)]);
            dstl[(int)(((15 - i) * nch))] = mp3d_scale_pcm(a[(int)(0)]);
            dstl[(int)(((17 + i) * nch))] = mp3d_scale_pcm(b[(int)(0)]);
            dstr[(int)(((47 - i) * nch))] = mp3d_scale_pcm(a[(int)(3)]);
            dstr[(int)(((49 + i) * nch))] = mp3d_scale_pcm(b[(int)(3)]);
            dstl[(int)(((47 - i) * nch))] = mp3d_scale_pcm(a[(int)(2)]);
            dstl[(int)(((49 + i) * nch))] = mp3d_scale_pcm(b[(int)(2)]);
            }
        }
    }
    private void mp3d_synth_granule(CPointer<float> qmf_state, CPointer<float> grbuf, int nbands, int nch, CPointer<short> pcm, CPointer<float> lins)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < nch); i++) {
            mp3d_DCT_II((grbuf + (576 * i)), nbands);
            }
            CopyFloats(lins, qmf_state, (15 * 64));
            for (i = 0; (i < nbands); i += 2) {
            mp3d_synth((grbuf + i), (pcm + ((32 * nch) * i)), nch, (lins + (i * 64)));
            }
            if ((nch == 1)) {
            for (i = 0; (i < (15 * 64)); i += 2) {
            qmf_state[(int)(i)] = lins[(int)(((nbands * 64) + i))];
            }
            } else {
            CopyFloats(qmf_state, (lins + (nbands * 64)), (15 * 64));
            }
        }
    }
    private int mp3d_match_frame(CPointer<byte> hdr, int mp3_bytes, int frame_bytes)
    {
        unchecked
        {
            int i = default;
            int nmatch = default;
            i = 0;
            nmatch = 0;
            for (; (nmatch < 10); nmatch++) {
            i += (hdr_frame_bytes((hdr + i), frame_bytes) + hdr_padding((hdr + i)));
            if (((i + 4) > mp3_bytes)) return ((nmatch > 0) ? (int)1 : (int)0);
            if ((!(hdr_compare(hdr, (hdr + i)) != 0))) return 0;
            }
            return 1;
        }
    }
    private int mp3d_find_frame(CPointer<byte> mp3, int mp3_bytes, CPointer<int> free_format_bytes, CPointer<int> ptr_frame_bytes)
    {
        unchecked
        {
            int i = default;
            int k = default;
            for (i = 0; (i < (mp3_bytes - 4)); i++, mp3++) {
            if ((hdr_valid(mp3) != 0)) {
            int frame_bytes = hdr_frame_bytes(mp3, free_format_bytes[0]);
            int frame_and_padding = (frame_bytes + hdr_padding(mp3));
            for (k = 4; (((!(frame_bytes != 0)) && (k < 2304)) && ((i + (2 * k)) < (mp3_bytes - 4))); k++) {
            if ((hdr_compare(mp3, (mp3 + k)) != 0)) {
            int fb = (k - hdr_padding(mp3));
            int nextfb = (fb + hdr_padding((mp3 + k)));
            if ((((((i + k) + nextfb) + 4) > mp3_bytes) || (!(hdr_compare(mp3, ((mp3 + k) + nextfb)) != 0)))) continue;
            frame_and_padding = k;
            frame_bytes = fb;
            free_format_bytes[0] = fb;
            }
            }
            if (((((frame_bytes != 0) && ((i + frame_and_padding) <= mp3_bytes)) && (mp3d_match_frame(mp3, (mp3_bytes - i), frame_bytes) != 0)) || ((!(i != 0)) && (frame_and_padding == mp3_bytes)))) {
            ptr_frame_bytes[0] = frame_and_padding;
            return i;
            }
            free_format_bytes[0] = 0;
            }
            }
            ptr_frame_bytes[0] = 0;
            return mp3_bytes;
        }
    }
    private void mp3dec_init(CPointer<mp3dec_t> dec)
    {
        unchecked
        {
            dec[0].header[(int)(0)] = unchecked((byte)(0));
        }
    }
    private int mp3dec_decode_frame(CPointer<mp3dec_t> dec, CPointer<byte> mp3, int mp3_bytes, CPointer<short> pcm, CPointer<mp3dec_frame_info_t> info)
    {
        unchecked
        {
            int i = 0;
            int igr = default;
            int frame_size = 0;
            int success = 1;
            CPointer<byte> hdr = default;
            CPointer<bs_t> bs_frame = frame_bits;
            if ((((mp3_bytes > 4) && (unchecked((int)(dec[0].header[(int)(0)])) == 255)) && (hdr_compare(new CPointer<byte>(dec[0].header), mp3) != 0))) {
            frame_size = (hdr_frame_bytes(mp3, dec[0].free_format_bytes) + hdr_padding(mp3));
            if (((frame_size != mp3_bytes) && (((frame_size + 4) > mp3_bytes) || (!(hdr_compare(mp3, (mp3 + frame_size)) != 0))))) {
            frame_size = 0;
            }
            }
            if ((!(frame_size != 0))) {
            ClearDecoder(dec);
            i = FindFrame(mp3, mp3_bytes, dec);
            frame_size = found_frame_size;
            if (((!(frame_size != 0)) || ((i + frame_size) > mp3_bytes))) {
            info[0].frame_bytes = i;
            return 0;
            }
            }
            hdr = (mp3 + i);
            CopyBytes(new CPointer<byte>(dec[0].header), hdr, 4);
            info[0].frame_bytes = (i + frame_size);
            info[0].frame_offset = i;
            info[0].channels = (((unchecked((int)(hdr[(int)(3)])) & 192) == 192) ? 1 : 2);
            info[0].hz = unchecked((int)(hdr_sample_rate_hz(hdr)));
            info[0].layer = (4 - ((unchecked((int)(hdr[(int)(1)])) >> 1) & 3));
            info[0].bitrate_kbps = unchecked((int)(hdr_bitrate_kbps(hdr)));
            if ((!(!pcm.IsNull))) {
            return unchecked((int)(hdr_frame_samples(hdr)));
            }
            bs_init(bs_frame, (hdr + 4), (frame_size - 4));
            if ((!((unchecked((int)(hdr[(int)(1)])) & 1) != 0))) {
            get_bits(bs_frame, 16);
            }
            if ((info[0].layer == 3)) {
            int main_data_begin = L3_read_side_info(bs_frame, new CPointer<L3_gr_info_t>(scratch.gr_info), hdr);
            if (((main_data_begin < 0) || (bs_frame[0].pos > bs_frame[0].limit))) {
            mp3dec_init(dec);
            return 0;
            }
            success = L3_restore_reservoir(dec, bs_frame, scratch_ptr, main_data_begin);
            if ((success != 0)) {
            for (igr = 0; (igr < (((unchecked((int)(hdr[(int)(1)])) & 8) != 0) ? 2 : 1)); igr++, pcm += (576 * info[0].channels)) {
            ClearFloats(new CPointer<float>(scratch.grbuf, (int)(0) * 576), 1152);
            L3_decode(dec, scratch_ptr, (new CPointer<L3_gr_info_t>(scratch.gr_info) + (igr * info[0].channels)), info[0].channels);
            mp3d_synth_granule(new CPointer<float>(dec[0].qmf_state), new CPointer<float>(scratch.grbuf, (int)(0) * 576), 18, info[0].channels, pcm, new CPointer<float>(scratch.syn, (int)(0) * 64));
            }
            }
            L3_save_reservoir(dec, scratch_ptr);
            } else {
            CPointer<L12_scale_info> sci = scale_info;
            L12_read_scale_info(hdr, bs_frame, sci);
            ClearFloats(new CPointer<float>(scratch.grbuf, (int)(0) * 576), 1152);
            i = 0;
            igr = 0;
            for (; (igr < 3); igr++) {
            if ((12 == (i += L12_dequantize_granule((new CPointer<float>(scratch.grbuf, (int)(0) * 576) + i), bs_frame, sci, (info[0].layer | 1))))) {
            i = 0;
            L12_apply_scf_384(sci, (new CPointer<float>(sci[0].scf) + igr), new CPointer<float>(scratch.grbuf, (int)(0) * 576));
            mp3d_synth_granule(new CPointer<float>(dec[0].qmf_state), new CPointer<float>(scratch.grbuf, (int)(0) * 576), 12, info[0].channels, pcm, new CPointer<float>(scratch.syn, (int)(0) * 64));
            ClearFloats(new CPointer<float>(scratch.grbuf, (int)(0) * 576), 1152);
            pcm += (384 * info[0].channels);
            }
            if ((bs_frame[0].pos > bs_frame[0].limit)) {
            mp3dec_init(dec);
            return 0;
            }
            }
            }
            return unchecked((int)((unchecked((uint)(success)) * hdr_frame_samples(new CPointer<byte>(dec[0].header)))));
        }
    }
}
