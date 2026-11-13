namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal sealed partial class Z80<TBus> where TBus : class, IZ80Bus
{
    private void LDI()
    {
        int io = RM(HL);
        WM(DE, io);
        F &= SF | ZF | CF;
        if (((A + io) & 2) != 0) F |= YF;
        if (((A + io) & 8) != 0) F |= XF;
        HL++; DE++; BC--;
        if (BC != 0) F |= VF;
    }
    private void LDD()
    {
        int io = RM(HL);
        WM(DE, io);
        F &= SF | ZF | CF;
        if (((A + io) & 2) != 0) F |= YF;
        if (((A + io) & 8) != 0) F |= XF;
        HL--; DE--; BC--;
        if (BC != 0) F |= VF;
    }
    private void CPI()
    {
        int value = RM(HL);
        int result = (A - value) & 255;
        WZ++; HL++; BC--;
        F = (F & CF) | (SZ[result] & ~(YF | XF)) | ((A ^ value ^ result) & HF) | NF;
        if ((F & HF) != 0) result = (result - 1) & 255;
        if ((result & 2) != 0) F |= YF;
        if ((result & 8) != 0) F |= XF;
        if (BC != 0) F |= VF;
    }
    private void CPD()
    {
        int value = RM(HL);
        int result = (A - value) & 255;
        WZ--; HL--; BC--;
        F = (F & CF) | (SZ[result] & ~(YF | XF)) | ((A ^ value ^ result) & HF) | NF;
        if ((F & HF) != 0) result = (result - 1) & 255;
        if ((result & 2) != 0) F |= YF;
        if ((result & 8) != 0) F |= XF;
        if (BC != 0) F |= VF;
    }
    private void INI()
    {
        int io = IN(BC);
        WZ = BC + 1;
        USE_CYCLES(cc_ex[0xa2]);
        B--;
        WM(HL, io);
        HL++;
        F = SZ[B];
        int t = ((C + 1) & 255) + io;
        if ((io & SF) != 0) F |= NF;
        if ((t & 0x100) != 0) F |= HF | CF;
        F |= SZP[(t & 7) ^ B] & PF;
    }
    private void IND()
    {
        int io = IN(BC);
        WZ = BC - 1;
        USE_CYCLES(cc_ex[0xaa]);
        B--;
        WM(HL, io);
        HL--;
        F = SZ[B];
        int t = ((C - 1) & 255) + io;
        if ((io & SF) != 0) F |= NF;
        if ((t & 0x100) != 0) F |= HF | CF;
        F |= SZP[(t & 7) ^ B] & PF;
    }
    private void OUTI()
    {
        int io = RM(HL);
        B--;
        WZ = BC + 1;
        OUT(BC, io);
        HL++;
        F = SZ[B];
        int t = L + io;
        if ((io & SF) != 0) F |= NF;
        if ((t & 0x100) != 0) F |= HF | CF;
        F |= SZP[(t & 7) ^ B] & PF;
    }
    private void OUTD()
    {
        int io = RM(HL);
        B--;
        WZ = BC - 1;
        OUT(BC, io);
        HL--;
        F = SZ[B];
        int t = L + io;
        if ((io & SF) != 0) F |= NF;
        if ((t & 0x100) != 0) F |= HF | CF;
        F |= SZP[(t & 7) ^ B] & PF;
    }
    private void LDIR() { LDI(); if (BC != 0) { PC -= 2; WZ = PC + 1; USE_CYCLES(cc_ex[0xb0]); } }
    private void LDDR() { LDD(); if (BC != 0) { PC -= 2; WZ = PC + 1; USE_CYCLES(cc_ex[0xb8]); } }
    private void CPIR() { CPI(); if (BC != 0 && (F & ZF) == 0) { PC -= 2; WZ = PC + 1; USE_CYCLES(cc_ex[0xb1]); } }
    private void CPDR() { CPD(); if (BC != 0 && (F & ZF) == 0) { PC -= 2; WZ = PC + 1; USE_CYCLES(cc_ex[0xb9]); } }
    private void INIR() { INI(); if (B != 0) { PC -= 2; USE_CYCLES(cc_ex[0xb2]); } }
    private void INDR() { IND(); if (B != 0) { PC -= 2; USE_CYCLES(cc_ex[0xba]); } }
    private void OTIR() { OUTI(); if (B != 0) { PC -= 2; USE_CYCLES(cc_ex[0xb3]); } }
    private void OTDR() { OUTD(); if (B != 0) { PC -= 2; USE_CYCLES(cc_ex[0xbb]); } }
}
