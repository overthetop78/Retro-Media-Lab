namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie CharacterGenerator.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> CharacterGeneratorRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["04c326a745ba6a78185ebc2adeecb53a166a6ab3"] = new("Acorn System 80-column VDU", null, FirmwareCategory.CharacterGenerator),
            ["6c8daba70374e5aa3a6402f24cdc5f8677d58a0f"] = new("SAA5050", null, FirmwareCategory.CharacterGenerator),
            ["9ab9d24b845fe2964fba2f4770d54025d2c8026a"] = new("SAA5051", "MAME 0.203", FirmwareCategory.CharacterGenerator),
            ["07cd9b7edbed6ef7b527622533d6957f5d56aa91"] = new("SAA5051", null, FirmwareCategory.CharacterGenerator),
            ["ec4bc515e28e851a6433f7ca0a11ede0f1d21a68"] = new("SAA5052", null, FirmwareCategory.CharacterGenerator),
            ["1e471a1b5670d7163e9f62d31be7cab0330a07cd"] = new("SAA5053", null, FirmwareCategory.CharacterGenerator),
            ["7a273ad7270507dca4ce621fc1e6b51a1ac25085"] = new("SAA5054", null, FirmwareCategory.CharacterGenerator),
            ["c5ce7fe84df6de6a317fa0e87bda413c82c04618"] = new("SAA5055", null, FirmwareCategory.CharacterGenerator),
            ["2d1ff08b4dda15cf70832881750a962189455f41"] = new("SAA5056", null, FirmwareCategory.CharacterGenerator),
            ["5a93445dde03066073e2909a935900e5f8439d81"] = new("SAA5057", null, FirmwareCategory.CharacterGenerator)
        };
}
