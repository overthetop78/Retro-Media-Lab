namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie OperatingSystem.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> OperatingSystemRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["b987354c55beb5e2ced761970c3339b895a8c09d"] = new("Acorn System 1 Monitor", null, FirmwareCategory.OperatingSystem),
            ["c587da5dcc6878dcd0bc823c508472d38296003e"] = new("Acorn System COS", null, FirmwareCategory.OperatingSystem),
            ["4cb322dadcfba9c452797d6cc2096f0c92e8792c"] = new("Acorn System DOS", null, FirmwareCategory.OperatingSystem),
            ["b14a884bf82a7a8c23bc03c2e112728dd1a74896"] = new("Acorn System 6809 Monitor", "4.9", FirmwareCategory.OperatingSystem),
            ["e588298239b5360b5d1e15d5cd9f7fe2b1693e5d"] = new("Acorn System 5F", null, FirmwareCategory.OperatingSystem),
            ["3becc10f6a938971f83b8898c28cfdd3e6897a41"] = new("Acorn System DOS40A", null, FirmwareCategory.OperatingSystem),
            ["8d492106a1b06aa8ae2744919fcd83c7b42a2612"] = new("Acorn System DOS40B", null, FirmwareCategory.OperatingSystem),
            ["df68b30e6f804fc3f9b1e642e968a872de0c11cd"] = new("Acorn System DOS80A", null, FirmwareCategory.OperatingSystem),
            ["3f787ab83e41e50889e367cbccddfa716b6a5c2b4"] = new("Acorn System 5 DOS80", null, FirmwareCategory.OperatingSystem),
            ["ea7d3a7e3ee1ecfaa1483af994048057362b01f2"] = new("MOS / BASIC II", "2.00", FirmwareCategory.OperatingSystem, Combined: true, VideoStandard: EmulationVideoStandard.Pal),
            ["d9426dd7454ca199cdef9435daa332cdf405c965"] = new("MOS B+", "2.00", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["4b0ece6dc139d5d3f4fabd023716fb6f25149b80"] = new("MOS 0.10", "0.10", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["dca33995c0d008a527efe923d03333394b01022c"] = new("MOS 0.92", "0.92", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["d35f6723132aabe3c4d00fc16fd9ecc6768df753"] = new("MOS 1.00", "1.00", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d"] = new("MOS 1.20", "1.20", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["2b6aecd33a43f296c20832524e47cc7e3a9c3b17"] = new("MOS NO", "1.20", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["aadf90338ee9d1c85dfa73beba50e930c2a38f10"] = new("MOS DE", null, FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Pal),
            ["83d91d089dca092d2c8b7c3650ff8143c9069b89"] = new("MOS US", "1.00", FirmwareCategory.OperatingSystem, VideoStandard: EmulationVideoStandard.Ntsc),
            ["0a07161fa2fbc102fb306233bd81790c7b04f793"] = new("Hope MOS/BASIC", "1989", FirmwareCategory.OperatingSystem, Combined: true),
            ["8382ed0203909711aca212684ae4a6eb6c54569c"] = new("BBC B+ Acorn MOS v2.00 [variant 4] ['Jameson2016']", "2.00", FirmwareCategory.OperatingSystem),
            ["1a663b9d1022aeea65f07d3d1d6e0a4ad81c0330"] = new("BBC B+ Acorn MOS v2.00 [variant 1]", "2.00", FirmwareCategory.OperatingSystem),
            ["59ad0cdff827ebc280de6aeb97cdce95bb9f3520"] = new("MOS v1.50 [unofficial update]", "1.50", FirmwareCategory.OperatingSystem),
            ["73d7fa828f7e14b639d6390d0b931be9478410de"] = new("MOS v1.26 [unofficial update]", "1.26", FirmwareCategory.OperatingSystem),
            ["5ccab2ed87c747f3bda50082d9d8238ac0c3e1ce"] = new("MOS v1.20 [variant 8]", "1.20", FirmwareCategory.OperatingSystem),
            ["021bf93329bbdf6a483ad52a30cade50327cd876"] = new("MOS v1.20 [variant 7] ['Patched by DJameson 2016']", "1.20", FirmwareCategory.OperatingSystem),
            ["6cc34a858524dc215628392a01228f82cd87e4c6"] = new("MOS v1.20 [variant 3] [hacked for some purpose?]", "1.20", FirmwareCategory.OperatingSystem),
            ["995f52d32da770dc220f5657d61e0b0d5433a53a"] = new("MOS v1.20 [variant 2] [hacked to remove locked bit]", "1.20", FirmwareCategory.OperatingSystem),
            ["096653aa187b0994b8d53a4b1ec3f89836e62b9d"] = new("MOS v1.00 [Low]", "1.00", FirmwareCategory.OperatingSystem),
            ["42c8a849e8397ba8e040118ae7fcc73219eec8df"] = new("MOS v1.00 [High]", "1.00", FirmwareCategory.OperatingSystem)
        };
}
