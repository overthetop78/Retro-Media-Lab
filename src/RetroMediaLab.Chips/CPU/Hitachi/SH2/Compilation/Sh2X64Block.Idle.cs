namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

internal sealed partial class Sh2X64Block
{
    private sealed partial class Emitter
    {
        internal int _idleLoopClocks;

        private void IdleCallback(uint kind, int register = -1)
        {
            if (_idleLoopClocks == 0)
            { Constant(0, 0); Constant(8, 0); Access(kind); return; }

            var done = new Label(); var changed = new Label();
            // An event in the opcode/branch step may have replaced a future
            // word. Re-read through the original helper in that case.
            LoadS(0, Sh2X64Context.CodeChanged); Reg(0x85, 0, 0); Jump(changed, 5);
            LoadS(0, Sh2X64Context.IdleEnabled); Reg(0x85, 0, 0); Jump(done, 4);
            if (register >= 0) { LoadR(0, register); Group(7, 0, 1); Jump(done, 6); }
            // IdleIterations rejects an actionable interrupt even in a delay slot.
            LoadS(0, Sh2X64Context.IrqLevel); Group(7, 0, 16); Jump(done, 4);
            LoadS(8, Sh2X64Context.Status); Shift(5, 8, 4); Group(4, 8, 15);
            Reg(0x39, 0, 8); Jump(done, 7);

            Load(0, 2, Sh2X64Context.EndLow * 4, true);
            Load(8, 2, Sh2X64Context.BusDeadlineLow * 4, true);
            Reg(0x39, 0, 8, true);
            var endEarlier = new Label(); Jump(endEarlier, 6); Move(0, 8, true); Mark(endEarlier);
            Load(9, 2, Sh2X64Context.CyclesLow * 4, true);
            Reg(0x39, 0, 9, true); Jump(done, 6); Reg(0x29, 0, 9, true);
            // Unsigned quotient, then cap exactly like IdleIterations(maximum).
            // RDX is restored before accessing the context again.
            Constant(8, (uint)_idleLoopClocks); Reg(0x31, 2, 2);
            Bytes.AddRange([0x49, 0xf7, 0xf0]); // DIV R8
            Move(1, 6, true); Move(2, 7, true);
            if (register >= 0) { LoadR(8, register); Group(5, 8, 1); }
            else Constant(8, uint.MaxValue);
            Reg(0x39, 0, 8, true);
            var withinMaximum = new Label(); Jump(withinMaximum, 6); Move(0, 8, true); Mark(withinMaximum);
            Reg(0x85, 0, 0); Jump(done, 4);
            if (register >= 0)
            {
                LoadR(8, register); Reg(0x29, 8, 0); StoreR(register, 8);
                LoadS(8, Sh2X64Context.PollingEpoch); Reg(0x01, 8, 0); StoreS(Sh2X64Context.PollingEpoch, 8);
                LoadS(8, Sh2X64Context.Pc); StoreS(Sh2X64Context.PreviousPc, 8);
            }
            // Preserve the existing uint multiplication and step(int) semantics.
            Move(8, 0); Bytes.AddRange([0x45, 0x69, 0xc0]); Int32(_idleLoopClocks); // IMUL R8D,R8D,imm32
            AdvanceExecutionClock(); Jump(done);
            Mark(changed); Constant(0, 0); Constant(8, 0); Access(kind); Mark(done);
        }
    }
}
