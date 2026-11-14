namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public sealed partial class CpuZ80<TBus> where TBus : class, ICpuZ80Bus
{
    private static byte[] SZ => FlagTables.SZ;
    private static byte[] SZ_BIT => FlagTables.SZ_BIT;
    private static byte[] SZP => FlagTables.SZP;
    private static byte[] SZHV_inc => FlagTables.SZHV_inc;
    private static byte[] SZHV_dec => FlagTables.SZHV_dec;
    private static byte[] SZHVC_add => FlagTables.SZHVC_add;
    private static byte[] SZHVC_sub => FlagTables.SZHVC_sub;

    private int INC(int value) { int result = (value + 1) & 255; F = (F & CF) | SZHV_inc[result]; return result; }
    private int DEC(int value) { int result = (value - 1) & 255; F = (F & CF) | SZHV_dec[result]; return result; }
    private void RLCA() { A = (A << 1) | (A >> 7); F = (F & (SF | ZF | PF)) | (A & (YF | XF | CF)); }
    private void RRCA() { F = (F & (SF | ZF | PF)) | (A & CF); A = (A >> 1) | (A << 7); F |= A & (YF | XF); }
    private void RLA()
    {
        int result = ((A << 1) | (F & CF)) & 255;
        int carry = (A & 0x80) != 0 ? CF : 0;
        F = (F & (SF | ZF | PF)) | carry | (result & (YF | XF));
        A = result;
    }
    private void RRA()
    {
        int result = ((A >> 1) | (F << 7)) & 255;
        int carry = A & CF;
        F = (F & (SF | ZF | PF)) | carry | (result & (YF | XF));
        A = result;
    }
    private void RRD()
    {
        int value = RM(HL);
        WZ = HL + 1;
        WM(HL, (value >> 4) | (A << 4));
        A = (A & 0xf0) | (value & 15);
        F = (F & CF) | SZP[A];
    }
    private void RLD()
    {
        int value = RM(HL);
        WZ = HL + 1;
        WM(HL, (value << 4) | (A & 15));
        A = (A & 0xf0) | (value >> 4);
        F = (F & CF) | SZP[A];
    }
    private void ADD(int value) { int ah = AFD & 0xff00; int result = ((ah >> 8) + value) & 255; F = SZHVC_add[ah | result]; A = result; }
    private void ADC(int value) { int ah = AFD & 0xff00, c = AFD & 1; int result = ((ah >> 8) + value + c) & 255; F = SZHVC_add[(c << 16) | ah | result]; A = result; }
    private void SUB(int value) { int ah = AFD & 0xff00; int result = ((ah >> 8) - value) & 255; F = SZHVC_sub[ah | result]; A = result; }
    private void SBC(int value) { int ah = AFD & 0xff00, c = AFD & 1; int result = ((ah >> 8) - value - c) & 255; F = SZHVC_sub[(c << 16) | ah | result]; A = result; }
    private void NEG() { int value = A; A = 0; SUB(value); }
    private void DAA()
    {
        int value = A;
        if ((F & NF) != 0)
        {
            if ((F & HF) != 0 || (A & 15) > 9) value = (value - 6) & 255;
            if ((F & CF) != 0 || A > 0x99) value = (value - 0x60) & 255;
        }
        else
        {
            if ((F & HF) != 0 || (A & 15) > 9) value = (value + 6) & 255;
            if ((F & CF) != 0 || A > 0x99) value = (value + 0x60) & 255;
        }
        F = (F & (CF | NF)) | (A > 0x99 ? 1 : 0) | ((A ^ value) & HF) | SZP[value];
        A = value;
    }
    private void AND(int value) { A &= value; F = SZP[A] | HF; }
    private void OR(int value) { A |= value; F = SZP[A]; }
    private void XOR(int value) { A ^= value; F = SZP[A]; }
    private void CP(int value) { int ah = AFD & 0xff00; int result = ((ah >> 8) - value) & 255; F = (SZHVC_sub[ah | result] & ~(YF | XF)) | (value & (YF | XF)); }
    private void EX_AF() { var temporary = _state.af; _state.af = _state.af2; _state.af2 = temporary; }
    private void EX_DE_HL() { var temporary = _state.de; _state.de = _state.hl; _state.hl = temporary; }
    private void EXX()
    {
        var temporary = _state.bc; _state.bc = _state.bc2; _state.bc2 = temporary;
        temporary = _state.de; _state.de = _state.de2; _state.de2 = temporary;
        temporary = _state.hl; _state.hl = _state.hl2; _state.hl2 = temporary;
    }
    private void EXSP(ref CpuZ80Pair register)
    {
        CpuZ80Pair temporary = default;
        RM16(SPD, ref temporary);
        WM16(SPD, ref register);
        register = temporary;
        WZ = register.Data;
    }
    private void ADD16(ref CpuZ80Pair destination, CpuZ80Pair source)
    {
        uint result = unchecked((uint)destination.Data + (uint)source.Data);
        WZ = destination.Data + 1;
        F = (F & (SF | ZF | VF)) | (((destination.Data ^ (int)result ^ source.Data) >> 8) & HF)
            | ((int)(result >> 16) & CF) | ((int)(result >> 8) & (YF | XF));
        destination.Word = (int)result;
    }
    private void ADC16(ref CpuZ80Pair register)
    {
        uint result = unchecked((uint)HLD + (uint)register.Data + (uint)(F & CF));
        WZ = HL + 1;
        F = (((HLD ^ (int)result ^ register.Data) >> 8) & HF) | ((int)(result >> 16) & CF)
            | ((int)(result >> 8) & (SF | YF | XF)) | ((result & 0xffff) != 0 ? 0 : ZF)
            | (((register.Data ^ HLD ^ 0x8000) & (register.Data ^ (int)result) & 0x8000) >> 13);
        HL = (int)result;
    }
    private void SBC16(ref CpuZ80Pair register)
    {
        uint result = unchecked((uint)HLD - (uint)register.Data - (uint)(F & CF));
        WZ = HL + 1;
        F = (((HLD ^ (int)result ^ register.Data) >> 8) & HF) | NF | ((int)(result >> 16) & CF)
            | ((int)(result >> 8) & (SF | YF | XF)) | ((result & 0xffff) != 0 ? 0 : ZF)
            | (((register.Data ^ HLD) & (HLD ^ (int)result) & 0x8000) >> 13);
        HL = (int)result;
    }
    private int RLC(int value) { int carry = (value & 0x80) != 0 ? CF : 0; int result = ((value << 1) | (value >> 7)) & 255; F = SZP[result] | carry; return result; }
    private int RRC(int value) { int carry = value & CF; int result = ((value >> 1) | (value << 7)) & 255; F = SZP[result] | carry; return result; }
    private int RL(int value) { int carry = (value & 0x80) != 0 ? CF : 0; int result = ((value << 1) | (F & CF)) & 255; F = SZP[result] | carry; return result; }
    private int RR(int value) { int carry = value & CF; int result = ((value >> 1) | (F << 7)) & 255; F = SZP[result] | carry; return result; }
    private int SLA(int value) { int carry = (value & 0x80) != 0 ? CF : 0; int result = (value << 1) & 255; F = SZP[result] | carry; return result; }
    private int SRA(int value) { int carry = value & CF; int result = ((value >> 1) | (value & 0x80)) & 255; F = SZP[result] | carry; return result; }
    private int SLL(int value) { int carry = (value & 0x80) != 0 ? CF : 0; int result = ((value << 1) | 1) & 255; F = SZP[result] | carry; return result; }
    private int SRL(int value) { int carry = value & CF; int result = value >> 1; F = SZP[result] | carry; return result; }
    private void BIT(int bit, int register) => F = (F & CF) | HF | (SZ_BIT[register & (1 << bit)] & ~(YF | XF)) | (register & (YF | XF));
    private void BIT_HL(int bit, int register) => F = (F & CF) | HF | (SZ_BIT[register & (1 << bit)] & ~(YF | XF)) | (WZ_H & (YF | XF));
    private void BIT_XY(int bit, int register) => F = (F & CF) | HF | (SZ_BIT[register & (1 << bit)] & ~(YF | XF)) | ((EA >> 8) & (YF | XF));
    private static int RES(int bit, int value) => value & ~(1 << bit);
    private static int SET(int bit, int value) => value | (1 << bit);

    private static class FlagTables
    {
        internal static readonly byte[] SZ = new byte[256], SZ_BIT = new byte[256], SZP = new byte[256], SZHV_inc = new byte[256], SZHV_dec = new byte[256];
        internal static readonly byte[] SZHVC_add = new byte[2 * 256 * 256], SZHVC_sub = new byte[2 * 256 * 256];
        static FlagTables()
        {
            for (int oldValue = 0; oldValue < 256; oldValue++)
                for (int newValue = 0; newValue < 256; newValue++)
                {
                    int index = oldValue * 256 + newValue;
                    int basic = (newValue != 0 ? newValue & SF : ZF) | (newValue & (YF | XF));
                    int value = newValue - oldValue;
                    SZHVC_add[index] = (byte)(basic | ((newValue & 15) < (oldValue & 15) ? HF : 0)
                        | (newValue < oldValue ? CF : 0) | (((value ^ oldValue ^ 0x80) & (value ^ newValue) & 0x80) != 0 ? VF : 0));
                    value = newValue - oldValue - 1;
                    SZHVC_add[index + 65536] = (byte)(basic | ((newValue & 15) <= (oldValue & 15) ? HF : 0)
                        | (newValue <= oldValue ? CF : 0) | (((value ^ oldValue ^ 0x80) & (value ^ newValue) & 0x80) != 0 ? VF : 0));
                    value = oldValue - newValue;
                    SZHVC_sub[index] = (byte)(NF | basic | ((newValue & 15) > (oldValue & 15) ? HF : 0)
                        | (newValue > oldValue ? CF : 0) | (((value ^ oldValue) & (oldValue ^ newValue) & 0x80) != 0 ? VF : 0));
                    value = oldValue - newValue - 1;
                    SZHVC_sub[index + 65536] = (byte)(NF | basic | ((newValue & 15) >= (oldValue & 15) ? HF : 0)
                        | (newValue >= oldValue ? CF : 0) | (((value ^ oldValue) & (oldValue ^ newValue) & 0x80) != 0 ? VF : 0));
                }
            for (int i = 0; i < 256; i++)
            {
                int parity = System.Numerics.BitOperations.PopCount((uint)i) & 1;
                SZ[i] = (byte)((i != 0 ? i & SF : ZF) | (i & (YF | XF)));
                SZ_BIT[i] = (byte)((i != 0 ? i & SF : ZF | PF) | (i & (YF | XF)));
                SZP[i] = (byte)(SZ[i] | (parity != 0 ? 0 : PF));
                SZHV_inc[i] = (byte)(SZ[i] | (i == 0x80 ? VF : 0) | ((i & 15) == 0 ? HF : 0));
                SZHV_dec[i] = (byte)(SZ[i] | NF | (i == 0x7f ? VF : 0) | ((i & 15) == 15 ? HF : 0));
            }
        }
    }
}
