using RetroMediaLab.Emulation.Nec.Emulators.BeetlePcfx.Constants;
using BeetlePceCore = RetroMediaLab.Emulation.Nec.Emulators.BeetlePce.Constants.CoreConstants;
using Quasi88Core = RetroMediaLab.Emulation.Nec.Emulators.Quasi88.Constants.CoreConstants;
using NekoProjectIICore = RetroMediaLab.Emulation.Nec.Emulators.NekoProjectII.Constants.CoreConstants;
using NP2KaiCore = RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Constants.CoreConstants;
using RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.BeetleSgx.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Geargrafx.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;

namespace RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Dictionaries;

internal static class CoreCatalog
{
    internal static IReadOnlyList<CoreDefinition> All { get; } =
    [
        new(BeetlePceFastConstants.Id, BeetlePceFastConstants.LibraryName, ReleaseConstants.InstalledLibraryName),
        new(BeetleSgxConstants.Id, BeetleSgxConstants.LibraryName, BeetleSgxConstants.InstalledLibraryName),
        new(GeargrafxConstants.Id, GeargrafxConstants.LibraryName, GeargrafxConstants.InstalledLibraryName),
        BeetlePcfxConstants.CoreDefinition,
        new(BeetlePceCore.Id, BeetlePceCore.LibraryName, BeetlePceCore.LibraryFileName),
        new(Quasi88Core.Id, Quasi88Core.LibraryName, Quasi88Core.LibraryFileName, Subsystems: RetroMediaLab.Emulation.Nec.Emulators.Quasi88.Constants.StorageConstants.Subsystems),
        new(NekoProjectIICore.Id, NekoProjectIICore.LibraryName, NekoProjectIICore.LibraryFileName, RetroMediaLab.Emulation.Nec.Emulators.NekoProjectII.Functions.ContentFunctions.Prepare),
        new(NP2KaiCore.Id, NP2KaiCore.LibraryName, NP2KaiCore.LibraryFileName, RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Functions.ContentFunctions.Prepare),
    ];

    internal static CoreDefinition Get(string id) => All.Single(core => core.Id == id);
}
