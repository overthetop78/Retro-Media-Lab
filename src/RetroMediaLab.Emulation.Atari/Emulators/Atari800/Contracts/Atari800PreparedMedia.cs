using RetroMediaLab.Emulation.Atari.Emulators.Atari800.Constants;
using RetroMediaLab.Emulation.Atari.Emulators.Atari800.Contracts;
using RetroMediaLab.Emulation.Atari.Emulators.Atari800.Enums;
using RetroMediaLab.Emulation.Atari.Emulators.Atari800.Functions;

namespace RetroMediaLab.Emulation.Atari.Emulators.Atari800.Contracts;


internal sealed record Atari800PreparedMedia(MediaConfiguration Configuration, Atari800ContentType ContentType, string RuntimePath, SessionMedia? SessionMedia);
