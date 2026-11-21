namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

internal sealed partial class Sh2X64Block
{
    private sealed partial class Emitter
    {
        private void Step(uint clocks)
        {
            if (_execution) { Constant(8, clocks); AdvanceExecutionClock(); }
            else { Constant(0, 0); Constant(8, clocks); Access(0x200); }
        }
        private void BranchToAccumulator(bool delayed)
        {
            StoreS(Sh2X64Context.PreviousPc);
            Constant(0, delayed ? 1u : 2u); StoreS(Sh2X64Context.Branch); Step(1);
        }
        private void Push(int slot, uint subtract = 0)
        {
            LoadR(0, 15); Group(5, 0, 4); StoreR(15);
            LoadS(8, slot); if (subtract != 0) Group(5, 8, subtract); Write(4);
        }
        private void IllegalSlot()
        {
            Push(Sh2X64Context.Status); Push(Sh2X64Context.PreviousPc, 4);
            LoadS(0, Sh2X64Context.VectorBase); Group(0, 0, 24); Read(4); Group(0, 0, 4);
            StoreS(Sh2X64Context.PreviousPc); Constant(0, 2); StoreS(Sh2X64Context.Branch);
        }
        // Returns the branch-around label for the normal instruction path.
        private Label BeginBranch()
        {
            var normal = new Label(); LoadS(0, Sh2X64Context.Branch); Group(7, 0, 2); Jump(normal, 5);
            IllegalSlot(); Jump(_instructionEnd ?? _exit); Mark(normal); return normal;
        }
        private bool ControlInstruction(ushort opcode)
        {
            if (opcode == 0x001b)
            { Constant(0, 1); StoreS(Sh2X64Context.Sleeping); return true; }
            int top = opcode >> 12;
            if (top is 10 or 11)
            {
                BeginBranch();
                if (top == 11) { LoadS(0, Sh2X64Context.Pc); StoreS(Sh2X64Context.Procedure); }
                int displacement = ((opcode & 0xfff) << 20) >> 20;
                LoadS(0, Sh2X64Context.Pc); Group(0, 0, unchecked((uint)(4 + displacement * 2))); BranchToAccumulator(true);
                if (top == 10 && (opcode & 0xfff) is 0xffe or 0xffd)
                { IdleCallback(0x400000u | (uint)(opcode & 0xfff)); }
                return true;
            }
            int operation = opcode >> 8;
            if (operation is 0x89 or 0x8b or 0x8d or 0x8f)
            {
                BeginBranch(); var notTaken = new Label(); LoadS(0, Sh2X64Context.Status); Group(4, 0, 1);
                Jump(notTaken, operation is 0x89 or 0x8d ? (byte)4 : (byte)5);
                LoadS(0, Sh2X64Context.Pc); Group(0, 0, unchecked((uint)(4 + (sbyte)opcode * 2)));
                BranchToAccumulator(operation is 0x8d or 0x8f);
                Mark(notTaken); return true;
            }
            if (operation == 0xc3)
            {
                BeginBranch(); Push(Sh2X64Context.Status); Push(Sh2X64Context.Pc, 2);
                LoadS(0, Sh2X64Context.VectorBase); Group(0, 0, (uint)((byte)opcode * 4)); Read(4); Group(0, 0, 4);
                BranchToAccumulator(false); return true;
            }
            int m = opcode >> 8 & 15;
            switch (opcode & 0xf0ff)
            {
                case 0x0003: case 0x0023:
                    BeginBranch();
                    if ((opcode & 0xff) == 3) { LoadS(0, Sh2X64Context.Pc); StoreS(Sh2X64Context.Procedure); }
                    LoadS(0, Sh2X64Context.Pc); LoadR(8, m); Reg(0x01, 0, 8); Group(0, 0, 4); BranchToAccumulator(true); return true;
                case 0x400b: case 0x402b:
                    BeginBranch();
                    if ((opcode & 0xff) == 0x0b) { LoadS(0, Sh2X64Context.Pc); StoreS(Sh2X64Context.Procedure); }
                    LoadR(0, m); Group(0, 0, 4); BranchToAccumulator(true); return true;
            }
            if (opcode == 0x000b)
            { BeginBranch(); LoadS(0, Sh2X64Context.Procedure); Group(0, 0, 4); BranchToAccumulator(true); return true; }
            if (opcode == 0x002b)
            {
                BeginBranch(); LoadR(0, 15); Read(4); Group(0, 0, 4); BranchToAccumulator(true);
                LoadR(0, 15); Group(0, 0, 4); Read(4); Group(4, 0, 0x3f3); StoreS(Sh2X64Context.Status); Increment(15, 8); return true;
            }
            if (Sh2OpcodeTable.Types[opcode] != 142) return false;
            BeginBranch(); Push(Sh2X64Context.Status); Push(Sh2X64Context.Pc, 4);
            LoadS(0, Sh2X64Context.VectorBase); Group(0, 0, 16); Read(4); Group(0, 0, 4);
            StoreS(Sh2X64Context.Pc); StoreS(Sh2X64Context.PreviousPc); Constant(0, 2); StoreS(Sh2X64Context.Branch); return true;
        }
    }
}
