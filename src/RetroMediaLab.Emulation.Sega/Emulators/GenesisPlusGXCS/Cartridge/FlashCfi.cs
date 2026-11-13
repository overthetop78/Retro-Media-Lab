using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class FlashCfi
{
    public const int M29W320Eb = 0, S29Gl064N04 = 1;
    public const int ContextSize = 12;
    private readonly BackupRam sram;
    private readonly CartridgeState cart;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly Action<uint, uint> m68k_unused_16_w;
    public FlashCfi(BackupRam backup, CartridgeState cartridge, CartridgeAddressSpace addresses, Action<uint, uint> unused16)
    {
        sram = backup; cart = cartridge; memory_map = addresses.Main; m68k_unused_16_w = unused16;
    }
    public void Initialize(int type, ReadOnlySpan<ushort> otpData)
    {
        if ((uint)type >= 2) throw new ArgumentOutOfRangeException(nameof(type));
        flash.addr = flash.data = 0;
        flash.mode = 0;
        flash.Type = type;
        flash.otp_area.AsSpan().Clear();
        flash.readmask = flash_readmask[type];
        if (!otpData.IsEmpty) otpData[..flash.otp_area.Length].CopyTo(flash.otp_area);
    }
    public void Write(uint address, uint data) => flash_cfi_write(address, data);
    public uint Read(uint address) => flash_cfi_read(address);
    private static void CopyMemory(CPointer<byte> destination, CPointer<byte> source, int length) => source.Bytes(length).CopyTo(destination.Bytes(length));
    private static void FillMemory(CPointer<byte> destination, int value, int length) => destination.Bytes(length).Fill(unchecked((byte)value));
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Flash context buffer is too short.", nameof(state));
        BinaryPrimitives.WriteUInt32LittleEndian(state, flash.addr);
        BinaryPrimitives.WriteUInt32LittleEndian(state[4..], flash.data);
        BinaryPrimitives.WriteInt32LittleEndian(state[8..], flash.mode);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Flash context buffer is too short.", nameof(state));
        flash.addr = BinaryPrimitives.ReadUInt32LittleEndian(state);
        flash.data = BinaryPrimitives.ReadUInt32LittleEndian(state[4..]);
        flash.mode = BinaryPrimitives.ReadInt32LittleEndian(state[8..]);
        return ContextSize;
    }
}
