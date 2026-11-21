namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

internal sealed partial class Sh2X64Block
{
    internal static Sh2X64Block? TryCompileExecution(ReadOnlySpan<ushort> words, ReadOnlySpan<int> fetchWaits, bool deferred, bool linked = false, int linkedEntryWait = 0, bool timing = false, bool cachedReads = false, int[]? contextOffsets = null, int[]? idleClocks = null, bool mappedReads = false)
    {
        if (words.IsEmpty || words.Length != fetchWaits.Length || idleClocks is not null && idleClocks.Length != words.Length || !OperatingSystem.IsWindows()
            || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64) return null;
        var emitter = new Emitter(true, deferred, timing, cachedReads, contextOffsets, mappedReads);
        emitter.BeginRegion(words.Length, linked, linkedEntryWait);
        for (int i = 0; i < words.Length; i++)
        {
            emitter.MarkRegionEntry(i);
            emitter.BeginInstruction(i, fetchWaits[i]);
            emitter._idleLoopClocks = deferred && idleClocks is not null ? idleClocks[i] : 0;
            if (!emitter.Instruction(words[i])) return null;
            emitter.CompleteExecutionInstruction();
        }
        emitter.EndRegion();
        emitter.Finish();
        return new(emitter.Bytes.ToArray(), emitter.RequiresMemory, true, linked ? linkedEntryWait : -1, timing);
    }

    private sealed partial class Emitter
    {
        private Label[]? _regionEntries;
        private int _linkedEntryWait;
        internal void BeginRegion(int count, bool linked, int entryWait)
        {
            if (!linked) return;
            _regionDispatch = new Label();
            _linkedEntryWait = entryWait;
            _regionEntries = Enumerable.Range(0, count).Select(_ => new Label()).ToArray();
        }
        internal void MarkRegionEntry(int index)
        { if (_regionEntries is not null) Mark(_regionEntries[index]); }
        internal void EndRegion()
        {
            if (_regionEntries is not { } entries) return;
            Mark(_regionDispatch!);
            GuardExecutionBoundary();
            LoadS(0, Sh2X64Context.Pc); LoadS(8, Sh2X64Context.EntryPc); Reg(0x29, 0, 8);
            Group(7, 0, (uint)(entries.Length * 2)); Jump(_exit, 3);
            Move(8, 0); Group(4, 8, 1); Reg(0x85, 8, 8); Jump(_exit, 5);
            Shift(5, 0, 1); Move(8, 0);
            var otherEntry = new Label(); Group(7, 8, 0); Jump(otherEntry, 5);
            Constant(0, (uint)_linkedEntryWait); StoreS(Sh2X64Context.FetchWait); Mark(otherEntry);
            LoadS(0, Sh2X64Context.LinkedHops); Group(0, 0, 1); StoreS(Sh2X64Context.LinkedHops);
            // Constant-time dispatch within the already validated mapped region.
            // Relative entries keep the table independent of its executable address.
            Bytes.AddRange([0x4c, 0x8d, 0x15]); int relative = Bytes.Count; Int32(0); // LEA R10,[RIP+table]
            Bytes.AddRange([0x4f, 0x63, 0x1c, 0x82]); // MOVSXD R11,[R10+R8*4]
            Reg(0x01, 10, 11, true); Bytes.AddRange([0x41, 0xff, 0xe2]); // ADD R10,R11; JMP R10
            int table = Bytes.Count;
            byte[] displacement = BitConverter.GetBytes(table - relative - 4);
            for (int i = 0; i < 4; i++) Bytes[relative + i] = displacement[i];
            foreach (var entry in entries) Int32(entry.Position - table);
        }

