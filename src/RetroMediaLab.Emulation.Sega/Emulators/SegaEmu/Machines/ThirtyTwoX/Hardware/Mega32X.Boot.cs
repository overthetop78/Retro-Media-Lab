using System.Buffers.Binary;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;
internal sealed partial class Mega32X
{
    // Replacement bootstrap instruction streams adapted from the collected PicoDrive reference.
    private static readonly ushort[] MasterBootstrap = [
        0xaffe, 0x0009, 0xd406, 0xc400, 0xc801, 0x8b0f, 0xd105, 0xd206, 0x71ff, 0x4115, 0x89fc, 0x6043, 0xc208, 0x6822, 0x482b, 0x0009, 0x4d5f, 0x4f4b, 0x0001, 0x0000, 0x2200, 0x03e0, 0xd20d, 0xc608, 0x3200, 0x8bfc, 0xe000, 0xcf80, 0xd80b, 0xd30c, 0x5b84, 0x5a82, 0x5081, 0x5980, 0x3036, 0x8b00, 0x6033, 0x7820, 0x6286, 0x2922, 0x7904, 0x70fc, 0x8800, 0x8bf9, 0x4b2e, 0x6043, 0xc208, 0x4a2b, 0x0009, 0x0009, 0x5f43, 0x445f, 0x2400, 0x0018, 0x0001, 0xffe0
    ];
    private static readonly ushort[] SlaveBootstrap = [
        0xaffe, 0x0009, 0xd106, 0xd208, 0xc608, 0x3100, 0x8bfc, 0xc400, 0xc801, 0xd004, 0x8b0a, 0xc209, 0x6822, 0x482b, 0x0009, 0x0009, 0x4d5f, 0x4f4b, 0x535f, 0x4f4b, 0x2200, 0x03e4, 0xd803, 0x5b85, 0x5a83, 0x4b2e, 0xc209, 0x4a2b, 0x0009, 0x0009, 0x2400, 0x0018
    ];

    private void PrepareBootRoms(Func<int, Memory<byte>, int> firmware)
    {
        if (firmware(0x3200, _bios68k) == 0)
        {
            for (int i = 1; i < 0xc0 / 4; i++) BinaryPrimitives.WriteUInt32BigEndian(_bios68k.AsSpan(i * 4), (uint)(0x880200 + (i - 1) * 6));
            BinaryPrimitives.WriteUInt32BigEndian(_bios68k.AsSpan(0x70), 0);
            for (int i = 0xc0; i < 0x100; i += 2) BinaryPrimitives.WriteUInt16BigEndian(_bios68k.AsSpan(i), 0x4e71);
            ushort[] write = [0x1280, 0x0239, 0x00fe, 0x00a1, 0x5107, 0x4e75];
            for (int i = 0; i < write.Length; i++) BinaryPrimitives.WriteUInt16BigEndian(_bios68k.AsSpan(0xc8 + i * 2), write[i]);
            ushort[] banks = [0x48e7,0x8040,0x227c,0x00a1,0x30f1,0x7007,0x12d8,0x5289,0x51c8,0xfffa,0x0239,0x00fe,0x00a1,0x5107,0x4cdf,0x0201];
            for (int i = 0; i < banks.Length; i++) BinaryPrimitives.WriteUInt16BigEndian(_bios68k.AsSpan(0xd4 + i * 2), banks[i]);
            BinaryPrimitives.WriteUInt16BigEndian(_bios68k.AsSpan(0xfe), 0x4e75);
        }
        _realMaster = firmware(0x3201, _masterRom) != 0; _realSlave = firmware(0x3202, _slaveRom) != 0;
        for (int cpu = 0; cpu < 2; cpu++)
        {
            if (cpu == 0 ? _realMaster : _realSlave) continue;
            byte[] rom = cpu == 0 ? _masterRom : _slaveRom;
            for (int i = 0; i < 0x200; i += 4) BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(i), 0x200);
            BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(0), 0x204); BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(8), 0x204);
            uint stack = cpu == 0 ? 0x06040000u : 0x0603f800u;
            BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(4), stack); BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(12), stack);
            ushort[] code = cpu == 0 ? MasterBootstrap : SlaveBootstrap;
            for (int i = 0; i < code.Length; i++) BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x200 + i * 2), code[i]);
        }
    }
}
