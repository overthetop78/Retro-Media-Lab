// Copyright (c) 2018 Renaud Toumazet
// Translated from Saturnin scu.h.
// SPDX-License-Identifier: Apache-2.0

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed partial class SaturnScu
{
    private const uint IndirectDmaEndCode = 0x80000000;
    private const int ProgramRamSize = 256 * 32 * 4;
    private const int DataRamSize = 64 * 32 * 4;
    private const int DataRamNumber = 4;

    internal enum DmaLevel : byte
    {
        Level0 = 0,
        Level1 = 1,
        Level2 = 2,
        LevelUnknown = 255,
    }

    internal enum DmaStatus : byte
    {
        Finished = 0,
        WaitingStartFactor = 1,
        Queued = 2,
        Active = 3,
    }

    private enum DmaBus
    {
        ABus,
        BBus,
        CpuBus,
        DspBus,
        UnknownBus,
    }

    private enum ScuRegion
    {
        Unknown,
        Rom,
        Smpc,
        BackupRam,
        WorkRamL,
        Minit,
        Sinit,
        ABusCs0,
        ABusCs1,
        ABusDummy,
        ABusCs2,
        Sound,
        Vdp1,
        Vdp2,
        ScuRegister,
        WorkRamH,
    }

    internal struct DmaConfiguration
    {
        public DmaLevel Level;
        public DmaStatus Status;
        public uint ReadAddress;
        public uint WriteAddress;
        public uint TransferByteNumber;
        public SaturnScuRegisters.Dxad.ReadAddressAddValue ReadAddValue;
        public SaturnScuRegisters.Dxad.WriteAddressAddValue WriteAddValue;
        public SaturnScuRegisters.Dxen.DmaEnable Enable;
        public SaturnScuRegisters.Dxen.DmaStarting Starting;
        public SaturnScuRegisters.Dxmd.DmaMode Mode;
        public SaturnScuRegisters.Dxmd.ReadAddressUpdate ReadAddressUpdate;
        public SaturnScuRegisters.Dxmd.WriteAddressUpdate WriteAddressUpdate;
        public SaturnScuRegisters.Dxmd.StartingFactorSelect StartingFactorSelect;
    }

    private readonly struct DmaCompare
    {
        public static bool LessThan(in DmaConfiguration first, in DmaConfiguration second)
            => first.Status < second.Status;
    }

    private readonly List<DmaConfiguration> _dmaQueue = new();
    private readonly byte[] _programRam = new byte[ProgramRamSize];
    private readonly byte[,] _dataRam = new byte[DataRamNumber, DataRamSize];
    private readonly SaturnScuRegisters _regs = new();
}