        private void GuardExecutionBoundary()
        {
            Load(0, 2, Sh2X64Context.CyclesLow * 4, true); Load(8, 2, Sh2X64Context.EndLow * 4, true);
            Reg(0x39, 0, 8, true); Jump(_exit, 3);
            LoadS(0, Sh2X64Context.Branch); Reg(0x85, 0, 0);
            var noInterrupt = new Label(); Jump(noInterrupt, 5);
            LoadS(0, Sh2X64Context.IrqDelay); Reg(0x85, 0, 0); Jump(noInterrupt, 5);
            LoadS(0, Sh2X64Context.IrqLevel); Group(7, 0, 16); Jump(_exit, 4);
            LoadS(8, Sh2X64Context.Status); Shift(5, 8, 4); Group(4, 8, 15);
            Reg(0x39, 0, 8); Jump(_exit, 7); Mark(noInterrupt);
        }
        // R8D carries a positive SH-2 clock count. A deferred bus can stay in
        // native code until its next event; immediate buses receive every step.
        private void AdvanceExecutionClock()
        {
            Load(0, 2, Sh2X64Context.CyclesLow * 4, true); Reg(0x01, 0, 8, true);
            StoreS(Sh2X64Context.CyclesLow, 0, true);
            if (_deferred)
            {
                LoadS(9, Sh2X64Context.PendingClocks); Reg(0x01, 9, 8); StoreS(Sh2X64Context.PendingClocks, 9);
                Load(9, 2, Sh2X64Context.BusDeadlineLow * 4, true); Reg(0x39, 0, 9, true);
                var within = new Label(); Jump(within, 2); // cycles < next event
                Constant(0, 0); Access(0x210); Mark(within);
            }
            else { Constant(0, 0); Access(0x211); }
        }

        internal void BeginInstruction(int index, int wait)
        {
            _instructionEnd = new Label();
            LoadS(0, Sh2X64Context.EntryPc); if (index != 0) Group(0, 0, (uint)(index * 2));
            LoadS(8, Sh2X64Context.Pc); Reg(0x39, 8, 0); Jump(_regionDispatch ?? _exit, 5);
            if (index != 0) GuardExecutionBoundary();
            Constant(0, 0); StoreS(Sh2X64Context.IrqDelay);
            if (index == 0) { LoadS(8, Sh2X64Context.FetchWait); Group(0, 8, 1); }
            else Constant(8, checked((uint)(wait + 1)));
            AdvanceExecutionClock();
        }

        internal void CompleteExecutionInstruction()
        {
            Mark(_instructionEnd!); _instructionEnd = null;
            LoadS(0, Sh2X64Context.Branch); Group(7, 0, 2);
            var jump = new Label(); var delay = new Label(); var complete = new Label(); Jump(jump, 4);
            Group(7, 0, 1); Jump(delay, 4);
            LoadS(0, Sh2X64Context.Pc); Group(0, 0, 2); StoreS(Sh2X64Context.Pc); Jump(complete);
            Mark(delay); LoadS(0, Sh2X64Context.Pc); Group(0, 0, 2); StoreS(Sh2X64Context.Pc);
            Constant(0, 2); StoreS(Sh2X64Context.Branch); Jump(complete);
            Mark(jump); LoadS(0, Sh2X64Context.PreviousPc); StoreS(Sh2X64Context.Pc);
            Constant(0, 0); StoreS(Sh2X64Context.Branch); Mark(complete);
            LoadS(0, Sh2X64Context.Executed); Group(0, 0, 1); StoreS(Sh2X64Context.Executed);
            var notYielded = new Label(); LoadS(0, Sh2X64Context.Yield); Reg(0x85, 0, 0); Jump(notYielded, 4);
            Constant(0, 0); StoreS(Sh2X64Context.Yield); Constant(8, 0); Access(0x250); Jump(_exit); Mark(notYielded);
            LoadS(0, Sh2X64Context.CodeChanged); Reg(0x85, 0, 0); Jump(_exit, 5);
            LoadS(0, Sh2X64Context.Sleeping); Reg(0x85, 0, 0); Jump(_exit, 5);
        }
    }
}
