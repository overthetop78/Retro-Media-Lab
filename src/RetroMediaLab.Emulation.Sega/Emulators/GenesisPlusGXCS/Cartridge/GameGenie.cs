namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class GameGenie : IDisposable
{
    private readonly CartridgeState cart;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly Action<uint, uint> m68k_unused_8_w, m68k_unused_16_w;
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
        rom[..size].CopyTo(cart.lockrom);
        for (int i = 0; i < 0x8000; i += 2)
            (cart.lockrom[i], cart.lockrom[i + 1]) = (cart.lockrom[i + 1], cart.lockrom[i]);
        cart.lockrom.AsSpan(0, 0x8000).CopyTo(cart.lockrom.AsSpan(0x8000));
        ggenie.enabled = 1;
    }
    public void Reset(bool hard) => ggenie_reset(hard ? 1 : 0);
    public void Switch(bool enabled) => ggenie_switch(enabled ? 1 : 0);
    public void Dispose() => ggenie_shutdown();
}
