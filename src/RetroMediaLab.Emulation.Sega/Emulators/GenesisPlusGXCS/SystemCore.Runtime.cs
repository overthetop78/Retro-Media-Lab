using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    public byte PicoPage { get => pico_current; set => pico_current = value; }
    public Memory<byte> CdBackupRam => _cd.BackupRam;
    public Memory<byte> CdCartridgeRam => _cdCartridge.Ram;
    public void RefreshConfiguration(int overclockPercent = 100)
    {
        main.aerr_enabled = sub.aerr_enabled = config.addr_error;
        _cd.PcmChip.Volume = config.pcm_volume;
        _cd.Drive.AudioVolume = config.cdda_volume;
        _cd.Drive.LatencyFactor = config.cd_latency;
        _cd.Drive.PrecacheImages = config.PrecacheCd;
        _sound.ConfigureChannels();
        _sound.Psg.Configure(0, config.Sound.PsgPreamp, SystemHardware.IsSixteenBit(system_hw) ? 0xffu : _input.IoRegisters.Span[6]);
        _sound.ConfigureEqualizer();
        _vdp.RefreshVideoConfiguration();
        _vdp.ViewportChanged |= 11;
        _vdp.ViewportX = system_hw == SystemHardware.GameGear && config.Video.gg_extra == 0
            ? (config.Video.overscan & 2) != 0 ? 14 : -48 : (config.Video.overscan & 2) * 7;
        if (overclockPercent <= 0) throw new ArgumentOutOfRangeException(nameof(overclockPercent));
        _mainCpu.SetCycleRatio(SystemHardware.IsSixteenBit(system_hw) ? (100 << 16) / overclockPercent : 1 << 16, 16);
        _z80.SetCycleRatio(SystemHardware.IsSixteenBit(system_hw) ? 1u << 16 : (uint)((100 << 16) / overclockPercent), 16);
    }
    public void ConfigureInputPort(int port, byte system, byte padType)
    {
        _input.ConfiguredPadTypes[port] = padType;
        if (port < 2) _input.PortSystems[port] = system;
        _input.InitializeIo();
    }
    public void MountDisc(string path)
    {
        Span<byte> header = stackalloc byte[0x210];
        if (_cd.Drive.Load(path, header) < 0 || _cd.Drive.Loaded == 0)
            throw new InvalidDataException("The CD image could not be mounted.");
        if (_cd.Drive.Status == 5) _cd.Drive.Status = 9;
    }
    public void UnmountDisc()
    {
        _cd.Drive.Status = 5;
        _cd.Registers[0x36 >> 1].High = 1;
    }
}
