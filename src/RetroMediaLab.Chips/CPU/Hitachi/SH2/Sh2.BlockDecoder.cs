using System.Reflection;
using System.Reflection.Emit;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private static readonly MethodInfo[] CompiledInstructions =
    [
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBS0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWS0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLS0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MULL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBL0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWL0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLL0), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MACL_), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLS4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DIV0S), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(TST), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(AND), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(XOR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(OR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPSTR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(XTRCT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MULU), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MULS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPEQ), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPHS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPGE), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DIV1), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DMULU), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPHI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPGT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SUB), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SUBC), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SUBV), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ADD), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DMULS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ADDC), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ADDV), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MACW), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLL4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOV), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(NOT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SWAPB), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SWAPW), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(NEGC), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(NEG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(EXTUB), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(EXTUW), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(EXTSB), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(EXTSW), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ADDI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BRA), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBS4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWS4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBL4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWL4), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPIM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BF), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BTS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BFS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBSG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWSG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLSG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(TRAPA), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVBLG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVWLG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVLLG), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVA), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(TSTI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ANDI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(XORI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ORI), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(TSTM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ANDM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(XORM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ORM), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BSRF), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSMACH), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCGBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSMACL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCVBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(BRAF), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(MOVT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSPR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSMMACH), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCMSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ROTL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ROTR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSMMACH), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCMSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLL2), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLR2), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSMACH), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(JSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCSR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPPZ), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSMMACL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCMGBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CMPPL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSMMACL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCMGBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLL8), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLR8), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSMACL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(TAS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCGBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHAL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHAR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STSMPR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(STCMVBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ROTCL), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ROTCR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSMPR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCMVBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLL16), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SHLR16), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDSPR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(JMP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(LDCVBR), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CLRT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(NOP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(RTS), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SETT), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(DIV0U), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(SLEEP), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(CLRMAC), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(RTE), BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(Sh2<TBus>).GetMethod(nameof(ILLEGAL), BindingFlags.NonPublic | BindingFlags.Instance)!,
    ];
    // Conservative arithmetic part of PicoDrive's scan_block separation between
    // register-only operations and memory/control-flow boundaries. These helpers
    // neither observe PC/cycles nor invoke the bus, so their clocks can be batched.
    private static bool IsCompiledPureInstruction(ushort opcode) => CompiledInstructions[Sh2OpcodeTable.Types[opcode]].Name is
        nameof(ADD) or nameof(ADDC) or nameof(ADDI) or nameof(ADDV) or nameof(AND) or nameof(ANDI)
        or nameof(CMPEQ) or nameof(CMPGE) or nameof(CMPGT) or nameof(CMPHI) or nameof(CMPHS)
        or nameof(CMPIM) or nameof(CMPPL) or nameof(CMPPZ) or nameof(CMPSTR)
        or nameof(DIV0S) or nameof(DIV0U) or nameof(DIV1) or nameof(DMULS) or nameof(DMULU)
        or nameof(EXTSB) or nameof(EXTSW) or nameof(EXTUB) or nameof(EXTUW)
        or nameof(MOV) or nameof(MOVI) or nameof(MOVT) or nameof(MULL) or nameof(MULS) or nameof(MULU)
        or nameof(NEG) or nameof(NEGC) or nameof(NOT) or nameof(OR) or nameof(ORI)
        or nameof(ROTCL) or nameof(ROTCR) or nameof(ROTL) or nameof(ROTR)
        or nameof(SHAL) or nameof(SHAR) or nameof(SHLL) or nameof(SHLL2) or nameof(SHLL8) or nameof(SHLL16)
        or nameof(SHLR) or nameof(SHLR2) or nameof(SHLR8) or nameof(SHLR16)
        or nameof(SUB) or nameof(SUBC) or nameof(SUBV) or nameof(SWAPB) or nameof(SWAPW)
        or nameof(TST) or nameof(TSTI) or nameof(XOR) or nameof(XORI) or nameof(XTRCT)
        or nameof(CLRT) or nameof(NOP) or nameof(SETT) or nameof(CLRMAC);

    private static bool IsCompiledReadInstruction(ushort opcode) => CompiledInstructions[Sh2OpcodeTable.Types[opcode]].Name is
        nameof(MOVBL0) or nameof(MOVWL0) or nameof(MOVLL0) or nameof(MOVLL4)
        or nameof(MOVBL) or nameof(MOVWL) or nameof(MOVLL) or nameof(MOVBP) or nameof(MOVWP) or nameof(MOVLP)
        or nameof(MOVWI) or nameof(MOVLI) or nameof(MOVBL4) or nameof(MOVWL4)
        or nameof(MOVBLG) or nameof(MOVWLG) or nameof(MOVLLG) or nameof(TSTM);
    private static bool IsCompiledFallThroughInstruction(ushort opcode) => IsCompiledPureInstruction(opcode)
        || IsCompiledReadInstruction(opcode) || CompiledInstructions[Sh2OpcodeTable.Types[opcode]].Name == nameof(MOVA);
    private static bool IsCompiledDelayedBranch(ushort opcode) => CompiledInstructions[Sh2OpcodeTable.Types[opcode]].Name is
        nameof(BRA) or nameof(BSR) or nameof(BRAF) or nameof(BSRF) or nameof(BTS) or nameof(BFS)
        or nameof(JMP) or nameof(JSR) or nameof(RTS) or nameof(RTE);

    private bool EmitCompiledInstruction(ILGenerator il, ushort opcode)
    {
        int instruction = Sh2OpcodeTable.Types[opcode];
        il.Emit(OpCodes.Ldarg_0);
        switch (instruction)
        {
            case 0: // MOVBS0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[0]); return true;
            case 1: // MOVWS0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[1]); return true;
            case 2: // MOVLS0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[2]); return true;
            case 3: // MULL
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[3]); return false;
            case 4: // MOVBL0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[4]); return false;
            case 5: // MOVWL0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[5]); return false;
            case 6: // MOVLL0
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[6]); return false;
            case 7: // MACL_
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[7]); return false;
            case 8: // MOVLS4
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[8]); return true;
            case 9: // MOVBS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[9]); return true;
            case 10: // MOVWS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[10]); return true;
            case 11: // MOVLS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[11]); return true;
            case 12: // MOVBM
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[12]); return true;
            case 13: // MOVWM
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[13]); return true;
            case 14: // MOVLM
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[14]); return true;
            case 15: // DIV0S
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[15]); return false;
            case 16: // TST
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[16]); return false;
            case 17: // AND
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[17]); return false;
            case 18: // XOR
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[18]); return false;
            case 19: // OR
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[19]); return false;
            case 20: // CMPSTR
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[20]); return false;
            case 21: // XTRCT
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[21]); return false;
            case 22: // MULU
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[22]); return false;
            case 23: // MULS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[23]); return false;
            case 24: // CMPEQ
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[24]); return false;
            case 25: // CMPHS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[25]); return false;
            case 26: // CMPGE
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[26]); return false;
            case 27: // DIV1
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[27]); return false;
            case 28: // DMULU
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[28]); return false;
            case 29: // CMPHI
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[29]); return false;
            case 30: // CMPGT
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[30]); return false;
            case 31: // SUB
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[31]); return false;
            case 32: // SUBC
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[32]); return false;
            case 33: // SUBV
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[33]); return false;
            case 34: // ADD
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[34]); return false;
            case 35: // DMULS
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[35]); return false;
            case 36: // ADDC
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[36]); return false;
            case 37: // ADDV
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[37]); return false;
            case 38: // MACW
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[38]); return false;
            case 39: // MOVLL4
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[39]); return false;
            case 40: // MOVBL
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[40]); return false;
            case 41: // MOVWL
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[41]); return false;
            case 42: // MOVLL
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[42]); return false;
            case 43: // MOV
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[43]); return false;
            case 44: // MOVBP
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[44]); return false;
            case 45: // MOVWP
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[45]); return false;
            case 46: // MOVLP
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[46]); return false;
            case 47: // NOT
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[47]); return false;
            case 48: // SWAPB
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[48]); return false;
            case 49: // SWAPW
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[49]); return false;
            case 50: // NEGC
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[50]); return false;
            case 51: // NEG
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[51]); return false;
            case 52: // EXTUB
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[52]); return false;
            case 53: // EXTUW
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[53]); return false;
            case 54: // EXTSB
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[54]); return false;
            case 55: // EXTSW
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x00f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[55]); return false;
            case 56: // ADDI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0ff);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[56]); return false;
            case 57: // MOVWI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0ff);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[57]); return false;
            case 58: // BRA
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xfff);
                EmitInlinedCall(il, CompiledInstructions[58]); return true;
            case 59: // BSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xfff);
                EmitInlinedCall(il, CompiledInstructions[59]); return true;
            case 60: // MOVLI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0ff);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[60]); return false;
            case 61: // MOVI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0ff);
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0x00f);
                EmitInlinedCall(il, CompiledInstructions[61]); return false;
            case 62: // MOVBS4
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x0f);
                EmitInlinedCall(il, CompiledInstructions[62]); return true;
            case 63: // MOVWS4
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x0f);
                EmitInlinedCall(il, CompiledInstructions[63]); return true;
            case 64: // MOVBL4
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x0f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0f);
                EmitInlinedCall(il, CompiledInstructions[64]); return false;
            case 65: // MOVWL4
                il.Emit(OpCodes.Ldc_I4, opcode >> 4 & 0x0f);
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0x0f);
                EmitInlinedCall(il, CompiledInstructions[65]); return false;
            case 66: // CMPIM
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[66]); return false;
            case 67: // BT
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[67]); return true;
            case 68: // BF
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[68]); return true;
            case 69: // BTS
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[69]); return true;
            case 70: // BFS
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[70]); return true;
            case 71: // MOVBSG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[71]); return true;
            case 72: // MOVWSG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[72]); return true;
            case 73: // MOVLSG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[73]); return true;
            case 74: // TRAPA
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[74]); return true;
            case 75: // MOVBLG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[75]); return false;
            case 76: // MOVWLG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[76]); return false;
            case 77: // MOVLLG
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[77]); return false;
            case 78: // MOVA
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[78]); return false;
            case 79: // TSTI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[79]); return false;
            case 80: // ANDI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[80]); return false;
            case 81: // XORI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[81]); return false;
            case 82: // ORI
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[82]); return false;
            case 83: // TSTM
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[83]); return false;
            case 84: // ANDM
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[84]); return true;
            case 85: // XORM
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[85]); return true;
            case 86: // ORM
                il.Emit(OpCodes.Ldc_I4, opcode >> 0 & 0xff);
                EmitInlinedCall(il, CompiledInstructions[86]); return true;
            case 87: // STCSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[87]); return true;
            case 88: // BSRF
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[88]); return true;
            case 89: // STSMACH
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[89]); return true;
            case 90: // STCGBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[90]); return true;
            case 91: // STSMACL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[91]); return true;
            case 92: // STCVBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[92]); return true;
            case 93: // BRAF
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[93]); return true;
            case 94: // MOVT
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[94]); return false;
            case 95: // STSPR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[95]); return true;
            case 96: // SHLL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[96]); return false;
            case 97: // SHLR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[97]); return false;
            case 98: // STSMMACH
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[98]); return true;
            case 99: // STCMSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[99]); return true;
            case 100: // ROTL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[100]); return false;
            case 101: // ROTR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[101]); return false;
            case 102: // LDSMMACH
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[102]); return true;
            case 103: // LDCMSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[103]); return true;
            case 104: // SHLL2
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[104]); return false;
            case 105: // SHLR2
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[105]); return false;
            case 106: // LDSMACH
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[106]); return true;
            case 107: // JSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[107]); return true;
            case 108: // LDCSR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[108]); return true;
            case 109: // DT
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[109]); return false;
            case 110: // CMPPZ
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[110]); return false;
            case 111: // STSMMACL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[111]); return true;
            case 112: // STCMGBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[112]); return true;
            case 113: // CMPPL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[113]); return false;
            case 114: // LDSMMACL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[114]); return true;
            case 115: // LDCMGBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[115]); return true;
            case 116: // SHLL8
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[116]); return false;
            case 117: // SHLR8
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[117]); return false;
            case 118: // LDSMACL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[118]); return true;
            case 119: // TAS
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[119]); return true;
            case 120: // LDCGBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[120]); return true;
            case 121: // SHAL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[121]); return false;
            case 122: // SHAR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[122]); return false;
            case 123: // STSMPR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[123]); return true;
            case 124: // STCMVBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[124]); return true;
            case 125: // ROTCL
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[125]); return false;
            case 126: // ROTCR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[126]); return false;
            case 127: // LDSMPR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[127]); return true;
            case 128: // LDCMVBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[128]); return true;
            case 129: // SHLL16
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[129]); return false;
            case 130: // SHLR16
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[130]); return false;
            case 131: // LDSPR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[131]); return true;
            case 132: // JMP
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[132]); return true;
            case 133: // LDCVBR
                il.Emit(OpCodes.Ldc_I4, opcode >> 8 & 0xf);
                EmitInlinedCall(il, CompiledInstructions[133]); return true;
            case 134: // CLRT
                EmitInlinedCall(il, CompiledInstructions[134]); return false;
            case 135: // NOP
                EmitInlinedCall(il, CompiledInstructions[135]); return false;
            case 136: // RTS
                EmitInlinedCall(il, CompiledInstructions[136]); return true;
            case 137: // SETT
                EmitInlinedCall(il, CompiledInstructions[137]); return false;
            case 138: // DIV0U
                EmitInlinedCall(il, CompiledInstructions[138]); return false;
            case 139: // SLEEP
                EmitInlinedCall(il, CompiledInstructions[139]); return true;
            case 140: // CLRMAC
                EmitInlinedCall(il, CompiledInstructions[140]); return false;
            case 141: // RTE
                EmitInlinedCall(il, CompiledInstructions[141]); return true;
            case 142: // ILLEGAL
                EmitInlinedCall(il, CompiledInstructions[142]); return true;
            default: throw new InvalidOperationException();
        }
    }
}
