using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed class SystemConfiguration
{
    public SoundConfiguration Sound { get; } = new() { PsgPreamp = 150, Filter = 1 };
    public Vdp.Configuration Video { get; } = new() { enhanced_vscroll_limit = 8 };
    public MdCartridgeConfiguration Cartridge { get; } = new();
    public SmsCartridgeConfiguration Sms { get; }
    public byte system { get => Sms.system; set => Sms.system = value; }
    public byte bios { get => Sms.bios; set { Sms.bios = value; Video.bios = value; } }
    public byte lock_on { get => Cartridge.lock_on; set => Cartridge.lock_on = value; }
    public byte add_on { get => Cartridge.add_on; set => Cartridge.add_on = value; }
    public byte region_detect, vdp_mode, master_clock, force_dtack;
    public byte addr_error = 1, cd_latency = 1;
    public short cdda_volume = 100, pcm_volume = 100;
    public byte CdCartridgeId = 6;
    public bool PrecacheCd;
    public readonly byte[] PortSystems = [1, 1];
    public readonly byte[] PadTypes = [3, 3, 3, 3, 3, 3, 3, 3];
    public SystemConfiguration() { Sms = new(Sound); }
}
