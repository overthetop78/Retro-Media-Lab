using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class MegaSd
{
    private static readonly byte[] megasd_version =
    [
        77, 69, 71, 65, 83, 68, 1, 4, 7, 0, 255, 255, 18, 52, 86, 120,
    ];
    private static readonly byte[] megasd_id =
    [
        66, 65, 84, 69,
    ];
}
