using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Constants;
using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Contracts;
using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Functions;

namespace RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Contracts;

internal sealed record PreparedJaguarCd(MediaConfiguration Configuration, string RuntimePath, bool NeedsFullPath, IReadOnlySet<string> ActivityPaths);
