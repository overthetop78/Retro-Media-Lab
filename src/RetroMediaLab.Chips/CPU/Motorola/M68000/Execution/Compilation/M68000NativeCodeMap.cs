namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>Guest-to-native address metadata adapted from BlastEm m68k_core.c,
/// tree ec73825ece2a134beb7825a5fd6fd21a398ff12e.</summary>
/// <remarks>Code pointers are borrowed. The code owner must clear this map before
/// releasing its executable arena. Alias normalization belongs to the machine bus.</remarks>
internal sealed class M68000NativeCodeMap(Func<uint, uint>? normalizeAlias = null)
{
    private const uint AddressMask = 0x00ffffff;
    private const int Invalid = -1, Extension = -2;
    private sealed class Slot(nint baseAddress)
    {
        public readonly nint BaseAddress = baseAddress;
        public readonly int[] Offsets = Enumerable.Repeat(Invalid, 256).ToArray();
    }
    private readonly Slot?[] _slots = new Slot?[65536];

    private uint Normalize(uint address) => (normalizeAlias?.Invoke(address & AddressMask) ?? address) & AddressMask;

    public nint GetNativeAddress(uint address)
    {
        address = Normalize(address);
        Slot? slot = _slots[address >> 8];
        if (slot is null) return 0;
        int offset = slot.Offsets[address & 255];
        return offset is Invalid or Extension ? 0 : slot.BaseAddress + offset;
    }

    public uint GetInstructionStart(uint address)
    {
        address = Normalize(address);
        Slot? slot = _slots[address >> 8];
        if (slot is null || slot.Offsets[address & 255] == Invalid) return 0;
        while (slot.Offsets[address & 255] == Extension)
        {
            address = (address - 1) & AddressMask;
            slot = _slots[address >> 8];
            if (slot is null || slot.Offsets[address & 255] == Invalid) return 0;
        }
        return address;
    }

    public void MapInstruction(uint address, nint nativeAddress, byte size)
    {
        if (nativeAddress == 0) throw new ArgumentNullException(nameof(nativeAddress));
        if (size == 0) throw new ArgumentOutOfRangeException(nameof(size));
        address = Normalize(address);
        Slot slot = _slots[address >> 8] ??= new(nativeAddress);
        int offset = checked((int)(nativeAddress - slot.BaseAddress));
        if (offset is Invalid or Extension) throw new ArgumentOutOfRangeException(nameof(nativeAddress));
        slot.Offsets[address & 255] = offset;
        for (int i = 1; i < size; i++)
        {
            uint next = (address + (uint)i) & AddressMask;
            Slot extensionSlot = _slots[next >> 8] ??= new(nativeAddress);
            // Like BlastEm, preserve an existing instruction entry in overlapping code.
            if (extensionSlot.Offsets[next & 255] == Invalid) extensionSlot.Offsets[next & 255] = Extension;
        }
    }

    public void Clear() => Array.Clear(_slots);
}
