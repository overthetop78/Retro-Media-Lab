using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Cdd = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media.CdDrive;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

internal abstract partial class SegaBoard
{
    public byte PicoPage { get => pico_current; set => pico_current = value; }
    public Memory<byte> CdBackupRam => _cd.BackupRam;
    public Memory<byte> CdCartridgeRam => _cdCartridge.Ram;
    public void RefreshConfiguration(int overclockPercent = 100)
    {
        main.aerr_enabled = config.addr_error; if (_cdComponent is not null) sub.aerr_enabled = config.addr_error;
        if (_cdComponent is not null)
        {
            _cdComponent.PcmChip.Volume = config.pcm_volume;
            _cdComponent.Drive.AudioVolume = config.cdda_volume;
            _cdComponent.Drive.LatencyFactor = config.cd_latency;
            _cdComponent.Drive.PrecacheImages = config.PrecacheCd;
        }
        _sound.ConfigureChannels();
        _sound.Psg.Configure(0, config.Sound.PsgPreamp, SystemHardware.IsSixteenBit(system_hw) ? 0xffu : _input.IoRegisters.Span[6]);
        _sound.ConfigureEqualizer();
        _vdp.RefreshVideoConfiguration();
        _vdp.ViewportChanged |= 11;
        _vdp.ViewportX = system_hw == SystemHardware.GameGear && config.Video.gg_extra == 0
            ? (config.Video.overscan & 2) != 0 ? 14 : -48 : (config.Video.overscan & 2) * 7;
        if (overclockPercent <= 0) throw new ArgumentOutOfRangeException(nameof(overclockPercent));
        if (_mainCpuComponent is not null)
            _mainCpuComponent.SetCycleRatio(SystemHardware.IsSixteenBit(system_hw) ? (100 << 16) / overclockPercent : 1 << 16, 16);
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
        if (_cd.Drive.Load(path, header) < 0 || DiscLoaded == 0)
            throw new InvalidDataException("The CD image could not be mounted.");
        if (_cd.Drive.Status == 5) _cd.Drive.Status = 9;
    }
    public void UnmountDisc()
    {
        _cd.Drive.Unload(); _cd.Drive.Status = 5;
        _cd.Registers[0x36 >> 1].High = 1;
    }
}
