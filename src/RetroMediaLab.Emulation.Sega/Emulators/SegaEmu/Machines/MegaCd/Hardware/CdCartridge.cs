using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;

internal sealed partial class CdCartridge : IDisposable
{
    private readonly IScdHost _host;
    private readonly Action _initializeMd;
    private readonly BackupRam _backup;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly CartridgeZ80Bank[] zbank_memory_map;
    private readonly byte[] area = new byte[0x810000];
    private byte boot, id, prot;
    private uint mask;
    public byte Boot { get => boot; set => boot = value; }
    public byte ConfiguredRamId { get; set; } = 6;
    public byte Id => id;
    public byte Protection => prot;
    public uint Mask => mask;
    public Memory<byte> Ram => id == 0 ? Memory<byte>.Empty : area.AsMemory(0, checked((int)mask + 1));

    public CdCartridge(IScdHost host, Action initializeMd, BackupRam backup, CartridgeAddressSpace addressSpace)
    { _host = host; _initializeMd = initializeMd; _backup = backup; memory_map = addressSpace.Main; zbank_memory_map = addressSpace.Z80; }
    public void Initialize() => cd_cart_init();
    private void md_cart_init() => _initializeMd();
    private void m68k_unused_8_w(uint address, uint data) => _host.WriteMainUnused8(address, data);
    private void m68k_unused_16_w(uint address, uint data) => _host.WriteMainUnused16(address, data);
    private void zbank_unused_w(uint address, uint data) => _host.WriteZ80Unused(address, data);
    private void ClearArea() => Array.Clear(area);
    private void ClearBackupMemory()
    {
        _backup.sram.AsSpan().Clear(); _backup.detected = _backup.on = _backup.type = 0;
        _backup.start = _backup.end = _backup.crc = 0;
    }
    public void Dispose()
    {
        try { }
        finally
        {
            foreach (CartridgeMemoryBank bank in memory_map)
            {
                if (ReferenceEquals(bank.read8?.Target, this)) bank.read8 = null;
                if (ReferenceEquals(bank.read16?.Target, this)) bank.read16 = null;
                if (ReferenceEquals(bank.write8?.Target, this)) bank.write8 = null;
                if (ReferenceEquals(bank.write16?.Target, this)) bank.write16 = null;
            }
            foreach (CartridgeZ80Bank bank in zbank_memory_map)
            {
                if (ReferenceEquals(bank.read?.Target, this)) bank.read = null;
                if (ReferenceEquals(bank.write?.Target, this)) bank.write = null;
            }
        }
    }
}
