// Instruction semantics adapted from ares SH-2, revision 0b928930af4cc59ed99e942996f0d3c815379388.
namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private void ADD(uint m, uint n)
    { unchecked {
  R[n] += R[m];

    } }

    private void ADDC(uint m, uint n)
    { unchecked {
  uint a = unchecked((uint)(R[n]));
  uint b = unchecked((uint)(a + R[m]));
  R[n] = unchecked((uint)(b + SR.T));
  SR.T = B(a > b || b > R[n]);

    } }

    private void ADDI(uint i, uint n)
    { unchecked {
  R[n] = unchecked((uint)(R[n] + (sbyte)i));
    } }

    private void ADDV(uint m, uint n)
    { unchecked {
  uint a = unchecked((uint)(R[n])); uint b = unchecked((uint)(R[m])); uint result = unchecked((uint)(unchecked(a + b)));
  SR.T = B(B(((~(a ^ b)) & (a ^ result) & 0x80000000u) != 0)); R[n] = unchecked((uint)(result));
    } }

    private void AND(uint m, uint n)
    { unchecked {
  R[n] &= R[m];

    } }

    private void ANDI(uint i)
    { unchecked {
  R[0] &= i;

    } }

    private void ANDM(uint i)
    { unchecked {
  byte b = unchecked((byte)(readByte(GBR + R[0])));
  writeByte(GBR + R[0], b & i);

    } }

    private void BF(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  if(SR.T == 0) {
    branch(PC + 4 + (sbyte)d * 2);
  }

    } }

    private void BFS(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  if(SR.T == 0) {
    delaySlot(PC + 4 + (sbyte)d * 2);
  }

    } }

    private void BRA(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  delaySlot(PC + 4 + (((int)d << 20) >> 20) * 2);
  // Gens SH2I_BRAfast1/2 and PicoDrive BUSY_LOOP_HACKS: the
  // BRA $ / NOP and NOP / BRA -3 / NOP loops have no data side effects.
  if (AccelerateIdleLoops && (d == 0xffe || d == 0xffd) && _bus.Fetch16(PC - 2) == 0x0009
      && (d == 0xffe || _bus.Fetch16(PC - 6) == 0x0009)) {
    int clocks = 3 + _bus.WaitStates(PC - 4, false) + _bus.WaitStates(PC - 2, false);
    if (d == 0xffd) clocks += 1 + _bus.WaitStates(PC - 6, false);
    uint loops = IdleIterations(uint.MaxValue, clocks);
    if (loops != 0) step((int)(loops * clocks));
  }

    } }

    private void BRAF(uint m)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  delaySlot(PC + 4 + R[m]);

    } }

    private void BSR(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  PR = PC;
  delaySlot(PC + 4 + (((int)d << 20) >> 20) * 2);

    } }

    private void BSRF(uint m)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  PR = PC;
  delaySlot(PC + 4 + R[m]);

    } }

    private void BT(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  if(SR.T == 1) {
    branch(PC + 4 + (sbyte)d * 2);
  }

    } }

    private void BTS(uint d)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  if(SR.T == 1) {
    delaySlot(PC + 4 + (sbyte)d * 2);
  }

    } }

    private void CLRMAC()
    { unchecked {
  MAC = 0;

    } }

    private void CLRT()
    { unchecked {
  SR.T = B(0);

    } }

    private void CMPEQ(uint m, uint n)
    { unchecked {
  SR.T = B(R[n] == R[m]);

    } }

    private void CMPGE(uint m, uint n)
    { unchecked {
  SR.T = B((int)R[n] >= (int)R[m]);

    } }

    private void CMPGT(uint m, uint n)
    { unchecked {
  SR.T = B((int)R[n] > (int)R[m]);

    } }

    private void CMPHI(uint m, uint n)
    { unchecked {
  SR.T = B(R[n] > R[m]);

    } }

    private void CMPHS(uint m, uint n)
    { unchecked {
  SR.T = B(R[n] >= R[m]);

    } }

    private void CMPIM(uint i)
    { unchecked {
  SR.T = B(R[0] == unchecked((uint)(sbyte)i));

    } }

    private void CMPPL(uint n)
    { unchecked {
  SR.T = B((int)R[n] > 0);

    } }

    private void CMPPZ(uint n)
    { unchecked {
  SR.T = B((int)R[n] >= 0);

    } }

    private void CMPSTR(uint m, uint n)
    { unchecked {
  uint t = unchecked((uint)(R[n] ^ R[m])); SR.T = B(B((t & 0xff000000u) == 0 || (t & 0xff0000u) == 0 || (t & 0xff00u) == 0 || (t & 255) == 0));
    } }

    private void DIV0S(uint m, uint n)
    { unchecked {
  SR.Q = B(R[n] >> 31);
  SR.M = B(R[m] >> 31);
  SR.T = B(SR.Q != SR.M);

    } }

    private void DIV0U()
    { unchecked {
  SR.Q = B(0);
  SR.M = B(0);
  SR.T = B(0);

    } }

    private void DIV1(uint m, uint n)
    { unchecked {
  uint q = unchecked((uint)(SR.Q));
  SR.Q = B(R[n] >> 31);
  long r = unchecked((uint)(R[n] << 1)) | SR.T;
  if(q == SR.M) {
    r -= R[m];
  } else {
    r += R[m];
  }
  R[n] = unchecked((uint)(r));
  SR.Q = B((SR.Q ^ SR.M) ^ (r >> 32 & 1));
  SR.T = B(1 - (SR.Q ^ SR.M));

    } }

    private void DMULS(uint m, uint n)
    { unchecked {
  MAC = (long)(int)R[n] * (int)R[m];

    } }

    private void DMULU(uint m, uint n)
    { unchecked {
  MAC = (long)R[n] * (long)R[m];

    } }

    private void DT(uint n)
    { unchecked {
  PollingEpoch++;
  SR.T = B(--R[n] == 0);
  // PicoDrive BUSY_LOOP_HACKS: coalesce DT Rn / BF $-2 while retaining count, T and clocks.
  if (AccelerateIdleLoops && R[n] > 1 && _bus.Fetch16(PC - 2) == 0x8bfd) {
    int clocks = 3 + _bus.WaitStates(PC - 4, false) + _bus.WaitStates(PC - 2, false);
    uint loops = IdleIterations(R[n] - 1, clocks);
    if (loops != 0) { R[n] -= loops; PollingEpoch += loops; PPC = PC; step((int)(loops * clocks)); }
  }

    } }

    private void EXTSB(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((sbyte)R[m]));

    } }

    private void EXTSW(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((short)R[m]));

    } }

    private void EXTUB(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((byte)R[m]));

    } }

    private void EXTUW(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((ushort)R[m]));

    } }

    private void ILLEGAL()
    { unchecked {
  illegalInstruction();

    } }

    private void JMP(uint m)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  delaySlot(R[m] + 4);

    } }

    private void JSR(uint m)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  PR = PC;
  delaySlot(R[m] + 4);

    } }

    private void LDCSR(uint m)
    { unchecked {
  SR = R[m];
  ID = 1;

    } }

    private void LDCGBR(uint m)
    { unchecked {
  GBR = R[m];
  ID = 1;

    } }

    private void LDCVBR(uint m)
    { unchecked {
  VBR = R[m];
  ID = 1;

    } }

    private void LDCMSR(uint m)
    { unchecked {
  SR = readLong(R[m]);
  R[m] += 4;
  ID = 1;

    } }

    private void LDCMGBR(uint m)
    { unchecked {
  GBR = readLong(R[m]);
  R[m] += 4;
  ID = 1;

    } }

    private void LDCMVBR(uint m)
    { unchecked {
  VBR = readLong(R[m]);
  R[m] += 4;
  ID = 1;

    } }

    private void LDSMACH(uint m)
    { unchecked {
  MACH = unchecked((uint)(R[m]));
  ID = 1;

    } }

    private void LDSMACL(uint m)
    { unchecked {
  MACL = unchecked((uint)(R[m]));
  ID = 1;

    } }

    private void LDSPR(uint m)
    { unchecked {
  PR = R[m];
  ID = 1;

    } }

    private void LDSMMACH(uint m)
    { unchecked {
  MACH = unchecked((uint)(readLong(R[m])));
  R[m] += 4;
  ID = 1;

    } }

    private void LDSMMACL(uint m)
    { unchecked {
  MACL = unchecked((uint)(readLong(R[m])));
  R[m] += 4;
  ID = 1;

    } }

    private void LDSMPR(uint m)
    { unchecked {
  PR = readLong(R[m]);
  R[m] += 4;
  ID = 1;

    } }

    private void MACW(uint m, uint n)
    { unchecked {
  short a = unchecked((short)((short)readWord(R[n]))); R[n] += 2;
  short b = unchecked((short)((short)readWord(R[m]))); R[m] += 2;
  if(SR.S == 0) MAC += (long)a * b;
  else { long p = (int)MACL + (long)a * b; if(p > int.MaxValue || p < int.MinValue) MACH |= 1; MACL = unchecked((uint)(unchecked((uint)Math.Clamp(p, int.MinValue, int.MaxValue)))); }
    } }

    private void MACL_(uint m, uint n)
    { unchecked {
  int a = (int)readLong(R[n]); R[n] += 4;
  int b = (int)readLong(R[m]); R[m] += 4;
  MAC = unchecked(MAC + (long)a * b);
  if(SR.S != 0) MAC = Math.Clamp(MAC, -(1L << 47), (1L << 47) - 1);
    } }

    private void MOV(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(R[m]));

    } }

    private void MOVBL(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((sbyte)readByte(R[m])));

    } }

    private void MOVBL0(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((sbyte)readByte(R[m] + R[0])));

    } }

    private void MOVBL4(uint m, uint d)
    { unchecked {
  R[0] = unchecked((uint)((sbyte)readByte(R[m] + d)));

    } }

    private void MOVBLG(uint d)
    { unchecked {
  R[0] = unchecked((uint)((sbyte)readByte(GBR + d)));

    } }

    private void MOVBM(uint m, uint n)
    { unchecked {
  uint t = unchecked((uint)(R[m]));
  R[n] -= 1;
  writeByte(R[n], t);

    } }

    private void MOVBP(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((sbyte)readByte(R[m])));
  if(n != m) R[m] += 1;

    } }

    private void MOVBS(uint m, uint n)
    { unchecked {
  writeByte(R[n], R[m]);

    } }

    private void MOVBS0(uint m, uint n)
    { unchecked {
  writeByte(R[n] + R[0], R[m]);

    } }

    private void MOVBS4(uint d, uint n)
    { unchecked {
  writeByte(R[n] + d, R[0]);

    } }

    private void MOVBSG(uint d)
    { unchecked {
  writeByte(GBR + d, R[0]);

    } }

    private void MOVWL(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((short)readWord(R[m])));

    } }

    private void MOVWL0(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((short)readWord(R[m] + R[0])));

    } }

    private void MOVWL4(uint m, uint d)
    { unchecked {
  R[0] = unchecked((uint)((short)readWord(R[m] + d * 2)));

    } }

    private void MOVWLG(uint d)
    { unchecked {
  R[0] = unchecked((uint)((short)readWord(GBR + d * 2)));

    } }

    private void MOVWM(uint m, uint n)
    { unchecked {
  uint t = unchecked((uint)(R[m]));
  R[n] -= 2;
  writeWord(R[n], t);

    } }

    private void MOVWP(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)((short)readWord(R[m])));
  if(n != m) R[m] += 2;

    } }

    private void MOVWS(uint m, uint n)
    { unchecked {
  writeWord(R[n], R[m]);

    } }

    private void MOVWS0(uint m, uint n)
    { unchecked {
  writeWord(R[n] + R[0], R[m]);

    } }

    private void MOVWS4(uint d, uint n)
    { unchecked {
  writeWord(R[n] + d * 2, R[0]);

    } }

    private void MOVWSG(uint d)
    { unchecked {
  writeWord(GBR + d * 2, R[0]);

    } }

    private void MOVLL(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(readLong(R[m])));

    } }

    private void MOVLL0(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(readLong(R[m] + R[0])));

    } }

    private void MOVLL4(uint m, uint d, uint n)
    { unchecked {
  R[n] = unchecked((uint)(readLong(R[m] + d * 4)));

    } }

    private void MOVLLG(uint d)
    { unchecked {
  R[0] = unchecked((uint)(readLong(GBR + d * 4)));

    } }

    private void MOVLM(uint m, uint n)
    { unchecked {
  uint t = unchecked((uint)(R[m]));
  R[n] -= 4;
  writeLong(R[n], t);

    } }

    private void MOVLP(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(readLong(R[m])));
  if(n != m) R[m] += 4;

    } }

    private void MOVLS(uint m, uint n)
    { unchecked {
  writeLong(R[n], R[m]);

    } }

    private void MOVLS0(uint m, uint n)
    { unchecked {
  writeLong(R[n] + R[0], R[m]);

    } }

    private void MOVLS4(uint m, uint d, uint n)
    { unchecked {
  writeLong(R[n] + d * 4, R[m]);

    } }

    private void MOVLSG(uint d)
    { unchecked {
  writeLong(GBR + d * 4, R[0]);

    } }

    private void MOVI(uint i, uint n)
    { unchecked {
  R[n] = unchecked((uint)((sbyte)i));

    } }

    private void MOVWI(uint d, uint n)
    { unchecked {
  uint pc = unchecked((uint)(inDelaySlot() ? PPC - 2 : PC));
  R[n] = unchecked((uint)((short)readWord(pc + d * 2)));

    } }

    private void MOVLI(uint d, uint n)
    { unchecked {
  uint pc = unchecked((uint)(inDelaySlot() ? PPC - 2 : PC));
  R[n] = unchecked((uint)(readLong((pc & ~3) + d * 4)));

    } }

    private void MOVA(uint d)
    { unchecked {
  uint pc = unchecked((uint)(inDelaySlot() ? PPC - 2 : PC));
  R[0] = unchecked((uint)((pc & ~3) + d * 4));

    } }

    private void MOVT(uint n)
    { unchecked {
  R[n] = unchecked((uint)(SR.T));

    } }

    private void MULL(uint m, uint n)
    { unchecked {
  MACL = unchecked((uint)(R[n] * R[m]));

    } }

    private void MULS(uint m, uint n)
    { unchecked {
  MACL = unchecked((uint)((short)R[n] * (short)R[m]));

    } }

    private void MULU(uint m, uint n)
    { unchecked {
  MACL = unchecked((uint)((ushort)R[n] * (ushort)R[m]));

    } }

    private void NEG(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(0 - R[m]));

    } }

    private void NEGC(uint m, uint n)
    { unchecked {
  uint t = unchecked((uint)(0 - R[m]));
  R[n] = unchecked((uint)(t - SR.T));
  SR.T = B(0 < t || t < R[n]);

    } }

    private void NOP()
    { unchecked {

    } }

    private void NOT(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(~R[m]));

    } }

    private void OR(uint m, uint n)
    { unchecked {
  R[n] |= R[m];

    } }

    private void ORI(uint i)
    { unchecked {
  R[0] |= i;

    } }

    private void ORM(uint i)
    { unchecked {
  byte b = unchecked((byte)(readByte(GBR + R[0])));
  writeByte(GBR + R[0], b | i);

    } }

    private void ROTCL(uint n)
    { unchecked {
  uint c = unchecked((uint)(R[n] >> 31));
  R[n] = unchecked((uint)(R[n] << 1 | SR.T));
  SR.T = B(c);

    } }

    private void ROTCR(uint n)
    { unchecked {
  uint c = unchecked((uint)(R[n] & 1));
  R[n] = unchecked((uint)(R[n] >> 1 | SR.T << 31));
  SR.T = B(c);

    } }

    private void ROTL(uint n)
    { unchecked {
  SR.T = B(R[n] >> 31);
  R[n] = unchecked((uint)(R[n] << 1 | R[n] >> 31));

    } }

    private void ROTR(uint n)
    { unchecked {
  SR.T = B(R[n] & 1);
  R[n] = unchecked((uint)(R[n] >> 1 | R[n] << 31));

    } }

    private void RTE()
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  delaySlot(readLong(SP + 0) + 4);
  SR  = readLong(SP + 4);
  SP += 8;

    } }

    private void RTS()
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  delaySlot(PR + 4);

    } }

    private void SETT()
    { unchecked {
  SR.T = B(1);

    } }

    private void SHAL(uint n)
    { unchecked {
  SR.T = B(R[n] >> 31);
  R[n] <<= 1;

    } }

    private void SHAR(uint n)
    { unchecked {
  SR.T = B(R[n] & 1);
  R[n] = unchecked((uint)((int)R[n] >> 1));

    } }

    private void SHLL(uint n)
    { unchecked {
  SR.T = B(R[n] >> 31);
  R[n] <<= 1;

    } }

    private void SHLL2(uint n)
    { unchecked {
  R[n] <<= 2;

    } }

    private void SHLL8(uint n)
    { unchecked {
  R[n] <<= 8;

    } }

    private void SHLL16(uint n)
    { unchecked {
  R[n] <<= 16;

    } }

    private void SHLR(uint n)
    { unchecked {
  SR.T = B(R[n] & 1);
  R[n] >>= 1;

    } }

    private void SHLR2(uint n)
    { unchecked {
  R[n] >>= 2;

    } }

    private void SHLR8(uint n)
    { unchecked {
  R[n] >>= 8;

    } }

    private void SHLR16(uint n)
    { unchecked {
  R[n] >>= 16;

    } }

    private void SLEEP()
    { unchecked {
  Sleeping = true;
    } }

    private void STCSR(uint n)
    { unchecked {
  R[n] = unchecked((uint)(SR));
  ID = 1;

    } }

    private void STCGBR(uint n)
    { unchecked {
  R[n] = unchecked((uint)(GBR));
  ID = 1;

    } }

    private void STCVBR(uint n)
    { unchecked {
  R[n] = unchecked((uint)(VBR));
  ID = 1;

    } }

    private void STCMSR(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], SR);
  ID = 1;

    } }

    private void STCMGBR(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], GBR);
  ID = 1;

    } }

    private void STCMVBR(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], VBR);
  ID = 1;

    } }

    private void STSMACH(uint n)
    { unchecked {
  R[n] = unchecked((uint)(MACH));
  ID = 1;

    } }

    private void STSMACL(uint n)
    { unchecked {
  R[n] = unchecked((uint)(MACL));
  ID = 1;

    } }

    private void STSPR(uint n)
    { unchecked {
  R[n] = unchecked((uint)(PR));
  ID = 1;

    } }

    private void STSMMACH(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], MACH);
  ID = 1;

    } }

    private void STSMMACL(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], MACL);
  ID = 1;

    } }

    private void STSMPR(uint n)
    { unchecked {
  R[n] -= 4;
  writeLong(R[n], PR);
  ID = 1;

    } }

    private void SUB(uint m, uint n)
    { unchecked {
  R[n] -= R[m];

    } }

    private void SUBC(uint m, uint n)
    { unchecked {
  uint a = unchecked((uint)(R[n]));
  uint b = unchecked((uint)(a - R[m]));
  R[n] = unchecked((uint)(b - SR.T));
  SR.T = B(a < b || b < R[n]);

    } }

    private void SUBV(uint m, uint n)
    { unchecked {
  uint a = unchecked((uint)(R[n])); uint b = unchecked((uint)(R[m])); uint result = unchecked((uint)(unchecked(a - b)));
  SR.T = B(B((((a ^ b)) & (a ^ result) & 0x80000000u) != 0)); R[n] = unchecked((uint)(result));
    } }

    private void SWAPB(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(R[m] & ~0xffff | R[m] >> 8 & 0x00ff | R[m] << 8 & 0xff00));

    } }

    private void SWAPW(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(R[m] << 16 | R[m] >> 16));

    } }

    private void TAS(uint n)
    { unchecked {
  uint data = unchecked((uint)(readByte(R[n])));
  SR.T = B(B(data == 0));
  writeByte(R[n], data | 0x80);
    } }

    private void TRAPA(uint i)
    { unchecked {
  if(inDelaySlot()) { illegalSlotInstruction(); return; }
  push(SR);
  push(PC - 2);
  branch(readLong(VBR + i * 4) + 4);

    } }

    private void TST(uint m, uint n)
    { unchecked {
  SR.T = B((R[n] & R[m]) == 0);

    } }

    private void TSTI(uint i)
    { unchecked {
  SR.T = B((R[0] & i) == 0);

    } }

    private void TSTM(uint i)
    { unchecked {
  byte b = unchecked((byte)(readByte(GBR + R[0])));
  SR.T = B((b & i) == 0);

    } }

    private void XOR(uint m, uint n)
    { unchecked {
  R[n] ^= R[m];

    } }

    private void XORI(uint i)
    { unchecked {
  R[0] ^= i;

    } }

    private void XORM(uint i)
    { unchecked {
  byte b = unchecked((byte)(readByte(GBR + R[0])));
  writeByte(GBR + R[0], b ^ i);

    } }

    private void XTRCT(uint m, uint n)
    { unchecked {
  R[n] = unchecked((uint)(R[n] >> 16 | R[m] << 16));

    } }
}
