using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Flash.Cfi;

public sealed partial class FlashCfi
{
    private static readonly byte[] cfi_query =
    [
        81, 82, 89, 2, 0, 64, 0, 0, 0, 0, 0, 39, 54, 181, 197, 4, 0, 10, 0, 4, 0, 3, 0, 22, 2, 0, 0, 0, 2, 7, 0, 32,
        0, 62, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 82, 73, 49, 48, 0, 2, 1, 1, 4, 0, 0, 0, 181, 197, 2,
        0, 81, 82, 89, 2, 0, 64, 0, 0, 0, 0, 0, 39, 54, 0, 0, 7, 7, 10, 0, 3, 5, 4, 0, 23, 2, 0, 5, 0, 2, 7, 0,
        32, 0, 126, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 82, 73, 49, 51, 0, 2, 1, 0, 8, 0, 0, 2, 181, 197,
        2, 1,
    ];
    private static readonly ushort[] flash_id =
    [
        32, 8791, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 8830, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8720, 8704,
    ];
    private static readonly byte[] flash_readmask =
    [
        3, 15,
    ];
}
