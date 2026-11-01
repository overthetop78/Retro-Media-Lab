using RetroMediaLab.Emulation.Interop;

namespace RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Interop.Contracts;
internal sealed record ExternalCoreInfo(Emulator Emulator, string LibraryName, string LibraryVersion, IReadOnlySet<string> Extensions, bool NeedsFullPath, bool BlocksArchiveExtraction);