namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

internal sealed partial class Sh2X64Block
{
    private sealed partial class Emitter
    {
        // Address in EAX, write data in R8D. The borrowed descriptor contains
        // a callback pointer and an optional borrowed diagnostic area.
        private void Access(uint kind)
        {
            RequiresMemory = true;
            if (_timing) { Store(4, 40, 0); StartTiming(Sh2NativeTimingLayout.CallbackStart); Load(0, 4, 40); }
            Move(2, 0); Move(1, 7, true); Constant(9, kind);
            Bytes.AddRange([0xff, 0x13]); // CALL [RBX]
            Move(1, 6, true); Move(2, 7, true);
            if (_timing) { Store(4, 40, 0); EndTiming(Sh2NativeTimingLayout.CallbackStart, Sh2NativeTimingLayout.CallbackTicks, Sh2NativeTimingLayout.CallbackCalls, Sh2NativeTimingLayout.Category(kind)); Load(0, 4, 40); }
            LoadS(10, Sh2X64Context.CallbackError); Reg(0x85, 10, 10); Jump(_exit, 5);
        }
        private void Read(int size, bool sign = true)
        {
            ReadFromBus(size);
            if (size != 4 && sign) Extend(0, true, size == 2);
        }

        private void ReadFromBus(int size)
        {
            if (_mappedReads) MappedRead(size);
            else if (_cachedReads) CachedRead(size); else Access((uint)size);
        }
        private void IndexedLoad(int destination, int pointer, int index, int scale, int size)
        {
            byte rex = (byte)((size == 8 ? 0x48 : 0x40) | ((destination >> 3) << 2) | ((index >> 3) << 1) | (pointer >> 3));
            if (rex != 0x40) Bytes.Add(rex);
            if (size is 4 or 8) Bytes.Add(0x8b); else Bytes.AddRange([0x0f, (byte)(size == 1 ? 0xb6 : 0xb7)]);
            Bytes.Add((byte)(((destination & 7) << 3) | 4));
            Bytes.Add((byte)((scale << 6) | ((index & 7) << 3) | (pointer & 7)));
        }
        private void NarrowLoad(int destination, int pointer, int offset)
        {
            Rex(destination, pointer); Bytes.AddRange([0x0f, 0xb6]);
            Bytes.Add((byte)(0x80 | ((destination & 7) << 3) | (pointer & 7))); Int32(offset);
        }
        private void MappedRead(int size)
        {
            // Yabause assem_x64.c do_map_r: index a physical page table, then
            // load directly from the mapped backing. Our arrays are big-endian
            // bytes, rather than Yabause's host-word-swapped T2 memory.
            var fallback = new Label(); var physical = new Label(); var done = new Label();
            Store(4, 40, 0);
            Load(11, 3, 56, true); Reg(0x85, 11, 11, true); Jump(fallback, 4);
            Group(7, 0, 0x40000000); Jump(fallback, 3);
            Load(9, 2, Sh2X64Context.CyclesLow * 4, true);
            Load(10, 2, Sh2X64Context.BusDeadlineLow * 4, true);
            Reg(0x39, 9, 10, true); Jump(fallback, 3);

            // P1 bypasses the hardware cache. P0 can bypass the controller only
            // when its data cache is disabled or its owner selects direct maps.
            Group(7, 0, 0x20000000); Jump(physical, 3);
            Load(11, 3, 64, true); Reg(0x85, 11, 11, true); Jump(physical, 4);
            Load(9, 3, 72, true); NarrowLoad(9, 9, 0); Reg(0x85, 9, 9); Jump(physical, 4);
            NarrowLoad(9, 11, 0x92); Group(4, 9, 5); Group(7, 9, 1); Jump(fallback, 4);

            Mark(physical);
            Move(9, 0); Group(4, 9, 0x1fffffff); Shift(5, 9, 12);
            Load(11, 3, 56, true); IndexedLoad(11, 11, 9, 3, 8);
            Reg(0x85, 11, 11, true); Jump(fallback, 4);
            Group(4, 0, 4095); Group(7, 0, (uint)(4096 - size)); Jump(fallback, 7);
            IndexedLoad(0, 11, 0, 0, size);
            if (size == 2) Bytes.AddRange([0x66, 0xc1, 0xc0, 8]);
            else if (size == 4) Bytes.AddRange([0x0f, 0xc8]);
            if (_timing) { Load(11, 3, 8, true); AddTimingCount(Sh2NativeTimingLayout.MappedReadHits); }
            Move(1, 6, true); Move(2, 7, true); Jump(done);
            Mark(fallback); Load(0, 4, 40); Move(1, 6, true); Move(2, 7, true);
            if (_cachedReads) CachedRead(size); else Access((uint)size);
            Mark(done);
        }
        private void CachedRead(int size)
        {
            // The 40-byte scratch slot is separate from MAC's live operand at +32.
            var fallback = new Label(); var ram = new Label(); var pure = new Label();
            var found = new Label(); var read = new Label(); var done = new Label();
            Store(4, 40, 0);
            Load(11, 3, 16, true); Reg(0x85, 11, 11, true); Jump(fallback, 4);
            Load(9, 3, 48, true); NarrowLoad(9, 9, 0); Reg(0x85, 9, 9); Jump(fallback, 4);
            // A zero-wait pure hit cannot itself reach an event. A due event is
            // handled through the normal callback, including step(0).
            Load(9, 2, Sh2X64Context.CyclesLow * 4, true); Load(10, 2, Sh2X64Context.BusDeadlineLow * 4, true);
            Reg(0x39, 9, 10, true); Jump(fallback, 3);
            Move(9, 0); Shift(5, 9, 29); Group(7, 9, 6); Jump(ram, 4);
            Group(7, 0, 0x20000000); Jump(fallback, 3); // Uncached aliases and IO stay on the bus.
            Group(7, 0, 0x4000); Jump(pure, 2);
            Group(7, 0, 0x02000000); Jump(fallback, 2);
            Group(7, 0, 0x06040000); Jump(fallback, 3);
            Mark(pure);
            Load(9, 3, 40, true); NarrowLoad(9, 9, 0x92); Group(4, 9, 5); Group(7, 9, 1); Jump(fallback, 5);
            Move(9, 0); Group(4, 9, 15); Group(7, 9, (uint)(16 - size)); Jump(fallback, 7);
            Move(8, 0); Shift(5, 8, 4); Group(4, 8, 63);
            Move(9, 0); Shift(5, 9, 10);
            Load(10, 3, 32, true); Load(11, 3, 24, true);
            for (int way = 3; way >= 0; way--)
            {
                var next = new Label(); Move(0, 8); if (way != 0) Group(0, 0, (uint)(way * 64));
                IndexedLoad(1, 10, 0, 0, 1); Reg(0x85, 1, 1); Jump(next, 4);
                IndexedLoad(1, 11, 0, 2, 4); Reg(0x39, 1, 9); Jump(found, 4); Mark(next);
                if (way == 2)
                {
                    Load(1, 3, 40, true); NarrowLoad(1, 1, 0x92); Group(4, 1, 8); Reg(0x85, 1, 1); Jump(fallback, 5);
                }
            }
            Jump(fallback);
            Mark(found); Shift(4, 0, 4); Load(1, 4, 40); Group(4, 1, 15); Reg(0x01, 0, 1);
            Load(11, 3, 16, true); Jump(read);
            Mark(ram); Group(4, 0, (uint)(4096 - size)); // Cache RAM aligns words/longs down, matching SH7095.
            Mark(read); IndexedLoad(0, 11, 0, 0, size);
            if (size == 2) Bytes.AddRange([0x66, 0xc1, 0xc0, 8]); // ROL AX,8
            else if (size == 4) Bytes.AddRange([0x0f, 0xc8]); // BSWAP EAX
            if (_timing) { Load(11, 3, 8, true); AddTimingCount(Sh2NativeTimingLayout.CachedReadHits); }
            Move(1, 6, true); Move(2, 7, true); Jump(done);
            Mark(fallback); Load(0, 4, 40); Move(1, 6, true); Move(2, 7, true); Access((uint)size);
            Mark(done);
        }
        private void Write(int size) => Access(0x100u | (uint)size);
        private void Increment(int guest, uint amount)
        { LoadR(8, guest); Group(0, 8, amount); StoreR(guest, 8); }
        private void GlobalAddress(uint displacement)
        { LoadS(0, Sh2X64Context.GlobalBase); Group(0, 0, displacement); }
        private void IndexedGlobalAddress()
        { LoadS(0, Sh2X64Context.GlobalBase); LoadR(9, 0); Reg(0x01, 0, 9); }
        private void RelativePc()
        {
            var plain = new Label(); LoadS(0, Sh2X64Context.Pc); LoadS(10, Sh2X64Context.Branch); Group(7, 10, 2); Jump(plain, 5);
            LoadS(0, Sh2X64Context.PreviousPc); Group(5, 0, 2); Mark(plain);
        }
        private void IrqDelay() { Constant(0, 1); StoreS(Sh2X64Context.IrqDelay); }
        private void Constant64(int host, ulong value)
        { Rex(0, host, true); Bytes.Add((byte)(0xb8 | (host & 7))); Bytes.AddRange(BitConverter.GetBytes(value)); }
        private void StackTemporary(bool store)
        { Bytes.AddRange(store ? [0x48, 0x89, 0x44, 0x24, 0x20] : new byte[] { 0x4c, 0x8b, 0x44, 0x24, 0x20 }); }

