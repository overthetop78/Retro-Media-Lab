namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Decoder
{
#if SH4_DYNAREC_NONE
    internal void Decode_illegalOp(uint opcode) { }
    internal void Decode_i1000_1011_iiii_iiii(uint opcode) { }
    internal void Decode_i1000_1111_iiii_iiii(uint opcode) { }
    internal void Decode_i1000_1001_iiii_iiii(uint opcode) { }
    internal void Decode_i1000_1101_iiii_iiii(uint opcode) { }
    internal void Decode_i1010_iiii_iiii_iiii(uint opcode) { }
    internal void Decode_i0000_nnnn_0010_0011(uint opcode) { }
    internal void Decode_i0100_nnnn_0010_1011(uint opcode) { }
    internal void Decode_i1011_iiii_iiii_iiii(uint opcode) { }
    internal void Decode_i0000_nnnn_0000_0011(uint opcode) { }
    internal void Decode_i0100_nnnn_0000_1011(uint opcode) { }
    internal void Decode_i0000_0000_0000_1011(uint opcode) { }
    internal void Decode_i0000_0000_0010_1011(uint opcode) { }
    internal void Decode_i1100_0011_iiii_iiii(uint opcode) { }
    internal void Decode_i0000_0000_0001_1011(uint opcode) { }
    internal void Decode_i0100_nnnn_0000_0111(uint opcode) { }
    internal void Decode_i0100_nnnn_0000_1110(uint opcode) { }
    internal void Decode_i0011_nnnn_mmmm_1000(uint opcode) { }
    internal void Decode_i0011_nnnn_mmmm_1100(uint opcode) { }
    internal void Decode_i0111_nnnn_iiii_iiii(uint opcode) { }
    internal void Decode_i0000_0000_0000_1001(uint opcode) { }
    internal void Decode_i1111_0011_1111_1101(uint opcode) { }
    internal void Decode_i1111_1011_1111_1101(uint opcode) { }
    internal void Decode_i0100_nnnn_0010_0100(uint opcode) { }
    internal void Decode_i0100_nnnn_0010_0101(uint opcode) { }
    internal void Decode_i0100_nnnn_0110_1010(uint opcode) { }
    internal void Decode_i0100_nnnn_0110_0110(uint opcode) { }
    internal void Decode_i0100_nnnn_0001_1011(uint opcode) { }
    internal void Decode_i0100_nnnn_0000_0011(uint opcode) { }
#else
    internal partial void Decode_illegalOp(uint opcode);
    internal partial void Decode_i1000_1011_iiii_iiii(uint opcode);
    internal partial void Decode_i1000_1111_iiii_iiii(uint opcode);
    internal partial void Decode_i1000_1001_iiii_iiii(uint opcode);
    internal partial void Decode_i1000_1101_iiii_iiii(uint opcode);
    internal partial void Decode_i1010_iiii_iiii_iiii(uint opcode);
    internal partial void Decode_i0000_nnnn_0010_0011(uint opcode);
    internal partial void Decode_i0100_nnnn_0010_1011(uint opcode);
    internal partial void Decode_i1011_iiii_iiii_iiii(uint opcode);
    internal partial void Decode_i0000_nnnn_0000_0011(uint opcode);
    internal partial void Decode_i0100_nnnn_0000_1011(uint opcode);
    internal partial void Decode_i0000_0000_0000_1011(uint opcode);
    internal partial void Decode_i0000_0000_0010_1011(uint opcode);
    internal partial void Decode_i1100_0011_iiii_iiii(uint opcode);
    internal partial void Decode_i0000_0000_0001_1011(uint opcode);
    internal partial void Decode_i0100_nnnn_0000_0111(uint opcode);
    internal partial void Decode_i0100_nnnn_0000_1110(uint opcode);
    partial void Decode_i0011_nnnn_mmmm_1000(uint opcode);
    partial void Decode_i0011_nnnn_mmmm_1100(uint opcode);
    partial void Decode_i0111_nnnn_iiii_iiii(uint opcode);
    internal partial void Decode_i0000_0000_0000_1001(uint opcode);
    internal partial void Decode_i1111_0011_1111_1101(uint opcode);
    internal partial void Decode_i1111_1011_1111_1101(uint opcode);
    internal partial void Decode_i0100_nnnn_0010_0100(uint opcode);
    internal partial void Decode_i0100_nnnn_0010_0101(uint opcode);
    internal partial void Decode_i0100_nnnn_0110_1010(uint opcode);
    internal partial void Decode_i0100_nnnn_0110_0110(uint opcode);
    internal partial void Decode_i0100_nnnn_0001_1011(uint opcode);
    internal partial void Decode_i0100_nnnn_0000_0011(uint opcode);
#endif
}
