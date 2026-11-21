// Copyright 2003-2004 Stephane Dallongeville
// Copyright 2004 Theo Berkau
// Translated from Yabause c68k/gen68k.inc.
// SPDX-License-Identifier: GPL-2.0-or-later

#if C68K_GEN
namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68kGenerator
{
    private const int EaDReg = 0;
    private const int EaAReg = 1;
    private const int EaAInd = 2;
    private const int EaAInc = 3;
    private const int EaADec = 4;
    private const int EaD16A = 5;
    private const int EaD8AX = 6;
    private const int EaA16 = 7;
    private const int EaA32 = 8;
    private const int EaD16P = 9;
    private const int EaD8PX = 10;
    private const int EaImm = 11;
    private const int EaAInc7 = 12;
    private const int EaADec7 = 13;
    private const int EaIllegal = 15;

    private const int SizeByte = 0;
    private const int SizeWord = 1;
    private const int SizeLong = 2;
    private const ushort OpIllegal = 0x4AFC;

    private static readonly uint[] JumpAndCallCycles =
        [0, 0, 4, 0, 0, 6, 10, 6, 8, 6, 10, 0, 0, 0, 0];
    private static readonly uint[] LeaAndPeaCycles =
        [0, 0, 0, 0, 0, 4, 8, 4, 8, 4, 8, 0, 0, 0, 0];
    private static readonly uint[] MovemCycles =
        [0, 0, 0, 0, 0, 4, 6, 4, 8, 4, 6, 0, 0, 0, 0];

    private OpcodeInfo _currentOp;
    private int _currentEa;
    private int _currentEam;
    private int _currentReg;
    private int _currentEa2;
    private int _currentEam2;
    private int _currentReg2;
    private int _currentSize;
    private uint _currentBitsMask;
    private int _currentShiftMask;

    private readonly ushort[] _opcodeJumpTable = new ushort[0x10000];

    private static OpcodeInfo[] CreateOpcodeInfoTable(Func<string, Action> getGenerator) =>
    [
        new("1010", 0xA000, 0xF000, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("Gen1010")),
        new("1111", 0xF000, 0xF000, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("Gen1111")),
        new("ORI", 0x0000, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenORI")),
        new("ORICCR", 0x003C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenORICCR")),
        new("ORISR", 0x007C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenORISR")),
        new("ANDI", 0x0200, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenANDI")),
        new("ANDICCR", 0x023C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenANDICCR")),
        new("ANDISR", 0x027C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenANDISR")),
        new("EORI", 0x0A00, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenEORI")),
        new("EORICCR", 0x0A3C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenEORICCR")),
        new("EORISR", 0x0A7C, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenEORISR")),
        new("SUBI", 0x0400, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenSUBI")),
        new("ADDI", 0x0600, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenADDI")),
        new("CMPI", 0x0C00, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenCMPI")),
        new("BTSTn", 0x0800, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooooo-", "------------", getGenerator("GenBTSTn")),
        new("BCHGn", 0x0840, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenBCHGn")),
        new("BCLRn", 0x0880, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenBCLRn")),
        new("BSETn", 0x08C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenBSETn")),
        new("BTST", 0x0100, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenBTST")),
        new("BCHG", 0x0140, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-ooooooo---", "------------", getGenerator("GenBCHG")),
        new("BCLR", 0x0180, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-ooooooo---", "------------", getGenerator("GenBCLR")),
        new("BSET", 0x01C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-ooooooo---", "------------", getGenerator("GenBSET")),
        new("MOVEPWaD", 0x0108, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenMOVEPWaD")),
        new("MOVEPLaD", 0x0148, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenMOVEPLaD")),
        new("MOVEPWDa", 0x0188, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenMOVEPWDa")),
        new("MOVEPLDa", 0x01C8, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenMOVEPLDa")),
        new("MOVEB", 0x1000, 0xF000, 0, 0, 3, 0, 6, 9, "oooooooooooo", "o-ooooooo---", getGenerator("GenMOVEB")),
        new("MOVEL", 0x2000, 0xF000, 0, 0, 3, 0, 6, 9, "oooooooooooo", "o-ooooooo---", getGenerator("GenMOVEL")),
        new("MOVEW", 0x3000, 0xF000, 0, 0, 3, 0, 6, 9, "oooooooooooo", "o-ooooooo---", getGenerator("GenMOVEW")),
        new("MOVEAL", 0x2040, 0xF1C0, 0, 0, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenMOVEAL")),
        new("MOVEAW", 0x3040, 0xF1C0, 0, 0, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenMOVEAW")),
        new("NEGX", 0x4000, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenNEGX")),
        new("CLR", 0x4200, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenCLR")),
        new("NEG", 0x4400, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenNEG")),
        new("NOT", 0x4600, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenNOT")),
        new("MOVESRa", 0x40C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenMOVESRa")),
        new("MOVEaCCR", 0x44C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-oooooooooo", "------------", getGenerator("GenMOVEaCCR")),
        new("MOVEaSR", 0x46C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-oooooooooo", "------------", getGenerator("GenMOVEaSR")),
        new("NBCD", 0x4800, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenNBCD")),
        new("PEA", 0x4840, 0xFFC0, 0, 0, 3, 0, -1, -1, "--o--oooooo-", "------------", getGenerator("GenPEA")),
        new("SWAP", 0x4840, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenSWAP")),
        new("MOVEMRa", 0x4880, 0xFF80, 1, 6, 3, 0, -1, -1, "--o-ooooo---", "------------", getGenerator("GenMOVEMRa")),
        new("EXT", 0x4880, 0xFFB8, 1, 6, -1, 0, -1, -1, "------------", "------------", getGenerator("GenEXT")),
        new("TST", 0x4A00, 0xFF00, 2, 6, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenTST")),
        new("TAS", 0x4AC0, 0xFFC0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenTAS")),
        new("ILLEGAL", 0x4AFC, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenILLEGAL")),
        new("MOVEMaR", 0x4C80, 0xFF80, 1, 6, 3, 0, -1, -1, "--oo-oooooo-", "------------", getGenerator("GenMOVEMaR")),
        new("TRAP", 0x4E40, 0xFFF0, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenTRAP")),
        new("LINK", 0x4E50, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenLINK")),
        new("LINKA7", 0x4E57, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenLINKA7")),
        new("ULNK", 0x4E58, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenULNK")),
        new("ULNKA7", 0x4E5F, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenULNKA7")),
        new("MOVEAUSP", 0x4E60, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenMOVEAUSP")),
        new("MOVEUSPA", 0x4E68, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenMOVEUSPA")),
        new("RESET", 0x4E70, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenRESET")),
        new("NOP", 0x4E71, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenNOP")),
        new("STOP", 0x4E72, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenSTOP")),
        new("RTE", 0x4E73, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenRTE")),
        new("RTS", 0x4E75, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenRTS")),
        new("TRAPV", 0x4E76, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenTRAPV")),
        new("RTR", 0x4E77, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenRTR")),
        new("JSR", 0x4E80, 0xFFC0, 0, 0, 3, 0, -1, -1, "--o--oooooo-", "------------", getGenerator("GenJSR")),
        new("JMP", 0x4EC0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--o--oooooo-", "------------", getGenerator("GenJMP")),
        new("CHK", 0x4180, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenCHK")),
        new("LEA", 0x41C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "--o--oooooo-", "------------", getGenerator("GenLEA")),
        new("STCC", 0x50C0, 0xF0C0, 0, 0, 3, 0, -1, -1, "o-ooooooo---", "------------", getGenerator("GenSTCC")),
        new("DBCC", 0x50C8, 0xF0F8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenDBCC")),
        new("ADDQ", 0x5000, 0xF100, 2, 6, 3, 0, -1, -1, "ooooooooo---", "------------", getGenerator("GenADDQ")),
        new("SUBQ", 0x5100, 0xF100, 2, 6, 3, 0, -1, -1, "ooooooooo---", "------------", getGenerator("GenSUBQ")),
        new("BCC", 0x6000, 0xF000, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBCC")),
        new("BCC16", 0x6000, 0xF0FF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBCC16")),
        new("BRA", 0x6000, 0xFF00, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBRA")),
        new("BRA16", 0x6000, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBRA16")),
        new("BSR", 0x6100, 0xFF00, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBSR")),
        new("BSR16", 0x6100, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenBSR16")),
        new("MOVEQ", 0x7000, 0xF100, 0, 0, -1, 9, -1, -1, "------------", "------------", getGenerator("GenMOVEQ")),
        new("ORaD", 0x8000, 0xF100, 2, 6, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenORaD")),
        new("ORDa", 0x8100, 0xF100, 2, 6, 3, 0, -1, 9, "--ooooooo---", "------------", getGenerator("GenORDa")),
        new("SBCD", 0x8100, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSBCD")),
        new("SBCDM", 0x8108, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSBCDM")),
        new("SBCD7M", 0x810F, 0xF1FF, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSBCD7M")),
        new("SBCDM7", 0x8F08, 0xFFF8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSBCDM7")),
        new("SBCD7M7", 0x8F0F, 0xFFFF, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSBCD7M7")),
        new("DIVU", 0x80C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenDIVU")),
        new("DIVS", 0x81C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenDIVS")),
        new("SUBaD", 0x9000, 0xF100, 2, 6, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenSUBaD")),
        new("SUBDa", 0x9100, 0xF100, 2, 6, 3, 0, -1, 9, "--ooooooo---", "------------", getGenerator("GenSUBDa")),
        new("SUBX", 0x9100, 0xF138, 2, 6, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSUBX")),
        new("SUBXM", 0x9108, 0xF138, 2, 6, -1, 0, -1, 9, "------------", "------------", getGenerator("GenSUBXM")),
        new("SUBX7M", 0x910F, 0xF13F, 2, 6, -1, -1, -1, 9, "------------", "------------", getGenerator("GenSUBX7M")),
        new("SUBXM7", 0x9F08, 0xFF38, 2, 6, -1, 0, -1, -1, "------------", "------------", getGenerator("GenSUBXM7")),
        new("SUBX7M7", 0x9F0F, 0xFF3F, 2, 6, -1, -1, -1, -1, "------------", "------------", getGenerator("GenSUBX7M7")),
        new("SUBA", 0x90C0, 0xF0C0, 1, 8, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenSUBA")),
        new("CMP", 0xB000, 0xF100, 2, 6, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenCMP")),
        new("CMPM", 0xB108, 0xF138, 2, 6, -1, 0, -1, 9, "------------", "------------", getGenerator("GenCMPM")),
        new("CMP7M", 0xB10F, 0xF13F, 2, 6, -1, -1, -1, 9, "------------", "------------", getGenerator("GenCMP7M")),
        new("CMPM7", 0xBF08, 0xFF38, 2, 6, -1, 0, -1, -1, "------------", "------------", getGenerator("GenCMPM7")),
        new("CMP7M7", 0xBF0F, 0xFF3F, 2, 6, -1, -1, -1, -1, "------------", "------------", getGenerator("GenCMP7M7")),
        new("EORDa", 0xB100, 0xF100, 2, 6, 3, 0, -1, 9, "o-ooooooo---", "------------", getGenerator("GenEORDa")),
        new("CMPA", 0xB0C0, 0xF0C0, 1, 8, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenCMPA")),
        new("ANDaD", 0xC000, 0xF100, 2, 6, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenANDaD")),
        new("ANDDa", 0xC100, 0xF100, 2, 6, 3, 0, -1, 9, "--ooooooo---", "------------", getGenerator("GenANDDa")),
        new("ABCD", 0xC100, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenABCD")),
        new("ABCDM", 0xC108, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenABCDM")),
        new("ABCD7M", 0xC10F, 0xF1FF, 0, 0, -1, -1, -1, 9, "------------", "------------", getGenerator("GenABCD7M")),
        new("ABCDM7", 0xCF08, 0xFFF8, 0, 0, -1, 0, -1, -1, "------------", "------------", getGenerator("GenABCDM7")),
        new("ABCD7M7", 0xCF0F, 0xFFFF, 0, 0, -1, -1, -1, -1, "------------", "------------", getGenerator("GenABCD7M7")),
        new("MULU", 0xC0C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenMULU")),
        new("MULS", 0xC1C0, 0xF1C0, 0, 0, 3, 0, -1, 9, "o-oooooooooo", "------------", getGenerator("GenMULS")),
        new("EXGDD", 0xC140, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenEXGDD")),
        new("EXGAA", 0xC148, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenEXGAA")),
        new("EXGAD", 0xC188, 0xF1F8, 0, 0, -1, 0, -1, 9, "------------", "------------", getGenerator("GenEXGAD")),
        new("ADDaD", 0xD000, 0xF100, 2, 6, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenADDaD")),
        new("ADDDa", 0xD100, 0xF100, 2, 6, 3, 0, -1, 9, "--ooooooo---", "------------", getGenerator("GenADDDa")),
        new("ADDX", 0xD100, 0xF138, 2, 6, -1, 0, -1, 9, "------------", "------------", getGenerator("GenADDX")),
        new("ADDXM", 0xD108, 0xF138, 2, 6, -1, 0, -1, 9, "------------", "------------", getGenerator("GenADDXM")),
        new("ADDX7M", 0xD10F, 0xF13F, 2, 6, -1, -1, -1, 9, "------------", "------------", getGenerator("GenADDX7M")),
        new("ADDXM7", 0xDF08, 0xFF38, 2, 6, -1, 0, -1, -1, "------------", "------------", getGenerator("GenADDXM7")),
        new("ADDX7M7", 0xDF0F, 0xFF3F, 2, 6, -1, -1, -1, -1, "------------", "------------", getGenerator("GenADDX7M7")),
        new("ADDA", 0xD0C0, 0xF0C0, 1, 8, 3, 0, -1, 9, "oooooooooooo", "------------", getGenerator("GenADDA")),
        new("ASRk", 0xE000, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenASRk")),
        new("LSRk", 0xE008, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenLSRk")),
        new("ROXRk", 0xE010, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenROXRk")),
        new("RORk", 0xE018, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenRORk")),
        new("ASLk", 0xE100, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenASLk")),
        new("LSLk", 0xE108, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenLSLk")),
        new("ROXLk", 0xE110, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenROXLk")),
        new("ROLk", 0xE118, 0xF138, 2, 6, -1, 0, -1, -1, "o-----------", "------------", getGenerator("GenROLk")),
        new("ASRD", 0xE020, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenASRD")),
        new("LSRD", 0xE028, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenLSRD")),
        new("ROXRD", 0xE030, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenROXRD")),
        new("RORD", 0xE038, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenRORD")),
        new("ASLD", 0xE120, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenASLD")),
        new("LSLD", 0xE128, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenLSLD")),
        new("ROXLD", 0xE130, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenROXLD")),
        new("ROLD", 0xE138, 0xF138, 2, 6, -1, 0, -1, 9, "o-----------", "o-----------", getGenerator("GenROLD")),
        new("ASR", 0xE0C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenASR")),
        new("LSR", 0xE2C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenLSR")),
        new("ROXR", 0xE4C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenROXR")),
        new("ROR", 0xE6C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenROR")),
        new("ASL", 0xE1C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenASL")),
        new("LSL", 0xE3C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenLSL")),
        new("ROXL", 0xE5C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenROXL")),
        new("ROL", 0xE7C0, 0xFFC0, 0, 0, 3, 0, -1, -1, "--ooooooo---", "------------", getGenerator("GenROL")),
    ];

    private void GenerateJumpTable(int opcodeBase, int start1, int end1, int step1,
        int start2, int end2, int step2, int start3, int end3, int step3, int operation)
    {
        opcodeBase &= 0xFFFF;
        start1 &= 0xFFFF;
        end1 &= 0xFFFF;
        step1 &= 0xFFFF;
        if (end1 < start1)
            end1 = start1;

        start2 &= 0xFFFF;
        end2 &= 0xFFFF;
        step2 &= 0xFFFF;
        if (end2 < start2)
            end2 = start2;

        var target = (ushort)(operation & 0xFFFF);
        if (start1 == end1)
        {
            _opcodeJumpTable[opcodeBase] = target;
        }
        else if (start2 == end2)
        {
            for (int i = start1; i <= end1; i += step1)
                _opcodeJumpTable[opcodeBase + i] = target;
        }
        else if (start3 == end3)
        {
            for (int i = start1; i <= end1; i += step1)
            for (int j = start2; j <= end2; j += step2)
                _opcodeJumpTable[opcodeBase + i + j] = target;
        }
        else
        {
            for (int i = start1; i <= end1; i += step1)
            for (int j = start2; j <= end2; j += step2)
            for (int k = start3; k <= end3; k += step3)
                _opcodeJumpTable[opcodeBase + i + j + k] = target;
        }
    }

    private void GenerateOpcodeJumpTableExtended(int opcodeBase, int start3, int end3, int step3, int operation)
    {
        int start1 = 0, end1 = 0, step1 = 0;
        int start2 = 0, end2 = 0, step2 = 0;

        if (_currentOp.RegisterShift != -1 && _currentEa < 7)
        {
            end1 = (_currentEa == EaAInc || _currentEa == EaADec ? 6 : 7) << _currentOp.RegisterShift;
            step1 = 1 << _currentOp.RegisterShift;
        }

        if (_currentOp.SecondRegisterShift != -1 && _currentEa2 < 7)
        {
            end2 = (_currentEa2 == EaAInc || _currentEa2 == EaADec ? 6 : 7) << _currentOp.SecondRegisterShift;
            step2 = 1 << _currentOp.SecondRegisterShift;
        }

        if (start1 != end1)
        {
            if (start2 != end2)
                GenerateJumpTable(opcodeBase, start1, end1, step1, start2, end2, step2, start3, end3, step3, operation);
            else
                GenerateJumpTable(opcodeBase, start1, end1, step1, start3, end3, step3, start2, end2, step2, operation);
        }
        else if (start2 != end2)
            GenerateJumpTable(opcodeBase, start2, end2, step2, start3, end3, step3, start1, end1, step1, operation);
        else
            GenerateJumpTable(opcodeBase, start3, end3, step3, start2, end2, step2, start1, end1, step1, operation);
    }

    private void GenerateOpcodeJumpTable(int operation) =>
        GenerateOpcodeJumpTableExtended(operation, 0, 0, 0, operation);

    private bool HasEffectiveAddress(int ea)
    {
        if (ea == EaAInc7) return _currentOp.SupportedEffectiveAddresses[EaAInc] == 'o';
        if (ea == EaADec7) return _currentOp.SupportedEffectiveAddresses[EaADec] == 'o';
        if (ea <= EaImm) return _currentOp.SupportedEffectiveAddresses[ea] == 'o';
        return false;
    }

    private bool HasSecondEffectiveAddress(int ea)
    {
        if (ea == EaAInc7) return _currentOp.SecondSupportedEffectiveAddresses[EaAInc] == 'o';
        if (ea == EaADec7) return _currentOp.SecondSupportedEffectiveAddresses[EaADec] == 'o';
        if (ea <= EaImm) return _currentOp.SecondSupportedEffectiveAddresses[ea] == 'o';
        return false;
    }

    private static int EffectiveAddressToModeAndRegister(int ea)
    {
        if (ea < 7) return ea << 3;
        if (ea == EaAInc7) return (EaAInc << 3) | 7;
        if (ea == EaADec7) return (EaADec << 3) | 7;
        if (ea <= EaImm) return (7 << 3) | (ea - 7);
        return (7 << 3) | 7;
    }

    private static bool IsMemoryEffectiveAddress(int ea) => ea > EaAReg && ea != EaImm;

    private int GetCurrentOpcodeBase()
    {
        int opcode = _currentOp.Base;
        if (_currentOp.EffectiveAddressModeShift != -1)
            opcode += (_currentEam & 7) << _currentOp.EffectiveAddressModeShift;
        if (_currentOp.RegisterShift != -1)
            opcode += (_currentReg & 7) << _currentOp.RegisterShift;
        if (_currentOp.SecondEffectiveAddressModeShift != -1)
            opcode += (_currentEam2 & 7) << _currentOp.SecondEffectiveAddressModeShift;
        if (_currentOp.SecondRegisterShift != -1)
            opcode += (_currentReg2 & 7) << _currentOp.SecondRegisterShift;
        if (_currentOp.SizeType == 1)
            opcode += (_currentSize - 1) << _currentOp.SizeShift;
        else if (_currentOp.SizeType == 2)
            opcode += (_currentSize & 3) << _currentOp.SizeShift;

        return opcode;
    }

    private void SetCurrentSize(int size)
    {
        _currentSize = size;
        switch (_currentSize)
        {
            case SizeByte:
                _currentBitsMask = 0xFF;
                _currentShiftMask = 7;
                break;
            case SizeWord:
                _currentBitsMask = 0xFFFF;
                _currentShiftMask = 15;
                break;
            case SizeLong:
                _currentBitsMask = 0xFFFFFFFF;
                _currentShiftMask = 31;
                break;
        }
    }
}
#endif
