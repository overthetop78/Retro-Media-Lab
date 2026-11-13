using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Mp3FrameDecoder
{
    private sealed class mp3dec_frame_info_t
    {
        public int frame_bytes;
        public int frame_offset;
        public int channels;
        public int hz;
        public int layer;
        public int bitrate_kbps;
    }
    private sealed class mp3dec_t
    {
        public readonly float[] mdct_overlap = new float[576];
        public readonly float[] qmf_state = new float[960];
        public int reserv;
        public int free_format_bytes;
        public readonly byte[] header = new byte[4];
        public readonly byte[] reserv_buf = new byte[511];
    }
    private sealed class bs_t
    {
        private Memory<byte> _buf;
        public CPointer<byte> buf { get => new(_buf); set => _buf = value.IsNull ? default : value.Memory; }
        public int pos;
        public int limit;
    }
    private sealed class L12_scale_info
    {
        public readonly float[] scf = new float[192];
        public byte total_bands;
        public byte stereo_bands;
        public readonly byte[] bitalloc = new byte[64];
        public readonly byte[] scfcod = new byte[64];
    }
    private sealed class L12_subband_alloc_t
    {
        public byte tab_offset;
        public byte code_tab_width;
        public byte band_count;
    }
    private sealed class L3_gr_info_t
    {
        private Memory<byte> _sfbtab;
        public CPointer<byte> sfbtab { get => new(_sfbtab); set => _sfbtab = value.IsNull ? default : value.Memory; }
        public ushort part_23_length;
        public ushort big_values;
        public ushort scalefac_compress;
        public byte global_gain;
        public byte block_type;
        public byte mixed_block_flag;
        public byte n_long_sfb;
        public byte n_short_sfb;
        public readonly byte[] table_select = new byte[3];
        public readonly byte[] region_count = new byte[3];
        public readonly byte[] subblock_gain = new byte[3];
        public byte preflag;
        public byte scalefac_scale;
        public byte count1_table;
        public byte scfsi;
    }
    private sealed class mp3dec_scratch_t
    {
        public readonly bs_t[] BitsState = [new()];
        public bs_t bs => BitsState[0];
        public readonly byte[] maindata = new byte[2815];
        public readonly L3_gr_info_t[] gr_info = new L3_gr_info_t[4];
        public readonly float[] grbuf = new float[1152];
        public readonly float[] scf = new float[40];
        public readonly float[] syn = new float[2112];
        public readonly byte[] ist_pos = new byte[78];
        public mp3dec_scratch_t()
        {
            for (int i=0;i<4;i++) gr_info[i] = new();
        }
    }
}
