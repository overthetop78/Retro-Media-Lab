// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause smpc.c.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnSmpc
{
    public sealed record Connections(
        Func<SaturnSmpcState.Region> Cs2GetRegionId,
        Action Cs2GetIp,
        Action StartSlave,
        Action StopSlave,
        Action StartM68k,
        Action StopM68k,
        Action ResetVdp1,
        Action ResetVdp2,
        Action ResetScu,
        Action ResetScsp,
        Action<int> ChangeTiming,
        Action MasterNmi,
        Action<int, int> SendMasterInterrupt,
        Action SendSystemManagerInterrupt,
        Func<int> FrameCounter,
        Func<bool> MovieActive,
        SaturnSmpcState.PortData Port1,
        SaturnSmpcState.PortData Port2,
        Action FlushPort1,
        Action FlushPort2,
        Action MarkFramePolled);

    private readonly Connections _connections;
    private readonly SaturnSmpcState _state = new();
    private bool _intbackWaitForLine;

    public SaturnSmpc(Connections connections, SaturnSmpcState.Region region, bool clockSync, uint baseTime)
    {
        _connections = connections;
        _state.RegionSetting = region;
        _state.RegionId = region;
        _state.ClockSync = clockSync;
        _state.BaseTime = baseTime != 0 ? baseTime : unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public SaturnSmpcState.Region RegionId => _state.RegionId;

    public void RecheckRegion()
    {
        if (_state.RegionSetting == SaturnSmpcState.Region.AutoDetect)
        {
            _state.RegionId = _connections.Cs2GetRegionId();
            if (_state.RegionId == SaturnSmpcState.Region.AutoDetect)
                _state.RegionId = SaturnSmpcState.Region.Japan;
        }
        else
            _connections.Cs2GetIp();
    }

    public void SetupQuickLoad()
    {
        _state.Command = 0x10;
        _state.ResetDisabled = 0;
    }


    public void Reset()
    {
        Array.Clear(_state.Registers);
        Array.Clear(_state.Smem);
        RecheckRegion();
        _state.DotSelect = 0;
        _state.MasterNmi = 0;
        _state.SystemReset = 0;
        _state.SoundReset = 0;
        _state.ResetDisabled = 1;
        _state.Ste = 0;
        _state.ResetButton = 0;
        _state.Intback = 0;
        _state.IntbackInput0 = 0;
        _state.FirstPeripheral = 0;
        _state.Timing = 0;
        _state.RtcSetAwaitingCommit = false;

        _state.Port1.Offset = 0;
        _state.Port1.Size = 0;
        Array.Clear(_state.Port1.Data);
        _state.Port2.Offset = 0;
        _state.Port2.Size = 0;
        Array.Clear(_state.Port2.Data);
    }

    private void StartSound()
    {
        _connections.StartM68k();
        _state.Output[31] = 0x06;
    }

    private void StopSound()
    {
        _connections.StopM68k();
        _state.Output[31] = 0x07;
    }

    public void ChangeClock352()
    {
        _connections.ResetVdp1();
        _connections.ResetVdp2();
        _connections.ResetScu();
        _connections.ResetScsp();
        _connections.StopSlave();
        _connections.ChangeTiming(1);
        _state.DotSelect = 1;
        _connections.MasterNmi();
    }

    public void ChangeClock320()
    {
        _connections.ResetVdp1();
        _connections.ResetVdp2();
        _connections.ResetScu();
        _connections.ResetScsp();
        _connections.StopSlave();
        _connections.ChangeTiming(0);
        _state.DotSelect = 0;
        _connections.MasterNmi();
    }

    private void IntbackStatus()
    {
        Span<byte> output = _state.Output;
        output[0] = (byte)(0x80 | (_state.ResetDisabled << 6));

        if (_state.RtcValid)
            _state.Rtc.AsSpan().CopyTo(output[1..]);
        else
        {
            long timestamp;
            if (_state.ClockSync)
                timestamp = (long)(_state.BaseTime + (ulong)_connections.FrameCounter() * 1001 / 60000);
            else
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime();
            output[1] = (byte)((time.Year / 1000 << 4) | (time.Year % 1000 / 100));
            output[2] = (byte)((time.Year % 100 / 10 << 4) | (time.Year % 10));
            output[3] = (byte)(((int)time.DayOfWeek << 4) | time.Month);
            output[4] = (byte)(((time.Day / 10) << 4) | (time.Day % 10));
            output[5] = (byte)(((time.Hour / 10) << 4) | (time.Hour % 10));
            output[6] = (byte)(((time.Minute / 10) << 4) | (time.Minute % 10));
            output[7] = (byte)(((time.Second / 10) << 4) | (time.Second % 10));
        }

        bool movieActive = _connections.MovieActive();
        if (movieActive)
        {
            const int year = 1900 + 0x62;
            const int noon = 43200;
            int totalSeconds = _connections.FrameCounter() / 60 + noon;
            int seconds = totalSeconds % 60;
            int minutes = (totalSeconds / 60) % 60;
            int hours = (totalSeconds / 60 / 60) % 24;
            output[1] = (byte)(((year / 1000) << 4) | (year % 1000 / 100));
            output[2] = (byte)(((year % 100 / 10) << 4) | (year % 10));
            output[3] = 0x41;
            output[4] = 1;
            output[5] = (byte)(((hours / 10) << 4) | (hours % 10));
            output[6] = (byte)(((minutes / 10) << 4) | (minutes % 10));
            output[7] = (byte)(((seconds / 10) << 4) | (seconds % 10));
        }

        output[8] = 0;
        output[9] = (byte)_state.RegionId;
        output[10] = (byte)(0x34 | (_state.DotSelect << 6) | (_state.MasterNmi << 3) |
            (_state.SystemReset << 1) | _state.SoundReset);
        output[11] = 0;
        for (int index = 0; index < 4; index++)
            output[12 + index] = _state.Smem[index];
        output[31] = 0x10;
    }

    private void IntbackPeripheral()
    {
        int portModes = _state.Registers[1] >> 4;
        if (_state.FirstPeripheral != 0)
            _state.Status = (byte)(0xC0 | portModes);
        else
            _state.Status = (byte)(0x80 | portModes);
        _state.FirstPeripheral = 0;

        int outputOffset = 0;
        SaturnSmpcState.PortData port1 = _state.Port1;
        SaturnSmpcState.PortData port2 = _state.Port2;
        if (port1.Size == 0 && port2.Size == 0)
        {
            port1.Size = _connections.Port1.Size;
            _connections.Port1.Data.AsSpan().CopyTo(port1.Data);
            port2.Size = _connections.Port2.Size;
            _connections.Port2.Data.AsSpan().CopyTo(port2.Data);
            _connections.FlushPort1();
            _connections.FlushPort2();
            port1.Offset = 0;
            port2.Offset = 0;
            _connections.MarkFramePolled();
        }

        Span<byte> output = _state.Output;
        if (port1.Size > 0)
        {
            if (port1.Size - port1.Offset < 32)
            {
                int remaining = port1.Size - port1.Offset;
                port1.Data.AsSpan(port1.Offset, remaining).CopyTo(output);
                outputOffset += remaining;
                port1.Size = 0;
            }
            else
            {
                port1.Data.AsSpan(0, 32).CopyTo(output);
                outputOffset += 32;
                port1.Offset += 32;
            }
        }

        if (port2.Size > 0 && outputOffset < 32)
        {
            if (port2.Size - port2.Offset < 32 - outputOffset)
            {
                port2.Data.AsSpan(port2.Offset, port2.Size - port2.Offset).CopyTo(output[outputOffset..]);
                port2.Size = 0;
            }
            else
            {
                port2.Data.AsSpan(0, 32 - outputOffset).CopyTo(output[outputOffset..]);
                port2.Offset += 32 - outputOffset;
            }
        }
    }

    private void Intback()
    {
        _state.StatusFlag = 1;
        if (_state.Intback != 0)
        {
            IntbackPeripheral();
            _connections.SendSystemManagerInterrupt();
            return;
        }

        _state.IntbackInput0 = (byte)(_state.Registers[0] & 1);
        if (_state.IntbackInput0 != 0)
        {
            _state.FirstPeripheral = 1;
            _state.Intback = (byte)((_state.Registers[1] & 8) >> 3);
            IntbackStatus();
            _state.Status = (byte)(0x4F | (_state.Intback << 5));
            _connections.SendSystemManagerInterrupt();
            return;
        }

        if ((_state.Registers[1] & 8) != 0)
        {
            _state.FirstPeripheral = 1;
            _state.Intback = 1;
            _state.Status = 0x40;
            IntbackPeripheral();
            _state.Output[31] = 0x10;
            _connections.SendSystemManagerInterrupt();
        }
    }

    public void EndIntback() => _state.Intback = 0;

    // Mednafen smpc.cpp, CMD_SETTIME and RTC_IncTime.
    private void SetTime()
    {
        _state.RtcMicroseconds = 0;
        _state.RtcValid = true;
        _state.Registers.AsSpan(0, 7).CopyTo(_state.Rtc);
    }

    private static int IncrementRtcBcd(byte value)
    {
        int next = (value & 0xF) + 1;
        if (next >= 0xA)
            next += 0x06;
        next += value & 0xF0;
        if (next >= 0xA0)
            next += 0x60;
        return next;
    }

    private void IncrementRtcTime()
    {
        Span<byte> rtc = _state.Rtc;
        if (rtc[6] != 0x59)
        {
            rtc[6] = unchecked((byte)IncrementRtcBcd(rtc[6]));
            return;
        }
        rtc[6] = 0;
        if (rtc[5] != 0x59)
        {
            rtc[5] = unchecked((byte)IncrementRtcBcd(rtc[5]));
            return;
        }
        rtc[5] = 0;
        if (rtc[4] != 0x23)
        {
            rtc[4] = unchecked((byte)IncrementRtcBcd(rtc[4]));
            return;
        }
        rtc[4] = 0;

        if (rtc[2] >= 0x60)
            rtc[2] &= 0x0F;
        else
            rtc[2] += 0x10;

        ReadOnlySpan<byte> monthDays = [0x10, 0x31, 0x28, 0x31, 0x30, 0x31, 0x30, 0x31,
            0x31, 0x30, 0x31, 0x30, 0x31, 0xC1, 0xF5, 0xFF];
        int month = rtc[2] & 0x0F;
        int lastDay = monthDays[month];
        if (month == 2 && (rtc[1] & 0x1F) < 0x1A && ((rtc[1] + ((rtc[1] & 0x10) >> 3)) & 3) == 0)
            lastDay++;

        if (rtc[3] < lastDay)
        {
            rtc[3] = unchecked((byte)IncrementRtcBcd(rtc[3]));
            return;
        }
        rtc[3] = 1;
        if (month != 0x0C)
        {
            rtc[2]++;
            return;
        }

        rtc[2] = (byte)((rtc[2] & 0xF0) | 1);
        int year = IncrementRtcBcd(rtc[1]);
        rtc[1] = unchecked((byte)year);
        if (year >= 0x100)
            rtc[0] = unchecked((byte)IncrementRtcBcd(rtc[0]));
    }

    private void SetSmem()
    {
        for (int index = 0; index < 4; index++)
            _state.Smem[index] = _state.Registers[index];
        _state.Output[31] = 0x17;
    }

    private void RequestNmi()
    {
        _connections.SendMasterInterrupt(0xB, 16);
        _state.Output[31] = 0x18;
    }

    public void ResetButton()
    {
        if (_state.ResetDisabled != 0)
            return;
        _connections.SendMasterInterrupt(0xB, 16);
    }

    private void EnableReset()
    {
        _state.ResetDisabled = 0;
        _state.Output[31] = 0x19;
    }

    private void DisableReset()
    {
        _state.ResetDisabled = 1;
        _state.Output[31] = 0x1A;
    }

    public void Execute(int microseconds, int line)
    {
        if (_state.RtcValid)
        {
            _state.RtcMicroseconds += microseconds;
            while (_state.RtcMicroseconds >= 1_000_000)
            {
                IncrementRtcTime();
                _state.RtcMicroseconds -= 1_000_000;
            }
        }

        if (_state.Timing > 0)
        {
            if (_intbackWaitForLine && line == 207)
            {
                _state.Timing = -1;
                _intbackWaitForLine = false;
            }

            _state.Timing -= microseconds;
            if (_state.Timing <= 0)
            {
                byte command = _state.Command;
                switch (command)
                {
                    case 0x02: _connections.StartSlave(); break;
                    case 0x03: _connections.StopSlave(); break;
                    case 0x06: StartSound(); break;
                    case 0x07: StopSound(); break;
                    case 0x0E: ChangeClock352(); break;
                    case 0x0F: ChangeClock320(); break;
                    case 0x10: Intback(); break;
                    case 0x16:
                        if (!_state.RtcSetAwaitingCommit)
                        {
                            _state.Output[31] = 0x16;
                            _state.RtcSetAwaitingCommit = true;
                            _state.Timing += 95; // 380 SMPC clocks at 4 MHz.
                            if (_state.Timing > 0)
                                return;
                        }
                        _state.RtcSetAwaitingCommit = false;
                        SetTime();
                        break;
                    case 0x17: SetSmem(); break;
                    case 0x18: RequestNmi(); break;
                    case 0x19: EnableReset(); break;
                    case 0x1A: DisableReset(); break;
                }
                _state.StatusFlag = 0;
            }
        }
    }

    public byte ReadByte(uint address) => _state.Registers[(address & 0x7F) >> 1];

    internal byte ExternalLatchEnable => _state.ExternalLatchEnable;
    internal ReadOnlySpan<byte> Port1Data => _connections.Port1.Data;

    public ushort ReadWord(uint address) => 0;

    public uint ReadLong(uint address) => 0;

    private void SetTiming()
    {
        switch (_state.Command)
        {
            case 0:
            case 0x08:
            case 0x09:
            case 0x0D:
            case 0x0E:
            case 0x0F:
                _state.Timing = 1;
                return;
            case 0x10:
                if (_state.Intback != 0)
                {
                    _state.Timing = 16000;
                    _intbackWaitForLine = true;
                }
                else
                {
                    if (_state.Registers[0] == 0x01 && (_state.Registers[1] & 8) != 0)
                        _state.Timing = 250;
                    else if (_state.Registers[0] == 0x01 && (_state.Registers[1] & 8) == 0)
                        _state.Timing = 250;
                    else if (_state.Registers[0] == 0 && (_state.Registers[1] & 8) != 0)
                    {
                        _state.Timing = 16000;
                        _intbackWaitForLine = true;
                    }
                }
                return;
            case 0x16:
                _state.RtcSetAwaitingCommit = false;
                _state.Timing = 23; // 92 SMPC clocks at 4 MHz.
                return;
            case 0x17:
            case 0x02:
            case 0x03:
            case 0x06:
            case 0x07:
            case 0x18:
            case 0x19:
            case 0x1A:
                _state.Timing = 1;
                return;
            default:
                _state.StatusFlag = 0;
                break;
        }
    }

    private static byte DoThMode(byte value, SaturnSmpcState.PortData port)
    {
        switch (value & 0x40)
        {
            case 0x40: return (byte)(0x70 | (port.Data[3] & 0x0C));
            case 0x00: return (byte)(0x30 | ((port.Data[2] >> 4) & 0x0F));
        }
        return 0;
    }

    public void WriteByte(uint address, byte value)
    {
        address &= 0x7F;
        _state.Registers[address >> 1] = value;

        const byte gun = 0x25;
        switch (address)
        {
            case 0x01:
                if (_state.Intback != 0)
                {
                    if ((value & 0x40) != 0)
                    {
                        EndIntback();
                        _state.Status &= 0x0F;
                    }
                    else if ((value & 0x80) != 0)
                    {
                        _state.Command = 0x10;
                        SetTiming();
                        _state.StatusFlag = 1;
                    }
                }
                return;
            case 0x1F:
                SetTiming();
                return;
            case 0x63:
                _state.StatusFlag = (byte)(value & 1);
                return;
            case 0x75:
            {
                byte[] data = _connections.Port1.Data;
                switch (_state.DataDirection[0] & 0x7F)
                {
                    case 0x00:
                        if (data[1] == gun && (value & 0x7F) == 0x7F)
                            _state.PortDataRegisters[0] = data[2];
                        break;
                    case 0x40:
                        _state.PortDataRegisters[0] = DoThMode(value, _connections.Port1);
                        break;
                    case 0x60:
                        switch (value & 0x60)
                        {
                            case 0x60: value = (byte)((value & 0x80) | 0x14 | (data[3] & 8)); break;
                            case 0x20: value = (byte)((value & 0x80) | 0x10 | ((data[2] >> 4) & 0xF)); break;
                            case 0x40: value = (byte)((value & 0x80) | 0x10 | (data[2] & 0xF)); break;
                            case 0x00: value = (byte)((value & 0x80) | 0x10 | ((data[3] >> 4) & 0xF)); break;
                        }
                        _state.PortDataRegisters[0] = value;
                        break;
                }
                break;
            }
            case 0x77:
            {
                byte[] data = _connections.Port2.Data;
                switch (_state.DataDirection[1] & 0x7F)
                {
                    case 0x00:
                        if (data[1] == gun && (value & 0x7F) == 0x7F)
                            _state.PortDataRegisters[1] = data[2];
                        break;
                    case 0x60:
                        switch (value & 0x60)
                        {
                            case 0x60: value = (byte)((value & 0x80) | 0x14 | (data[3] & 8)); break;
                            case 0x20: value = (byte)((value & 0x80) | 0x10 | ((data[2] >> 4) & 0xF)); break;
                            case 0x40: value = (byte)((value & 0x80) | 0x10 | (data[2] & 0xF)); break;
                            case 0x00: value = (byte)((value & 0x80) | 0x10 | ((data[3] >> 4) & 0xF)); break;
                        }
                        _state.PortDataRegisters[1] = value;
                        break;
                }
                break;
            }
            case 0x79:
                switch (_state.DataDirection[0] & 0x7F)
                {
                    case 0x00:
                    case 0x40:
                        switch (_connections.Port1.Data[0])
                        {
                            case 0xA0:
                                if (_connections.Port1.Data[1] == gun)
                                    _state.PortDataRegisters[0] = 0x7C;
                                break;
                            case 0xF0:
                                _state.PortDataRegisters[0] = 0x7F;
                                break;
                            case 0xF1:
                                switch (_connections.Port1.Data[1])
                                {
                                    case 0x02: _state.PortDataRegisters[0] = 0x7C; break;
                                    case 0x16:
                                    case 0x34: _state.PortDataRegisters[0] = 0x71; break;
                                    case 0xE3: _state.PortDataRegisters[0] = 0x70; break;
                                }
                                break;
                            default:
                                _state.PortDataRegisters[0] = 0x71;
                                break;
                        }
                        break;
                }
                break;
            case 0x7B:
                break;
            case 0x7D:
                _state.IoSelect = value;
                break;
            case 0x7F:
                _state.ExternalLatchEnable = value;
                break;
            default:
                return;
        }
    }

    public void WriteWord(uint address, ushort value) { }

    public void WriteLong(uint address, uint value) { }
}
