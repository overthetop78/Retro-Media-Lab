using RetroMediaLab.Emulation.Interop;

namespace RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Contracts;
internal sealed record ExternalCoreInfo(Emulator Emulator, string LibraryName, string LibraryVersion, IReadOnlySet<string> Extensions, bool NeedsFullPath, bool BlocksArchiveExtraction);