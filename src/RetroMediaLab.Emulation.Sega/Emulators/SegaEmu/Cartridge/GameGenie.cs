namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class GameGenie : IDisposable
{
    private readonly CartridgeState cart;
    internal byte[] Firmware { get; } = new byte[0x10000];
    internal Func<bool>? PassThrough { get; set; }
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly Action<uint, uint> m68k_unused_8_w, m68k_unused_16_w;
    private bool _patchesApplied;
    public bool Enabled => ggenie.enabled != 0;
    public GameGenie(CartridgeState cartridge, CartridgeAddressSpace addresses, Action<uint, uint> unused8, Action<uint, uint> unused16)
    {
        cart = cartridge; memory_map = addresses.Main; m68k_unused_8_w = unused8; m68k_unused_16_w = unused16;
    }
    public void Initialize(ReadOnlySpan<byte> rom)
    {
        ggenie.enabled = 0;
        if (rom.IsEmpty) return;
        int size = Math.Min(rom.Length, 0x8000);
        rom[..size].CopyTo(Firmware);
        for (int i = 0; i < 0x8000; i += 2)
            (Firmware[i], Firmware[i + 1]) = (Firmware[i + 1], Firmware[i]);
        Firmware.AsSpan(0, 0x8000).CopyTo(Firmware.AsSpan(0x8000));
        ggenie.enabled = 1;
    }
    public void Reset(bool hard) => ggenie_reset(hard ? 1 : 0);
    public void Switch(bool enabled) => ggenie_switch(enabled ? 1 : 0);
    public void Dispose() => ggenie_shutdown();
}