        private bool MemoryInstruction(ushort opcode)
        {
            int n = opcode >> 8 & 15, m = opcode >> 4 & 15, displacement = opcode & 15;
            int top = opcode >> 12;
            if (top is 1 or 5)
            {
                if (top == 1) { LoadR(8, m); LoadR(0, n); Group(0, 0, (uint)(displacement * 4)); Write(4); }
                else { LoadR(0, m); Group(0, 0, (uint)(displacement * 4)); Read(4); StoreR(n); }
                return true;
            }
            if (top is 9 or 13)
            {
                RelativePc(); if (top == 13) Group(4, 0, 0xfffffffc);
                Group(0, 0, (uint)((byte)opcode * (top == 9 ? 2 : 4))); Read(top == 9 ? 2 : 4); StoreR(n); return true;
            }
            int operation = opcode >> 8;
            if (operation is 0x80 or 0x81 or 0x84 or 0x85)
            {
                int size = (operation & 1) + 1;
                if (operation < 0x84)
                { LoadR(8, 0); LoadR(0, m); Group(0, 0, (uint)(displacement * size)); Write(size); }
                else { LoadR(0, m); Group(0, 0, (uint)(displacement * size)); Read(size); StoreR(0); }
                return true;
            }
            if (operation is >= 0xc0 and <= 0xc7 && operation != 0xc3)
            {
                if (operation == 0xc7)
                { RelativePc(); Group(4, 0, 0xfffffffc); Group(0, 0, (uint)((byte)opcode * 4)); StoreR(0); return true; }
                int size = 1 << (operation & 3);
                if (operation < 0xc4) { LoadR(8, 0); GlobalAddress((uint)((byte)opcode * size)); Write(size); }
                else { GlobalAddress((uint)((byte)opcode * size)); Read(size); StoreR(0); }
                return true;
            }
            if (operation is >= 0xcc and <= 0xcf)
            {
                IndexedGlobalAddress(); Read(1, false);
                Group(operation switch { 0xcc or 0xcd => 4, 0xce => 6, _ => 1 }, 0, (byte)opcode);
                if (operation == 0xcc) { Reg(0x85, 0, 0); T(4); }
                else { Move(8, 0); IndexedGlobalAddress(); Write(1); }
                return true;
            }
            operation = opcode & 0xf00f;
            switch (operation)
            {
                case 0x2000: case 0x2001: case 0x2002:
                    LoadR(8, m); LoadR(0, n); Write(1 << (operation & 3)); return true;
                case 0x2004: case 0x2005: case 0x2006:
                    int size = 1 << (operation & 3);
                    LoadR(8, m); LoadR(0, n); Group(5, 0, (uint)size); StoreR(n); Write(size); return true;
                case 0x0004: case 0x0005: case 0x0006:
                    LoadR(8, m); LoadR(0, n); LoadR(9, 0); Reg(0x01, 0, 9); Write(1 << (operation & 3)); return true;
                case 0x6000: case 0x6001: case 0x6002:
                    LoadR(0, m); Read(1 << (operation & 3)); StoreR(n); return true;
                case 0x6004: case 0x6005: case 0x6006:
                    size = 1 << (operation & 3);
                    LoadR(0, m); Read(size); StoreR(n); if (m != n) Increment(m, (uint)size); return true;
                case 0x000c: case 0x000d: case 0x000e:
                    LoadR(0, m); LoadR(9, 0); Reg(0x01, 0, 9); Read(1 << (operation & 3)); StoreR(n); return true;
                case 0x000f: MultiplyAccumulate(n, m, false); return true;
                case 0x400f: MultiplyAccumulate(n, m, true); return true;
            }
            operation = opcode & 0xf0ff;
            switch (operation)
            {
                case 0x401b:
                    LoadR(0, n); Read(1, false); Reg(0x85, 0, 0); T(4); Group(1, 0, 0x80);
                    Move(8, 0); LoadR(0, n); Write(1); return true;
                case 0x4002: case 0x4012: case 0x4022: case 0x4003: case 0x4013: case 0x4023:
                    LoadR(0, n); Group(5, 0, 4); StoreR(n);
                    LoadS(8, operation switch { 0x4002 => 5, 0x4012 => 4, 0x4022 => 3, 0x4003 => 0, 0x4013 => 1, _ => 2 });
                    Write(4); IrqDelay(); return true;
                case 0x4006: case 0x4016: case 0x4026: case 0x4007: case 0x4017: case 0x4027:
                    LoadR(0, n); Read(4); if (operation == 0x4007) Group(4, 0, 0x3f3);
                    StoreS(operation switch { 0x4007 => 0, 0x4017 => 1, 0x4027 => 2, 0x4006 => 5, 0x4016 => 4, _ => 3 });
                    Increment(n, 4); IrqDelay(); return true;
            }
            return ControlInstruction(opcode);
        }

