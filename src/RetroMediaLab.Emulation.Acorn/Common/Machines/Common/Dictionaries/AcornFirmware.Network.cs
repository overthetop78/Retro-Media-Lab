namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie Network.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> NetworkRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["8564d944fedd17caf2023454ba5a7c3947aedc1f"] = new("Acorn NFS", "3.34", FirmwareCategory.Network),
            ["7081f1d834645e501c8c9862cf4a50f282ca1f32"] = new("Acorn NFS", "3.35K", FirmwareCategory.Network),
            ["7462ced7b83d74b822815bc00ed40a89f84e0276"] = new("Torch NET", "3.35K", FirmwareCategory.Network),
            ["3d01b75a9dc51ec0edcdaa7ec07810f7ffa76ccb"] = new("Acorn ANFS v4.18", "4.18", FirmwareCategory.Network),
            ["e9e3860651ea5cb344e5a30b2c484eb232e5d07b"] = new("Acorn ANFS v4.08.53", "4.08.53", FirmwareCategory.Network),
            ["ffdab09962f61ff674157533ee977abce64dbe63"] = new("Digistore Tape Streamer v0.09", "0.09", FirmwareCategory.Network),
            ["72f574edee756dc3ea91221ff6b8a4c97136a360"] = new("Digistore Tape Streamer v0.07", "0.07", FirmwareCategory.Network),
            ["8f3bfd1b1201942641236b4fbf282410fdfe9a80"] = new("Digistore Tape Streamer v0.05", "0.05", FirmwareCategory.Network),
            ["16221546a298b9d68e45e4377ecf81dc9f20add0"] = new("Econet Bridge [variant 2]", "", FirmwareCategory.Network),
            ["ce5ef6edbececb7790c6dd9ed0da45f85cc20dff"] = new("Econet Bridge [variant 1]", "", FirmwareCategory.Network),
            ["523824777e2812052c4ba8c6a721ae01a5b6b4bc"] = new("SJ Research Econet Bridge v3.04", "3.04", FirmwareCategory.Network),
            ["34c1737dc7849b3044211ab1f3f3c52f5c3b92a5"] = new("Supervisor v3.03 ['PCHS'] ['Winchester']", "3.03", FirmwareCategory.Network),
            ["e1e00b339cc6dbe3b64ebd6ca7343dd40a697d3d"] = new("Supervisor v3.03 ['Floppy'] ['PCHS']", "3.03", FirmwareCategory.Network),
            ["cb874c958285d4ac4b22eda5050c57b5b49aab34"] = new("Supervisor v2.10 ['DFS'] ['Padgate']", "2.10", FirmwareCategory.Network),
            ["a9da6867e780fc72e42f1216c398ef5329fabe4d"] = new("Micropulse Econet Supervisor v1.00", "1.00", FirmwareCategory.Network),
            ["c733b108d74cf3b1c5de395335236800a7c9c0d8"] = new("Econet X25 Gateway 2201248 03 ANFS v4.25", "4.25", FirmwareCategory.Network),
            ["bbfa892fdcc6f753dda5134ecb97cc7c42b959c2"] = new("Econet X25 Gateway 0246215 02 TSI v0.51", "0.51", FirmwareCategory.Network),
            ["9adb6fbb9020682572596e5189fa112441cc3f6f"] = new("Amcom File Server v6.23", "6.23", FirmwareCategory.Network),
            ["5d934c256144ed72ada7118cfcb0b6cab56fc7ba"] = new("Amcom File Server v5.21", "5.21", FirmwareCategory.Network),
            ["0d8b06d89a191589291057028011f078888ea799"] = new("LAN Manager v0.37", "0.37", FirmwareCategory.Network),
            ["7dcde45c0c878f3ba66bf33f714360fbb93303f1"] = new("LAN Manager v0.32", "0.32", FirmwareCategory.Network),
            ["0894545e139b2817038569713cb120271009ae8c"] = new("MDFS v1.0 [variant 2]", "1.0", FirmwareCategory.Network),
            ["ae13deff79b60a4c14441e2333bf154271c971bb"] = new("MDFS v1.0 [variant 1]", "1.0", FirmwareCategory.Network),
            ["be96d911aa276c635a089691a71c89ca36d627df"] = new("MDFS File Server Boot EPROM v1.05", "1.05", FirmwareCategory.Network),
            ["1304176ec4e36b8e9fbc1cad2678c91fa44d90af"] = new("MDFS File Server Boot EPROM v1.04", "1.04", FirmwareCategory.Network),
            ["fd7367cb5c0d1f92c3f2f5a629cf8d437cac8f90"] = new("Network Expansion ROM v1.20 [variant 2]", "1.20", FirmwareCategory.Network),
            ["f6cd5bc45ed9c3de541ce889cf41d812875c514a"] = new("Network Expansion ROM v1.20 [variant 1]", "1.20", FirmwareCategory.Network),
            ["1e63aa3bf4b37bf9ba41e454f95db05c3d15bfbf"] = new("ReutersB", "", FirmwareCategory.Network),
            ["a7887e1e5c206203491e1e06682b9508b0fef49d"] = new("ReutersA100", "", FirmwareCategory.Network),
            ["e5d178d19b2c22852f576a90fae4109e22d324ac"] = new("Reuters System ROM v6", "6", FirmwareCategory.Network),
            ["9a8c1977b08399455a55f4b83f4b8f637cf2bc04"] = new("Acorn NFS v3.65 (for Reuters)", "3.65", FirmwareCategory.Network),
            ["6a5c7ace255c8ac96c983d5ba67084fbd71ff61e"] = new("Acorn MOS v2.30R (for Reuters)", "2.30R", FirmwareCategory.Network),
            ["08cec09c3459ea87a93d57c7bd6f4ddb9a7bcc89"] = new("Wild ROM v1.27 [variant 2] ['21.3.90']", "1.27", FirmwareCategory.Network)
        };
}
