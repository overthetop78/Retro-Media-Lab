// Generated from Dispatch_op in CpuZ80.Opcodes.cs; experimental direct-call targets.
namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
public sealed partial class CpuZ80<TBus> where TBus : class, ICpuZ80Bus
{
    private void DynamicOpcode00() { unchecked {  } }
    private void DynamicOpcode01() { unchecked { BC = ARG16(); } }
    private void DynamicOpcode02() { unchecked { WM( BC, A ); WZ_L = (BC + 1) & 0xFF;  WZ_H = A; } }
    private void DynamicOpcode03() { unchecked { BC++; } }
    private void DynamicOpcode04() { unchecked { B = INC(B); } }
    private void DynamicOpcode05() { unchecked { B = DEC(B); } }
    private void DynamicOpcode06() { unchecked { B = ARG(); } }
    private void DynamicOpcode07() { unchecked { RLCA(); } }
    private void DynamicOpcode08() { unchecked { EX_AF(); } }
    private void DynamicOpcode09() { unchecked { ADD16(ref _state.hl, _state.bc); } }
    private void DynamicOpcode0A() { unchecked { A = RM( BC ); WZ=BC+1; } }
    private void DynamicOpcode0B() { unchecked { BC--; } }
    private void DynamicOpcode0C() { unchecked { C = INC(C); } }
    private void DynamicOpcode0D() { unchecked { C = DEC(C); } }
    private void DynamicOpcode0E() { unchecked { C = ARG(); } }
    private void DynamicOpcode0F() { unchecked { RRCA(); } }
    private void DynamicOpcode10() { unchecked { B--; JR_COND( (B) != 0, 0x10 ); } }
    private void DynamicOpcode11() { unchecked { DE = ARG16(); } }
    private void DynamicOpcode12() { unchecked { WM( DE, A ); WZ_L = (DE + 1) & 0xFF;  WZ_H = A; } }
    private void DynamicOpcode13() { unchecked { DE++; } }
    private void DynamicOpcode14() { unchecked { D = INC(D); } }
    private void DynamicOpcode15() { unchecked { D = DEC(D); } }
    private void DynamicOpcode16() { unchecked { D = ARG(); } }
    private void DynamicOpcode17() { unchecked { RLA(); } }
    private void DynamicOpcode18() { unchecked { JR(); } }
    private void DynamicOpcode19() { unchecked { ADD16(ref _state.hl, _state.de); } }
    private void DynamicOpcode1A() { unchecked { A = RM( DE ); WZ=DE+1; } }
    private void DynamicOpcode1B() { unchecked { DE--; } }
    private void DynamicOpcode1C() { unchecked { E = INC(E); } }
    private void DynamicOpcode1D() { unchecked { E = DEC(E); } }
    private void DynamicOpcode1E() { unchecked { E = ARG(); } }
    private void DynamicOpcode1F() { unchecked { RRA(); } }
    private void DynamicOpcode20() { unchecked { JR_COND( ((F & ZF) == 0), 0x20 ); } }
    private void DynamicOpcode21() { unchecked { HL = ARG16(); } }
    private void DynamicOpcode22() { unchecked { EA = ARG16(); WM16( EA, ref _state.hl ); WZ = EA+1; } }
    private void DynamicOpcode23() { unchecked { HL++; } }
    private void DynamicOpcode24() { unchecked { H = INC(H); } }
    private void DynamicOpcode25() { unchecked { H = DEC(H); } }
    private void DynamicOpcode26() { unchecked { H = ARG(); } }
    private void DynamicOpcode27() { unchecked { DAA(); } }
    private void DynamicOpcode28() { unchecked { JR_COND( (F & ZF) != 0, 0x28 ); } }
    private void DynamicOpcode29() { unchecked { ADD16(ref _state.hl, _state.hl); } }
    private void DynamicOpcode2A() { unchecked { EA = ARG16(); RM16( EA, ref _state.hl ); WZ = EA+1; } }
    private void DynamicOpcode2B() { unchecked { HL--; } }
    private void DynamicOpcode2C() { unchecked { L = INC(L); } }
    private void DynamicOpcode2D() { unchecked { L = DEC(L); } }
    private void DynamicOpcode2E() { unchecked { L = ARG(); } }
    private void DynamicOpcode2F() { unchecked { A ^= 0xff; F = (F&(SF|ZF|PF|CF))|HF|NF|(A&(YF|XF)); } }
    private void DynamicOpcode30() { unchecked { JR_COND( ((F & CF) == 0), 0x30 ); } }
    private void DynamicOpcode31() { unchecked { SP = ARG16(); } }
    private void DynamicOpcode32() { unchecked { EA = ARG16(); WM( EA, A ); WZ_L=(EA+1)&0xFF;WZ_H=A; } }
    private void DynamicOpcode33() { unchecked { SP++; } }
    private void DynamicOpcode34() { unchecked { WM( HL, INC(RM(HL)) ); } }
    private void DynamicOpcode35() { unchecked { WM( HL, DEC(RM(HL)) ); } }
    private void DynamicOpcode36() { unchecked { WM( HL, ARG() ); } }
    private void DynamicOpcode37() { unchecked { F = (F & (SF|ZF|YF|XF|PF)) | CF | (A & (YF|XF)); } }
    private void DynamicOpcode38() { unchecked { JR_COND( (F & CF) != 0, 0x38 ); } }
    private void DynamicOpcode39() { unchecked { ADD16(ref _state.hl, _state.sp); } }
    private void DynamicOpcode3A() { unchecked { EA = ARG16(); A = RM( EA ); WZ = EA+1; } }
    private void DynamicOpcode3B() { unchecked { SP--; } }
    private void DynamicOpcode3C() { unchecked { A = INC(A); } }
    private void DynamicOpcode3D() { unchecked { A = DEC(A); } }
    private void DynamicOpcode3E() { unchecked { A = ARG(); } }
    private void DynamicOpcode3F() { unchecked { F = ((F&(SF|ZF|YF|XF|PF|CF))|((F&CF)<<4)|(A&(YF|XF)))^CF; } }
    private void DynamicOpcode40() { unchecked {  } }
    private void DynamicOpcode41() { unchecked { B = C; } }
    private void DynamicOpcode42() { unchecked { B = D; } }
    private void DynamicOpcode43() { unchecked { B = E; } }
    private void DynamicOpcode44() { unchecked { B = H; } }
    private void DynamicOpcode45() { unchecked { B = L; } }
    private void DynamicOpcode46() { unchecked { B = RM(HL); } }
    private void DynamicOpcode47() { unchecked { B = A; } }
    private void DynamicOpcode48() { unchecked { C = B; } }
    private void DynamicOpcode49() { unchecked {  } }
    private void DynamicOpcode4A() { unchecked { C = D; } }
    private void DynamicOpcode4B() { unchecked { C = E; } }
    private void DynamicOpcode4C() { unchecked { C = H; } }
    private void DynamicOpcode4D() { unchecked { C = L; } }
    private void DynamicOpcode4E() { unchecked { C = RM(HL); } }
    private void DynamicOpcode4F() { unchecked { C = A; } }
    private void DynamicOpcode50() { unchecked { D = B; } }
    private void DynamicOpcode51() { unchecked { D = C; } }
    private void DynamicOpcode52() { unchecked {  } }
    private void DynamicOpcode53() { unchecked { D = E; } }
    private void DynamicOpcode54() { unchecked { D = H; } }
    private void DynamicOpcode55() { unchecked { D = L; } }
    private void DynamicOpcode56() { unchecked { D = RM(HL); } }
    private void DynamicOpcode57() { unchecked { D = A; } }
    private void DynamicOpcode58() { unchecked { E = B; } }
    private void DynamicOpcode59() { unchecked { E = C; } }
    private void DynamicOpcode5A() { unchecked { E = D; } }
    private void DynamicOpcode5B() { unchecked {  } }
    private void DynamicOpcode5C() { unchecked { E = H; } }
    private void DynamicOpcode5D() { unchecked { E = L; } }
    private void DynamicOpcode5E() { unchecked { E = RM(HL); } }
    private void DynamicOpcode5F() { unchecked { E = A; } }
    private void DynamicOpcode60() { unchecked { H = B; } }
    private void DynamicOpcode61() { unchecked { H = C; } }
    private void DynamicOpcode62() { unchecked { H = D; } }
    private void DynamicOpcode63() { unchecked { H = E; } }
    private void DynamicOpcode64() { unchecked {  } }
    private void DynamicOpcode65() { unchecked { H = L; } }
    private void DynamicOpcode66() { unchecked { H = RM(HL); } }
    private void DynamicOpcode67() { unchecked { H = A; } }
    private void DynamicOpcode68() { unchecked { L = B; } }
    private void DynamicOpcode69() { unchecked { L = C; } }
    private void DynamicOpcode6A() { unchecked { L = D; } }
    private void DynamicOpcode6B() { unchecked { L = E; } }
    private void DynamicOpcode6C() { unchecked { L = H; } }
    private void DynamicOpcode6D() { unchecked {  } }
    private void DynamicOpcode6E() { unchecked { L = RM(HL); } }
    private void DynamicOpcode6F() { unchecked { L = A; } }
    private void DynamicOpcode70() { unchecked { WM( HL, B ); } }
    private void DynamicOpcode71() { unchecked { WM( HL, C ); } }
    private void DynamicOpcode72() { unchecked { WM( HL, D ); } }
    private void DynamicOpcode73() { unchecked { WM( HL, E ); } }
    private void DynamicOpcode74() { unchecked { WM( HL, H ); } }
    private void DynamicOpcode75() { unchecked { WM( HL, L ); } }
    private void DynamicOpcode76() { unchecked { ENTER_HALT(); } }
    private void DynamicOpcode77() { unchecked { WM( HL, A ); } }
    private void DynamicOpcode78() { unchecked { A = B; } }
    private void DynamicOpcode79() { unchecked { A = C; } }
    private void DynamicOpcode7A() { unchecked { A = D; } }
    private void DynamicOpcode7B() { unchecked { A = E; } }
    private void DynamicOpcode7C() { unchecked { A = H; } }
    private void DynamicOpcode7D() { unchecked { A = L; } }
    private void DynamicOpcode7E() { unchecked { A = RM(HL); } }
    private void DynamicOpcode7F() { unchecked {  } }
    private void DynamicOpcode80() { unchecked { ADD(B); } }
    private void DynamicOpcode81() { unchecked { ADD(C); } }
    private void DynamicOpcode82() { unchecked { ADD(D); } }
    private void DynamicOpcode83() { unchecked { ADD(E); } }
    private void DynamicOpcode84() { unchecked { ADD(H); } }
    private void DynamicOpcode85() { unchecked { ADD(L); } }
    private void DynamicOpcode86() { unchecked { ADD(RM(HL)); } }
    private void DynamicOpcode87() { unchecked { ADD(A); } }
    private void DynamicOpcode88() { unchecked { ADC(B); } }
    private void DynamicOpcode89() { unchecked { ADC(C); } }
    private void DynamicOpcode8A() { unchecked { ADC(D); } }
    private void DynamicOpcode8B() { unchecked { ADC(E); } }
    private void DynamicOpcode8C() { unchecked { ADC(H); } }
    private void DynamicOpcode8D() { unchecked { ADC(L); } }
    private void DynamicOpcode8E() { unchecked { ADC(RM(HL)); } }
    private void DynamicOpcode8F() { unchecked { ADC(A); } }
    private void DynamicOpcode90() { unchecked { SUB(B); } }
    private void DynamicOpcode91() { unchecked { SUB(C); } }
    private void DynamicOpcode92() { unchecked { SUB(D); } }
    private void DynamicOpcode93() { unchecked { SUB(E); } }
    private void DynamicOpcode94() { unchecked { SUB(H); } }
    private void DynamicOpcode95() { unchecked { SUB(L); } }
    private void DynamicOpcode96() { unchecked { SUB(RM(HL)); } }
    private void DynamicOpcode97() { unchecked { SUB(A); } }
    private void DynamicOpcode98() { unchecked { SBC(B); } }
    private void DynamicOpcode99() { unchecked { SBC(C); } }
    private void DynamicOpcode9A() { unchecked { SBC(D); } }
    private void DynamicOpcode9B() { unchecked { SBC(E); } }
    private void DynamicOpcode9C() { unchecked { SBC(H); } }
    private void DynamicOpcode9D() { unchecked { SBC(L); } }
    private void DynamicOpcode9E() { unchecked { SBC(RM(HL)); } }
    private void DynamicOpcode9F() { unchecked { SBC(A); } }
    private void DynamicOpcodeA0() { unchecked { AND(B); } }
    private void DynamicOpcodeA1() { unchecked { AND(C); } }
    private void DynamicOpcodeA2() { unchecked { AND(D); } }
    private void DynamicOpcodeA3() { unchecked { AND(E); } }
    private void DynamicOpcodeA4() { unchecked { AND(H); } }
    private void DynamicOpcodeA5() { unchecked { AND(L); } }
    private void DynamicOpcodeA6() { unchecked { AND(RM(HL)); } }
    private void DynamicOpcodeA7() { unchecked { AND(A); } }
    private void DynamicOpcodeA8() { unchecked { XOR(B); } }
    private void DynamicOpcodeA9() { unchecked { XOR(C); } }
    private void DynamicOpcodeAA() { unchecked { XOR(D); } }
    private void DynamicOpcodeAB() { unchecked { XOR(E); } }
    private void DynamicOpcodeAC() { unchecked { XOR(H); } }
    private void DynamicOpcodeAD() { unchecked { XOR(L); } }
    private void DynamicOpcodeAE() { unchecked { XOR(RM(HL)); } }
    private void DynamicOpcodeAF() { unchecked { XOR(A); } }
    private void DynamicOpcodeB0() { unchecked { OR(B); } }
    private void DynamicOpcodeB1() { unchecked { OR(C); } }
    private void DynamicOpcodeB2() { unchecked { OR(D); } }
    private void DynamicOpcodeB3() { unchecked { OR(E); } }
    private void DynamicOpcodeB4() { unchecked { OR(H); } }
    private void DynamicOpcodeB5() { unchecked { OR(L); } }
    private void DynamicOpcodeB6() { unchecked { OR(RM(HL)); } }
    private void DynamicOpcodeB7() { unchecked { OR(A); } }
    private void DynamicOpcodeB8() { unchecked { CP(B); } }
    private void DynamicOpcodeB9() { unchecked { CP(C); } }
    private void DynamicOpcodeBA() { unchecked { CP(D); } }
    private void DynamicOpcodeBB() { unchecked { CP(E); } }
    private void DynamicOpcodeBC() { unchecked { CP(H); } }
    private void DynamicOpcodeBD() { unchecked { CP(L); } }
    private void DynamicOpcodeBE() { unchecked { CP(RM(HL)); } }
    private void DynamicOpcodeBF() { unchecked { CP(A); } }
    private void DynamicOpcodeC0() { unchecked { RET_COND( ((F & ZF) == 0), 0xc0 ); } }
    private void DynamicOpcodeC1() { unchecked { POP(ref _state.bc); } }
    private void DynamicOpcodeC2() { unchecked { JP_COND( ((F & ZF) == 0) ); } }
    private void DynamicOpcodeC3() { unchecked { JP(); } }
    private void DynamicOpcodeC4() { unchecked { CALL_COND( ((F & ZF) == 0), 0xc4 ); } }
    private void DynamicOpcodeC5() { unchecked { PUSH(ref _state.bc); } }
    private void DynamicOpcodeC6() { unchecked { ADD(ARG()); } }
    private void DynamicOpcodeC7() { unchecked { RST(0x00); } }
    private void DynamicOpcodeC8() { unchecked { RET_COND( (F & ZF) != 0, 0xc8 ); } }
    private void DynamicOpcodeC9() { unchecked { POP(ref _state.pc); WZ=PCD; } }
    private void DynamicOpcodeCA() { unchecked { JP_COND( (F & ZF) != 0 ); } }
    private void DynamicOpcodeCB() { unchecked { R++; Execute_cb(ROP()); } }
    private void DynamicOpcodeCC() { unchecked { CALL_COND( (F & ZF) != 0, 0xcc ); } }
    private void DynamicOpcodeCD() { unchecked { CALL(); } }
    private void DynamicOpcodeCE() { unchecked { ADC(ARG()); } }
    private void DynamicOpcodeCF() { unchecked { RST(0x08); } }
    private void DynamicOpcodeD0() { unchecked { RET_COND( ((F & CF) == 0), 0xd0 ); } }
    private void DynamicOpcodeD1() { unchecked { POP(ref _state.de); } }
    private void DynamicOpcodeD2() { unchecked { JP_COND( ((F & CF) == 0) ); } }
    private void DynamicOpcodeD3() { unchecked { int n = ARG() | (A << 8); OUT( n, A ); WZ_L = ((n & 0xff) + 1) & 0xff;  WZ_H = A; } }
    private void DynamicOpcodeD4() { unchecked { CALL_COND( ((F & CF) == 0), 0xd4 ); } }
    private void DynamicOpcodeD5() { unchecked { PUSH(ref _state.de); } }
    private void DynamicOpcodeD6() { unchecked { SUB(ARG()); } }
    private void DynamicOpcodeD7() { unchecked { RST(0x10); } }
    private void DynamicOpcodeD8() { unchecked { RET_COND( (F & CF) != 0, 0xd8 ); } }
    private void DynamicOpcodeD9() { unchecked { EXX(); } }
    private void DynamicOpcodeDA() { unchecked { JP_COND( (F & CF) != 0 ); } }
    private void DynamicOpcodeDB() { unchecked { int n = ARG() | (A << 8); A = IN( n ); WZ = n + 1; } }
    private void DynamicOpcodeDC() { unchecked { CALL_COND( (F & CF) != 0, 0xdc ); } }
    private void DynamicOpcodeDD() { unchecked { R++; Execute_dd(ROP()); } }
    private void DynamicOpcodeDE() { unchecked { SBC(ARG()); } }
    private void DynamicOpcodeDF() { unchecked { RST(0x18); } }
    private void DynamicOpcodeE0() { unchecked { RET_COND( ((F & PF) == 0), 0xe0 ); } }
    private void DynamicOpcodeE1() { unchecked { POP(ref _state.hl); } }
    private void DynamicOpcodeE2() { unchecked { JP_COND( ((F & PF) == 0) ); } }
    private void DynamicOpcodeE3() { unchecked { EXSP(ref _state.hl); } }
    private void DynamicOpcodeE4() { unchecked { CALL_COND( ((F & PF) == 0), 0xe4 ); } }
    private void DynamicOpcodeE5() { unchecked { PUSH(ref _state.hl); } }
    private void DynamicOpcodeE6() { unchecked { AND(ARG()); } }
    private void DynamicOpcodeE7() { unchecked { RST(0x20); } }
    private void DynamicOpcodeE8() { unchecked { RET_COND( (F & PF) != 0, 0xe8 ); } }
    private void DynamicOpcodeE9() { unchecked { PC = HL; } }
    private void DynamicOpcodeEA() { unchecked { JP_COND( (F & PF) != 0 ); } }
    private void DynamicOpcodeEB() { unchecked { EX_DE_HL(); } }
    private void DynamicOpcodeEC() { unchecked { CALL_COND( (F & PF) != 0, 0xec ); } }
    private void DynamicOpcodeED() { unchecked { R++; Execute_ed(ROP()); } }
    private void DynamicOpcodeEE() { unchecked { XOR(ARG()); } }
    private void DynamicOpcodeEF() { unchecked { RST(0x28); } }
    private void DynamicOpcodeF0() { unchecked { RET_COND( ((F & SF) == 0), 0xf0 ); } }
    private void DynamicOpcodeF1() { unchecked { POP(ref _state.af); } }
    private void DynamicOpcodeF2() { unchecked { JP_COND( ((F & SF) == 0) ); } }
    private void DynamicOpcodeF3() { unchecked { IFF1 = IFF2 = 0; } }
    private void DynamicOpcodeF4() { unchecked { CALL_COND( ((F & SF) == 0), 0xf4 ); } }
    private void DynamicOpcodeF5() { unchecked { PUSH(ref _state.af); } }
    private void DynamicOpcodeF6() { unchecked { OR(ARG()); } }
    private void DynamicOpcodeF7() { unchecked { RST(0x30); } }
    private void DynamicOpcodeF8() { unchecked { RET_COND( (F & SF) != 0, 0xf8 ); } }
    private void DynamicOpcodeF9() { unchecked { SP = HL; } }
    private void DynamicOpcodeFA() { unchecked { JP_COND((F & SF) != 0); } }
    private void DynamicOpcodeFB() { unchecked { EI(); } }
    private void DynamicOpcodeFC() { unchecked { CALL_COND( (F & SF) != 0, 0xfc ); } }
    private void DynamicOpcodeFD() { unchecked { R++; Execute_fd(ROP()); } }
    private void DynamicOpcodeFE() { unchecked { CP(ARG()); } }
    private void DynamicOpcodeFF() { unchecked { RST(0x38); } }
}