        private void MultiplyAccumulate(int n, int m, bool word)
        {
            int size = word ? 2 : 4;
            LoadR(0, n); Read(size); SignExtend64(0); StackTemporary(true); Increment(n, (uint)size);
            LoadR(0, m); Read(size); SignExtend64(0); Increment(m, (uint)size); StackTemporary(false); Multiply(true);
            LoadS(9, Sh2X64Context.Status); Group(4, 9, 2);
            var saturated = new Label(); var done = new Label(); Jump(saturated, 5);
            Load(8, 2, Sh2X64Context.MacLow * 4, true); Reg(0x01, 0, 8, true); StoreS(Sh2X64Context.MacLow, 0, true); Jump(done);
            Mark(saturated);
            if (word) { LoadS(8, Sh2X64Context.MacLow); SignExtend64(8); }
            else Load(8, 2, Sh2X64Context.MacLow * 4, true);
            Reg(0x01, 0, 8, true);
            long minimum = word ? int.MinValue : -(1L << 47), maximum = word ? int.MaxValue : (1L << 47) - 1;
            var upper = new Label(); var store = new Label(); Constant64(8, unchecked((ulong)minimum)); Reg(0x39, 0, 8, true); Jump(upper, 13);
            Move(0, 8, true); if (word) MarkMacOverflow(); Jump(store); Mark(upper);
            Constant64(8, (ulong)maximum); Reg(0x39, 0, 8, true); Jump(store, 14);
            Move(0, 8, true); if (word) MarkMacOverflow(); Mark(store);
            StoreS(Sh2X64Context.MacLow, 0, !word); Mark(done);
        }
        private void MarkMacOverflow()
        { LoadS(9, Sh2X64Context.MacHigh); Group(1, 9, 1); StoreS(Sh2X64Context.MacHigh, 9); }
    }
}
