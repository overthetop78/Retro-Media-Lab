using System.Security.Cryptography;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;
using RetroMediaLab.Chips.Memory.Fujitsu.MB84xx.MB8464.Models;
using RetroMediaLab.Chips.Memory.NEC.UPD40xx.UPD4016.Models;
using RetroMediaLab.Chips.Memory.NEC.UPD23xx.UPD23256A.Models;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private Sg1000RomConfiguration _romConfiguration;
    private readonly byte[] _internalGame, _internalGameHash;
    private byte[] _cartridgeHash;
    private Tmm23256? _cartridgeLow, _cartridgeHigh, _internalRom;
    private Tmm2016? _cartridgeRam;
    private int _cartridgeLowMask, _cartridgeHighMask;
    private long _cartridgeOrigin;

    public bool CartridgePresent => _cartridgeLow is not null;
    public bool InternalGameEnabled => _internalRom is not null;
    public bool InternalGameSelected => _internalRom is not null && !CartridgePresent;

    private static Tmm23256Timing LogicalRom(int capacity) => new(
        $"unidentified-static-parallel-ROM-{capacity}",0,0,0,0,0,0,0,0,0,
        Tmm23256OutputEnableMode.ActiveLow,false,capacity:capacity,electricallyCharacterized:false);

    private static Tmm23256 CreateRom(ReadOnlySpan<byte> contents,Tmm23256Timing? profile,bool internalGame = false)
    {
        var timing = profile ?? LogicalRom(contents.Length);
        if (timing.OeMode != Tmm23256OutputEnableMode.ActiveLow
            && !(internalGame && timing.OeMode == Tmm23256OutputEnableMode.AlwaysEnabled))
            throw new ArgumentException("The selected ROM profile is incompatible with this board's CE/OE wiring.",nameof(profile));
        return new(contents,timing);
    }

    private static (Tmm23256? Low,Tmm23256? High,int LowMask,int HighMask,Tmm2016? Ram) CreateCartridge(
        ReadOnlySpan<byte> image,Sg1000RomConfiguration configuration)
    {
        if (!Enum.IsDefined(configuration.CartridgeLayout)) throw new ArgumentException(nameof(configuration));
        if (image.IsEmpty) return default;
        ReadOnlySpan<byte> low, high;
        if (configuration.CartridgeLayout == Sg1000CartridgeLayout.Two8KiB)
        {
            if (image.Length is not (16384 or 40960 or 49152))
                throw new ArgumentException("The two8KiB layout requires16KiB or a verified mirrored40/48KiB dump.",nameof(image));
            low = image[..8192];
            int second = image.Length == 16384 ? 8192 : 32768;
            for (int offset = 8192; offset < second; offset += 8192)
                if (!low.SequenceEqual(image.Slice(offset,8192)))
                    throw new ArgumentException("The first ROM mirrors contain different bytes.",nameof(image));
            high = image.Slice(second,8192);
            if (image.Length == 49152 && !high.SequenceEqual(image.Slice(40960,8192)))
                throw new ArgumentException("The second ROM mirrors contain different bytes.",nameof(image));
        }
        else
        {
            if (image.Length is not (8192 or 16384 or 32768 or 49152))
                throw new ArgumentException("Standard SG ROM layout requires8,16,32 or48KiB.",nameof(image));
            low = image[..Math.Min(image.Length,32768)];
            high = image.Length == 49152 ? image[32768..] : default;
        }
        Tmm2016? ram = configuration.CartridgeLayout switch
        {
            Sg1000CartridgeLayout.TheCastle => new(Mb8464Profile.Grade15L),
            Sg1000CartridgeLayout.Othello => new(Upd4016Profile.Grade2),
            _ => null
        };
        if (ram is not null && image.Length != 32768)
            throw new ArgumentException("The selected SRAM cartridge requires a complete32KiB ROM.",nameof(image));
        // The physical Castle photos identify MB83256, but not a characterized
        // grade. Use the documented -25 bounds nominally, explicitly unqualified.
        var lowProfile = configuration.CartridgeLowProfile ?? configuration.CartridgeLayout switch
        {
            Sg1000CartridgeLayout.Othello => Upd23256AProfile.ActiveLow,
            Sg1000CartridgeLayout.TheCastle => new Tmm23256Timing(
                "Fujitsu-MB83256-Sega-MPR10152-unqualified-grade",250,100,80,0,0,0,0,0,0,
                Tmm23256OutputEnableMode.ActiveLow,false,electricallyCharacterized:false),
            _ => null
        };
        if (configuration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle && lowProfile?.LatchesAddress == true)
            throw new ArgumentException("The Castle ROM has a constant low CE and requires a static-address profile.",nameof(configuration));
        var lowChip = CreateRom(low,lowProfile);
        var highChip = high.IsEmpty ? null : CreateRom(high,configuration.CartridgeHighProfile);
        return (lowChip,highChip,low.Length - 1,high.IsEmpty ? 0 : high.Length - 1,ram);
    }

    /// <summary>Host transaction between advances. Creates/validates new parts
    /// before replacing the slot; insertion waveforms/contact bounce are not
    /// characterized. A newly powered cartridge has its own time origin.</summary>
    public void SetCartridge(ReadOnlySpan<byte> image,Sg1000CartridgeLayout? layout = null)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        var configuration = _romConfiguration with { CartridgeLayout = layout ?? _romConfiguration.CartridgeLayout };
        var replacement = CreateCartridge(image,configuration);
        var contents = image.ToArray();
        var hash = SHA256.HashData(contents);
        DisconnectCartridge();
        _rom = contents; _cartridgeHash = hash; _romConfiguration = configuration;
        (_cartridgeLow,_cartridgeHigh,_cartridgeLowMask,_cartridgeHighMask,_cartridgeRam) = replacement;
        _cartridgeOrigin = TimeNanoseconds;
        ConnectCartridge();
        UpdateRomBus(TimeNanoseconds);
        ResolveCpuData();
    }

    private void ConnectCartridge()
    {
        if (_cartridgeLow is { } low)
        {
            low.OutputChanged = _ => ResolveCpuData();
            low.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(15, GlobalRomDue(next));
        }
        if (_cartridgeHigh is { } high)
        {
            high.OutputChanged = _ => ResolveCpuData();
            high.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(16, GlobalRomDue(next));
        }
        if (_cartridgeRam is { } ram)
        {
            ram.OutputChanged = _ => ResolveCpuData();
            ram.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(18, GlobalRomDue(next));
        }
        _due[15] = GlobalRomDue(_cartridgeLow?.NextEventNanoseconds) ?? long.MaxValue;
        _due[16] = GlobalRomDue(_cartridgeHigh?.NextEventNanoseconds) ?? long.MaxValue;
        _due[18] = GlobalRomDue(_cartridgeRam?.NextEventNanoseconds) ?? long.MaxValue;
    }

    private long? GlobalRomDue(long? time) => time.HasValue ? checked(time.Value + _cartridgeOrigin) : null;

    private void DisconnectCartridge()
    {
        if (_cartridgeLow is { } low) { low.OutputChanged = null; low.NextEventNanosecondsChanged = null; }
        if (_cartridgeHigh is { } high) { high.OutputChanged = null; high.NextEventNanosecondsChanged = null; }
        if (_cartridgeRam is { } ram) { ram.OutputChanged = null; ram.NextEventNanosecondsChanged = null; }
        _due[15] = _due[16] = _due[18] = long.MaxValue;
    }

    private void UpdateRomBus(long time, int memories = 15)
    {
        ushort address = _cpuAddress.GetValueOrDefault();
        bool mreqHigh = _memoryHigh != false;
        long cartridgeTime = time - _cartridgeOrigin;
        // Enri48K: CE=/MREQ, OE=/EXM2 or /EXM1. Addresses above a
        // package's pins are unwired, giving its physical mirrors.
        if ((memories & 1) != 0) _cartridgeLow?.ReceivePins(cartridgeTime,new((ushort)(address & _cartridgeLowMask),
            _cpuAddress.HasValue,_romConfiguration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle ? false : mreqHigh,
            _gatePins[0] != LogicPinState.Low));
        if ((memories & 2) != 0) _cartridgeHigh?.ReceivePins(cartridgeTime,new((ushort)(address & _cartridgeHighMask),
            _cpuAddress.HasValue,mreqHigh,_decodePins[2] != LogicPinState.Low));
        if ((memories & 8) != 0 && _cartridgeRam is { } ram)
        {
            int mask = _romConfiguration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle ? 8191 : 2047;
            // Enri Castle/Othello drawings: /EXM1 selects SRAM, /RD->OE,
            // /WR->WE; the Castle's second chip select is tied to VDD.
            ram.ReceivePins(cartridgeTime,new((ushort)(address & mask),_cpuAddress.HasValue,
                _decodePins[2] != LogicPinState.Low,_readHigh != false,_writeHigh != false,
                _cpuData.GetValueOrDefault(),_cpuData.HasValue ? (byte)255 : (byte)0,Cs2High:true));
        }
        // Optional16KiB internal game. Cartridge presence inhibits the ROM,
        // independently of the current cartridge address or data bus value.
        if ((memories & 4) != 0) _internalRom?.ReceivePins(time,new((ushort)(address & 16383),_cpuAddress.HasValue,
            mreqHigh || CartridgePresent || address >= 0x4000,_readHigh != false));
    }
}
