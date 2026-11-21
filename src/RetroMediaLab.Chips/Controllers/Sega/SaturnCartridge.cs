// Copyright 2004-2005 Theo Berkau
// Copyright 2006 Ex-Cyber
// Copyright 2005 Guillaume Duhamel
// Translated from Yabause cs0.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.IO;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnCartridge<TSh2>
{
    private static byte DummyCs0ReadByte(TSh2 sh, uint address) => 0xFF;
    private static ushort DummyCs0ReadWord(TSh2 sh, uint address) => 0xFFFF;
    private static uint DummyCs0ReadLong(TSh2 sh, uint address) => 0xFFFFFFFF;
    private static void DummyCs0WriteByte(TSh2 sh, uint address, byte value) { }
    private static void DummyCs0WriteWord(TSh2 sh, uint address, ushort value) { }
    private static void DummyCs0WriteLong(TSh2 sh, uint address, uint value) { }

    private static byte DummyCs1ReadByte(TSh2 sh, uint address) => 0xFF;
    private static ushort DummyCs1ReadWord(TSh2 sh, uint address) => 0xFFFF;
    private static uint DummyCs1ReadLong(TSh2 sh, uint address) => 0xFFFFFFFF;
    private static void DummyCs1WriteByte(TSh2 sh, uint address, byte value) { }
    private static void DummyCs1WriteWord(TSh2 sh, uint address, ushort value) { }
    private static void DummyCs1WriteLong(TSh2 sh, uint address, uint value) { }

    private static byte DummyCs2ReadByte(TSh2 sh, uint address) => 0xFF;
    private static ushort DummyCs2ReadWord(TSh2 sh, uint address) => 0xFFFF;
    private static uint DummyCs2ReadLong(TSh2 sh, uint address) => 0xFFFFFFFF;
    private static void DummyCs2WriteByte(TSh2 sh, uint address, byte value) { }
    private static void DummyCs2WriteWord(TSh2 sh, uint address, ushort value) { }
    private static void DummyCs2WriteLong(TSh2 sh, uint address, uint value) { }

    private enum FlashState
    {
        Read,
        SoftwareDataProtection,
        Command,
        Identification,
        IdentificationSoftwareDataProtection,
        IdentificationCommand,
        WriteBuffer,
        WriteArray
    }

    private byte _flashRegister0;
    private byte _flashRegister1;
    private byte _vendorId = 0x1F;
    private byte _deviceId = 0xD5;
    private FlashState _flashState0;
    private FlashState _flashState1;
    private readonly byte[] _flashBuffer0 = new byte[128];
    private readonly byte[] _flashBuffer1 = new byte[128];

    private byte FlashCs0ReadByte(uint address, Func<uint, byte> readRomByte)
    {
        bool odd = (address & 1) != 0;
        ref FlashState state = ref (odd ? ref _flashState1 : ref _flashState0);
        ref byte register = ref (odd ? ref _flashRegister1 : ref _flashRegister0);

        switch (state)
        {
            case FlashState.Identification:
            case FlashState.IdentificationSoftwareDataProtection:
            case FlashState.IdentificationCommand:
                return (address & 2) != 0 ? _deviceId : _vendorId;
            case FlashState.WriteArray:
                register ^= 0x02;
                goto case FlashState.WriteBuffer;
            case FlashState.WriteBuffer:
                return register;
            case FlashState.SoftwareDataProtection:
            case FlashState.Command:
                state = FlashState.Read;
                goto case FlashState.Read;
            case FlashState.Read:
            default:
                return readRomByte(address);
        }
    }

    private ushort FlashCs0ReadWord(uint address, Func<uint, byte> readRomByte) =>
        (ushort)((FlashCs0ReadByte(address, readRomByte) << 8) | FlashCs0ReadByte(address + 1, readRomByte));

    private uint FlashCs0ReadLong(uint address, Func<uint, byte> readRomByte) =>
        ((uint)FlashCs0ReadWord(address, readRomByte) << 16) | FlashCs0ReadWord(address + 2, readRomByte);

    private void FlashCs0WriteByte(uint address, byte value, Action<uint, byte> writeRomByte)
    {
        bool odd = (address & 1) != 0;
        ref FlashState state = ref (odd ? ref _flashState1 : ref _flashState0);
        byte[] buffer = odd ? _flashBuffer1 : _flashBuffer0;

        switch (state)
        {
            case FlashState.Read:
                if ((address & 0xFFFE) == 0xAAAA && value == 0xAA)
                    state = FlashState.SoftwareDataProtection;
                return;
            case FlashState.WriteBuffer:
                buffer[(address >> 1) & 0x7F] = value;
                if (((address >> 1) & 0x7F) == 0x7F)
                {
                    uint lane = address & 1;
                    address &= 0xFFFFFF00;
                    for (uint index = 0; index <= 127; index++)
                        writeRomByte(address + index * 2 + lane, buffer[index]);
                    state = FlashState.Read;
                }
                return;
            case FlashState.SoftwareDataProtection:
                state = (address & 0xFFFE) == 0x5554 && value == 0x55
                    ? FlashState.Command
                    : FlashState.Read;
                return;
            case FlashState.Identification:
                state = (address & 0xFFFE) == 0xAAAA && value == 0xAA
                    ? FlashState.IdentificationSoftwareDataProtection
                    : FlashState.Identification;
                return;
            case FlashState.IdentificationSoftwareDataProtection:
                state = (address & 0xFFFE) == 0x5554 && value == 0x55
                    ? FlashState.Read
                    : FlashState.Identification;
                return;
            case FlashState.IdentificationCommand:
                state = (address & 0xFFFE) == 0xAAAA && value == 0xF0
                    ? FlashState.Read
                    : FlashState.Identification;
                return;
            case FlashState.Command:
                if ((address & 0xFFFE) != 0xAAAA)
                {
                    state = FlashState.Read;
                    return;
                }

                switch (value)
                {
                    case 0xA0:
                        state = FlashState.WriteBuffer;
                        return;
                    case 0x90:
                        state = FlashState.Identification;
                        return;
                    default:
                        state = FlashState.Read;
                        return;
                }
            default:
                break;
        }
    }

    private void FlashCs0WriteWord(uint address, ushort value, Action<uint, byte> writeRomByte)
    {
        FlashCs0WriteByte(address, (byte)(value >> 8), writeRomByte);
        FlashCs0WriteByte(address + 1, (byte)(value & 0xFF), writeRomByte);
    }

    private void FlashCs0WriteLong(uint address, uint value, Action<uint, byte> writeRomByte)
    {
        FlashCs0WriteWord(address, (ushort)(value >> 16), writeRomByte);
        FlashCs0WriteWord(address + 2, (ushort)(value & 0xFFFF), writeRomByte);
    }

    private byte[] _rom = [];
    private byte[] _dram = [];
    private byte[] _backupRam = [];

    private byte ReadFlashRomByte(uint address) => T2ReadByte(_rom, address);
    private void WriteFlashRomByte(uint address, byte value) => T2WriteByte(_rom, address, value);

    private byte AR4MCs0ReadByte(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        return FlashCs0ReadByte(address, ReadFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Dram area
                return T1ReadByte(_dram, address & 0x3FFFFF);
            default:   // The rest doesn't matter
                break;
        }

        return 0xFF;
    }

    private ushort AR4MCs0ReadWord(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        return FlashCs0ReadWord(address, ReadFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                return T1ReadWord(_dram, address & 0x3FFFFF);
            case 0x12:
            case 0x1E:
                return 0xFFFD;
            case 0x13:
            case 0x16:
            case 0x17:
            case 0x1A:
            case 0x1B:
            case 0x1F:
                return 0xFFFD;
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFF;
    }

    private uint AR4MCs0ReadLong(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        return FlashCs0ReadLong(address, ReadFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                return T1ReadLong(_dram, address & 0x3FFFFF);
            case 0x12:
            case 0x1E:
                return 0xFFFDFFFD;
            case 0x13:
            case 0x16:
            case 0x17:
            case 0x1A:
            case 0x1B:
            case 0x1F:
                return 0xFFFDFFFD;
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFFFFFF;
    }

    private void AR4MCs0WriteByte(TSh2 sh, uint address, byte value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        FlashCs0WriteByte(address, value, WriteFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteByte(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void AR4MCs0WriteWord(TSh2 sh, uint address, ushort value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        FlashCs0WriteWord(address, value, WriteFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteWord(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void AR4MCs0WriteLong(TSh2 sh, uint address, uint value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x00:
            {
                if ((address & 0x80000) == 0) // EEPROM
        FlashCs0WriteLong(address, value, WriteFlashRomByte);
                break;
            }
            case 0x01:
            {
                break;
            }
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteLong(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private byte DRAM8MBITCs0ReadByte(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                return T1ReadByte(_dram, address & 0x7FFFF);
            case 0x06: // Dram area
                return T1ReadByte(_dram, 0x80000 | (address & 0x7FFFF));
            default:   // The rest doesn't matter
                break;
        }

        return 0xFF;
    }

    private ushort DRAM8MBITCs0ReadWord(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                return T1ReadWord(_dram, address & 0x7FFFF);
            case 0x06: // Dram area
                return T1ReadWord(_dram, 0x80000 | (address & 0x7FFFF));
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFF;
    }

    private uint DRAM8MBITCs0ReadLong(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                return T1ReadLong(_dram, address & 0x7FFFF);
            case 0x06: // Dram area
                return T1ReadLong(_dram, 0x80000 | (address & 0x7FFFF));
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFFFFFF;
    }

    private void DRAM8MBITCs0WriteByte(TSh2 sh, uint address, byte value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                T1WriteByte(_dram, address & 0x7FFFF, value);
                break;
            case 0x06: // Dram area
                T1WriteByte(_dram, 0x80000 | (address & 0x7FFFF), value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void DRAM8MBITCs0WriteWord(TSh2 sh, uint address, ushort value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                T1WriteWord(_dram, address & 0x7FFFF, value);
                break;
            case 0x06: // Dram area
                T1WriteWord(_dram, 0x80000 | (address & 0x7FFFF), value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void DRAM8MBITCs0WriteLong(TSh2 sh, uint address, uint value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04: // Dram area
                T1WriteLong(_dram, address & 0x7FFFF, value);
                break;
            case 0x06: // Dram area
                T1WriteLong(_dram, 0x80000 | (address & 0x7FFFF), value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private byte DRAM32MBITCs0ReadByte(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Dram area
                return T1ReadByte(_dram, address & 0x3FFFFF);
            default:   // The rest doesn't matter
                break;
        }

        return 0xFF;
    }

    private ushort DRAM32MBITCs0ReadWord(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                return T1ReadWord(_dram, address & 0x3FFFFF);
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFF;
    }

    private uint DRAM32MBITCs0ReadLong(TSh2 sh, uint address)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                return T1ReadLong(_dram, address & 0x3FFFFF);
            default:   // The rest doesn't matter
                break;
        }

        return 0xFFFFFFFF;
    }

    private void DRAM32MBITCs0WriteByte(TSh2 sh, uint address, byte value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteByte(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void DRAM32MBITCs0WriteWord(TSh2 sh, uint address, ushort value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteWord(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private void DRAM32MBITCs0WriteLong(TSh2 sh, uint address, uint value)
    {
        address &= 0x1FFFFFF;

        switch (address >> 20)
        {
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07: // Ram cart area
                T1WriteLong(_dram, address & 0x3FFFFF, value);
                break;
            default:   // The rest doesn't matter
                break;
        }
    }

    private byte BUP4MBITCs1ReadByte(TSh2 sh, uint address)
    {
        return T1ReadByte(_backupRam, address & 0xFFFFF);
    }

    private ushort BUP4MBITCs1ReadWord(TSh2 sh, uint address)
    {
        return T1ReadWord(_backupRam, address & 0xFFFFF);
    }

    private uint BUP4MBITCs1ReadLong(TSh2 sh, uint address)
    {
        return T1ReadLong(_backupRam, address & 0xFFFFF);
    }

    private void BUP4MBITCs1WriteByte(TSh2 sh, uint address, byte value)
    {
        T1WriteByte(_backupRam, address & 0xFFFFF, value);
    }

    private void BUP4MBITCs1WriteWord(TSh2 sh, uint address, ushort value)
    {
        T1WriteWord(_backupRam, address & 0xFFFFF, value);
    }

    private void BUP4MBITCs1WriteLong(TSh2 sh, uint address, uint value)
    {
        T1WriteLong(_backupRam, address & 0xFFFFF, value);
    }

    private byte BUP8MBITCs1ReadByte(TSh2 sh, uint address)
    {
        return T1ReadByte(_backupRam, address & 0x1FFFFF);
    }

    private ushort BUP8MBITCs1ReadWord(TSh2 sh, uint address)
    {
        return T1ReadWord(_backupRam, address & 0x1FFFFF);
    }

    private uint BUP8MBITCs1ReadLong(TSh2 sh, uint address)
    {
        return T1ReadLong(_backupRam, address & 0x1FFFFF);
    }

    private void BUP8MBITCs1WriteByte(TSh2 sh, uint address, byte value)
    {
        T1WriteByte(_backupRam, address & 0x1FFFFF, value);
    }

    private void BUP8MBITCs1WriteWord(TSh2 sh, uint address, ushort value)
    {
        T1WriteWord(_backupRam, address & 0x1FFFFF, value);
    }

    private void BUP8MBITCs1WriteLong(TSh2 sh, uint address, uint value)
    {
        T1WriteLong(_backupRam, address & 0x1FFFFF, value);
    }

    private byte BUP16MBITCs1ReadByte(TSh2 sh, uint address)
    {
        return T1ReadByte(_backupRam, address & 0x3FFFFF);
    }

    private ushort BUP16MBITCs1ReadWord(TSh2 sh, uint address)
    {
        return T1ReadWord(_backupRam, address & 0x3FFFFF);
    }

    private uint BUP16MBITCs1ReadLong(TSh2 sh, uint address)
    {
        return T1ReadLong(_backupRam, address & 0x3FFFFF);
    }

    private void BUP16MBITCs1WriteByte(TSh2 sh, uint address, byte value)
    {
        T1WriteByte(_backupRam, address & 0x3FFFFF, value);
    }

    private void BUP16MBITCs1WriteWord(TSh2 sh, uint address, ushort value)
    {
        T1WriteWord(_backupRam, address & 0x3FFFFF, value);
    }

    private void BUP16MBITCs1WriteLong(TSh2 sh, uint address, uint value)
    {
        T1WriteLong(_backupRam, address & 0x3FFFFF, value);
    }

    private byte BUP32MBITCs1ReadByte(TSh2 sh, uint address)
    {
        return T1ReadByte(_backupRam, address & 0x7FFFFF);
    }

    private ushort BUP32MBITCs1ReadWord(TSh2 sh, uint address)
    {
        return T1ReadWord(_backupRam, address & 0x7FFFFF);
    }

    private uint BUP32MBITCs1ReadLong(TSh2 sh, uint address)
    {
        return T1ReadLong(_backupRam, address & 0x7FFFFF);
    }

    private void BUP32MBITCs1WriteByte(TSh2 sh, uint address, byte value)
    {
        T1WriteByte(_backupRam, address & 0x7FFFFF, value);
    }

    private void BUP32MBITCs1WriteWord(TSh2 sh, uint address, ushort value)
    {
        T1WriteWord(_backupRam, address & 0x7FFFFF, value);
    }

    private void BUP32MBITCs1WriteLong(TSh2 sh, uint address, uint value)
    {
        T1WriteLong(_backupRam, address & 0x7FFFFF, value);
    }

    private byte ROM16MBITCs0ReadByte(TSh2 sh, uint address)
    {
        return T1ReadByte(_rom, address & 0x1FFFFF);
    }

    private ushort ROM16MBITCs0ReadWord(TSh2 sh, uint address)
    {
        return T1ReadWord(_rom, address & 0x1FFFFF);
    }

    private uint ROM16MBITCs0ReadLong(TSh2 sh, uint address)
    {
        return T1ReadLong(_rom, address & 0x1FFFFF);
    }

    private void ROM16MBITCs0WriteByte(TSh2 sh, uint address, byte value)
    {
        T1WriteByte(_rom, address & 0x1FFFFF, value);
    }

    private void ROM16MBITCs0WriteWord(TSh2 sh, uint address, ushort value)
    {
        T1WriteWord(_rom, address & 0x1FFFFF, value);
    }

    private void ROM16MBITCs0WriteLong(TSh2 sh, uint address, uint value)
    {
        T1WriteLong(_rom, address & 0x1FFFFF, value);
    }

    public enum CartridgeType
    {
        None = 0,
        ActionReplay = 1,
        BackupRam4Mbit = 2,
        BackupRam8Mbit = 3,
        BackupRam16Mbit = 4,
        BackupRam32Mbit = 5,
        Dram8Mbit = 6,
        Dram32Mbit = 7,
        Netlink = 8,
        Rom16Mbit = 9,
        JapaneseModem = 10,
        UsbDevelopment = 11
    }

    private CartridgeType _type;
    private byte _cartridgeId = 0xFF;
    private Func<TSh2, uint, byte> _cs0ReadByte = DummyCs0ReadByte;
    private Action<TSh2, uint, byte> _cs0WriteByte = DummyCs0WriteByte;
    private Func<TSh2, uint, ushort> _cs0ReadWord = DummyCs0ReadWord;
    private Action<TSh2, uint, ushort> _cs0WriteWord = DummyCs0WriteWord;
    private Func<TSh2, uint, uint> _cs0ReadLong = DummyCs0ReadLong;
    private Action<TSh2, uint, uint> _cs0WriteLong = DummyCs0WriteLong;
    private Func<TSh2, uint, byte> _cs1ReadByte = DummyCs1ReadByte;
    private Action<TSh2, uint, byte> _cs1WriteByte = DummyCs1WriteByte;
    private Func<TSh2, uint, ushort> _cs1ReadWord = DummyCs1ReadWord;
    private Action<TSh2, uint, ushort> _cs1WriteWord = DummyCs1WriteWord;
    private Func<TSh2, uint, uint> _cs1ReadLong = DummyCs1ReadLong;
    private Action<TSh2, uint, uint> _cs1WriteLong = DummyCs1WriteLong;
    private Func<TSh2, uint, byte> _cs2ReadByte = DummyCs2ReadByte;
    private Action<TSh2, uint, byte> _cs2WriteByte = DummyCs2WriteByte;
    private readonly SaturnCartridgeChipSelect<TSh2> _cs1;

    public SaturnCartridge()
    {
        _cs1 = new SaturnCartridgeChipSelect<TSh2>(
            () => _cartridgeId,
            (sh, address) => _cs1ReadByte(sh, address),
            (sh, address) => _cs1ReadWord(sh, address),
            (sh, address) => _cs1ReadLong(sh, address),
            (sh, address, value) => _cs1WriteByte(sh, address, value),
            (sh, address, value) => _cs1WriteWord(sh, address, value),
            (sh, address, value) => _cs1WriteLong(sh, address, value));
    }

    public byte ReadCs0Byte(TSh2 sh, uint address) => _cs0ReadByte(sh, address);
    public void WriteCs0Byte(TSh2 sh, uint address, byte value) => _cs0WriteByte(sh, address, value);
    public ushort ReadCs0Word(TSh2 sh, uint address) => _cs0ReadWord(sh, address);
    public void WriteCs0Word(TSh2 sh, uint address, ushort value) => _cs0WriteWord(sh, address, value);
    public uint ReadCs0Long(TSh2 sh, uint address) => _cs0ReadLong(sh, address);
    public void WriteCs0Long(TSh2 sh, uint address, uint value) => _cs0WriteLong(sh, address, value);

    public byte ReadCs1Byte(TSh2 sh, uint address) => _cs1.Cs1ReadByte(sh, address);
    public void WriteCs1Byte(TSh2 sh, uint address, byte value) => _cs1.Cs1WriteByte(sh, address, value);
    public ushort ReadCs1Word(TSh2 sh, uint address) => _cs1.Cs1ReadWord(sh, address);
    public void WriteCs1Word(TSh2 sh, uint address, ushort value) => _cs1.Cs1WriteWord(sh, address, value);
    public uint ReadCs1Long(TSh2 sh, uint address) => _cs1.Cs1ReadLong(sh, address);
    public void WriteCs1Long(TSh2 sh, uint address, uint value) => _cs1.Cs1WriteLong(sh, address, value);

    public byte ReadCs2Byte(TSh2 sh, uint address) => _cs2ReadByte(sh, address);
    public void WriteCs2Byte(TSh2 sh, uint address, byte value) => _cs2WriteByte(sh, address, value);
    public ushort ReadCs2Word(TSh2 sh, uint address) => DummyCs2ReadWord(sh, address);
    public void WriteCs2Word(TSh2 sh, uint address, ushort value) => DummyCs2WriteWord(sh, address, value);
    public uint ReadCs2Long(TSh2 sh, uint address) => DummyCs2ReadLong(sh, address);
    public void WriteCs2Long(TSh2 sh, uint address, uint value) => DummyCs2WriteLong(sh, address, value);

    public bool Initialize(CartridgeType type, Stream? image)
    {
        _type = type;
        _cartridgeId = 0xFF;
        _rom = [];
        _dram = [];
        _backupRam = [];

        _cs0ReadByte = DummyCs0ReadByte;
        _cs0WriteByte = DummyCs0WriteByte;
        _cs0ReadWord = DummyCs0ReadWord;
        _cs0WriteWord = DummyCs0WriteWord;
        _cs0ReadLong = DummyCs0ReadLong;
        _cs0WriteLong = DummyCs0WriteLong;
        _cs1ReadByte = DummyCs1ReadByte;
        _cs1WriteByte = DummyCs1WriteByte;
        _cs1ReadWord = DummyCs1ReadWord;
        _cs1WriteWord = DummyCs1WriteWord;
        _cs1ReadLong = DummyCs1ReadLong;
        _cs1WriteLong = DummyCs1WriteLong;
        _cs2ReadByte = DummyCs2ReadByte;
        _cs2WriteByte = DummyCs2WriteByte;

        switch (type)
        {
            case CartridgeType.ActionReplay:
                _rom = new byte[0x40000];
                _dram = new byte[0x400000];
                _cartridgeId = 0x5C;
                if (!T123Load(_rom, 2, image))
                    return false;

                _vendorId = 0x1F;
                _deviceId = 0xD5;
                _flashState0 = FlashState.Read;
                _flashState1 = FlashState.Read;
                _cs0ReadByte = AR4MCs0ReadByte;
                _cs0WriteByte = AR4MCs0WriteByte;
                _cs0ReadWord = AR4MCs0ReadWord;
                _cs0WriteWord = AR4MCs0WriteWord;
                _cs0ReadLong = AR4MCs0ReadLong;
                _cs0WriteLong = AR4MCs0WriteLong;
                break;
            case CartridgeType.BackupRam4Mbit:
                _backupRam = new byte[0x100000];
                _cartridgeId = 0x21;
                if (!T123Load(_backupRam, 1, image))
                    SaturnMemory.FormatBackupRam(_backupRam);

                _cs1ReadByte = BUP4MBITCs1ReadByte;
                _cs1WriteByte = BUP4MBITCs1WriteByte;
                _cs1ReadWord = BUP4MBITCs1ReadWord;
                _cs1WriteWord = BUP4MBITCs1WriteWord;
                _cs1ReadLong = BUP4MBITCs1ReadLong;
                _cs1WriteLong = BUP4MBITCs1WriteLong;
                break;
            case CartridgeType.BackupRam8Mbit:
                _backupRam = new byte[0x200000];
                _cartridgeId = 0x22;
                if (!T123Load(_backupRam, 1, image))
                    SaturnMemory.FormatBackupRam(_backupRam);

                _cs1ReadByte = BUP8MBITCs1ReadByte;
                _cs1WriteByte = BUP8MBITCs1WriteByte;
                _cs1ReadWord = BUP8MBITCs1ReadWord;
                _cs1WriteWord = BUP8MBITCs1WriteWord;
                _cs1ReadLong = BUP8MBITCs1ReadLong;
                _cs1WriteLong = BUP8MBITCs1WriteLong;
                break;
            case CartridgeType.BackupRam16Mbit:
                _backupRam = new byte[0x400000];
                _cartridgeId = 0x23;
                if (!T123Load(_backupRam, 1, image))
                    SaturnMemory.FormatBackupRam(_backupRam);

                _cs1ReadByte = BUP16MBITCs1ReadByte;
                _cs1WriteByte = BUP16MBITCs1WriteByte;
                _cs1ReadWord = BUP16MBITCs1ReadWord;
                _cs1WriteWord = BUP16MBITCs1WriteWord;
                _cs1ReadLong = BUP16MBITCs1ReadLong;
                _cs1WriteLong = BUP16MBITCs1WriteLong;
                break;
            case CartridgeType.BackupRam32Mbit:
                _backupRam = new byte[0x800000];
                _cartridgeId = 0x24;
                if (!T123Load(_backupRam, 1, image))
                    SaturnMemory.FormatBackupRam(_backupRam);

                _cs1ReadByte = BUP32MBITCs1ReadByte;
                _cs1WriteByte = BUP32MBITCs1WriteByte;
                _cs1ReadWord = BUP32MBITCs1ReadWord;
                _cs1WriteWord = BUP32MBITCs1WriteWord;
                _cs1ReadLong = BUP32MBITCs1ReadLong;
                _cs1WriteLong = BUP32MBITCs1WriteLong;
                break;
            case CartridgeType.Dram8Mbit:
                _dram = new byte[0x100000];
                _cartridgeId = 0x5A;
                _cs0ReadByte = DRAM8MBITCs0ReadByte;
                _cs0WriteByte = DRAM8MBITCs0WriteByte;
                _cs0ReadWord = DRAM8MBITCs0ReadWord;
                _cs0WriteWord = DRAM8MBITCs0WriteWord;
                _cs0ReadLong = DRAM8MBITCs0ReadLong;
                _cs0WriteLong = DRAM8MBITCs0WriteLong;
                break;
            case CartridgeType.Dram32Mbit:
                _dram = new byte[0x400000];
                _cartridgeId = 0x5C;
                _cs0ReadByte = DRAM32MBITCs0ReadByte;
                _cs0WriteByte = DRAM32MBITCs0WriteByte;
                _cs0ReadWord = DRAM32MBITCs0ReadWord;
                _cs0WriteWord = DRAM32MBITCs0WriteWord;
                _cs0ReadLong = DRAM32MBITCs0ReadLong;
                _cs0WriteLong = DRAM32MBITCs0WriteLong;
                break;
            case CartridgeType.Netlink:
                return false;
            case CartridgeType.Rom16Mbit:
                _rom = new byte[0x200000];
                if (!T123Load(_rom, 1, image))
                    return false;

                _cs0ReadByte = ROM16MBITCs0ReadByte;
                _cs0WriteByte = ROM16MBITCs0WriteByte;
                _cs0ReadWord = ROM16MBITCs0ReadWord;
                _cs0WriteWord = ROM16MBITCs0WriteWord;
                _cs0ReadLong = ROM16MBITCs0ReadLong;
                _cs0WriteLong = ROM16MBITCs0WriteLong;
                break;
            case CartridgeType.JapaneseModem:
                _cs0ReadByte = SaturnJapaneseModem<TSh2>.Cs0ReadByte;
                _cs0ReadWord = SaturnJapaneseModem<TSh2>.Cs0ReadWord;
                _cs0ReadLong = SaturnJapaneseModem<TSh2>.Cs0ReadLong;

                _cs1ReadByte = SaturnJapaneseModem<TSh2>.Cs1ReadByte;
                _cs1ReadWord = SaturnJapaneseModem<TSh2>.Cs1ReadWord;
                _cs1ReadLong = SaturnJapaneseModem<TSh2>.Cs1ReadLong;
                _cs1WriteByte = SaturnJapaneseModem<TSh2>.Cs1WriteByte;
                _cs1WriteWord = SaturnJapaneseModem<TSh2>.Cs1WriteWord;
                _cs1WriteLong = SaturnJapaneseModem<TSh2>.Cs1WriteLong;

                _cs2ReadByte = SaturnJapaneseModem<TSh2>.Cs2ReadByte;
                _cs2WriteByte = SaturnJapaneseModem<TSh2>.Cs2WriteByte;
                break;
            case CartridgeType.UsbDevelopment:
                _rom = new byte[0x40000];
                _dram = new byte[0x400000];
                _cartridgeId = 0;
                if (!T123Load(_rom, 2, image))
                    return false;

                _vendorId = 0xBF;
                _deviceId = 0xB5;
                _flashState0 = FlashState.Read;
                _flashState1 = FlashState.Read;
                _cs0ReadByte = AR4MCs0ReadByte;
                _cs0WriteByte = AR4MCs0WriteByte;
                _cs0ReadWord = AR4MCs0ReadWord;
                _cs0WriteWord = AR4MCs0WriteWord;
                _cs0ReadLong = AR4MCs0ReadLong;
                _cs0WriteLong = AR4MCs0WriteLong;
                break;
        }

        return true;
    }

    internal byte CartridgeId => _cartridgeId;

    internal void FormatBackup()
    {
        if (_cartridgeId is >= 0x21 and <= 0x24)
            SaturnMemory.FormatBackupRam(_backupRam);
    }

    public bool Flush(Stream? image)
    {
        if (_type == CartridgeType.ActionReplay && _rom.Length != 0)
            return T123Save(_rom, 2, image);

        if (_backupRam.Length != 0)
            return T123Save(_backupRam, 1, image);

        return true;
    }

}
