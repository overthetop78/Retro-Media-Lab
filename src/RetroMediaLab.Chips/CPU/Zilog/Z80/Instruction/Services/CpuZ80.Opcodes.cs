namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public sealed partial class CpuZ80<TBus> where TBus : class, ICpuZ80Bus
{
    private void Execute_op(int opcode) { USE_CYCLES(cc_op[opcode]); Dispatch_op(opcode); }
    private void Dispatch_op(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: {  break; }
                case 0x01: { BC = ARG16(); break; }
                case 0x02: { WM( BC, A ); WZ_L = (BC + 1) & 0xFF;  WZ_H = A; break; }
                case 0x03: { BC++; break; }
                case 0x04: { B = INC(B); break; }
                case 0x05: { B = DEC(B); break; }
                case 0x06: { B = ARG(); break; }
                case 0x07: { RLCA(); break; }
                case 0x08: { EX_AF(); break; }
                case 0x09: { ADD16(ref _state.hl, _state.bc); break; }
                case 0x0a: { A = RM( BC ); WZ=BC+1; break; }
                case 0x0b: { BC--; break; }
                case 0x0c: { C = INC(C); break; }
                case 0x0d: { C = DEC(C); break; }
                case 0x0e: { C = ARG(); break; }
                case 0x0f: { RRCA(); break; }
                case 0x10: { B--; JR_COND( (B) != 0, 0x10 ); break; }
                case 0x11: { DE = ARG16(); break; }
                case 0x12: { WM( DE, A ); WZ_L = (DE + 1) & 0xFF;  WZ_H = A; break; }
                case 0x13: { DE++; break; }
                case 0x14: { D = INC(D); break; }
                case 0x15: { D = DEC(D); break; }
                case 0x16: { D = ARG(); break; }
                case 0x17: { RLA(); break; }
                case 0x18: { JR(); break; }
                case 0x19: { ADD16(ref _state.hl, _state.de); break; }
                case 0x1a: { A = RM( DE ); WZ=DE+1; break; }
                case 0x1b: { DE--; break; }
                case 0x1c: { E = INC(E); break; }
                case 0x1d: { E = DEC(E); break; }
                case 0x1e: { E = ARG(); break; }
                case 0x1f: { RRA(); break; }
                case 0x20: { JR_COND( ((F & ZF) == 0), 0x20 ); break; }
                case 0x21: { HL = ARG16(); break; }
                case 0x22: { EA = ARG16(); WM16( EA, ref _state.hl ); WZ = EA+1; break; }
                case 0x23: { HL++; break; }
                case 0x24: { H = INC(H); break; }
                case 0x25: { H = DEC(H); break; }
                case 0x26: { H = ARG(); break; }
                case 0x27: { DAA(); break; }
                case 0x28: { JR_COND( (F & ZF) != 0, 0x28 ); break; }
                case 0x29: { ADD16(ref _state.hl, _state.hl); break; }
                case 0x2a: { EA = ARG16(); RM16( EA, ref _state.hl ); WZ = EA+1; break; }
                case 0x2b: { HL--; break; }
                case 0x2c: { L = INC(L); break; }
                case 0x2d: { L = DEC(L); break; }
                case 0x2e: { L = ARG(); break; }
                case 0x2f: { A ^= 0xff; F = (F&(SF|ZF|PF|CF))|HF|NF|(A&(YF|XF)); break; }
                case 0x30: { JR_COND( ((F & CF) == 0), 0x30 ); break; }
                case 0x31: { SP = ARG16(); break; }
                case 0x32: { EA = ARG16(); WM( EA, A ); WZ_L=(EA+1)&0xFF;WZ_H=A; break; }
                case 0x33: { SP++; break; }
                case 0x34: { WM( HL, INC(RM(HL)) ); break; }
                case 0x35: { WM( HL, DEC(RM(HL)) ); break; }
                case 0x36: { WM( HL, ARG() ); break; }
                case 0x37: { F = (F & (SF|ZF|YF|XF|PF)) | CF | (A & (YF|XF)); break; }
                case 0x38: { JR_COND( (F & CF) != 0, 0x38 ); break; }
                case 0x39: { ADD16(ref _state.hl, _state.sp); break; }
                case 0x3a: { EA = ARG16(); A = RM( EA ); WZ = EA+1; break; }
                case 0x3b: { SP--; break; }
                case 0x3c: { A = INC(A); break; }
                case 0x3d: { A = DEC(A); break; }
                case 0x3e: { A = ARG(); break; }
                case 0x3f: { F = ((F&(SF|ZF|YF|XF|PF|CF))|((F&CF)<<4)|(A&(YF|XF)))^CF; break; }
                case 0x40: {  break; }
                case 0x41: { B = C; break; }
                case 0x42: { B = D; break; }
                case 0x43: { B = E; break; }
                case 0x44: { B = H; break; }
                case 0x45: { B = L; break; }
                case 0x46: { B = RM(HL); break; }
                case 0x47: { B = A; break; }
                case 0x48: { C = B; break; }
                case 0x49: {  break; }
                case 0x4a: { C = D; break; }
                case 0x4b: { C = E; break; }
                case 0x4c: { C = H; break; }
                case 0x4d: { C = L; break; }
                case 0x4e: { C = RM(HL); break; }
                case 0x4f: { C = A; break; }
                case 0x50: { D = B; break; }
                case 0x51: { D = C; break; }
                case 0x52: {  break; }
                case 0x53: { D = E; break; }
                case 0x54: { D = H; break; }
                case 0x55: { D = L; break; }
                case 0x56: { D = RM(HL); break; }
                case 0x57: { D = A; break; }
                case 0x58: { E = B; break; }
                case 0x59: { E = C; break; }
                case 0x5a: { E = D; break; }
                case 0x5b: {  break; }
                case 0x5c: { E = H; break; }
                case 0x5d: { E = L; break; }
                case 0x5e: { E = RM(HL); break; }
                case 0x5f: { E = A; break; }
                case 0x60: { H = B; break; }
                case 0x61: { H = C; break; }
                case 0x62: { H = D; break; }
                case 0x63: { H = E; break; }
                case 0x64: {  break; }
                case 0x65: { H = L; break; }
                case 0x66: { H = RM(HL); break; }
                case 0x67: { H = A; break; }
                case 0x68: { L = B; break; }
                case 0x69: { L = C; break; }
                case 0x6a: { L = D; break; }
                case 0x6b: { L = E; break; }
                case 0x6c: { L = H; break; }
                case 0x6d: {  break; }
                case 0x6e: { L = RM(HL); break; }
                case 0x6f: { L = A; break; }
                case 0x70: { WM( HL, B ); break; }
                case 0x71: { WM( HL, C ); break; }
                case 0x72: { WM( HL, D ); break; }
                case 0x73: { WM( HL, E ); break; }
                case 0x74: { WM( HL, H ); break; }
                case 0x75: { WM( HL, L ); break; }
                case 0x76: { ENTER_HALT(); break; }
                case 0x77: { WM( HL, A ); break; }
                case 0x78: { A = B; break; }
                case 0x79: { A = C; break; }
                case 0x7a: { A = D; break; }
                case 0x7b: { A = E; break; }
                case 0x7c: { A = H; break; }
                case 0x7d: { A = L; break; }
                case 0x7e: { A = RM(HL); break; }
                case 0x7f: {  break; }
                case 0x80: { ADD(B); break; }
                case 0x81: { ADD(C); break; }
                case 0x82: { ADD(D); break; }
                case 0x83: { ADD(E); break; }
                case 0x84: { ADD(H); break; }
                case 0x85: { ADD(L); break; }
                case 0x86: { ADD(RM(HL)); break; }
                case 0x87: { ADD(A); break; }
                case 0x88: { ADC(B); break; }
                case 0x89: { ADC(C); break; }
                case 0x8a: { ADC(D); break; }
                case 0x8b: { ADC(E); break; }
                case 0x8c: { ADC(H); break; }
                case 0x8d: { ADC(L); break; }
                case 0x8e: { ADC(RM(HL)); break; }
                case 0x8f: { ADC(A); break; }
                case 0x90: { SUB(B); break; }
                case 0x91: { SUB(C); break; }
                case 0x92: { SUB(D); break; }
                case 0x93: { SUB(E); break; }
                case 0x94: { SUB(H); break; }
                case 0x95: { SUB(L); break; }
                case 0x96: { SUB(RM(HL)); break; }
                case 0x97: { SUB(A); break; }
                case 0x98: { SBC(B); break; }
                case 0x99: { SBC(C); break; }
                case 0x9a: { SBC(D); break; }
                case 0x9b: { SBC(E); break; }
                case 0x9c: { SBC(H); break; }
                case 0x9d: { SBC(L); break; }
                case 0x9e: { SBC(RM(HL)); break; }
                case 0x9f: { SBC(A); break; }
                case 0xa0: { AND(B); break; }
                case 0xa1: { AND(C); break; }
                case 0xa2: { AND(D); break; }
                case 0xa3: { AND(E); break; }
                case 0xa4: { AND(H); break; }
                case 0xa5: { AND(L); break; }
                case 0xa6: { AND(RM(HL)); break; }
                case 0xa7: { AND(A); break; }
                case 0xa8: { XOR(B); break; }
                case 0xa9: { XOR(C); break; }
                case 0xaa: { XOR(D); break; }
                case 0xab: { XOR(E); break; }
                case 0xac: { XOR(H); break; }
                case 0xad: { XOR(L); break; }
                case 0xae: { XOR(RM(HL)); break; }
                case 0xaf: { XOR(A); break; }
                case 0xb0: { OR(B); break; }
                case 0xb1: { OR(C); break; }
                case 0xb2: { OR(D); break; }
                case 0xb3: { OR(E); break; }
                case 0xb4: { OR(H); break; }
                case 0xb5: { OR(L); break; }
                case 0xb6: { OR(RM(HL)); break; }
                case 0xb7: { OR(A); break; }
                case 0xb8: { CP(B); break; }
                case 0xb9: { CP(C); break; }
                case 0xba: { CP(D); break; }
                case 0xbb: { CP(E); break; }
                case 0xbc: { CP(H); break; }
                case 0xbd: { CP(L); break; }
                case 0xbe: { CP(RM(HL)); break; }
                case 0xbf: { CP(A); break; }
                case 0xc0: { RET_COND( ((F & ZF) == 0), 0xc0 ); break; }
                case 0xc1: { POP(ref _state.bc); break; }
                case 0xc2: { JP_COND( ((F & ZF) == 0) ); break; }
                case 0xc3: { JP(); break; }
                case 0xc4: { CALL_COND( ((F & ZF) == 0), 0xc4 ); break; }
                case 0xc5: { PUSH(ref _state.bc); break; }
                case 0xc6: { ADD(ARG()); break; }
                case 0xc7: { RST(0x00); break; }
                case 0xc8: { RET_COND( (F & ZF) != 0, 0xc8 ); break; }
                case 0xc9: { POP(ref _state.pc); WZ=PCD; break; }
                case 0xca: { JP_COND( (F & ZF) != 0 ); break; }
                case 0xcb: { R++; Execute_cb(ROP()); break; }
                case 0xcc: { CALL_COND( (F & ZF) != 0, 0xcc ); break; }
                case 0xcd: { CALL(); break; }
                case 0xce: { ADC(ARG()); break; }
                case 0xcf: { RST(0x08); break; }
                case 0xd0: { RET_COND( ((F & CF) == 0), 0xd0 ); break; }
                case 0xd1: { POP(ref _state.de); break; }
                case 0xd2: { JP_COND( ((F & CF) == 0) ); break; }
                case 0xd3: { int n = ARG() | (A << 8); OUT( n, A ); WZ_L = ((n & 0xff) + 1) & 0xff;  WZ_H = A; break; }
                case 0xd4: { CALL_COND( ((F & CF) == 0), 0xd4 ); break; }
                case 0xd5: { PUSH(ref _state.de); break; }
                case 0xd6: { SUB(ARG()); break; }
                case 0xd7: { RST(0x10); break; }
                case 0xd8: { RET_COND( (F & CF) != 0, 0xd8 ); break; }
                case 0xd9: { EXX(); break; }
                case 0xda: { JP_COND( (F & CF) != 0 ); break; }
                case 0xdb: { int n = ARG() | (A << 8); A = IN( n ); WZ = n + 1; break; }
                case 0xdc: { CALL_COND( (F & CF) != 0, 0xdc ); break; }
                case 0xdd: { R++; Execute_dd(ROP()); break; }
                case 0xde: { SBC(ARG()); break; }
                case 0xdf: { RST(0x18); break; }
                case 0xe0: { RET_COND( ((F & PF) == 0), 0xe0 ); break; }
                case 0xe1: { POP(ref _state.hl); break; }
                case 0xe2: { JP_COND( ((F & PF) == 0) ); break; }
                case 0xe3: { EXSP(ref _state.hl); break; }
                case 0xe4: { CALL_COND( ((F & PF) == 0), 0xe4 ); break; }
                case 0xe5: { PUSH(ref _state.hl); break; }
                case 0xe6: { AND(ARG()); break; }
                case 0xe7: { RST(0x20); break; }
                case 0xe8: { RET_COND( (F & PF) != 0, 0xe8 ); break; }
                case 0xe9: { PC = HL; break; }
                case 0xea: { JP_COND( (F & PF) != 0 ); break; }
                case 0xeb: { EX_DE_HL(); break; }
                case 0xec: { CALL_COND( (F & PF) != 0, 0xec ); break; }
                case 0xed: { R++; Execute_ed(ROP()); break; }
                case 0xee: { XOR(ARG()); break; }
                case 0xef: { RST(0x28); break; }
                case 0xf0: { RET_COND( ((F & SF) == 0), 0xf0 ); break; }
                case 0xf1: { POP(ref _state.af); break; }
                case 0xf2: { JP_COND( ((F & SF) == 0) ); break; }
                case 0xf3: { IFF1 = IFF2 = 0; break; }
                case 0xf4: { CALL_COND( ((F & SF) == 0), 0xf4 ); break; }
                case 0xf5: { PUSH(ref _state.af); break; }
                case 0xf6: { OR(ARG()); break; }
                case 0xf7: { RST(0x30); break; }
                case 0xf8: { RET_COND( (F & SF) != 0, 0xf8 ); break; }
                case 0xf9: { SP = HL; break; }
                case 0xfa: { JP_COND((F & SF) != 0); break; }
                case 0xfb: { EI(); break; }
                case 0xfc: { CALL_COND( (F & SF) != 0, 0xfc ); break; }
                case 0xfd: { R++; Execute_fd(ROP()); break; }
                case 0xfe: { CP(ARG()); break; }
                case 0xff: { RST(0x38); break; }
            }
        }
    }
    private void Execute_cb(int opcode) { USE_CYCLES(cc_cb[opcode]); Dispatch_cb(opcode); }
    private void Dispatch_cb(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: { B = RLC(B); break; }
                case 0x01: { C = RLC(C); break; }
                case 0x02: { D = RLC(D); break; }
                case 0x03: { E = RLC(E); break; }
                case 0x04: { H = RLC(H); break; }
                case 0x05: { L = RLC(L); break; }
                case 0x06: { WM( HL, RLC(RM(HL)) ); break; }
                case 0x07: { A = RLC(A); break; }
                case 0x08: { B = RRC(B); break; }
                case 0x09: { C = RRC(C); break; }
                case 0x0a: { D = RRC(D); break; }
                case 0x0b: { E = RRC(E); break; }
                case 0x0c: { H = RRC(H); break; }
                case 0x0d: { L = RRC(L); break; }
                case 0x0e: { WM( HL, RRC(RM(HL)) ); break; }
                case 0x0f: { A = RRC(A); break; }
                case 0x10: { B = RL(B); break; }
                case 0x11: { C = RL(C); break; }
                case 0x12: { D = RL(D); break; }
                case 0x13: { E = RL(E); break; }
                case 0x14: { H = RL(H); break; }
                case 0x15: { L = RL(L); break; }
                case 0x16: { WM( HL, RL(RM(HL)) ); break; }
                case 0x17: { A = RL(A); break; }
                case 0x18: { B = RR(B); break; }
                case 0x19: { C = RR(C); break; }
                case 0x1a: { D = RR(D); break; }
                case 0x1b: { E = RR(E); break; }
                case 0x1c: { H = RR(H); break; }
                case 0x1d: { L = RR(L); break; }
                case 0x1e: { WM( HL, RR(RM(HL)) ); break; }
                case 0x1f: { A = RR(A); break; }
                case 0x20: { B = SLA(B); break; }
                case 0x21: { C = SLA(C); break; }
                case 0x22: { D = SLA(D); break; }
                case 0x23: { E = SLA(E); break; }
                case 0x24: { H = SLA(H); break; }
                case 0x25: { L = SLA(L); break; }
                case 0x26: { WM( HL, SLA(RM(HL)) ); break; }
                case 0x27: { A = SLA(A); break; }
                case 0x28: { B = SRA(B); break; }
                case 0x29: { C = SRA(C); break; }
                case 0x2a: { D = SRA(D); break; }
                case 0x2b: { E = SRA(E); break; }
                case 0x2c: { H = SRA(H); break; }
                case 0x2d: { L = SRA(L); break; }
                case 0x2e: { WM( HL, SRA(RM(HL)) ); break; }
                case 0x2f: { A = SRA(A); break; }
                case 0x30: { B = SLL(B); break; }
                case 0x31: { C = SLL(C); break; }
                case 0x32: { D = SLL(D); break; }
                case 0x33: { E = SLL(E); break; }
                case 0x34: { H = SLL(H); break; }
                case 0x35: { L = SLL(L); break; }
                case 0x36: { WM( HL, SLL(RM(HL)) ); break; }
                case 0x37: { A = SLL(A); break; }
                case 0x38: { B = SRL(B); break; }
                case 0x39: { C = SRL(C); break; }
                case 0x3a: { D = SRL(D); break; }
                case 0x3b: { E = SRL(E); break; }
                case 0x3c: { H = SRL(H); break; }
                case 0x3d: { L = SRL(L); break; }
                case 0x3e: { WM( HL, SRL(RM(HL)) ); break; }
                case 0x3f: { A = SRL(A); break; }
                case 0x40: { BIT(0,B); break; }
                case 0x41: { BIT(0,C); break; }
                case 0x42: { BIT(0,D); break; }
                case 0x43: { BIT(0,E); break; }
                case 0x44: { BIT(0,H); break; }
                case 0x45: { BIT(0,L); break; }
                case 0x46: { BIT_HL(0,RM(HL)); break; }
                case 0x47: { BIT(0,A); break; }
                case 0x48: { BIT(1,B); break; }
                case 0x49: { BIT(1,C); break; }
                case 0x4a: { BIT(1,D); break; }
                case 0x4b: { BIT(1,E); break; }
                case 0x4c: { BIT(1,H); break; }
                case 0x4d: { BIT(1,L); break; }
                case 0x4e: { BIT_HL(1,RM(HL)); break; }
                case 0x4f: { BIT(1,A); break; }
                case 0x50: { BIT(2,B); break; }
                case 0x51: { BIT(2,C); break; }
                case 0x52: { BIT(2,D); break; }
                case 0x53: { BIT(2,E); break; }
                case 0x54: { BIT(2,H); break; }
                case 0x55: { BIT(2,L); break; }
                case 0x56: { BIT_HL(2,RM(HL)); break; }
                case 0x57: { BIT(2,A); break; }
                case 0x58: { BIT(3,B); break; }
                case 0x59: { BIT(3,C); break; }
                case 0x5a: { BIT(3,D); break; }
                case 0x5b: { BIT(3,E); break; }
                case 0x5c: { BIT(3,H); break; }
                case 0x5d: { BIT(3,L); break; }
                case 0x5e: { BIT_HL(3,RM(HL)); break; }
                case 0x5f: { BIT(3,A); break; }
                case 0x60: { BIT(4,B); break; }
                case 0x61: { BIT(4,C); break; }
                case 0x62: { BIT(4,D); break; }
                case 0x63: { BIT(4,E); break; }
                case 0x64: { BIT(4,H); break; }
                case 0x65: { BIT(4,L); break; }
                case 0x66: { BIT_HL(4,RM(HL)); break; }
                case 0x67: { BIT(4,A); break; }
                case 0x68: { BIT(5,B); break; }
                case 0x69: { BIT(5,C); break; }
                case 0x6a: { BIT(5,D); break; }
                case 0x6b: { BIT(5,E); break; }
                case 0x6c: { BIT(5,H); break; }
                case 0x6d: { BIT(5,L); break; }
                case 0x6e: { BIT_HL(5,RM(HL)); break; }
                case 0x6f: { BIT(5,A); break; }
                case 0x70: { BIT(6,B); break; }
                case 0x71: { BIT(6,C); break; }
                case 0x72: { BIT(6,D); break; }
                case 0x73: { BIT(6,E); break; }
                case 0x74: { BIT(6,H); break; }
                case 0x75: { BIT(6,L); break; }
                case 0x76: { BIT_HL(6,RM(HL)); break; }
                case 0x77: { BIT(6,A); break; }
                case 0x78: { BIT(7,B); break; }
                case 0x79: { BIT(7,C); break; }
                case 0x7a: { BIT(7,D); break; }
                case 0x7b: { BIT(7,E); break; }
                case 0x7c: { BIT(7,H); break; }
                case 0x7d: { BIT(7,L); break; }
                case 0x7e: { BIT_HL(7,RM(HL)); break; }
                case 0x7f: { BIT(7,A); break; }
                case 0x80: { B = RES(0,B); break; }
                case 0x81: { C = RES(0,C); break; }
                case 0x82: { D = RES(0,D); break; }
                case 0x83: { E = RES(0,E); break; }
                case 0x84: { H = RES(0,H); break; }
                case 0x85: { L = RES(0,L); break; }
                case 0x86: { WM( HL, RES(0,RM(HL)) ); break; }
                case 0x87: { A = RES(0,A); break; }
                case 0x88: { B = RES(1,B); break; }
                case 0x89: { C = RES(1,C); break; }
                case 0x8a: { D = RES(1,D); break; }
                case 0x8b: { E = RES(1,E); break; }
                case 0x8c: { H = RES(1,H); break; }
                case 0x8d: { L = RES(1,L); break; }
                case 0x8e: { WM( HL, RES(1,RM(HL)) ); break; }
                case 0x8f: { A = RES(1,A); break; }
                case 0x90: { B = RES(2,B); break; }
                case 0x91: { C = RES(2,C); break; }
                case 0x92: { D = RES(2,D); break; }
                case 0x93: { E = RES(2,E); break; }
                case 0x94: { H = RES(2,H); break; }
                case 0x95: { L = RES(2,L); break; }
                case 0x96: { WM( HL, RES(2,RM(HL)) ); break; }
                case 0x97: { A = RES(2,A); break; }
                case 0x98: { B = RES(3,B); break; }
                case 0x99: { C = RES(3,C); break; }
                case 0x9a: { D = RES(3,D); break; }
                case 0x9b: { E = RES(3,E); break; }
                case 0x9c: { H = RES(3,H); break; }
                case 0x9d: { L = RES(3,L); break; }
                case 0x9e: { WM( HL, RES(3,RM(HL)) ); break; }
                case 0x9f: { A = RES(3,A); break; }
                case 0xa0: { B = RES(4,B); break; }
                case 0xa1: { C = RES(4,C); break; }
                case 0xa2: { D = RES(4,D); break; }
                case 0xa3: { E = RES(4,E); break; }
                case 0xa4: { H = RES(4,H); break; }
                case 0xa5: { L = RES(4,L); break; }
                case 0xa6: { WM( HL, RES(4,RM(HL)) ); break; }
                case 0xa7: { A = RES(4,A); break; }
                case 0xa8: { B = RES(5,B); break; }
                case 0xa9: { C = RES(5,C); break; }
                case 0xaa: { D = RES(5,D); break; }
                case 0xab: { E = RES(5,E); break; }
                case 0xac: { H = RES(5,H); break; }
                case 0xad: { L = RES(5,L); break; }
                case 0xae: { WM( HL, RES(5,RM(HL)) ); break; }
                case 0xaf: { A = RES(5,A); break; }
                case 0xb0: { B = RES(6,B); break; }
                case 0xb1: { C = RES(6,C); break; }
                case 0xb2: { D = RES(6,D); break; }
                case 0xb3: { E = RES(6,E); break; }
                case 0xb4: { H = RES(6,H); break; }
                case 0xb5: { L = RES(6,L); break; }
                case 0xb6: { WM( HL, RES(6,RM(HL)) ); break; }
                case 0xb7: { A = RES(6,A); break; }
                case 0xb8: { B = RES(7,B); break; }
                case 0xb9: { C = RES(7,C); break; }
                case 0xba: { D = RES(7,D); break; }
                case 0xbb: { E = RES(7,E); break; }
                case 0xbc: { H = RES(7,H); break; }
                case 0xbd: { L = RES(7,L); break; }
                case 0xbe: { WM( HL, RES(7,RM(HL)) ); break; }
                case 0xbf: { A = RES(7,A); break; }
                case 0xc0: { B = SET(0,B); break; }
                case 0xc1: { C = SET(0,C); break; }
                case 0xc2: { D = SET(0,D); break; }
                case 0xc3: { E = SET(0,E); break; }
                case 0xc4: { H = SET(0,H); break; }
                case 0xc5: { L = SET(0,L); break; }
                case 0xc6: { WM( HL, SET(0,RM(HL)) ); break; }
                case 0xc7: { A = SET(0,A); break; }
                case 0xc8: { B = SET(1,B); break; }
                case 0xc9: { C = SET(1,C); break; }
                case 0xca: { D = SET(1,D); break; }
                case 0xcb: { E = SET(1,E); break; }
                case 0xcc: { H = SET(1,H); break; }
                case 0xcd: { L = SET(1,L); break; }
                case 0xce: { WM( HL, SET(1,RM(HL)) ); break; }
                case 0xcf: { A = SET(1,A); break; }
                case 0xd0: { B = SET(2,B); break; }
                case 0xd1: { C = SET(2,C); break; }
                case 0xd2: { D = SET(2,D); break; }
                case 0xd3: { E = SET(2,E); break; }
                case 0xd4: { H = SET(2,H); break; }
                case 0xd5: { L = SET(2,L); break; }
                case 0xd6: { WM( HL, SET(2,RM(HL)) ); break; }
                case 0xd7: { A = SET(2,A); break; }
                case 0xd8: { B = SET(3,B); break; }
                case 0xd9: { C = SET(3,C); break; }
                case 0xda: { D = SET(3,D); break; }
                case 0xdb: { E = SET(3,E); break; }
                case 0xdc: { H = SET(3,H); break; }
                case 0xdd: { L = SET(3,L); break; }
                case 0xde: { WM( HL, SET(3,RM(HL)) ); break; }
                case 0xdf: { A = SET(3,A); break; }
                case 0xe0: { B = SET(4,B); break; }
                case 0xe1: { C = SET(4,C); break; }
                case 0xe2: { D = SET(4,D); break; }
                case 0xe3: { E = SET(4,E); break; }
                case 0xe4: { H = SET(4,H); break; }
                case 0xe5: { L = SET(4,L); break; }
                case 0xe6: { WM( HL, SET(4,RM(HL)) ); break; }
                case 0xe7: { A = SET(4,A); break; }
                case 0xe8: { B = SET(5,B); break; }
                case 0xe9: { C = SET(5,C); break; }
                case 0xea: { D = SET(5,D); break; }
                case 0xeb: { E = SET(5,E); break; }
                case 0xec: { H = SET(5,H); break; }
                case 0xed: { L = SET(5,L); break; }
                case 0xee: { WM( HL, SET(5,RM(HL)) ); break; }
                case 0xef: { A = SET(5,A); break; }
                case 0xf0: { B = SET(6,B); break; }
                case 0xf1: { C = SET(6,C); break; }
                case 0xf2: { D = SET(6,D); break; }
                case 0xf3: { E = SET(6,E); break; }
                case 0xf4: { H = SET(6,H); break; }
                case 0xf5: { L = SET(6,L); break; }
                case 0xf6: { WM( HL, SET(6,RM(HL)) ); break; }
                case 0xf7: { A = SET(6,A); break; }
                case 0xf8: { B = SET(7,B); break; }
                case 0xf9: { C = SET(7,C); break; }
                case 0xfa: { D = SET(7,D); break; }
                case 0xfb: { E = SET(7,E); break; }
                case 0xfc: { H = SET(7,H); break; }
                case 0xfd: { L = SET(7,L); break; }
                case 0xfe: { WM( HL, SET(7,RM(HL)) ); break; }
                case 0xff: { A = SET(7,A); break; }
            }
        }
    }
    private void Execute_dd(int opcode) { USE_CYCLES(cc_xy[opcode]); Dispatch_dd(opcode); }
    private void Dispatch_dd(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: { Dispatch_op(0x00); break; }
                case 0x01: { Dispatch_op(0x01); break; }
                case 0x02: { Dispatch_op(0x02); break; }
                case 0x03: { Dispatch_op(0x03); break; }
                case 0x04: { Dispatch_op(0x04); break; }
                case 0x05: { Dispatch_op(0x05); break; }
                case 0x06: { Dispatch_op(0x06); break; }
                case 0x07: { Dispatch_op(0x07); break; }
                case 0x08: { Dispatch_op(0x08); break; }
                case 0x09: { ADD16(ref _state.ix, _state.bc); break; }
                case 0x0a: { Dispatch_op(0x0a); break; }
                case 0x0b: { Dispatch_op(0x0b); break; }
                case 0x0c: { Dispatch_op(0x0c); break; }
                case 0x0d: { Dispatch_op(0x0d); break; }
                case 0x0e: { Dispatch_op(0x0e); break; }
                case 0x0f: { Dispatch_op(0x0f); break; }
                case 0x10: { Dispatch_op(0x10); break; }
                case 0x11: { Dispatch_op(0x11); break; }
                case 0x12: { Dispatch_op(0x12); break; }
                case 0x13: { Dispatch_op(0x13); break; }
                case 0x14: { Dispatch_op(0x14); break; }
                case 0x15: { Dispatch_op(0x15); break; }
                case 0x16: { Dispatch_op(0x16); break; }
                case 0x17: { Dispatch_op(0x17); break; }
                case 0x18: { Dispatch_op(0x18); break; }
                case 0x19: { ADD16(ref _state.ix, _state.de); break; }
                case 0x1a: { Dispatch_op(0x1a); break; }
                case 0x1b: { Dispatch_op(0x1b); break; }
                case 0x1c: { Dispatch_op(0x1c); break; }
                case 0x1d: { Dispatch_op(0x1d); break; }
                case 0x1e: { Dispatch_op(0x1e); break; }
                case 0x1f: { Dispatch_op(0x1f); break; }
                case 0x20: { Dispatch_op(0x20); break; }
                case 0x21: { IX = ARG16(); break; }
                case 0x22: { EA = ARG16(); WM16( EA, ref _state.ix ); WZ = EA+1; break; }
                case 0x23: { IX++; break; }
                case 0x24: { HX = INC(HX); break; }
                case 0x25: { HX = DEC(HX); break; }
                case 0x26: { HX = ARG(); break; }
                case 0x27: { Dispatch_op(0x27); break; }
                case 0x28: { Dispatch_op(0x28); break; }
                case 0x29: { ADD16(ref _state.ix, _state.ix); break; }
                case 0x2a: { EA = ARG16(); RM16( EA, ref _state.ix ); WZ = EA+1; break; }
                case 0x2b: { IX--; break; }
                case 0x2c: { LX = INC(LX); break; }
                case 0x2d: { LX = DEC(LX); break; }
                case 0x2e: { LX = ARG(); break; }
                case 0x2f: { Dispatch_op(0x2f); break; }
                case 0x30: { Dispatch_op(0x30); break; }
                case 0x31: { Dispatch_op(0x31); break; }
                case 0x32: { Dispatch_op(0x32); break; }
                case 0x33: { Dispatch_op(0x33); break; }
                case 0x34: { EAX(); WM( EA, INC(RM(EA)) ); break; }
                case 0x35: { EAX(); WM( EA, DEC(RM(EA)) ); break; }
                case 0x36: { EAX(); WM( EA, ARG() ); break; }
                case 0x37: { Dispatch_op(0x37); break; }
                case 0x38: { Dispatch_op(0x38); break; }
                case 0x39: { ADD16(ref _state.ix, _state.sp); break; }
                case 0x3a: { Dispatch_op(0x3a); break; }
                case 0x3b: { Dispatch_op(0x3b); break; }
                case 0x3c: { Dispatch_op(0x3c); break; }
                case 0x3d: { Dispatch_op(0x3d); break; }
                case 0x3e: { Dispatch_op(0x3e); break; }
                case 0x3f: { Dispatch_op(0x3f); break; }
                case 0x40: { Dispatch_op(0x40); break; }
                case 0x41: { Dispatch_op(0x41); break; }
                case 0x42: { Dispatch_op(0x42); break; }
                case 0x43: { Dispatch_op(0x43); break; }
                case 0x44: { B = HX; break; }
                case 0x45: { B = LX; break; }
                case 0x46: { EAX(); B = RM(EA); break; }
                case 0x47: { Dispatch_op(0x47); break; }
                case 0x48: { Dispatch_op(0x48); break; }
                case 0x49: { Dispatch_op(0x49); break; }
                case 0x4a: { Dispatch_op(0x4a); break; }
                case 0x4b: { Dispatch_op(0x4b); break; }
                case 0x4c: { C = HX; break; }
                case 0x4d: { C = LX; break; }
                case 0x4e: { EAX(); C = RM(EA); break; }
                case 0x4f: { Dispatch_op(0x4f); break; }
                case 0x50: { Dispatch_op(0x50); break; }
                case 0x51: { Dispatch_op(0x51); break; }
                case 0x52: { Dispatch_op(0x52); break; }
                case 0x53: { Dispatch_op(0x53); break; }
                case 0x54: { D = HX; break; }
                case 0x55: { D = LX; break; }
                case 0x56: { EAX(); D = RM(EA); break; }
                case 0x57: { Dispatch_op(0x57); break; }
                case 0x58: { Dispatch_op(0x58); break; }
                case 0x59: { Dispatch_op(0x59); break; }
                case 0x5a: { Dispatch_op(0x5a); break; }
                case 0x5b: { Dispatch_op(0x5b); break; }
                case 0x5c: { E = HX; break; }
                case 0x5d: { E = LX; break; }
                case 0x5e: { EAX(); E = RM(EA); break; }
                case 0x5f: { Dispatch_op(0x5f); break; }
                case 0x60: { HX = B; break; }
                case 0x61: { HX = C; break; }
                case 0x62: { HX = D; break; }
                case 0x63: { HX = E; break; }
                case 0x64: {  break; }
                case 0x65: { HX = LX; break; }
                case 0x66: { EAX(); H = RM(EA); break; }
                case 0x67: { HX = A; break; }
                case 0x68: { LX = B; break; }
                case 0x69: { LX = C; break; }
                case 0x6a: { LX = D; break; }
                case 0x6b: { LX = E; break; }
                case 0x6c: { LX = HX; break; }
                case 0x6d: {  break; }
                case 0x6e: { EAX(); L = RM(EA); break; }
                case 0x6f: { LX = A; break; }
                case 0x70: { EAX(); WM( EA, B ); break; }
                case 0x71: { EAX(); WM( EA, C ); break; }
                case 0x72: { EAX(); WM( EA, D ); break; }
                case 0x73: { EAX(); WM( EA, E ); break; }
                case 0x74: { EAX(); WM( EA, H ); break; }
                case 0x75: { EAX(); WM( EA, L ); break; }
                case 0x76: { Dispatch_op(0x76); break; }
                case 0x77: { EAX(); WM( EA, A ); break; }
                case 0x78: { Dispatch_op(0x78); break; }
                case 0x79: { Dispatch_op(0x79); break; }
                case 0x7a: { Dispatch_op(0x7a); break; }
                case 0x7b: { Dispatch_op(0x7b); break; }
                case 0x7c: { A = HX; break; }
                case 0x7d: { A = LX; break; }
                case 0x7e: { EAX(); A = RM(EA); break; }
                case 0x7f: { Dispatch_op(0x7f); break; }
                case 0x80: { Dispatch_op(0x80); break; }
                case 0x81: { Dispatch_op(0x81); break; }
                case 0x82: { Dispatch_op(0x82); break; }
                case 0x83: { Dispatch_op(0x83); break; }
                case 0x84: { ADD(HX); break; }
                case 0x85: { ADD(LX); break; }
                case 0x86: { EAX(); ADD(RM(EA)); break; }
                case 0x87: { Dispatch_op(0x87); break; }
                case 0x88: { Dispatch_op(0x88); break; }
                case 0x89: { Dispatch_op(0x89); break; }
                case 0x8a: { Dispatch_op(0x8a); break; }
                case 0x8b: { Dispatch_op(0x8b); break; }
                case 0x8c: { ADC(HX); break; }
                case 0x8d: { ADC(LX); break; }
                case 0x8e: { EAX(); ADC(RM(EA)); break; }
                case 0x8f: { Dispatch_op(0x8f); break; }
                case 0x90: { Dispatch_op(0x90); break; }
                case 0x91: { Dispatch_op(0x91); break; }
                case 0x92: { Dispatch_op(0x92); break; }
                case 0x93: { Dispatch_op(0x93); break; }
                case 0x94: { SUB(HX); break; }
                case 0x95: { SUB(LX); break; }
                case 0x96: { EAX(); SUB(RM(EA)); break; }
                case 0x97: { Dispatch_op(0x97); break; }
                case 0x98: { Dispatch_op(0x98); break; }
                case 0x99: { Dispatch_op(0x99); break; }
                case 0x9a: { Dispatch_op(0x9a); break; }
                case 0x9b: { Dispatch_op(0x9b); break; }
                case 0x9c: { SBC(HX); break; }
                case 0x9d: { SBC(LX); break; }
                case 0x9e: { EAX(); SBC(RM(EA)); break; }
                case 0x9f: { Dispatch_op(0x9f); break; }
                case 0xa0: { Dispatch_op(0xa0); break; }
                case 0xa1: { Dispatch_op(0xa1); break; }
                case 0xa2: { Dispatch_op(0xa2); break; }
                case 0xa3: { Dispatch_op(0xa3); break; }
                case 0xa4: { AND(HX); break; }
                case 0xa5: { AND(LX); break; }
                case 0xa6: { EAX(); AND(RM(EA)); break; }
                case 0xa7: { Dispatch_op(0xa7); break; }
                case 0xa8: { Dispatch_op(0xa8); break; }
                case 0xa9: { Dispatch_op(0xa9); break; }
                case 0xaa: { Dispatch_op(0xaa); break; }
                case 0xab: { Dispatch_op(0xab); break; }
                case 0xac: { XOR(HX); break; }
                case 0xad: { XOR(LX); break; }
                case 0xae: { EAX(); XOR(RM(EA)); break; }
                case 0xaf: { Dispatch_op(0xaf); break; }
                case 0xb0: { Dispatch_op(0xb0); break; }
                case 0xb1: { Dispatch_op(0xb1); break; }
                case 0xb2: { Dispatch_op(0xb2); break; }
                case 0xb3: { Dispatch_op(0xb3); break; }
                case 0xb4: { OR(HX); break; }
                case 0xb5: { OR(LX); break; }
                case 0xb6: { EAX(); OR(RM(EA)); break; }
                case 0xb7: { Dispatch_op(0xb7); break; }
                case 0xb8: { Dispatch_op(0xb8); break; }
                case 0xb9: { Dispatch_op(0xb9); break; }
                case 0xba: { Dispatch_op(0xba); break; }
                case 0xbb: { Dispatch_op(0xbb); break; }
                case 0xbc: { CP(HX); break; }
                case 0xbd: { CP(LX); break; }
                case 0xbe: { EAX(); CP(RM(EA)); break; }
                case 0xbf: { Dispatch_op(0xbf); break; }
                case 0xc0: { Dispatch_op(0xc0); break; }
                case 0xc1: { Dispatch_op(0xc1); break; }
                case 0xc2: { Dispatch_op(0xc2); break; }
                case 0xc3: { Dispatch_op(0xc3); break; }
                case 0xc4: { Dispatch_op(0xc4); break; }
                case 0xc5: { Dispatch_op(0xc5); break; }
                case 0xc6: { Dispatch_op(0xc6); break; }
                case 0xc7: { Dispatch_op(0xc7); break; }
                case 0xc8: { Dispatch_op(0xc8); break; }
                case 0xc9: { Dispatch_op(0xc9); break; }
                case 0xca: { Dispatch_op(0xca); break; }
                case 0xcb: { EAX(); Execute_xycb(ARG()); break; }
                case 0xcc: { Dispatch_op(0xcc); break; }
                case 0xcd: { Dispatch_op(0xcd); break; }
                case 0xce: { Dispatch_op(0xce); break; }
                case 0xcf: { Dispatch_op(0xcf); break; }
                case 0xd0: { Dispatch_op(0xd0); break; }
                case 0xd1: { Dispatch_op(0xd1); break; }
                case 0xd2: { Dispatch_op(0xd2); break; }
                case 0xd3: { Dispatch_op(0xd3); break; }
                case 0xd4: { Dispatch_op(0xd4); break; }
                case 0xd5: { Dispatch_op(0xd5); break; }
                case 0xd6: { Dispatch_op(0xd6); break; }
                case 0xd7: { Dispatch_op(0xd7); break; }
                case 0xd8: { Dispatch_op(0xd8); break; }
                case 0xd9: { Dispatch_op(0xd9); break; }
                case 0xda: { Dispatch_op(0xda); break; }
                case 0xdb: { Dispatch_op(0xdb); break; }
                case 0xdc: { Dispatch_op(0xdc); break; }
                case 0xdd: { Execute_dd(ROP()); break; }
                case 0xde: { Dispatch_op(0xde); break; }
                case 0xdf: { Dispatch_op(0xdf); break; }
                case 0xe0: { Dispatch_op(0xe0); break; }
                case 0xe1: { POP(ref _state.ix); break; }
                case 0xe2: { Dispatch_op(0xe2); break; }
                case 0xe3: { EXSP(ref _state.ix); break; }
                case 0xe4: { Dispatch_op(0xe4); break; }
                case 0xe5: { PUSH(ref _state.ix); break; }
                case 0xe6: { Dispatch_op(0xe6); break; }
                case 0xe7: { Dispatch_op(0xe7); break; }
                case 0xe8: { Dispatch_op(0xe8); break; }
                case 0xe9: { PC = IX; break; }
                case 0xea: { Dispatch_op(0xea); break; }
                case 0xeb: { Dispatch_op(0xeb); break; }
                case 0xec: { Dispatch_op(0xec); break; }
                case 0xed: { Dispatch_op(0xed); break; }
                case 0xee: { Dispatch_op(0xee); break; }
                case 0xef: { Dispatch_op(0xef); break; }
                case 0xf0: { Dispatch_op(0xf0); break; }
                case 0xf1: { Dispatch_op(0xf1); break; }
                case 0xf2: { Dispatch_op(0xf2); break; }
                case 0xf3: { Dispatch_op(0xf3); break; }
                case 0xf4: { Dispatch_op(0xf4); break; }
                case 0xf5: { Dispatch_op(0xf5); break; }
                case 0xf6: { Dispatch_op(0xf6); break; }
                case 0xf7: { Dispatch_op(0xf7); break; }
                case 0xf8: { Dispatch_op(0xf8); break; }
                case 0xf9: { SP = IX; break; }
                case 0xfa: { Dispatch_op(0xfa); break; }
                case 0xfb: { Dispatch_op(0xfb); break; }
                case 0xfc: { Dispatch_op(0xfc); break; }
                case 0xfd: { Execute_fd(ROP()); break; }
                case 0xfe: { Dispatch_op(0xfe); break; }
                case 0xff: { Dispatch_op(0xff); break; }
            }
        }
    }
    private void Execute_ed(int opcode) { USE_CYCLES(cc_ed[opcode]); Dispatch_ed(opcode); }
    private void Dispatch_ed(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: {  break; }
                case 0x01: {  break; }
                case 0x02: {  break; }
                case 0x03: {  break; }
                case 0x04: {  break; }
                case 0x05: {  break; }
                case 0x06: {  break; }
                case 0x07: {  break; }
                case 0x08: {  break; }
                case 0x09: {  break; }
                case 0x0a: {  break; }
                case 0x0b: {  break; }
                case 0x0c: {  break; }
                case 0x0d: {  break; }
                case 0x0e: {  break; }
                case 0x0f: {  break; }
                case 0x10: {  break; }
                case 0x11: {  break; }
                case 0x12: {  break; }
                case 0x13: {  break; }
                case 0x14: {  break; }
                case 0x15: {  break; }
                case 0x16: {  break; }
                case 0x17: {  break; }
                case 0x18: {  break; }
                case 0x19: {  break; }
                case 0x1a: {  break; }
                case 0x1b: {  break; }
                case 0x1c: {  break; }
                case 0x1d: {  break; }
                case 0x1e: {  break; }
                case 0x1f: {  break; }
                case 0x20: {  break; }
                case 0x21: {  break; }
                case 0x22: {  break; }
                case 0x23: {  break; }
                case 0x24: {  break; }
                case 0x25: {  break; }
                case 0x26: {  break; }
                case 0x27: {  break; }
                case 0x28: {  break; }
                case 0x29: {  break; }
                case 0x2a: {  break; }
                case 0x2b: {  break; }
                case 0x2c: {  break; }
                case 0x2d: {  break; }
                case 0x2e: {  break; }
                case 0x2f: {  break; }
                case 0x30: {  break; }
                case 0x31: {  break; }
                case 0x32: {  break; }
                case 0x33: {  break; }
                case 0x34: {  break; }
                case 0x35: {  break; }
                case 0x36: {  break; }
                case 0x37: {  break; }
                case 0x38: {  break; }
                case 0x39: {  break; }
                case 0x3a: {  break; }
                case 0x3b: {  break; }
                case 0x3c: {  break; }
                case 0x3d: {  break; }
                case 0x3e: {  break; }
                case 0x3f: {  break; }
                case 0x40: { B = IN(BC); F = (F & CF) | SZP[B]; break; }
                case 0x41: { OUT(BC, B); break; }
                case 0x42: { SBC16(ref _state.bc); break; }
                case 0x43: { EA = ARG16(); WM16( EA, ref _state.bc ); WZ = EA+1; break; }
                case 0x44: { NEG(); break; }
                case 0x45: { RETN(); break; }
                case 0x46: { IM = 0; break; }
                case 0x47: { LD_I_A(); break; }
                case 0x48: { C = IN(BC); F = (F & CF) | SZP[C]; break; }
                case 0x49: { OUT(BC, C); break; }
                case 0x4a: { ADC16(ref _state.bc); break; }
                case 0x4b: { EA = ARG16(); RM16( EA, ref _state.bc ); WZ = EA+1; break; }
                case 0x4c: { NEG(); break; }
                case 0x4d: { RETI(); break; }
                case 0x4e: { IM = 0; break; }
                case 0x4f: { LD_R_A(); break; }
                case 0x50: { D = IN(BC); F = (F & CF) | SZP[D]; break; }
                case 0x51: { OUT(BC, D); break; }
                case 0x52: { SBC16(ref _state.de); break; }
                case 0x53: { EA = ARG16(); WM16( EA, ref _state.de ); WZ = EA+1; break; }
                case 0x54: { NEG(); break; }
                case 0x55: { RETN(); break; }
                case 0x56: { IM = 1; break; }
                case 0x57: { LD_A_I(); break; }
                case 0x58: { E = IN(BC); F = (F & CF) | SZP[E]; break; }
                case 0x59: { OUT(BC, E); break; }
                case 0x5a: { ADC16(ref _state.de); break; }
                case 0x5b: { EA = ARG16(); RM16( EA, ref _state.de ); WZ = EA+1; break; }
                case 0x5c: { NEG(); break; }
                case 0x5d: { RETI(); break; }
                case 0x5e: { IM = 2; break; }
                case 0x5f: { LD_A_R(); break; }
                case 0x60: { H = IN(BC); F = (F & CF) | SZP[H]; break; }
                case 0x61: { OUT(BC, H); break; }
                case 0x62: { SBC16(ref _state.hl); break; }
                case 0x63: { EA = ARG16(); WM16( EA, ref _state.hl ); WZ = EA+1; break; }
                case 0x64: { NEG(); break; }
                case 0x65: { RETN(); break; }
                case 0x66: { IM = 0; break; }
                case 0x67: { RRD(); break; }
                case 0x68: { L = IN(BC); F = (F & CF) | SZP[L]; break; }
                case 0x69: { OUT(BC, L); break; }
                case 0x6a: { ADC16(ref _state.hl); break; }
                case 0x6b: { EA = ARG16(); RM16( EA, ref _state.hl ); WZ = EA+1; break; }
                case 0x6c: { NEG(); break; }
                case 0x6d: { RETI(); break; }
                case 0x6e: { IM = 0; break; }
                case 0x6f: { RLD(); break; }
                case 0x70: { int res = IN(BC); F = (F & CF) | SZP[res]; break; }
                case 0x71: { OUT(BC, 0); break; }
                case 0x72: { SBC16(ref _state.sp); break; }
                case 0x73: { EA = ARG16(); WM16( EA, ref _state.sp ); WZ = EA+1; break; }
                case 0x74: { NEG(); break; }
                case 0x75: { RETN(); break; }
                case 0x76: { IM = 1; break; }
                case 0x77: {  break; }
                case 0x78: { A = IN(BC); F = (F & CF) | SZP[A]; WZ = BC+1; break; }
                case 0x79: { OUT(BC, A); WZ = BC + 1; break; }
                case 0x7a: { ADC16(ref _state.sp); break; }
                case 0x7b: { EA = ARG16(); RM16( EA, ref _state.sp ); WZ = EA+1; break; }
                case 0x7c: { NEG(); break; }
                case 0x7d: { RETI(); break; }
                case 0x7e: { IM = 2; break; }
                case 0x7f: {  break; }
                case 0x80: {  break; }
                case 0x81: {  break; }
                case 0x82: {  break; }
                case 0x83: {  break; }
                case 0x84: {  break; }
                case 0x85: {  break; }
                case 0x86: {  break; }
                case 0x87: {  break; }
                case 0x88: {  break; }
                case 0x89: {  break; }
                case 0x8a: {  break; }
                case 0x8b: {  break; }
                case 0x8c: {  break; }
                case 0x8d: {  break; }
                case 0x8e: {  break; }
                case 0x8f: {  break; }
                case 0x90: {  break; }
                case 0x91: {  break; }
                case 0x92: {  break; }
                case 0x93: {  break; }
                case 0x94: {  break; }
                case 0x95: {  break; }
                case 0x96: {  break; }
                case 0x97: {  break; }
                case 0x98: {  break; }
                case 0x99: {  break; }
                case 0x9a: {  break; }
                case 0x9b: {  break; }
                case 0x9c: {  break; }
                case 0x9d: {  break; }
                case 0x9e: {  break; }
                case 0x9f: {  break; }
                case 0xa0: { LDI(); break; }
                case 0xa1: { CPI(); break; }
                case 0xa2: { INI(); break; }
                case 0xa3: { OUTI(); break; }
                case 0xa4: {  break; }
                case 0xa5: {  break; }
                case 0xa6: {  break; }
                case 0xa7: {  break; }
                case 0xa8: { LDD(); break; }
                case 0xa9: { CPD(); break; }
                case 0xaa: { IND(); break; }
                case 0xab: { OUTD(); break; }
                case 0xac: {  break; }
                case 0xad: {  break; }
                case 0xae: {  break; }
                case 0xaf: {  break; }
                case 0xb0: { LDIR(); break; }
                case 0xb1: { CPIR(); break; }
                case 0xb2: { INIR(); break; }
                case 0xb3: { OTIR(); break; }
                case 0xb4: {  break; }
                case 0xb5: {  break; }
                case 0xb6: {  break; }
                case 0xb7: {  break; }
                case 0xb8: { LDDR(); break; }
                case 0xb9: { CPDR(); break; }
                case 0xba: { INDR(); break; }
                case 0xbb: { OTDR(); break; }
                case 0xbc: {  break; }
                case 0xbd: {  break; }
                case 0xbe: {  break; }
                case 0xbf: {  break; }
                case 0xc0: {  break; }
                case 0xc1: {  break; }
                case 0xc2: {  break; }
                case 0xc3: {  break; }
                case 0xc4: {  break; }
                case 0xc5: {  break; }
                case 0xc6: {  break; }
                case 0xc7: {  break; }
                case 0xc8: {  break; }
                case 0xc9: {  break; }
                case 0xca: {  break; }
                case 0xcb: {  break; }
                case 0xcc: {  break; }
                case 0xcd: {  break; }
                case 0xce: {  break; }
                case 0xcf: {  break; }
                case 0xd0: {  break; }
                case 0xd1: {  break; }
                case 0xd2: {  break; }
                case 0xd3: {  break; }
                case 0xd4: {  break; }
                case 0xd5: {  break; }
                case 0xd6: {  break; }
                case 0xd7: {  break; }
                case 0xd8: {  break; }
                case 0xd9: {  break; }
                case 0xda: {  break; }
                case 0xdb: {  break; }
                case 0xdc: {  break; }
                case 0xdd: {  break; }
                case 0xde: {  break; }
                case 0xdf: {  break; }
                case 0xe0: {  break; }
                case 0xe1: {  break; }
                case 0xe2: {  break; }
                case 0xe3: {  break; }
                case 0xe4: {  break; }
                case 0xe5: {  break; }
                case 0xe6: {  break; }
                case 0xe7: {  break; }
                case 0xe8: {  break; }
                case 0xe9: {  break; }
                case 0xea: {  break; }
                case 0xeb: {  break; }
                case 0xec: {  break; }
                case 0xed: {  break; }
                case 0xee: {  break; }
                case 0xef: {  break; }
                case 0xf0: {  break; }
                case 0xf1: {  break; }
                case 0xf2: {  break; }
                case 0xf3: {  break; }
                case 0xf4: {  break; }
                case 0xf5: {  break; }
                case 0xf6: {  break; }
                case 0xf7: {  break; }
                case 0xf8: {  break; }
                case 0xf9: {  break; }
                case 0xfa: {  break; }
                case 0xfb: {  break; }
                case 0xfc: {  break; }
                case 0xfd: {  break; }
                case 0xfe: {  break; }
                case 0xff: {  break; }
            }
        }
    }
    private void Execute_fd(int opcode) { USE_CYCLES(cc_xy[opcode]); Dispatch_fd(opcode); }
    private void Dispatch_fd(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: { Dispatch_op(0x00); break; }
                case 0x01: { Dispatch_op(0x01); break; }
                case 0x02: { Dispatch_op(0x02); break; }
                case 0x03: { Dispatch_op(0x03); break; }
                case 0x04: { Dispatch_op(0x04); break; }
                case 0x05: { Dispatch_op(0x05); break; }
                case 0x06: { Dispatch_op(0x06); break; }
                case 0x07: { Dispatch_op(0x07); break; }
                case 0x08: { Dispatch_op(0x08); break; }
                case 0x09: { ADD16(ref _state.iy, _state.bc); break; }
                case 0x0a: { Dispatch_op(0x0a); break; }
                case 0x0b: { Dispatch_op(0x0b); break; }
                case 0x0c: { Dispatch_op(0x0c); break; }
                case 0x0d: { Dispatch_op(0x0d); break; }
                case 0x0e: { Dispatch_op(0x0e); break; }
                case 0x0f: { Dispatch_op(0x0f); break; }
                case 0x10: { Dispatch_op(0x10); break; }
                case 0x11: { Dispatch_op(0x11); break; }
                case 0x12: { Dispatch_op(0x12); break; }
                case 0x13: { Dispatch_op(0x13); break; }
                case 0x14: { Dispatch_op(0x14); break; }
                case 0x15: { Dispatch_op(0x15); break; }
                case 0x16: { Dispatch_op(0x16); break; }
                case 0x17: { Dispatch_op(0x17); break; }
                case 0x18: { Dispatch_op(0x18); break; }
                case 0x19: { ADD16(ref _state.iy, _state.de); break; }
                case 0x1a: { Dispatch_op(0x1a); break; }
                case 0x1b: { Dispatch_op(0x1b); break; }
                case 0x1c: { Dispatch_op(0x1c); break; }
                case 0x1d: { Dispatch_op(0x1d); break; }
                case 0x1e: { Dispatch_op(0x1e); break; }
                case 0x1f: { Dispatch_op(0x1f); break; }
                case 0x20: { Dispatch_op(0x20); break; }
                case 0x21: { IY = ARG16(); break; }
                case 0x22: { EA = ARG16(); WM16( EA, ref _state.iy ); WZ = EA+1; break; }
                case 0x23: { IY++; break; }
                case 0x24: { HY = INC(HY); break; }
                case 0x25: { HY = DEC(HY); break; }
                case 0x26: { HY = ARG(); break; }
                case 0x27: { Dispatch_op(0x27); break; }
                case 0x28: { Dispatch_op(0x28); break; }
                case 0x29: { ADD16(ref _state.iy, _state.iy); break; }
                case 0x2a: { EA = ARG16(); RM16( EA, ref _state.iy ); WZ = EA+1; break; }
                case 0x2b: { IY--; break; }
                case 0x2c: { LY = INC(LY); break; }
                case 0x2d: { LY = DEC(LY); break; }
                case 0x2e: { LY = ARG(); break; }
                case 0x2f: { Dispatch_op(0x2f); break; }
                case 0x30: { Dispatch_op(0x30); break; }
                case 0x31: { Dispatch_op(0x31); break; }
                case 0x32: { Dispatch_op(0x32); break; }
                case 0x33: { Dispatch_op(0x33); break; }
                case 0x34: { EAY(); WM( EA, INC(RM(EA)) ); break; }
                case 0x35: { EAY(); WM( EA, DEC(RM(EA)) ); break; }
                case 0x36: { EAY(); WM( EA, ARG() ); break; }
                case 0x37: { Dispatch_op(0x37); break; }
                case 0x38: { Dispatch_op(0x38); break; }
                case 0x39: { ADD16(ref _state.iy, _state.sp); break; }
                case 0x3a: { Dispatch_op(0x3a); break; }
                case 0x3b: { Dispatch_op(0x3b); break; }
                case 0x3c: { Dispatch_op(0x3c); break; }
                case 0x3d: { Dispatch_op(0x3d); break; }
                case 0x3e: { Dispatch_op(0x3e); break; }
                case 0x3f: { Dispatch_op(0x3f); break; }
                case 0x40: { Dispatch_op(0x40); break; }
                case 0x41: { Dispatch_op(0x41); break; }
                case 0x42: { Dispatch_op(0x42); break; }
                case 0x43: { Dispatch_op(0x43); break; }
                case 0x44: { B = HY; break; }
                case 0x45: { B = LY; break; }
                case 0x46: { EAY(); B = RM(EA); break; }
                case 0x47: { Dispatch_op(0x47); break; }
                case 0x48: { Dispatch_op(0x48); break; }
                case 0x49: { Dispatch_op(0x49); break; }
                case 0x4a: { Dispatch_op(0x4a); break; }
                case 0x4b: { Dispatch_op(0x4b); break; }
                case 0x4c: { C = HY; break; }
                case 0x4d: { C = LY; break; }
                case 0x4e: { EAY(); C = RM(EA); break; }
                case 0x4f: { Dispatch_op(0x4f); break; }
                case 0x50: { Dispatch_op(0x50); break; }
                case 0x51: { Dispatch_op(0x51); break; }
                case 0x52: { Dispatch_op(0x52); break; }
                case 0x53: { Dispatch_op(0x53); break; }
                case 0x54: { D = HY; break; }
                case 0x55: { D = LY; break; }
                case 0x56: { EAY(); D = RM(EA); break; }
                case 0x57: { Dispatch_op(0x57); break; }
                case 0x58: { Dispatch_op(0x58); break; }
                case 0x59: { Dispatch_op(0x59); break; }
                case 0x5a: { Dispatch_op(0x5a); break; }
                case 0x5b: { Dispatch_op(0x5b); break; }
                case 0x5c: { E = HY; break; }
                case 0x5d: { E = LY; break; }
                case 0x5e: { EAY(); E = RM(EA); break; }
                case 0x5f: { Dispatch_op(0x5f); break; }
                case 0x60: { HY = B; break; }
                case 0x61: { HY = C; break; }
                case 0x62: { HY = D; break; }
                case 0x63: { HY = E; break; }
                case 0x64: {  break; }
                case 0x65: { HY = LY; break; }
                case 0x66: { EAY(); H = RM(EA); break; }
                case 0x67: { HY = A; break; }
                case 0x68: { LY = B; break; }
                case 0x69: { LY = C; break; }
                case 0x6a: { LY = D; break; }
                case 0x6b: { LY = E; break; }
                case 0x6c: { LY = HY; break; }
                case 0x6d: {  break; }
                case 0x6e: { EAY(); L = RM(EA); break; }
                case 0x6f: { LY = A; break; }
                case 0x70: { EAY(); WM( EA, B ); break; }
                case 0x71: { EAY(); WM( EA, C ); break; }
                case 0x72: { EAY(); WM( EA, D ); break; }
                case 0x73: { EAY(); WM( EA, E ); break; }
                case 0x74: { EAY(); WM( EA, H ); break; }
                case 0x75: { EAY(); WM( EA, L ); break; }
                case 0x76: { Dispatch_op(0x76); break; }
                case 0x77: { EAY(); WM( EA, A ); break; }
                case 0x78: { Dispatch_op(0x78); break; }
                case 0x79: { Dispatch_op(0x79); break; }
                case 0x7a: { Dispatch_op(0x7a); break; }
                case 0x7b: { Dispatch_op(0x7b); break; }
                case 0x7c: { A = HY; break; }
                case 0x7d: { A = LY; break; }
                case 0x7e: { EAY(); A = RM(EA); break; }
                case 0x7f: { Dispatch_op(0x7f); break; }
                case 0x80: { Dispatch_op(0x80); break; }
                case 0x81: { Dispatch_op(0x81); break; }
                case 0x82: { Dispatch_op(0x82); break; }
                case 0x83: { Dispatch_op(0x83); break; }
                case 0x84: { ADD(HY); break; }
                case 0x85: { ADD(LY); break; }
                case 0x86: { EAY(); ADD(RM(EA)); break; }
                case 0x87: { Dispatch_op(0x87); break; }
                case 0x88: { Dispatch_op(0x88); break; }
                case 0x89: { Dispatch_op(0x89); break; }
                case 0x8a: { Dispatch_op(0x8a); break; }
                case 0x8b: { Dispatch_op(0x8b); break; }
                case 0x8c: { ADC(HY); break; }
                case 0x8d: { ADC(LY); break; }
                case 0x8e: { EAY(); ADC(RM(EA)); break; }
                case 0x8f: { Dispatch_op(0x8f); break; }
                case 0x90: { Dispatch_op(0x90); break; }
                case 0x91: { Dispatch_op(0x91); break; }
                case 0x92: { Dispatch_op(0x92); break; }
                case 0x93: { Dispatch_op(0x93); break; }
                case 0x94: { SUB(HY); break; }
                case 0x95: { SUB(LY); break; }
                case 0x96: { EAY(); SUB(RM(EA)); break; }
                case 0x97: { Dispatch_op(0x97); break; }
                case 0x98: { Dispatch_op(0x98); break; }
                case 0x99: { Dispatch_op(0x99); break; }
                case 0x9a: { Dispatch_op(0x9a); break; }
                case 0x9b: { Dispatch_op(0x9b); break; }
                case 0x9c: { SBC(HY); break; }
                case 0x9d: { SBC(LY); break; }
                case 0x9e: { EAY(); SBC(RM(EA)); break; }
                case 0x9f: { Dispatch_op(0x9f); break; }
                case 0xa0: { Dispatch_op(0xa0); break; }
                case 0xa1: { Dispatch_op(0xa1); break; }
                case 0xa2: { Dispatch_op(0xa2); break; }
                case 0xa3: { Dispatch_op(0xa3); break; }
                case 0xa4: { AND(HY); break; }
                case 0xa5: { AND(LY); break; }
                case 0xa6: { EAY(); AND(RM(EA)); break; }
                case 0xa7: { Dispatch_op(0xa7); break; }
                case 0xa8: { Dispatch_op(0xa8); break; }
                case 0xa9: { Dispatch_op(0xa9); break; }
                case 0xaa: { Dispatch_op(0xaa); break; }
                case 0xab: { Dispatch_op(0xab); break; }
                case 0xac: { XOR(HY); break; }
                case 0xad: { XOR(LY); break; }
                case 0xae: { EAY(); XOR(RM(EA)); break; }
                case 0xaf: { Dispatch_op(0xaf); break; }
                case 0xb0: { Dispatch_op(0xb0); break; }
                case 0xb1: { Dispatch_op(0xb1); break; }
                case 0xb2: { Dispatch_op(0xb2); break; }
                case 0xb3: { Dispatch_op(0xb3); break; }
                case 0xb4: { OR(HY); break; }
                case 0xb5: { OR(LY); break; }
                case 0xb6: { EAY(); OR(RM(EA)); break; }
                case 0xb7: { Dispatch_op(0xb7); break; }
                case 0xb8: { Dispatch_op(0xb8); break; }
                case 0xb9: { Dispatch_op(0xb9); break; }
                case 0xba: { Dispatch_op(0xba); break; }
                case 0xbb: { Dispatch_op(0xbb); break; }
                case 0xbc: { CP(HY); break; }
                case 0xbd: { CP(LY); break; }
                case 0xbe: { EAY(); CP(RM(EA)); break; }
                case 0xbf: { Dispatch_op(0xbf); break; }
                case 0xc0: { Dispatch_op(0xc0); break; }
                case 0xc1: { Dispatch_op(0xc1); break; }
                case 0xc2: { Dispatch_op(0xc2); break; }
                case 0xc3: { Dispatch_op(0xc3); break; }
                case 0xc4: { Dispatch_op(0xc4); break; }
                case 0xc5: { Dispatch_op(0xc5); break; }
                case 0xc6: { Dispatch_op(0xc6); break; }
                case 0xc7: { Dispatch_op(0xc7); break; }
                case 0xc8: { Dispatch_op(0xc8); break; }
                case 0xc9: { Dispatch_op(0xc9); break; }
                case 0xca: { Dispatch_op(0xca); break; }
                case 0xcb: { EAY(); Execute_xycb(ARG()); break; }
                case 0xcc: { Dispatch_op(0xcc); break; }
                case 0xcd: { Dispatch_op(0xcd); break; }
                case 0xce: { Dispatch_op(0xce); break; }
                case 0xcf: { Dispatch_op(0xcf); break; }
                case 0xd0: { Dispatch_op(0xd0); break; }
                case 0xd1: { Dispatch_op(0xd1); break; }
                case 0xd2: { Dispatch_op(0xd2); break; }
                case 0xd3: { Dispatch_op(0xd3); break; }
                case 0xd4: { Dispatch_op(0xd4); break; }
                case 0xd5: { Dispatch_op(0xd5); break; }
                case 0xd6: { Dispatch_op(0xd6); break; }
                case 0xd7: { Dispatch_op(0xd7); break; }
                case 0xd8: { Dispatch_op(0xd8); break; }
                case 0xd9: { Dispatch_op(0xd9); break; }
                case 0xda: { Dispatch_op(0xda); break; }
                case 0xdb: { Dispatch_op(0xdb); break; }
                case 0xdc: { Dispatch_op(0xdc); break; }
                case 0xdd: { Execute_dd(ROP()); break; }
                case 0xde: { Dispatch_op(0xde); break; }
                case 0xdf: { Dispatch_op(0xdf); break; }
                case 0xe0: { Dispatch_op(0xe0); break; }
                case 0xe1: { POP(ref _state.iy); break; }
                case 0xe2: { Dispatch_op(0xe2); break; }
                case 0xe3: { EXSP(ref _state.iy); break; }
                case 0xe4: { Dispatch_op(0xe4); break; }
                case 0xe5: { PUSH(ref _state.iy); break; }
                case 0xe6: { Dispatch_op(0xe6); break; }
                case 0xe7: { Dispatch_op(0xe7); break; }
                case 0xe8: { Dispatch_op(0xe8); break; }
                case 0xe9: { PC = IY; break; }
                case 0xea: { Dispatch_op(0xea); break; }
                case 0xeb: { Dispatch_op(0xeb); break; }
                case 0xec: { Dispatch_op(0xec); break; }
                case 0xed: { Dispatch_op(0xed); break; }
                case 0xee: { Dispatch_op(0xee); break; }
                case 0xef: { Dispatch_op(0xef); break; }
                case 0xf0: { Dispatch_op(0xf0); break; }
                case 0xf1: { Dispatch_op(0xf1); break; }
                case 0xf2: { Dispatch_op(0xf2); break; }
                case 0xf3: { Dispatch_op(0xf3); break; }
                case 0xf4: { Dispatch_op(0xf4); break; }
                case 0xf5: { Dispatch_op(0xf5); break; }
                case 0xf6: { Dispatch_op(0xf6); break; }
                case 0xf7: { Dispatch_op(0xf7); break; }
                case 0xf8: { Dispatch_op(0xf8); break; }
                case 0xf9: { SP = IY; break; }
                case 0xfa: { Dispatch_op(0xfa); break; }
                case 0xfb: { Dispatch_op(0xfb); break; }
                case 0xfc: { Dispatch_op(0xfc); break; }
                case 0xfd: { Execute_fd(ROP()); break; }
                case 0xfe: { Dispatch_op(0xfe); break; }
                case 0xff: { Dispatch_op(0xff); break; }
            }
        }
    }
    private void Execute_xycb(int opcode) { USE_CYCLES(cc_xycb[opcode]); Dispatch_xycb(opcode); }
    private void Dispatch_xycb(int opcode)
    {
        unchecked
        {
            switch (opcode)
            {
                case 0x00: { B = RLC( RM(EA) ); WM( EA,B ); break; }
                case 0x01: { C = RLC( RM(EA) ); WM( EA,C ); break; }
                case 0x02: { D = RLC( RM(EA) ); WM( EA,D ); break; }
                case 0x03: { E = RLC( RM(EA) ); WM( EA,E ); break; }
                case 0x04: { H = RLC( RM(EA) ); WM( EA,H ); break; }
                case 0x05: { L = RLC( RM(EA) ); WM( EA,L ); break; }
                case 0x06: { WM( EA, RLC( RM(EA) ) ); break; }
                case 0x07: { A = RLC( RM(EA) ); WM( EA,A ); break; }
                case 0x08: { B = RRC( RM(EA) ); WM( EA,B ); break; }
                case 0x09: { C = RRC( RM(EA) ); WM( EA,C ); break; }
                case 0x0a: { D = RRC( RM(EA) ); WM( EA,D ); break; }
                case 0x0b: { E = RRC( RM(EA) ); WM( EA,E ); break; }
                case 0x0c: { H = RRC( RM(EA) ); WM( EA,H ); break; }
                case 0x0d: { L = RRC( RM(EA) ); WM( EA,L ); break; }
                case 0x0e: { WM( EA,RRC( RM(EA) ) ); break; }
                case 0x0f: { A = RRC( RM(EA) ); WM( EA,A ); break; }
                case 0x10: { B = RL( RM(EA) ); WM( EA,B ); break; }
                case 0x11: { C = RL( RM(EA) ); WM( EA,C ); break; }
                case 0x12: { D = RL( RM(EA) ); WM( EA,D ); break; }
                case 0x13: { E = RL( RM(EA) ); WM( EA,E ); break; }
                case 0x14: { H = RL( RM(EA) ); WM( EA,H ); break; }
                case 0x15: { L = RL( RM(EA) ); WM( EA,L ); break; }
                case 0x16: { WM( EA,RL( RM(EA) ) ); break; }
                case 0x17: { A = RL( RM(EA) ); WM( EA,A ); break; }
                case 0x18: { B = RR( RM(EA) ); WM( EA,B ); break; }
                case 0x19: { C = RR( RM(EA) ); WM( EA,C ); break; }
                case 0x1a: { D = RR( RM(EA) ); WM( EA,D ); break; }
                case 0x1b: { E = RR( RM(EA) ); WM( EA,E ); break; }
                case 0x1c: { H = RR( RM(EA) ); WM( EA,H ); break; }
                case 0x1d: { L = RR( RM(EA) ); WM( EA,L ); break; }
                case 0x1e: { WM( EA,RR( RM(EA) ) ); break; }
                case 0x1f: { A = RR( RM(EA) ); WM( EA,A ); break; }
                case 0x20: { B = SLA( RM(EA) ); WM( EA,B ); break; }
                case 0x21: { C = SLA( RM(EA) ); WM( EA,C ); break; }
                case 0x22: { D = SLA( RM(EA) ); WM( EA,D ); break; }
                case 0x23: { E = SLA( RM(EA) ); WM( EA,E ); break; }
                case 0x24: { H = SLA( RM(EA) ); WM( EA,H ); break; }
                case 0x25: { L = SLA( RM(EA) ); WM( EA,L ); break; }
                case 0x26: { WM( EA,SLA( RM(EA) ) ); break; }
                case 0x27: { A = SLA( RM(EA) ); WM( EA,A ); break; }
                case 0x28: { B = SRA( RM(EA) ); WM( EA,B ); break; }
                case 0x29: { C = SRA( RM(EA) ); WM( EA,C ); break; }
                case 0x2a: { D = SRA( RM(EA) ); WM( EA,D ); break; }
                case 0x2b: { E = SRA( RM(EA) ); WM( EA,E ); break; }
                case 0x2c: { H = SRA( RM(EA) ); WM( EA,H ); break; }
                case 0x2d: { L = SRA( RM(EA) ); WM( EA,L ); break; }
                case 0x2e: { WM( EA,SRA( RM(EA) ) ); break; }
                case 0x2f: { A = SRA( RM(EA) ); WM( EA,A ); break; }
                case 0x30: { B = SLL( RM(EA) ); WM( EA,B ); break; }
                case 0x31: { C = SLL( RM(EA) ); WM( EA,C ); break; }
                case 0x32: { D = SLL( RM(EA) ); WM( EA,D ); break; }
                case 0x33: { E = SLL( RM(EA) ); WM( EA,E ); break; }
                case 0x34: { H = SLL( RM(EA) ); WM( EA,H ); break; }
                case 0x35: { L = SLL( RM(EA) ); WM( EA,L ); break; }
                case 0x36: { WM( EA,SLL( RM(EA) ) ); break; }
                case 0x37: { A = SLL( RM(EA) ); WM( EA,A ); break; }
                case 0x38: { B = SRL( RM(EA) ); WM( EA,B ); break; }
                case 0x39: { C = SRL( RM(EA) ); WM( EA,C ); break; }
                case 0x3a: { D = SRL( RM(EA) ); WM( EA,D ); break; }
                case 0x3b: { E = SRL( RM(EA) ); WM( EA,E ); break; }
                case 0x3c: { H = SRL( RM(EA) ); WM( EA,H ); break; }
                case 0x3d: { L = SRL( RM(EA) ); WM( EA,L ); break; }
                case 0x3e: { WM( EA,SRL( RM(EA) ) ); break; }
                case 0x3f: { A = SRL( RM(EA) ); WM( EA,A ); break; }
                case 0x40: { Dispatch_xycb(0x46); break; }
                case 0x41: { Dispatch_xycb(0x46); break; }
                case 0x42: { Dispatch_xycb(0x46); break; }
                case 0x43: { Dispatch_xycb(0x46); break; }
                case 0x44: { Dispatch_xycb(0x46); break; }
                case 0x45: { Dispatch_xycb(0x46); break; }
                case 0x46: { BIT_XY(0,RM(EA)); break; }
                case 0x47: { Dispatch_xycb(0x46); break; }
                case 0x48: { Dispatch_xycb(0x4e); break; }
                case 0x49: { Dispatch_xycb(0x4e); break; }
                case 0x4a: { Dispatch_xycb(0x4e); break; }
                case 0x4b: { Dispatch_xycb(0x4e); break; }
                case 0x4c: { Dispatch_xycb(0x4e); break; }
                case 0x4d: { Dispatch_xycb(0x4e); break; }
                case 0x4e: { BIT_XY(1,RM(EA)); break; }
                case 0x4f: { Dispatch_xycb(0x4e); break; }
                case 0x50: { Dispatch_xycb(0x56); break; }
                case 0x51: { Dispatch_xycb(0x56); break; }
                case 0x52: { Dispatch_xycb(0x56); break; }
                case 0x53: { Dispatch_xycb(0x56); break; }
                case 0x54: { Dispatch_xycb(0x56); break; }
                case 0x55: { Dispatch_xycb(0x56); break; }
                case 0x56: { BIT_XY(2,RM(EA)); break; }
                case 0x57: { Dispatch_xycb(0x56); break; }
                case 0x58: { Dispatch_xycb(0x5e); break; }
                case 0x59: { Dispatch_xycb(0x5e); break; }
                case 0x5a: { Dispatch_xycb(0x5e); break; }
                case 0x5b: { Dispatch_xycb(0x5e); break; }
                case 0x5c: { Dispatch_xycb(0x5e); break; }
                case 0x5d: { Dispatch_xycb(0x5e); break; }
                case 0x5e: { BIT_XY(3,RM(EA)); break; }
                case 0x5f: { Dispatch_xycb(0x5e); break; }
                case 0x60: { Dispatch_xycb(0x66); break; }
                case 0x61: { Dispatch_xycb(0x66); break; }
                case 0x62: { Dispatch_xycb(0x66); break; }
                case 0x63: { Dispatch_xycb(0x66); break; }
                case 0x64: { Dispatch_xycb(0x66); break; }
                case 0x65: { Dispatch_xycb(0x66); break; }
                case 0x66: { BIT_XY(4,RM(EA)); break; }
                case 0x67: { Dispatch_xycb(0x66); break; }
                case 0x68: { Dispatch_xycb(0x6e); break; }
                case 0x69: { Dispatch_xycb(0x6e); break; }
                case 0x6a: { Dispatch_xycb(0x6e); break; }
                case 0x6b: { Dispatch_xycb(0x6e); break; }
                case 0x6c: { Dispatch_xycb(0x6e); break; }
                case 0x6d: { Dispatch_xycb(0x6e); break; }
                case 0x6e: { BIT_XY(5,RM(EA)); break; }
                case 0x6f: { Dispatch_xycb(0x6e); break; }
                case 0x70: { Dispatch_xycb(0x76); break; }
                case 0x71: { Dispatch_xycb(0x76); break; }
                case 0x72: { Dispatch_xycb(0x76); break; }
                case 0x73: { Dispatch_xycb(0x76); break; }
                case 0x74: { Dispatch_xycb(0x76); break; }
                case 0x75: { Dispatch_xycb(0x76); break; }
                case 0x76: { BIT_XY(6,RM(EA)); break; }
                case 0x77: { Dispatch_xycb(0x76); break; }
                case 0x78: { Dispatch_xycb(0x7e); break; }
                case 0x79: { Dispatch_xycb(0x7e); break; }
                case 0x7a: { Dispatch_xycb(0x7e); break; }
                case 0x7b: { Dispatch_xycb(0x7e); break; }
                case 0x7c: { Dispatch_xycb(0x7e); break; }
                case 0x7d: { Dispatch_xycb(0x7e); break; }
                case 0x7e: { BIT_XY(7,RM(EA)); break; }
                case 0x7f: { Dispatch_xycb(0x7e); break; }
                case 0x80: { B = RES(0, RM(EA) ); WM( EA,B ); break; }
                case 0x81: { C = RES(0, RM(EA) ); WM( EA,C ); break; }
                case 0x82: { D = RES(0, RM(EA) ); WM( EA,D ); break; }
                case 0x83: { E = RES(0, RM(EA) ); WM( EA,E ); break; }
                case 0x84: { H = RES(0, RM(EA) ); WM( EA,H ); break; }
                case 0x85: { L = RES(0, RM(EA) ); WM( EA,L ); break; }
                case 0x86: { WM( EA, RES(0,RM(EA)) ); break; }
                case 0x87: { A = RES(0, RM(EA) ); WM( EA,A ); break; }
                case 0x88: { B = RES(1, RM(EA) ); WM( EA,B ); break; }
                case 0x89: { C = RES(1, RM(EA) ); WM( EA,C ); break; }
                case 0x8a: { D = RES(1, RM(EA) ); WM( EA,D ); break; }
                case 0x8b: { E = RES(1, RM(EA) ); WM( EA,E ); break; }
                case 0x8c: { H = RES(1, RM(EA) ); WM( EA,H ); break; }
                case 0x8d: { L = RES(1, RM(EA) ); WM( EA,L ); break; }
                case 0x8e: { WM( EA, RES(1,RM(EA)) ); break; }
                case 0x8f: { A = RES(1, RM(EA) ); WM( EA,A ); break; }
                case 0x90: { B = RES(2, RM(EA) ); WM( EA,B ); break; }
                case 0x91: { C = RES(2, RM(EA) ); WM( EA,C ); break; }
                case 0x92: { D = RES(2, RM(EA) ); WM( EA,D ); break; }
                case 0x93: { E = RES(2, RM(EA) ); WM( EA,E ); break; }
                case 0x94: { H = RES(2, RM(EA) ); WM( EA,H ); break; }
                case 0x95: { L = RES(2, RM(EA) ); WM( EA,L ); break; }
                case 0x96: { WM( EA, RES(2,RM(EA)) ); break; }
                case 0x97: { A = RES(2, RM(EA) ); WM( EA,A ); break; }
                case 0x98: { B = RES(3, RM(EA) ); WM( EA,B ); break; }
                case 0x99: { C = RES(3, RM(EA) ); WM( EA,C ); break; }
                case 0x9a: { D = RES(3, RM(EA) ); WM( EA,D ); break; }
                case 0x9b: { E = RES(3, RM(EA) ); WM( EA,E ); break; }
                case 0x9c: { H = RES(3, RM(EA) ); WM( EA,H ); break; }
                case 0x9d: { L = RES(3, RM(EA) ); WM( EA,L ); break; }
                case 0x9e: { WM( EA, RES(3,RM(EA)) ); break; }
                case 0x9f: { A = RES(3, RM(EA) ); WM( EA,A ); break; }
                case 0xa0: { B = RES(4, RM(EA) ); WM( EA,B ); break; }
                case 0xa1: { C = RES(4, RM(EA) ); WM( EA,C ); break; }
                case 0xa2: { D = RES(4, RM(EA) ); WM( EA,D ); break; }
                case 0xa3: { E = RES(4, RM(EA) ); WM( EA,E ); break; }
                case 0xa4: { H = RES(4, RM(EA) ); WM( EA,H ); break; }
                case 0xa5: { L = RES(4, RM(EA) ); WM( EA,L ); break; }
                case 0xa6: { WM( EA, RES(4,RM(EA)) ); break; }
                case 0xa7: { A = RES(4, RM(EA) ); WM( EA,A ); break; }
                case 0xa8: { B = RES(5, RM(EA) ); WM( EA,B ); break; }
                case 0xa9: { C = RES(5, RM(EA) ); WM( EA,C ); break; }
                case 0xaa: { D = RES(5, RM(EA) ); WM( EA,D ); break; }
                case 0xab: { E = RES(5, RM(EA) ); WM( EA,E ); break; }
                case 0xac: { H = RES(5, RM(EA) ); WM( EA,H ); break; }
                case 0xad: { L = RES(5, RM(EA) ); WM( EA,L ); break; }
                case 0xae: { WM( EA, RES(5,RM(EA)) ); break; }
                case 0xaf: { A = RES(5, RM(EA) ); WM( EA,A ); break; }
                case 0xb0: { B = RES(6, RM(EA) ); WM( EA,B ); break; }
                case 0xb1: { C = RES(6, RM(EA) ); WM( EA,C ); break; }
                case 0xb2: { D = RES(6, RM(EA) ); WM( EA,D ); break; }
                case 0xb3: { E = RES(6, RM(EA) ); WM( EA,E ); break; }
                case 0xb4: { H = RES(6, RM(EA) ); WM( EA,H ); break; }
                case 0xb5: { L = RES(6, RM(EA) ); WM( EA,L ); break; }
                case 0xb6: { WM( EA, RES(6,RM(EA)) ); break; }
                case 0xb7: { A = RES(6, RM(EA) ); WM( EA,A ); break; }
                case 0xb8: { B = RES(7, RM(EA) ); WM( EA,B ); break; }
                case 0xb9: { C = RES(7, RM(EA) ); WM( EA,C ); break; }
                case 0xba: { D = RES(7, RM(EA) ); WM( EA,D ); break; }
                case 0xbb: { E = RES(7, RM(EA) ); WM( EA,E ); break; }
                case 0xbc: { H = RES(7, RM(EA) ); WM( EA,H ); break; }
                case 0xbd: { L = RES(7, RM(EA) ); WM( EA,L ); break; }
                case 0xbe: { WM( EA, RES(7,RM(EA)) ); break; }
                case 0xbf: { A = RES(7, RM(EA) ); WM( EA,A ); break; }
                case 0xc0: { B = SET(0, RM(EA) ); WM( EA,B ); break; }
                case 0xc1: { C = SET(0, RM(EA) ); WM( EA,C ); break; }
                case 0xc2: { D = SET(0, RM(EA) ); WM( EA,D ); break; }
                case 0xc3: { E = SET(0, RM(EA) ); WM( EA,E ); break; }
                case 0xc4: { H = SET(0, RM(EA) ); WM( EA,H ); break; }
                case 0xc5: { L = SET(0, RM(EA) ); WM( EA,L ); break; }
                case 0xc6: { WM( EA, SET(0,RM(EA)) ); break; }
                case 0xc7: { A = SET(0, RM(EA) ); WM( EA,A ); break; }
                case 0xc8: { B = SET(1, RM(EA) ); WM( EA,B ); break; }
                case 0xc9: { C = SET(1, RM(EA) ); WM( EA,C ); break; }
                case 0xca: { D = SET(1, RM(EA) ); WM( EA,D ); break; }
                case 0xcb: { E = SET(1, RM(EA) ); WM( EA,E ); break; }
                case 0xcc: { H = SET(1, RM(EA) ); WM( EA,H ); break; }
                case 0xcd: { L = SET(1, RM(EA) ); WM( EA,L ); break; }
                case 0xce: { WM( EA, SET(1,RM(EA)) ); break; }
                case 0xcf: { A = SET(1, RM(EA) ); WM( EA,A ); break; }
                case 0xd0: { B = SET(2, RM(EA) ); WM( EA,B ); break; }
                case 0xd1: { C = SET(2, RM(EA) ); WM( EA,C ); break; }
                case 0xd2: { D = SET(2, RM(EA) ); WM( EA,D ); break; }
                case 0xd3: { E = SET(2, RM(EA) ); WM( EA,E ); break; }
                case 0xd4: { H = SET(2, RM(EA) ); WM( EA,H ); break; }
                case 0xd5: { L = SET(2, RM(EA) ); WM( EA,L ); break; }
                case 0xd6: { WM( EA, SET(2,RM(EA)) ); break; }
                case 0xd7: { A = SET(2, RM(EA) ); WM( EA,A ); break; }
                case 0xd8: { B = SET(3, RM(EA) ); WM( EA,B ); break; }
                case 0xd9: { C = SET(3, RM(EA) ); WM( EA,C ); break; }
                case 0xda: { D = SET(3, RM(EA) ); WM( EA,D ); break; }
                case 0xdb: { E = SET(3, RM(EA) ); WM( EA,E ); break; }
                case 0xdc: { H = SET(3, RM(EA) ); WM( EA,H ); break; }
                case 0xdd: { L = SET(3, RM(EA) ); WM( EA,L ); break; }
                case 0xde: { WM( EA, SET(3,RM(EA)) ); break; }
                case 0xdf: { A = SET(3, RM(EA) ); WM( EA,A ); break; }
                case 0xe0: { B = SET(4, RM(EA) ); WM( EA,B ); break; }
                case 0xe1: { C = SET(4, RM(EA) ); WM( EA,C ); break; }
                case 0xe2: { D = SET(4, RM(EA) ); WM( EA,D ); break; }
                case 0xe3: { E = SET(4, RM(EA) ); WM( EA,E ); break; }
                case 0xe4: { H = SET(4, RM(EA) ); WM( EA,H ); break; }
                case 0xe5: { L = SET(4, RM(EA) ); WM( EA,L ); break; }
                case 0xe6: { WM( EA, SET(4,RM(EA)) ); break; }
                case 0xe7: { A = SET(4, RM(EA) ); WM( EA,A ); break; }
                case 0xe8: { B = SET(5, RM(EA) ); WM( EA,B ); break; }
                case 0xe9: { C = SET(5, RM(EA) ); WM( EA,C ); break; }
                case 0xea: { D = SET(5, RM(EA) ); WM( EA,D ); break; }
                case 0xeb: { E = SET(5, RM(EA) ); WM( EA,E ); break; }
                case 0xec: { H = SET(5, RM(EA) ); WM( EA,H ); break; }
                case 0xed: { L = SET(5, RM(EA) ); WM( EA,L ); break; }
                case 0xee: { WM( EA, SET(5,RM(EA)) ); break; }
                case 0xef: { A = SET(5, RM(EA) ); WM( EA,A ); break; }
                case 0xf0: { B = SET(6, RM(EA) ); WM( EA,B ); break; }
                case 0xf1: { C = SET(6, RM(EA) ); WM( EA,C ); break; }
                case 0xf2: { D = SET(6, RM(EA) ); WM( EA,D ); break; }
                case 0xf3: { E = SET(6, RM(EA) ); WM( EA,E ); break; }
                case 0xf4: { H = SET(6, RM(EA) ); WM( EA,H ); break; }
                case 0xf5: { L = SET(6, RM(EA) ); WM( EA,L ); break; }
                case 0xf6: { WM( EA, SET(6,RM(EA)) ); break; }
                case 0xf7: { A = SET(6, RM(EA) ); WM( EA,A ); break; }
                case 0xf8: { B = SET(7, RM(EA) ); WM( EA,B ); break; }
                case 0xf9: { C = SET(7, RM(EA) ); WM( EA,C ); break; }
                case 0xfa: { D = SET(7, RM(EA) ); WM( EA,D ); break; }
                case 0xfb: { E = SET(7, RM(EA) ); WM( EA,E ); break; }
                case 0xfc: { H = SET(7, RM(EA) ); WM( EA,H ); break; }
                case 0xfd: { L = SET(7, RM(EA) ); WM( EA,L ); break; }
                case 0xfe: { WM( EA, SET(7,RM(EA)) ); break; }
                case 0xff: { A = SET(7, RM(EA) ); WM( EA,A ); break; }
            }
        }
    }
}
