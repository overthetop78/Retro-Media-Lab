using System.IO;
using System.Buffers.Binary;


namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdZstd
{
    private static readonly uint[] LL_base =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        12, 13, 14, 15, 16, 18, 20, 22, 24, 28, 32, 40,
        48, 64, 0x80, 0x100, 0x200, 0x400, 0x800, 0x1000, 0x2000, 0x4000, 0x8000, 0x10000,
    ];

    private static readonly uint[] ML_base =
    [
        3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26,
        27, 28, 29, 30, 31, 32, 33, 34, 35, 37, 39, 41,
        43, 47, 51, 59, 67, 83, 99, 0x83, 0x103, 0x203, 0x403, 0x803,
        0x1003, 0x2003, 0x4003, 0x8003, 0x10003,
    ];

    private static readonly uint[] OF_base =
    [
        0, 1, 1, 5, 0xD, 0x1D, 0x3D, 0x7D, 0xFD, 0x1FD, 0x3FD, 0x7FD,
        0xFFD, 0x1FFD, 0x3FFD, 0x7FFD, 0xFFFD, 0x1FFFD, 0x3FFFD, 0x7FFFD, 0xFFFFD, 0x1FFFFD, 0x3FFFFD, 0x7FFFFD,
        0xFFFFFD, 0x1FFFFFD, 0x3FFFFFD, 0x7FFFFFD, 0xFFFFFFD, 0x1FFFFFFD, 0x3FFFFFFD, 0x7FFFFFFD,
    ];

    private static readonly byte[] LL_bits =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3,
        4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
    ];

    private static readonly byte[] ML_bits =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1,
        2, 2, 3, 3, 4, 4, 5, 7, 8, 9, 10, 11,
        12, 13, 14, 15, 16,
    ];

    private static readonly short[] LL_defaultNorm =
    [
        4, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 3, 2, 1, 1, 1, 1, 1, -1, -1, -1, -1,
    ];

    private static readonly short[] ML_defaultNorm =
    [
        1, 4, 3, 2, 2, 2, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, -1, -1,
        -1, -1, -1, -1, -1,
    ];

    private static readonly short[] OF_defaultNorm =
    [
        1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        -1, -1, -1, -1, -1,
    ];
}
