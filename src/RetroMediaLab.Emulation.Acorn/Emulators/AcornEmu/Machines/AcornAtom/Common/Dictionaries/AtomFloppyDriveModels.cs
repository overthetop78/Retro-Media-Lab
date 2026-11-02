using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomDriveConstants;
using S = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using CommonHost = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;

/// <summary>Lecteur simple face de 40 pistes du Disc Pack et format Atom DOS.</summary>
internal static class AtomFloppyDriveModels
{
    private static string Extension => CommonHost.MediaProcessing.FileExtensions[C.ExtensionKey];

    internal static IReadOnlyList<FloppyDriveModelChoice> All { get; } =
    [
        new(S.Single40, string.Empty, BlankImageSize: C.Capacity)
        {
            DiameterInches = C.DiameterInches,
            Tracks = C.Tracks,
            Sides = C.Sides,
            DoubleDensity = false,
            AcceptedExtensions = [Extension],
            CreationFormats = [new(C.FormatId, Extension, C.FormatName, C.Capacity) { Tracks = C.Tracks, Sides = C.Sides }],
            BlankFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [Extension] = C.FormatId }
        }
    ];
}
