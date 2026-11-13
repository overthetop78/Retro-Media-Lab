namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    private sealed class IoPort
    {
        public Action<byte, byte> data_w = null!;
        public Func<byte> data_r = null!;
    }
    private readonly IoPort[] port = [new(), new(), new()];
    private byte bios_configuration => _bus.BiosConfiguration;
    private uint psg_preamp => _bus.PsgPreamp;
    private bool IsBiosMapped() => _bus.BootRomMapped;
    private void psg_config(uint cycles, uint preamp, uint panning) => _bus.ConfigurePsg(cycles, preamp, panning);
    private void sms_cart_switch(byte mode) => _bus.SwitchSmsCartridge(mode);
    public void InitializeIo() => io_init();
    public void ResetIo() => io_reset();
    public void Write68K(uint offset, uint data) => io_68k_write(offset, data);
    public uint Read68K(uint offset) => io_68k_read(offset);
    public void WriteZ80(uint offset, uint data, uint cycles) => io_z80_write(offset, data, cycles);
    public uint ReadZ80(uint offset) => io_z80_read(offset);
    public void WriteGameGear(uint offset, uint data) => io_gg_write(offset, data);
    public uint ReadGameGear(uint offset) => io_gg_read(offset);
}
