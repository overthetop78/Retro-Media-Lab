using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.Memory.Flash.Cfi;
using FlashChip = RetroMediaLab.Chips.Memory.Flash.Cfi.FlashCfi;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed class FlashCfi : IFlashCfiBus
{
    public const int M29W320Eb = FlashChip.M29W320Eb, S29Gl064N04 = FlashChip.S29Gl064N04, ContextSize = FlashChip.ContextSize;
    private readonly BackupRam _backup;
    private readonly CartridgeState _cartridge;
    private readonly CartridgeMemoryBank[] _banks;
    private readonly Action<uint,uint> _unused;
    private readonly FlashChip _chip;
    public FlashCfi(BackupRam backup, CartridgeState cartridge, CartridgeAddressSpace addresses, Action<uint,uint> unused16)
    {
        _backup = backup; _cartridge = cartridge; _banks = addresses.Main; _unused = unused16; _chip = new(this);
    }
    public void Initialize(int type, ReadOnlySpan<ushort> otpData) => _chip.Initialize(type,otpData);
    public void Write(uint address, uint data) => _chip.Write(address,data);
    public uint Read(uint address) => _chip.Read(address);
    public int SaveContext(Span<byte> state) => _chip.SaveContext(state);
    public int LoadContext(ReadOnlySpan<byte> state) => _chip.LoadContext(state);
    ushort IFlashCfiBus.ReadWord(uint address)
    {
        CartridgeMemoryBank bank = _banks[address >> 15 & 255];
        int offset = (int)(address << 1 & 65534);
        return bank.@base.Same(_backup.sram) ? BinaryPrimitives.ReadUInt16BigEndian(_backup.sram.AsSpan(offset,2)) : (bank.@base + offset).Cast<ushort>()[0];
    }
    void IFlashCfiBus.ProgramWord(uint address, ushort value)
    {
        int index = (int)(address >> 15 & 255);
        if (index == 0) return;
        if (!_banks[index].@base.Same(_backup.sram))
        {
            for (int i = 127; i > 0; i--)
            {
                if (!_banks[i].@base.Same(_backup.sram)) continue;
                _banks[i].@base = new ArrayPointer<byte>(_cartridge.rom) + (i << 16);
                _backup.sram.AsSpan().CopyTo(_banks[i].@base.Bytes(65536)); break;
            }
            _banks[index].@base.Bytes(65536).CopyTo(_backup.sram);
            _banks[index].@base = new(_backup.sram);
        }
        BinaryPrimitives.WriteUInt16BigEndian(_backup.sram.AsSpan((int)(address << 1 & 65534),2),value);
    }
    void IFlashCfiBus.EraseChip()
    {
        for (int i = 1; i < 128; i++)
        {
            if (_banks[i].write16 == _unused) continue;
            _banks[i].@base.Bytes(65536).Fill(255); break;
        }
    }
    void IFlashCfiBus.EraseBlock(uint block) { if (block > 0) _banks[block].@base.Bytes(65536).Fill(255); }
}
