using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class ActionReplay : IDisposable
{
    public const int Off = 0, On = 1, Trainer = 2;
    private const byte TypeAr = 2, TypePro1 = 0x12, TypePro2 = 0x22;
    private readonly CartridgeState cart;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly Action<uint, uint> m68k_unused_16_w;
    public int Status { get => areplay_get_status(); set => areplay_set_status(value); }
    public ActionReplay(CartridgeState cartridge, CartridgeAddressSpace addresses, Action<uint, uint> unused16)
    {
        cart = cartridge; memory_map = addresses.Main; m68k_unused_16_w = unused16;
    }
    public void Initialize(ReadOnlySpan<byte> rom)
    {
        action_replay.enabled = action_replay.status = 0;
        if (rom.IsEmpty) return;
        rom[..Math.Min(rom.Length, 0x10000)].CopyTo(cart.lockrom);
        if (cart.lockrom.AsSpan(0x120, 16).SequenceEqual("ACTION REPLAY   "u8))
        {
            action_replay.enabled = TypeAr;
            cart.lockrom.AsSpan(0, 0x8000).CopyTo(cart.lockrom.AsSpan(0x8000));
            memory_map[1].write16 = ar_write_regs;
        }
        else
        {
            byte sp = cart.lockrom[1];
            if (sp == 0x42 && cart.lockrom.AsSpan(0x120, 16).SequenceEqual("ACTION REPLAY 2 "u8))
            {
                action_replay.enabled = TypePro1;
                memory_map[1].write16 = ar_write_regs;
            }
            else if (sp == 0x60 && cart.lockrom.AsSpan(0x3c6, 16).SequenceEqual("ACTION REPLAY II"u8))
            {
                action_replay.enabled = TypePro2;
                memory_map[0x10].write16 = ar2_write_reg;
            }
            if (action_replay.enabled != 0)
            {
                memory_map[sp].Data = action_replay.ram;
                memory_map[sp].read8 = memory_map[sp].read16 = null;
                memory_map[sp].write8 = ar_write_ram_8;
                memory_map[sp].write16 = null;
            }
        }
        if (action_replay.enabled != 0)
            for (int i = 0; i < 0x10000; i += 2)
                (cart.lockrom[i], cart.lockrom[i + 1]) = (cart.lockrom[i + 1], cart.lockrom[i]);
    }
    private static void FillMemory(ArrayPointer<byte> memory, int value, int size) => memory.Bytes(size).Fill(unchecked((byte)value));
    public void Reset(bool hard) => areplay_reset(hard ? 1 : 0);
    public void Dispose() => areplay_shutdown();
}
