namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

internal sealed partial class Vdp
{
    private void merge(CPointer<byte> srca, CPointer<byte> srcb, CPointer<byte> dst, CPointer<byte> table, int width)
    {
        unchecked
        {
            do {
            (dst++)[0] = table[(int)(((unchecked((int)((srcb++)[0])) << 8) | unchecked((int)((srca++)[0]))))];
            } while (((--width) != 0));
        }
    }
    private void render_bg_m5(int line)
    {
        unchecked
        {
            int column = default;
            uint atex = default;
            uint atbuf = default;
            CPointer<uint> src = default;
            CPointer<uint> dst = default;
            uint xscroll = new CPointer<byte>(vram, (int)((unchecked((int)(hscb)) + ((line & unchecked((int)(hscroll_mask))) << 2)))).Cast<uint>()[0];
            uint yscroll = new CPointer<byte>(vsram, (int)(0)).Cast<uint>()[0];
            uint pf_col_mask = unchecked((uint)(playfield_col_mask));
            uint pf_row_mask = unchecked((uint)(playfield_row_mask));
            uint pf_shift = unchecked((uint)(playfield_shift));
            int a = ((unchecked((int)(reg[(int)(18)])) & 31) << 3);
            int w = ((unchecked((int)(reg[(int)(18)])) >> 7) & 1);
            int start = 0;
            int end = (bitmap.viewport.w >> 4);
            uint shift = ((xscroll >> 16) & unchecked((uint)(15)));
            uint index = ((pf_col_mask + unchecked((uint)(1))) - ((xscroll >> 20) & pf_col_mask));
            uint v_line = ((unchecked((uint)(line)) + (yscroll >> 16)) & pf_row_mask);
            CPointer<uint> nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            if ((shift != 0)) {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((unchecked((uint)(16)) + shift))).Cast<uint>();
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)).Cast<uint>();
            }
            for (column = 0; (column < end); column++, index++) {
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            if ((w == ((line >= a) ? (int)1 : (int)0))) {
            a = 0;
            w = 1;
            } else {
            a = unchecked((int)(clip[(int)(0)].enable));
            w = unchecked((int)(clip[(int)(1)].enable));
            }
            if ((a != 0)) {
            start = unchecked((int)(clip[(int)(0)].left));
            end = unchecked((int)(clip[(int)(0)].right));
            shift = (xscroll & unchecked((uint)(15)));
            index = (((pf_col_mask + unchecked((uint)(start))) + unchecked((uint)(1))) - ((xscroll >> 4) & pf_col_mask));
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            if ((shift != 0)) {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(((unchecked((uint)(16)) + shift) + unchecked((uint)((start << 4)))))).Cast<uint>();
            if ((start != 0)) {
            atbuf = nt[(int)((index & pf_col_mask))];
            } else {
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            }
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            }
            for (column = start; (column < end); column++, index++) {
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            start = unchecked((int)(clip[(int)(1)].left));
            end = unchecked((int)(clip[(int)(1)].right));
            }
            if ((w != 0)) {
            nt = new CPointer<byte>(vram, (int)((unchecked((int)(ntwb)) | ((line >> 3) << (6 + (unchecked((int)(reg[(int)(12)])) & 1)))))).Cast<uint>();
            v_line = unchecked((uint)(((line & 7) << 3)));
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            for (column = start; (column < end); column++) {
            atbuf = nt[(int)(column)];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            }
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(((unchecked((int)(reg[(int)(12)])) & 8) >> 2)) * 65536), bitmap.viewport.w);
        }
    }
    private void render_bg_m5_vs(int line)
    {
        unchecked
        {
            int column = default;
            uint atex = default;
            uint atbuf = default;
            CPointer<uint> src = default;
            CPointer<uint> dst = default;
            uint v_line = default;
            CPointer<uint> nt = default;
            uint xscroll = new CPointer<byte>(vram, (int)((unchecked((int)(hscb)) + ((line & unchecked((int)(hscroll_mask))) << 2)))).Cast<uint>()[0];
            uint yscroll = unchecked((uint)(0));
            uint pf_col_mask = unchecked((uint)(playfield_col_mask));
            uint pf_row_mask = unchecked((uint)(playfield_row_mask));
            uint pf_shift = unchecked((uint)(playfield_shift));
            CPointer<uint> vs = new CPointer<byte>(vsram, (int)(0)).Cast<uint>();
            int a = ((unchecked((int)(reg[(int)(18)])) & 31) << 3);
            int w = ((unchecked((int)(reg[(int)(18)])) >> 7) & 1);
            int start = 0;
            int end = (bitmap.viewport.w >> 4);
            uint shift = ((xscroll >> 16) & unchecked((uint)(15)));
            uint index = ((pf_col_mask + unchecked((uint)(1))) - ((xscroll >> 20) & pf_col_mask));
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            yscroll = (vs[(int)(19)] & (vs[(int)(19)] >> 16));
            }
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((unchecked((uint)(16)) + shift))).Cast<uint>();
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)).Cast<uint>();
            }
            for (column = 0; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + (vs[(int)(column)] >> 16)) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            if ((w == ((line >= a) ? (int)1 : (int)0))) {
            a = 0;
            w = 1;
            } else {
            a = unchecked((int)(clip[(int)(0)].enable));
            w = unchecked((int)(clip[(int)(1)].enable));
            }
            if ((a != 0)) {
            start = unchecked((int)(clip[(int)(0)].left));
            end = unchecked((int)(clip[(int)(0)].right));
            shift = (xscroll & unchecked((uint)(15)));
            index = (((pf_col_mask + unchecked((uint)(start))) + unchecked((uint)(1))) - ((xscroll >> 4) & pf_col_mask));
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(((unchecked((uint)(16)) + shift) + unchecked((uint)((start << 4)))))).Cast<uint>();
            if ((start != 0)) {
            atbuf = nt[(int)((index & pf_col_mask))];
            } else {
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            }
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            }
            for (column = start; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + vs[(int)(column)]) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            start = unchecked((int)(clip[(int)(1)].left));
            end = unchecked((int)(clip[(int)(1)].right));
            }
            if ((w != 0)) {
            nt = new CPointer<byte>(vram, (int)((unchecked((int)(ntwb)) | ((line >> 3) << (6 + (unchecked((int)(reg[(int)(12)])) & 1)))))).Cast<uint>();
            v_line = unchecked((uint)(((line & 7) << 3)));
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            for (column = start; (column < end); column++) {
            atbuf = nt[(int)(column)];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            }
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(((unchecked((int)(reg[(int)(12)])) & 8) >> 2)) * 65536), bitmap.viewport.w);
        }
    }
    private void render_bg_m5_vs_enhanced(int line)
    {
        unchecked
        {
            int column = default;
            uint atex = default;
            uint atbuf = default;
            CPointer<uint> src = default;
            CPointer<uint> dst = default;
            uint v_line = default;
            uint next_v_line = default;
            CPointer<uint> nt = default;
            int v_offset = 0;
            uint xscroll = new CPointer<byte>(vram, (int)((unchecked((int)(hscb)) + ((line & unchecked((int)(hscroll_mask))) << 2)))).Cast<uint>()[0];
            uint yscroll = unchecked((uint)(0));
            uint pf_col_mask = unchecked((uint)(playfield_col_mask));
            uint pf_row_mask = unchecked((uint)(playfield_row_mask));
            uint pf_shift = unchecked((uint)(playfield_shift));
            CPointer<uint> vs = new CPointer<byte>(vsram, (int)(0)).Cast<uint>();
            int a = ((unchecked((int)(reg[(int)(18)])) & 31) << 3);
            int w = ((unchecked((int)(reg[(int)(18)])) >> 7) & 1);
            int start = 0;
            int end = (bitmap.viewport.w >> 4);
            uint shift = ((xscroll >> 16) & unchecked((uint)(15)));
            uint index = ((pf_col_mask + unchecked((uint)(1))) - ((xscroll >> 20) & pf_col_mask));
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            yscroll = (vs[(int)(19)] & (vs[(int)(19)] >> 16));
            }
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((unchecked((uint)(16)) + shift))).Cast<uint>();
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)).Cast<uint>();
            }
            for (column = 0; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + (vs[(int)(column)] >> 16)) & pf_row_mask);
            next_v_line = ((unchecked((uint)(line)) + (vs[(int)((column + 1))] >> 16)) & pf_row_mask);
            if ((column != (end - 1))) {
            v_offset = ((unchecked((int)(next_v_line)) - unchecked((int)(v_line))) / 2);
            v_offset = ((abs(v_offset) >= config.enhanced_vscroll_limit) ? 0 : v_offset);
            }
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            v_line = ((unchecked((uint)((line + v_offset))) + (vs[(int)(column)] >> 16)) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            if ((w == ((line >= a) ? (int)1 : (int)0))) {
            a = 0;
            w = 1;
            } else {
            a = unchecked((int)(clip[(int)(0)].enable));
            w = unchecked((int)(clip[(int)(1)].enable));
            }
            if ((a != 0)) {
            start = unchecked((int)(clip[(int)(0)].left));
            end = unchecked((int)(clip[(int)(0)].right));
            shift = (xscroll & unchecked((uint)(15)));
            index = (((pf_col_mask + unchecked((uint)(start))) + unchecked((uint)(1))) - ((xscroll >> 4) & pf_col_mask));
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(((unchecked((uint)(16)) + shift) + unchecked((uint)((start << 4)))))).Cast<uint>();
            if ((start != 0)) {
            atbuf = nt[(int)((index & pf_col_mask))];
            } else {
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            }
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            }
            for (column = start; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + vs[(int)(column)]) & pf_row_mask);
            next_v_line = ((unchecked((uint)(line)) + vs[(int)((column + 1))]) & pf_row_mask);
            if ((column != (end - 1))) {
            v_offset = ((unchecked((int)(next_v_line)) - unchecked((int)(v_line))) / 2);
            v_offset = ((abs(v_offset) >= config.enhanced_vscroll_limit) ? 0 : v_offset);
            }
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            v_line = ((unchecked((uint)((line + v_offset))) + vs[(int)(column)]) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 3) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            start = unchecked((int)(clip[(int)(1)].left));
            end = unchecked((int)(clip[(int)(1)].right));
            }
            if ((w != 0)) {
            nt = new CPointer<byte>(vram, (int)((unchecked((int)(ntwb)) | ((line >> 3) << (6 + (unchecked((int)(reg[(int)(12)])) & 1)))))).Cast<uint>();
            v_line = unchecked((uint)(((line & 7) << 3)));
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            for (column = start; (column < end); column++) {
            atbuf = nt[(int)(column)];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(8191))) << 6) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((atbuf & unchecked((uint)(536805376))) >> 10) | v_line))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            }
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(((unchecked((int)(reg[(int)(12)])) & 8) >> 2)) * 65536), bitmap.viewport.w);
        }
    }
    private void render_bg_m5_im2(int line)
    {
        unchecked
        {
            int column = default;
            int start = default;
            int end = default;
            int a = default;
            int w = default;
            uint atex = default;
            uint atbuf = default;
            CPointer<uint> src = default;
            CPointer<uint> dst = default;
            uint shift = default;
            uint index = default;
            uint v_line = default;
            CPointer<uint> nt = default;
            uint xscroll = new CPointer<byte>(vram, (int)((unchecked((int)(hscb)) + ((line & unchecked((int)(hscroll_mask))) << 2)))).Cast<uint>()[0];
            uint yscroll = new CPointer<byte>(vsram, (int)(0)).Cast<uint>()[0];
            uint pf_col_mask = unchecked((uint)(playfield_col_mask));
            uint pf_row_mask = unchecked((uint)(((unchecked((int)(playfield_row_mask)) << 1) | 1)));
            uint pf_shift = unchecked((uint)(playfield_shift));
            line = ((line << 1) + unchecked((int)(odd_frame)));
            start = 0;
            end = (bitmap.viewport.w >> 4);
            shift = ((xscroll >> 16) & unchecked((uint)(15)));
            index = ((pf_col_mask + unchecked((uint)(1))) - ((xscroll >> 20) & pf_col_mask));
            v_line = ((unchecked((uint)(line)) + (yscroll >> 16)) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            if ((shift != 0)) {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((unchecked((uint)(16)) + shift))).Cast<uint>();
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)).Cast<uint>();
            }
            for (column = 0; (column < end); column++, index++) {
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            a = ((unchecked((int)(reg[(int)(18)])) & 31) << 4);
            w = ((unchecked((int)(reg[(int)(18)])) >> 7) & 1);
            if ((w == ((line >= a) ? (int)1 : (int)0))) {
            a = 0;
            w = 1;
            } else {
            a = unchecked((int)(clip[(int)(0)].enable));
            w = unchecked((int)(clip[(int)(1)].enable));
            }
            if ((a != 0)) {
            start = unchecked((int)(clip[(int)(0)].left));
            end = unchecked((int)(clip[(int)(0)].right));
            shift = (xscroll & unchecked((uint)(15)));
            index = (((pf_col_mask + unchecked((uint)(start))) + unchecked((uint)(1))) - ((xscroll >> 4) & pf_col_mask));
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            if ((shift != 0)) {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(((unchecked((uint)(16)) + shift) + unchecked((uint)((start << 4)))))).Cast<uint>();
            if ((start != 0)) {
            atbuf = nt[(int)((index & pf_col_mask))];
            } else {
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            }
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            }
            for (column = start; (column < end); column++, index++) {
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            start = unchecked((int)(clip[(int)(1)].left));
            end = unchecked((int)(clip[(int)(1)].right));
            }
            if ((w != 0)) {
            nt = new CPointer<byte>(vram, (int)((unchecked((int)(ntwb)) | ((line >> 4) << (6 + (unchecked((int)(reg[(int)(12)])) & 1)))))).Cast<uint>();
            v_line = unchecked((uint)(((line & 15) << 3)));
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            for (column = start; (column < end); column++) {
            atbuf = nt[(int)(column)];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            }
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(((unchecked((int)(reg[(int)(12)])) & 8) >> 2)) * 65536), bitmap.viewport.w);
        }
    }
    private void render_bg_m5_im2_vs(int line)
    {
        unchecked
        {
            int column = default;
            int start = default;
            int end = default;
            int a = default;
            int w = default;
            uint atex = default;
            uint atbuf = default;
            CPointer<uint> src = default;
            CPointer<uint> dst = default;
            uint shift = default;
            uint index = default;
            uint v_line = default;
            CPointer<uint> nt = default;
            uint xscroll = new CPointer<byte>(vram, (int)((unchecked((int)(hscb)) + ((line & unchecked((int)(hscroll_mask))) << 2)))).Cast<uint>()[0];
            uint yscroll = unchecked((uint)(0));
            uint pf_col_mask = unchecked((uint)(playfield_col_mask));
            uint pf_row_mask = unchecked((uint)(((unchecked((int)(playfield_row_mask)) << 1) | 1)));
            uint pf_shift = unchecked((uint)(playfield_shift));
            CPointer<uint> vs = new CPointer<byte>(vsram, (int)(0)).Cast<uint>();
            line = ((line << 1) + unchecked((int)(odd_frame)));
            start = 0;
            end = (bitmap.viewport.w >> 4);
            shift = ((xscroll >> 16) & unchecked((uint)(15)));
            index = ((pf_col_mask + unchecked((uint)(1))) - ((xscroll >> 20) & pf_col_mask));
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            yscroll = (vs[(int)(19)] & (vs[(int)(19)] >> 16));
            }
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((unchecked((uint)(16)) + shift))).Cast<uint>();
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)).Cast<uint>();
            }
            for (column = 0; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + (vs[(int)(column)] >> 16)) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntbb)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            a = ((unchecked((int)(reg[(int)(18)])) & 31) << 4);
            w = ((unchecked((int)(reg[(int)(18)])) >> 7) & 1);
            if ((w == ((line >= a) ? (int)1 : (int)0))) {
            a = 0;
            w = 1;
            } else {
            a = unchecked((int)(clip[(int)(0)].enable));
            w = unchecked((int)(clip[(int)(1)].enable));
            }
            if ((a != 0)) {
            start = unchecked((int)(clip[(int)(0)].left));
            end = unchecked((int)(clip[(int)(0)].right));
            shift = (xscroll & unchecked((uint)(15)));
            index = (((pf_col_mask + unchecked((uint)(start))) + unchecked((uint)(1))) - ((xscroll >> 4) & pf_col_mask));
            if ((shift != 0)) {
            v_line = ((unchecked((uint)(line)) + yscroll) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(((unchecked((uint)(16)) + shift) + unchecked((uint)((start << 4)))))).Cast<uint>();
            if ((start != 0)) {
            atbuf = nt[(int)((index & pf_col_mask))];
            } else {
            atbuf = nt[(int)(((index - unchecked((uint)(1))) & pf_col_mask))];
            }
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            } else {
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            }
            for (column = start; (column < end); column++, index++) {
            v_line = ((unchecked((uint)(line)) + vs[(int)(column)]) & pf_row_mask);
            nt = new CPointer<byte>(vram, (int)((unchecked((uint)(ntab)) + (((v_line >> 4) << unchecked((int)(pf_shift))) & unchecked((uint)(8128)))))).Cast<uint>();
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            atbuf = nt[(int)((index & pf_col_mask))];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            start = unchecked((int)(clip[(int)(1)].left));
            end = unchecked((int)(clip[(int)(1)].right));
            }
            if ((w != 0)) {
            nt = new CPointer<byte>(vram, (int)((unchecked((int)(ntwb)) | ((line >> 4) << (6 + (unchecked((int)(reg[(int)(12)])) & 1)))))).Cast<uint>();
            v_line = unchecked((uint)(((line & 15) << 3)));
            dst = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + (start << 4)))).Cast<uint>();
            for (column = start; (column < end); column++) {
            atbuf = nt[(int)(column)];
            atex = atex_table[(int)(((atbuf >> 13) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(1023))) << 7) | ((atbuf & unchecked((uint)(6144))) << 6)) | v_line) ^ ((atbuf & unchecked((uint)(4096))) >> 6)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            atex = atex_table[(int)(((atbuf >> 29) & unchecked((uint)(7))))];
            src = new CPointer<byte>(bg_pattern_cache, (int)((((((atbuf & unchecked((uint)(67043328))) >> 9) | ((atbuf & unchecked((uint)(402653184))) >> 10)) | v_line) ^ ((atbuf & unchecked((uint)(268435456))) >> 22)))).Cast<uint>();
            (dst++)[0] = (src[(int)(0)] | atex);
            (dst++)[0] = (src[(int)(1)] | atex);
            }
            }
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(((unchecked((int)(reg[(int)(12)])) & 8) >> 2)) * 65536), bitmap.viewport.w);
        }
    }
    private void render_obj_tms(int line)
    {
        unchecked
        {
            int x = default;
            int start = default;
            int end = default;
            CPointer<byte> lb = default;
            CPointer<byte> sg = default;
            byte color = default;
            Span<byte> pattern = stackalloc byte[(int)(2)];
            ushort temp = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            int width = 8;
            width <<= ((unchecked((int)(reg[(int)(1)])) & 2) >> 1);
            width <<= (unchecked((int)(reg[(int)(1)])) & 1);
            status |= unchecked((ushort)(spr_ovr));
            spr_ovr = unchecked((byte)(0));
            while (((count--) != 0)) {
            start = unchecked((int)(object_info[0].xpos));
            color = unchecked((byte)(object_info[0].size));
            start -= ((unchecked((int)(color)) & 128) >> 2);
            lb = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + start)));
            if (((start + width) > 256)) {
            end = (256 - start);
            start = 0;
            } else {
            end = width;
            if ((start < 0)) {
            start = (0 - start);
            } else {
            start = 0;
            }
            }
            color &= unchecked((byte)(15));
            temp = object_info[0].attr;
            temp &= unchecked((ushort)((~((unchecked((int)(reg[(int)(1)])) & 2) >> 0))));
            temp &= unchecked((ushort)((~((unchecked((int)(reg[(int)(1)])) & 2) >> 1))));
            sg = new CPointer<byte>(vram, (int)(((((unchecked((int)(reg[(int)(6)])) << 11) & 14336) | (unchecked((int)(temp)) << 3)) | unchecked((int)(object_info[0].ypos)))));
            pattern[(int)(0)] = sg[(int)(0)];
            pattern[(int)(1)] = sg[(int)(16)];
            if (((unchecked((int)(reg[(int)(1)])) & 1) != 0)) {
            for (x = start; (x < end); x += 2) {
            temp = unchecked((ushort)(pattern[(int)(((x >> 4) & 1))]));
            temp = unchecked((ushort)(((unchecked((int)(temp)) >> (7 - ((x >> 1) & 7))) & 1)));
            temp = unchecked((ushort)((unchecked((int)(temp)) * unchecked((int)(color)))));
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)(x)])) << 8)));
            lb[(int)(x)] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)(temp)];
            status |= unchecked((ushort)(((unchecked((int)(temp)) & 32768) >> 10)));
            temp &= unchecked((ushort)(255));
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)((x + 1))])) << 8)));
            lb[(int)((x + 1))] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)(temp)];
            status |= unchecked((ushort)(((unchecked((int)(temp)) & 32768) >> 10)));
            }
            } else {
            for (x = start; (x < end); x++) {
            temp = unchecked((ushort)(pattern[(int)(((x >> 3) & 1))]));
            temp = unchecked((ushort)(((unchecked((int)(temp)) >> (7 - (x & 7))) & 1)));
            temp = unchecked((ushort)((unchecked((int)(temp)) * unchecked((int)(color)))));
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)(x)])) << 8)));
            lb[(int)(x)] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)(temp)];
            status |= unchecked((ushort)(((unchecked((int)(temp)) & 32768) >> 10)));
            }
            }
            object_info++;
            }
            if ((((unchecked((int)(system_hw)) == SYSTEM_GG) && (!(config.gg_extra != 0))) && (unchecked((int)(v_counter)) < bitmap.viewport.h))) {
            int visibleLine = (unchecked((int)(v_counter)) - ((bitmap.viewport.h - 144) / 2));
            if (((visibleLine < 0) || (visibleLine >= 144))) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), 64, unchecked((uint)(256)));
            } else {
            if ((bitmap.viewport.x > 0)) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), 64, unchecked((uint)(48)));
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(((32 + 48) + 160))), 64, unchecked((uint)(48)));
            }
            }
            }
        }
    }
    private void render_obj_m4(int line)
    {
        unchecked
        {
            int i = default;
            int xpos = default;
            int end = default;
            CPointer<byte> src = default;
            CPointer<byte> lb = default;
            ushort temp = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            int width = 8;
            ushort sg_mask = unchecked((ushort)(((~448) ^ (unchecked((int)(reg[(int)(6)])) << 6))));
            if ((unchecked((int)(system_hw)) < SYSTEM_MD)) {
            width <<= (unchecked((int)(reg[(int)(1)])) & 1);
            }
            if ((unchecked((int)(system_hw)) > SYSTEM_SMS)) {
            sg_mask |= unchecked((ushort)(192));
            }
            status |= unchecked((ushort)(spr_ovr));
            spr_ovr = unchecked((byte)(0));
            while (((count--) != 0)) {
            if ((unchecked((int)(system_hw)) <= SYSTEM_SMS)) {
            if ((count < 4)) {
            width = 8;
            }
            }
            temp = unchecked((ushort)(((unchecked((int)(object_info[0].attr)) | 256) & unchecked((int)(sg_mask)))));
            src = new CPointer<byte>(bg_pattern_cache, (int)(((unchecked((int)(temp)) << 6) | (unchecked((int)(object_info[0].ypos)) << 3))));
            xpos = unchecked((int)(object_info[0].xpos));
            xpos -= (unchecked((int)(reg[(int)(0)])) & 8);
            if ((xpos < 0)) {
            src = (src - xpos);
            end = (xpos + width);
            xpos = 0;
            } else if (((xpos + width) > 256)) {
            end = (256 - xpos);
            } else {
            end = width;
            }
            lb = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + xpos)));
            if ((width > 8)) {
            for (i = 0; (i < end); i += 2) {
            temp = unchecked((ushort)((src++)[0]));
            if (((unchecked((int)(temp)) & 15) != 0)) {
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)((unchecked((int)(temp)) | 0))];
            if ((((unchecked((int)(temp)) & 32768) != 0) && (!((unchecked((int)(status)) & 32) != 0)))) {
            spr_col = unchecked((ushort)(((unchecked((int)(v_counter)) << 8) | (((xpos + i) + 13) >> 1))));
            status |= unchecked((ushort)(32));
            }
            temp &= unchecked((ushort)(255));
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)((i + 1))])) << 8)));
            lb[(int)((i + 1))] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)((unchecked((int)(temp)) | 0))];
            if ((((unchecked((int)(temp)) & 32768) != 0) && (!((unchecked((int)(status)) & 32) != 0)))) {
            spr_col = unchecked((ushort)(((unchecked((int)(v_counter)) << 8) | ((((xpos + i) + 1) + 13) >> 1))));
            status |= unchecked((ushort)(32));
            }
            }
            }
            } else {
            for (i = 0; (i < end); i++) {
            temp = unchecked((ushort)((src++)[0]));
            if (((unchecked((int)(temp)) & 15) != 0)) {
            temp |= unchecked((ushort)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(5) * 65536)[(int)((unchecked((int)(temp)) | 0))];
            if ((((unchecked((int)(temp)) & 32768) != 0) && (!((unchecked((int)(status)) & 32) != 0)))) {
            spr_col = unchecked((ushort)(((unchecked((int)(v_counter)) << 8) | (((xpos + i) + 13) >> 1))));
            status |= unchecked((ushort)(32));
            }
            }
            }
            }
            object_info++;
            }
            if ((((unchecked((int)(system_hw)) == SYSTEM_GG) && (!(config.gg_extra != 0))) && (unchecked((int)(v_counter)) < bitmap.viewport.h))) {
            int visibleLine = (unchecked((int)(v_counter)) - ((bitmap.viewport.h - 144) / 2));
            if (((visibleLine < 0) || (visibleLine >= 144))) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), 64, unchecked((uint)(256)));
            } else {
            if ((bitmap.viewport.x > 0)) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), 64, unchecked((uint)(48)));
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(((32 + 48) + 160))), 64, unchecked((uint)(48)));
            }
            }
            }
        }
    }
    private void render_obj_m5(int line)
    {
        unchecked
        {
            int i = default;
            int column = default;
            int xpos = default;
            int width = default;
            int pixelcount = 0;
            int masked = 0;
            int max_pixels = unchecked((int)(max_sprite_pixels));
            CPointer<byte> src = default;
            CPointer<byte> s = default;
            CPointer<byte> lb = default;
            uint temp = default;
            uint v_line = default;
            uint attr = default;
            uint name = default;
            uint atex = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            while (((count--) != 0)) {
            xpos = unchecked((int)(object_info[0].xpos));
            if ((xpos != 0)) {
            spr_ovr = unchecked((byte)(1));
            } else if ((spr_ovr != 0)) {
            masked = 1;
            }
            xpos = (xpos - 128);
            temp = unchecked((uint)(object_info[0].size));
            width = unchecked((int)((unchecked((uint)(8)) + ((temp & unchecked((uint)(12))) << 1))));
            pixelcount += width;
            if (((((xpos + width) > 0) && (xpos < bitmap.viewport.w)) && (!(masked != 0)))) {
            attr = unchecked((uint)(object_info[0].attr));
            v_line = unchecked((uint)(object_info[0].ypos));
            atex = ((attr >> 9) & unchecked((uint)(112)));
            name = (attr & unchecked((uint)(2047)));
            attr &= unchecked((uint)(6144));
            s = new CPointer<byte>(name_lut, (int)(((((attr >> 3) & unchecked((uint)(768))) | (temp << 4)) | ((v_line & unchecked((uint)(24))) >> 1))));
            lb = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + xpos)));
            if ((pixelcount > max_pixels)) {
            width -= (pixelcount - max_pixels);
            }
            width = (width >> 3);
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            for (column = 0; (column < width); column++, lb += 8) {
            temp = (attr | ((name + unchecked((uint)(s[(int)(column)]))) & unchecked((uint)(2047))));
            src = new CPointer<byte>(bg_pattern_cache, (int)(((temp << 6) | v_line)));
            for (i = 0; (i < 8); i++) {
            temp = unchecked((uint)((src++)[0]));
            if (((temp & unchecked((uint)(15))) != 0)) {
            temp |= unchecked((uint)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(1) * 65536)[(int)((temp | atex))];
            status |= unchecked((ushort)(((temp & unchecked((uint)(32768))) >> 10)));
            }
            }
            }
            }
            if ((pixelcount >= max_pixels)) {
            spr_ovr = ((pixelcount >= bitmap.viewport.w) ? (byte)1 : (byte)0);
            return;
            }
            object_info++;
            }
            spr_ovr = unchecked((byte)(0));
        }
    }
    private void render_obj_m5_ste(int line)
    {
        unchecked
        {
            int i = default;
            int column = default;
            int xpos = default;
            int width = default;
            int pixelcount = 0;
            int masked = 0;
            int max_pixels = unchecked((int)(max_sprite_pixels));
            CPointer<byte> src = default;
            CPointer<byte> s = default;
            CPointer<byte> lb = default;
            uint temp = default;
            uint v_line = default;
            uint attr = default;
            uint name = default;
            uint atex = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            memset((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(0)), 0, unchecked((uint)((bitmap.viewport.w + 64))));
            while (((count--) != 0)) {
            xpos = unchecked((int)(object_info[0].xpos));
            if ((xpos != 0)) {
            spr_ovr = unchecked((byte)(1));
            } else if ((spr_ovr != 0)) {
            masked = 1;
            }
            xpos = (xpos - 128);
            temp = unchecked((uint)(object_info[0].size));
            width = unchecked((int)((unchecked((uint)(8)) + ((temp & unchecked((uint)(12))) << 1))));
            pixelcount += width;
            if (((((xpos + width) > 0) && (xpos < bitmap.viewport.w)) && (!(masked != 0)))) {
            attr = unchecked((uint)(object_info[0].attr));
            v_line = unchecked((uint)(object_info[0].ypos));
            atex = ((attr >> 9) & unchecked((uint)(112)));
            name = (attr & unchecked((uint)(2047)));
            attr &= unchecked((uint)(6144));
            s = new CPointer<byte>(name_lut, (int)(((((attr >> 3) & unchecked((uint)(768))) | (temp << 4)) | ((v_line & unchecked((uint)(24))) >> 1))));
            lb = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + xpos)));
            if ((pixelcount > max_pixels)) {
            width -= (pixelcount - max_pixels);
            }
            width = (width >> 3);
            v_line = ((v_line & unchecked((uint)(7))) << 3);
            for (column = 0; (column < width); column++, lb += 8) {
            temp = (attr | ((name + unchecked((uint)(s[(int)(column)]))) & unchecked((uint)(2047))));
            src = new CPointer<byte>(bg_pattern_cache, (int)(((temp << 6) | v_line)));
            for (i = 0; (i < 8); i++) {
            temp = unchecked((uint)((src++)[0]));
            if (((temp & unchecked((uint)(15))) != 0)) {
            temp |= unchecked((uint)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(3) * 65536)[(int)((temp | atex))];
            status |= unchecked((ushort)(((temp & unchecked((uint)(32768))) >> 10)));
            }
            }
            }
            }
            if ((pixelcount >= max_pixels)) {
            spr_ovr = ((pixelcount >= bitmap.viewport.w) ? (byte)1 : (byte)0);
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(4) * 65536), bitmap.viewport.w);
            return;
            }
            object_info++;
            }
            spr_ovr = unchecked((byte)(0));
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(4) * 65536), bitmap.viewport.w);
        }
    }
    private void render_obj_m5_im2(int line)
    {
        unchecked
        {
            int i = default;
            int column = default;
            int xpos = default;
            int width = default;
            int pixelcount = 0;
            int masked = 0;
            int max_pixels = unchecked((int)(max_sprite_pixels));
            CPointer<byte> src = default;
            CPointer<byte> s = default;
            CPointer<byte> lb = default;
            uint temp = default;
            uint v_line = default;
            uint attr = default;
            uint name = default;
            uint atex = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            while (((count--) != 0)) {
            xpos = unchecked((int)(object_info[0].xpos));
            if ((xpos != 0)) {
            spr_ovr = unchecked((byte)(1));
            } else if ((spr_ovr != 0)) {
            masked = 1;
            }
            xpos = (xpos - 128);
            temp = unchecked((uint)(object_info[0].size));
            width = unchecked((int)((unchecked((uint)(8)) + ((temp & unchecked((uint)(12))) << 1))));
            pixelcount += width;
            if (((((xpos + width) > 0) && (xpos < bitmap.viewport.w)) && (!(masked != 0)))) {
            attr = unchecked((uint)(object_info[0].attr));
            v_line = unchecked((uint)(object_info[0].ypos));
            atex = ((attr >> 9) & unchecked((uint)(112)));
            name = (attr & unchecked((uint)(1023)));
            attr &= unchecked((uint)(6144));
            s = new CPointer<byte>(name_lut, (int)(((((attr >> 3) & unchecked((uint)(768))) | (temp << 4)) | ((v_line & unchecked((uint)(48))) >> 2))));
            lb = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + xpos)));
            if ((pixelcount > max_pixels)) {
            width -= (pixelcount - max_pixels);
            }
            width = (width >> 3);
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            for (column = 0; (column < width); column++, lb += 8) {
            temp = (attr | (((name + unchecked((uint)(s[(int)(column)]))) & unchecked((uint)(1023))) << 1));
            src = new CPointer<byte>(bg_pattern_cache, (int)((((temp << 6) | v_line) ^ ((attr & unchecked((uint)(4096))) >> 6))));
            for (i = 0; (i < 8); i++) {
            temp = unchecked((uint)((src++)[0]));
            if (((temp & unchecked((uint)(15))) != 0)) {
            temp |= unchecked((uint)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(1) * 65536)[(int)((temp | atex))];
            status |= unchecked((ushort)(((temp & unchecked((uint)(32768))) >> 10)));
            }
            }
            }
            }
            if ((pixelcount >= max_pixels)) {
            spr_ovr = ((pixelcount >= bitmap.viewport.w) ? (byte)1 : (byte)0);
            return;
            }
            object_info++;
            }
            spr_ovr = unchecked((byte)(0));
        }
    }
    private void render_obj_m5_im2_ste(int line)
    {
        unchecked
        {
            int i = default;
            int column = default;
            int xpos = default;
            int width = default;
            int pixelcount = 0;
            int masked = 0;
            int max_pixels = unchecked((int)(max_sprite_pixels));
            CPointer<byte> src = default;
            CPointer<byte> s = default;
            CPointer<byte> lb = default;
            uint temp = default;
            uint v_line = default;
            uint attr = default;
            uint name = default;
            uint atex = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(line) * 20);
            int count = unchecked((int)(object_count[(int)(line)]));
            memset((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(0)), 0, unchecked((uint)((bitmap.viewport.w + 64))));
            while (((count--) != 0)) {
            xpos = unchecked((int)(object_info[0].xpos));
            if ((xpos != 0)) {
            spr_ovr = unchecked((byte)(1));
            } else if ((spr_ovr != 0)) {
            masked = 1;
            }
            xpos = (xpos - 128);
            temp = unchecked((uint)(object_info[0].size));
            width = unchecked((int)((unchecked((uint)(8)) + ((temp & unchecked((uint)(12))) << 1))));
            pixelcount += width;
            if (((((xpos + width) > 0) && (xpos < bitmap.viewport.w)) && (!(masked != 0)))) {
            attr = unchecked((uint)(object_info[0].attr));
            v_line = unchecked((uint)(object_info[0].ypos));
            atex = ((attr >> 9) & unchecked((uint)(112)));
            name = (attr & unchecked((uint)(1023)));
            attr &= unchecked((uint)(6144));
            s = new CPointer<byte>(name_lut, (int)(((((attr >> 3) & unchecked((uint)(768))) | (temp << 4)) | ((v_line & unchecked((uint)(48))) >> 2))));
            lb = (new CPointer<byte>(linebuf, (int)(1) * 512) + (int)((32 + xpos)));
            if ((pixelcount > max_pixels)) {
            width -= (pixelcount - max_pixels);
            }
            width = (width >> 3);
            v_line = ((v_line & unchecked((uint)(15))) << 3);
            for (column = 0; (column < width); column++, lb += 8) {
            temp = (attr | (((name + unchecked((uint)(s[(int)(column)]))) & unchecked((uint)(1023))) << 1));
            src = new CPointer<byte>(bg_pattern_cache, (int)((((temp << 6) | v_line) ^ ((attr & unchecked((uint)(4096))) >> 6))));
            for (i = 0; (i < 8); i++) {
            temp = unchecked((uint)((src++)[0]));
            if (((temp & unchecked((uint)(15))) != 0)) {
            temp |= unchecked((uint)((unchecked((int)(lb[(int)(i)])) << 8)));
            lb[(int)(i)] = new CPointer<byte>(lut, (int)(3) * 65536)[(int)((temp | atex))];
            status |= unchecked((ushort)(((temp & unchecked((uint)(32768))) >> 10)));
            }
            }
            }
            }
            if ((pixelcount >= max_pixels)) {
            spr_ovr = ((pixelcount >= bitmap.viewport.w) ? (byte)1 : (byte)0);
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(4) * 65536), bitmap.viewport.w);
            return;
            }
            object_info++;
            }
            spr_ovr = unchecked((byte)(0));
            merge((new CPointer<byte>(linebuf, (int)(1) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), new CPointer<byte>(lut, (int)(4) * 65536), bitmap.viewport.w);
        }
    }
    private void parse_satb_tms(int line)
    {
        unchecked
        {
            int i = 0;
            int count = 0;
            if ((!((unchecked((int)(reg[(int)(1)])) & 16) != 0))) {
            int ypos = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(((line + 1) & 1)) * 20);
            CPointer<byte> st = new CPointer<byte>(vram, (int)(((unchecked((int)(reg[(int)(5)])) << 7) & 16256)));
            int height = 8;
            height <<= ((unchecked((int)(reg[(int)(1)])) & 2) >> 1);
            do {
            ypos = unchecked((int)(st[(int)((i << 2))]));
            if ((ypos == 208)) {
            break;
            }
            if ((ypos >= 224)) {
            ypos -= 256;
            }
            ypos = (line - ypos);
            ypos >>= (unchecked((int)(reg[(int)(1)])) & 1);
            if (((ypos >= 0) && (ypos < height))) {
            if ((count == 4)) {
            if ((line < bitmap.viewport.h)) {
            spr_ovr = unchecked((byte)(64));
            }
            break;
            }
            object_info[0].ypos = unchecked((ushort)(ypos));
            object_info[0].xpos = unchecked((ushort)(st[(int)(((i << 2) + 1))]));
            object_info[0].attr = unchecked((ushort)(st[(int)(((i << 2) + 2))]));
            object_info[0].size = unchecked((ushort)(st[(int)(((i << 2) + 3))]));
            ++count;
            object_info++;
            }
            } while (((++i) < 32));
            }
            object_count[(int)(((line + 1) & 1))] = unchecked((byte)(count));
            status = unchecked((ushort)(((unchecked((int)(status)) & 224) | (i & 31))));
        }
    }
    private void parse_satb_m4(int line)
    {
        unchecked
        {
            int i = 0;
            CPointer<byte> st = default;
            int count = 0;
            int ypos = default;
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(((line + 1) & 1)) * 20);
            int height = (8 + ((unchecked((int)(reg[(int)(1)])) & 2) << 2));
            ushort st_mask = unchecked((ushort)(((~16256) ^ (unchecked((int)(reg[(int)(5)])) << 7))));
            if ((unchecked((int)(system_hw)) > SYSTEM_SMS)) {
            st_mask |= unchecked((ushort)(128));
            }
            st = new CPointer<byte>(vram, (int)((unchecked((int)(st_mask)) & 16128)));
            do {
            ypos = unchecked((int)(st[(int)(i)]));
            if (((ypos == 208) && (bitmap.viewport.h == 192))) {
            break;
            }
            if ((ypos > (bitmap.viewport.h + 16))) {
            ypos -= 256;
            }
            ypos = (line - ypos);
            if ((unchecked((int)(system_hw)) < SYSTEM_MD)) {
            ypos >>= (unchecked((int)(reg[(int)(1)])) & 1);
            }
            if (((ypos >= 0) && (ypos < height))) {
            if ((count == 8)) {
            if (((line >= 0) && (line < bitmap.viewport.h))) {
            spr_ovr = unchecked((byte)(64));
            }
            break;
            }
            object_info[0].ypos = unchecked((ushort)(ypos));
            object_info[0].xpos = unchecked((ushort)(st[(int)(((128 + (i << 1)) & unchecked((int)(st_mask))))]));
            object_info[0].attr = unchecked((ushort)(st[(int)(((129 + (i << 1)) & unchecked((int)(st_mask))))]));
            if (((unchecked((int)(reg[(int)(1)])) & 2) != 0)) {
            object_info[0].attr &= unchecked((ushort)(254));
            }
            ++count;
            object_info++;
            }
            } while (((++i) < 64));
            object_count[(int)(((line + 1) & 1))] = unchecked((byte)(count));
        }
    }
    private void parse_satb_m5(int line)
    {
        unchecked
        {
            int ypos = default;
            int height = default;
            int size = default;
            int link = 0;
            int count = 0;
            int max = (bitmap.viewport.w >> 4);
            int total = (unchecked((int)(max_sprite_pixels)) >> 2);
            CPointer<ushort> p = new CPointer<byte>(vram, (int)(satb)).Cast<ushort>();
            CPointer<ushort> q = new CPointer<byte>(sat, (int)(0)).Cast<ushort>();
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(((line + 1) & 1)) * 20);
            line += 129;
            do {
            ypos = (unchecked((int)(q[(int)(link)])) & 511);
            if ((line >= ypos)) {
            size = (unchecked((int)(q[(int)((link + 1))])) >> 8);
            height = (8 + ((size & 3) << 3));
            ypos = (line - ypos);
            if ((ypos < height)) {
            if ((count == max)) {
            status |= unchecked((ushort)(64));
            break;
            }
            object_info[0].attr = p[(int)((link + 2))];
            object_info[0].xpos = unchecked((ushort)((unchecked((int)(p[(int)((link + 3))])) & 511)));
            object_info[0].ypos = unchecked((ushort)(ypos));
            object_info[0].size = unchecked((ushort)((size & 15)));
            ++count;
            object_info++;
            }
            }
            link = ((unchecked((int)(q[(int)((link + 1))])) & 127) << 2);
            if (((link == 0) || (link >= bitmap.viewport.w))) break;
            } while (((--total) != 0));
            object_count[(int)((line & 1))] = unchecked((byte)(count));
        }
    }
    private void parse_satb_m5_im2(int line)
    {
        unchecked
        {
            int ypos = default;
            int height = default;
            int size = default;
            int link = 0;
            int count = 0;
            int max = (bitmap.viewport.w >> 4);
            int total = (unchecked((int)(max_sprite_pixels)) >> 2);
            CPointer<ushort> p = new CPointer<byte>(vram, (int)(satb)).Cast<ushort>();
            CPointer<ushort> q = new CPointer<byte>(sat, (int)(0)).Cast<ushort>();
            CPointer<ObjectInfo> object_info = new CPointer<ObjectInfo>(obj_info, (int)(((line + 1) & 1)) * 20);
            line = (((line + 129) << 1) + unchecked((int)(odd_frame)));
            do {
            ypos = (unchecked((int)(q[(int)(link)])) & 1023);
            if ((line >= ypos)) {
            size = (unchecked((int)(q[(int)((link + 1))])) >> 8);
            height = (16 + ((size & 3) << 4));
            ypos = (line - ypos);
            if ((ypos < height)) {
            if ((count == max)) {
            status |= unchecked((ushort)(64));
            break;
            }
            object_info[0].attr = p[(int)((link + 2))];
            object_info[0].xpos = unchecked((ushort)((unchecked((int)(p[(int)((link + 3))])) & 511)));
            object_info[0].ypos = unchecked((ushort)(ypos));
            object_info[0].size = unchecked((ushort)((size & 15)));
            ++count;
            object_info++;
            }
            }
            link = ((unchecked((int)(q[(int)((link + 1))])) & 127) << 2);
            if (((link == 0) || (link >= bitmap.viewport.w))) break;
            } while (((--total) != 0));
            object_count[(int)(((line >> 1) & 1))] = unchecked((byte)(count));
        }
    }
    private void render_reset()
    {
        unchecked
        {
            memset(new CPointer<byte>(bitmap.data), 0, unchecked((uint)((bitmap.pitch * bitmap.height))));
            memset(new CPointer<byte>(linebuf), 0, unchecked((uint)(1024)));
            memset(new CPointer<uint>(pixel).Cast<byte>(), 0, unchecked((uint)(1024)));
            memset(new CPointer<byte>(bg_pattern_cache).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(524288)));
            spr_ovr = unchecked((byte)((spr_col = unchecked((ushort)((object_count[(int)(0)] = (object_count[(int)(1)] = unchecked((byte)(0)))))))));
        }
    }
    private void render_line(int line)
    {
        unchecked
        {
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            if ((bg_list_index != 0)) {
            update_bg_pattern_cache(unchecked((int)(bg_list_index)));
            bg_list_index = unchecked((ushort)(0));
            }
            render_bg(line);
            render_obj((line & 1));
            if (((unchecked((int)(reg[(int)(0)])) & 32) != 0)) {
            if ((unchecked((int)(system_hw)) >= SYSTEM_MARKIII)) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)(32)), 64, unchecked((uint)(8)));
            }
            }
            if ((line < (bitmap.viewport.h - 1))) {
            parse_satb(line);
            }
            if ((bitmap.viewport.x > 0)) {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 - bitmap.viewport.x))), 64, unchecked((uint)(bitmap.viewport.x)));
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + bitmap.viewport.w))), 64, unchecked((uint)(bitmap.viewport.x)));
            }
            } else {
            if ((unchecked((int)(system_hw)) < SYSTEM_MD)) {
            status |= unchecked((ushort)(spr_ovr));
            spr_ovr = unchecked((byte)(0));
            parse_satb(line);
            }
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 - bitmap.viewport.x))), 64, unchecked((uint)((bitmap.viewport.w + (2 * bitmap.viewport.x)))));
            }
            remap_line(line);
        }
    }
    private void blank_line(int line, int offset, int width)
    {
        unchecked
        {
            memset((new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 + offset))), 64, unchecked((uint)(width)));
            remap_line(line);
        }
    }
    private void remap_line_32(int line)
    {
        unchecked
        {
            int width = (bitmap.viewport.w + (2 * bitmap.viewport.x));
            CPointer<byte> src = (new CPointer<byte>(linebuf, (int)(0) * 512) + (int)((32 - bitmap.viewport.x)));
            line = ((line + bitmap.viewport.y) % unchecked((int)(lines_per_frame)));
            if ((line < 0)) return;
            if (((interlaced != 0) && (config.render != 0))) {
            line = ((line * 2) + unchecked((int)(odd_frame)));
            }
            {
            CPointer<uint> dst = new CPointer<byte>(bitmap.data, (int)((line * bitmap.pitch))).Cast<uint>();
            if ((config.lcd != 0)) {
            do {
            {
            uint pixel_out = pixel[(int)((src++)[0])];
            uint pixel_old = dst[0];
            byte r = unchecked((byte)(((pixel_out & unchecked((uint)(16711680))) >> 16)));
            byte g = unchecked((byte)(((pixel_out & unchecked((uint)(65280))) >> 8)));
            byte b = unchecked((byte)(((pixel_out & unchecked((uint)(255))) >> 0)));
            int r_decay = unchecked((int)((((pixel_old & unchecked((uint)(16711680))) >> 16) - unchecked((uint)(r)))));
            int g_decay = unchecked((int)((((pixel_old & unchecked((uint)(65280))) >> 8) - unchecked((uint)(g)))));
            int b_decay = unchecked((int)((((pixel_old & unchecked((uint)(255))) >> 0) - unchecked((uint)(b)))));
            if ((r_decay > 0)) r += unchecked((byte)(((config.lcd * r_decay) >> 8)));
            if ((g_decay > 0)) g += unchecked((byte)(((config.lcd * g_decay) >> 8)));
            if ((b_decay > 0)) b += unchecked((byte)(((config.lcd * b_decay) >> 8)));
            (dst++)[0] = unchecked((uint)(((((255 << 24) | (unchecked((int)(r)) << 16)) | (unchecked((int)(g)) << 8)) | unchecked((int)(b)))));
            }
            ;
            } while (((--width) != 0));
            } else {
            do {
            (dst++)[0] = pixel[(int)((src++)[0])];
            } while (((--width) != 0));
            }
            }
        }
    }
}
