using System.Globalization;
using System.IO;
using RetroMediaLab.Emulation.Acorn.Common.Exceptions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Équipement de série du B+ ; les images ROM restent des fichiers choisis par l'utilisateur.</summary>
internal static class ModelBPlusFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => configuration.Model is MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128;
    internal static int MemoryBytes(MachineConfiguration configuration) => configuration.Options.GetValueOrDefault(SettingsConstants.MainMemory,
        configuration.Model == MachineModel.BbcMicroModelBPlus128 ? C.Memory128 : C.Memory64) switch
    {
        C.Memory64 => C.BaseBytes,
        C.Memory128 => C.ExpandedBytes,
        _ => throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration))
    };
    internal static int Bank(MachineConfiguration configuration, int original) => IsModel(configuration) ? original switch
    {
        Machines.BbcMicro.Common.Constants.DiskConstants.DfsBank => C.DfsBank,
        Machines.BbcMicro.Common.Constants.DiskConstants.NetworkBank => C.NetworkBank,
        _ => original
    } : original;

    internal static string ChipField(int socket) => C.ChipPrefix + C.SocketNames[socket];
    internal static string ChipLabel(int socket) => C.RomGroup + "." + C.SocketNames[socket];
    internal static bool ProvidesBank(MachineConfiguration configuration, int bank) => C.SocketBanks
        .Select((first, socket) => (first, socket)).Any(item => bank >= item.first && bank <= item.first + 1
            && !string.IsNullOrWhiteSpace(configuration.Options.GetValueOrDefault(ChipField(item.socket))));

    internal static IEnumerable<FirmwareSlot> Slots(MachineConfiguration configuration) => Enumerable.Range(0, C.SocketNames.Count)
        .Select(socket => new FirmwareSlot(ChipField(socket), string.Empty, ChipLabel(socket)));

    internal static EmulationSettingsBlock DescribeRoms(MachineConfiguration configuration) => new(C.RomGroup, EmulationMachineTab.Rom, C.RomGroup,
        Enumerable.Range(0, C.SocketNames.Count).Select(socket => new EmulationSettingsField(ChipField(socket), EmulationMachineTab.Rom, C.RomGroup,
            ChipLabel(socket), EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(ChipField(socket)),
            DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true)).ToArray());

    internal static EmulationSettingsBlock DescribeMemory(MachineConfiguration configuration) => new(C.MemoryGroup, EmulationMachineTab.Ram, C.MemoryGroup,
        [new(C.MemoryDescription, EmulationMachineTab.Ram, C.MemoryGroup, C.MemoryDescription, EmulationSettingsEditor.Information,
            string.Create(CultureInfo.InvariantCulture, $"{C.MainBytes / SettingsConstants.Kibibyte} + {C.ShadowBytes / SettingsConstants.Kibibyte} + {C.PrivateBytes / SettingsConstants.Kibibyte} KiB")),
         new(SidewaysExpansionConstants.RamCapacity, EmulationMachineTab.Ram, C.MemoryGroup, SidewaysExpansionConstants.RamCapacity, EmulationSettingsEditor.Information,
            string.Create(CultureInfo.InvariantCulture, $"{(MemoryBytes(configuration) - C.BaseBytes) / SettingsConstants.Kibibyte} KiB"))]);

    internal static IReadOnlyDictionary<int, byte[]> ChipBanks(MachineConfiguration configuration)
    {
        var result = new Dictionary<int, byte[]>();
        for (var socket = 0; socket < C.SocketNames.Count; socket++)
        {
            var path = configuration.Options.GetValueOrDefault(ChipField(socket));
            if (string.IsNullOrWhiteSpace(path)) continue;
            var bytes = File.ReadAllBytes(path);
            if (!AcceptsLength(bytes.Length)) throw new InvalidDataException(ExceptionText.Get(C.RomSizeError));
            var pair = SplitChip(bytes);
            result.Add(C.SocketBanks[socket], pair.First);
            result.Add(C.SocketBanks[socket] + 1, pair.Second);
        }
        return result;
    }

    internal static bool AcceptsLength(long length) => length is C.SmallChipBytes or C.BankBytes or C.ChipBytes;
    internal static (byte[] First, byte[] Second) SplitChip(ReadOnlySpan<byte> bytes)
    {
        if (!AcceptsLength(bytes.Length)) throw new ArgumentException(nameof(bytes));
        var first = new byte[C.BankBytes];
        var second = new byte[C.BankBytes];
        for (var offset = 0; offset < C.BankBytes; offset++)
        {
            first[offset] = bytes[offset % bytes.Length];
            second[offset] = bytes[(offset + C.BankBytes) % bytes.Length];
        }
        return (first, second);
    }
}
