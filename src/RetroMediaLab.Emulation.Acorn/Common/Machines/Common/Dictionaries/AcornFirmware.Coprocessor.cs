namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> CoprocessorRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["eb2a043d139ae0f3d512b4f1afa32286510c886c"] = new("Solidisk PC-Plus 80186 BIOS", "1.00", FirmwareCategory.Coprocessor, CoprocessorCpu: HardwareCpu.Intel80186),
            ["8acaf42a4a6b5f62f6be5e62df5fcffbf48890dd"] = new("80186 Tube Client", "1.01", FirmwareCategory.Coprocessor, CoprocessorCpu: HardwareCpu.Intel80186),
            ["d95815c367e07f2d5a00e860092e7d6da795cbe5"] = new("Acorn Master Turbo 65C102", null, FirmwareCategory.Coprocessor, CoprocessorCpu: HardwareCpu.Gte65C102),
        };
}
