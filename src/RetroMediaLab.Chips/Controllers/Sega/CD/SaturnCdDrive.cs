// Copyright 2014-2016 James Laird-Wah
// Copyright 2004-2006, 2013 Theo Berkau
// Translated from Yabause cd_drive.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Hitachi.SH1;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed class SaturnCdDrive(
    SaturnCdBlock cdBlock,
    SaturnSh7034 sh1,
    SaturnMemory memory,
    SaturnYgr ygr,
    SaturnCdBlock.CddaReceiver receiveCdda)
{
    private const int TimeOe = 26;
    private const int TimePeriodic = 13992;
    private const int TimeStart = 187;
    private const int TimePowerOn = 451448;
    private const int TimeWaiting = 416509;
    private const int TimeByte = 150;
    private const int TimeReading = 6600;
    private const int TimeReadSector = 8730;
    private const int TimeAudioSector = 13333 - 2299;

    private enum StatusOperation : byte
    {
        ReadToc = 0x04,
        Idle = 0x46,
        Stopped = 0x12,
        Seeking = 0x22,
        LidOpen = 0x80,
        NoDisc = 0x83,
        ReadingDataSectors = 0x36,
        ReadingAudioData = 0x34,
        Unknown = 0x30,
        SeekSecurityRing1 = 0xB2,
        SeekSecurityRing2 = 0xB6
    }

    private enum CommunicationState
    {
        NoTransfer,
        Reset,
        Started,
        SendingFirstByte,
        ByteFinished,
        FirstByteFinished,
        SendingByte,
        SendingByteFinished,
        Running,
        NewTransfer,
        WaitToOe,
        WaitToOeFirstByte,
        WaitToRxio
    }

    private SaturnCdDriveState _drive = new();
    private CommunicationState _communication;
    private SaturnCdInterface CdInterface => cdBlock.CdInterface;

    private static void FadToMsf(int fad, Span<byte> msf)
    {
        msf[0] = unchecked((byte)(fad / (75 * 60)));
        fad -= msf[0] * (75 * 60);
        msf[1] = unchecked((byte)(fad / 75));
        fad -= msf[1] * 75;
        msf[2] = unchecked((byte)fad);
    }

    private static byte NumberToBcd(byte number) => (byte)(((number / 10) << 4) | (number % 10));

    private static void FadToMsfBcd(int fad, Span<byte> msf)
    {
        FadToMsf(fad, msf);
        msf[0] = NumberToBcd(msf[0]);
        msf[1] = NumberToBcd(msf[1]);
        msf[2] = NumberToBcd(msf[2]);
    }

    private static byte BcdToNumber(byte bcd) => (byte)((bcd >> 4) * 10 + (bcd & 0xF));

    internal static uint MsfBcdToFad(byte minutes, byte seconds, byte frames)
    {
        uint fad = 0;
        fad += BcdToNumber(minutes);
        fad *= 60;
        fad += BcdToNumber(seconds);
        fad *= 75;
        fad += BcdToNumber(frames);
        return fad;
    }

    internal int GetSerialBit()
    {
        int bit = 1 << (7 - _drive.BitCounter);
        return (_drive.StateData[_drive.ByteCounter] & bit) != 0 ? 1 : 0;
    }

    internal void SetSerialBit(int bit)
    {
        _drive.ReceivedData[_drive.ByteCounter] |= (byte)(bit << _drive.BitCounter);
        _drive.BitCounter++;

        if (_drive.BitCounter == 8)
        {
            _drive.ByteCounter++;
            _drive.BitCounter = 0;
            // In cd_drive.c the OE rising edge is trace-only.

            if (_communication == CommunicationState.SendingFirstByte)
                _communication = CommunicationState.WaitToOeFirstByte;
            else if (_communication == CommunicationState.SendingByte)
                _communication = CommunicationState.WaitToOe;

            if (_drive.ByteCounter == 13)
                _communication = CommunicationState.WaitToRxio;
        }
    }

    private void DoToc()
    {
        _drive.State.CurrentOperation = (byte)StatusOperation.ReadToc;
        _drive.StateData[0] = _drive.State.CurrentOperation;
        _communication = CommunicationState.NoTransfer;

        SaturnCdInterface.Toc10Entry entry = _drive.Toc[_drive.TocEntry++];
        _drive.StateData[1] = entry.ControlAddress;
        _drive.StateData[2] = entry.TrackNumber;
        _drive.StateData[3] = entry.Point;
        _drive.StateData[4] = entry.Minute;
        _drive.StateData[5] = entry.Second;
        _drive.StateData[6] = entry.Frame;
        _drive.StateData[7] = entry.Zero;
        _drive.StateData[8] = entry.PointMinute;
        _drive.StateData[9] = entry.PointSecond;
        _drive.StateData[10] = entry.PointFrame;
        SetChecksum(_drive.StateData);

        if (_drive.TocEntry >= _drive.NumTocEntries)
        {
            _drive.State.CurrentOperation = (byte)StatusOperation.Idle;
            MakeStatusData(_drive.State, _drive.StateData);
        }
    }

    private void UpdateStatusInfo()
    {
        int index = 0;
        int trackNumber = 0;
        int trackFad;
        if (_drive.DiscFad >= GetTrackStartFad(-1))
            trackFad = unchecked((int)_drive.DiscFad);
        else
        {
            trackNumber = Toc10GetTrack((int)_drive.DiscFad);
            GetTrackFad(trackNumber, (int)(_drive.DiscFad + 4), ref index);
            trackFad = unchecked((int)(_drive.DiscFad - GetTrackStartFad(trackNumber)));
        }

        if (trackFad < 0)
            trackFad = -trackFad;
        else
            index = 1;

        StateSetMsfInfo(ref _drive.State, trackFad, unchecked((int)_drive.DiscFad));

        if (_drive.DiscFad >= GetTrackStartFad(-1))
        {
            _drive.State.QSubcode = 1;
            _drive.State.IndexField = 1;
            _drive.State.TrackNumber = 0xAA;
        }
        else
        {
            _drive.State.QSubcode = _drive.Tracks[trackNumber - 1].ControlAddress;
            _drive.State.IndexField = (byte)index;
            _drive.State.TrackNumber = NumberToBcd((byte)trackNumber);
        }
    }

    private static byte RotateRight(byte value) => unchecked((byte)((value >> 1) | (value << 7)));

    private static void MakeRingData(Span<byte> buffer)
    {
        ushort lfsr = 1;
        for (int i = 12; i < 2352; i++)
        {
            byte data = (i & 1) != 0 ? (byte)0x59 : (byte)0xA8;
            for (int j = 0; j < 8; j++)
            {
                data ^= (byte)(lfsr & 1);
                data = RotateRight(data);
                uint feedback = (uint)((lfsr >> 1) ^ lfsr);
                lfsr |= unchecked((ushort)(feedback << 15));
                lfsr >>= 1;
            }
            buffer[i - 12] = data;
        }
    }

    private void DoDataRead()
    {
        ref SaturnSh7034State.DmaChannel dma = ref sh1.DmaChannel0;
        if ((dma.Chcr & 1) != 0 && (dma.Chcr & 2) == 0)
        {
            Span<byte> buffer = stackalloc byte[2448];
            if (_drive.DiscFad < 150)
            {
                buffer.Clear();
                FadToMsfBcd((int)_drive.DiscFad, buffer[12..]);
                buffer[15] = 1;
            }
            else if (_drive.DiscFad >= GetTrackStartFad(-1))
            {
                Span<byte> sector = buffer[12..];
                MakeRingData(sector);
                FadToMsfBcd(unchecked((int)_drive.DiscFad), sector);
                sector[3] = 2;
                sector.Slice(4, 8).Clear();
                sector[6] = 28;
                sector[10] = 28;
                sector.Slice(2352 - 4, 4).Clear();
            }
            else
                CdInterface.ReadSectorFad(_drive.DiscFad, buffer);

            uint destination = dma.Dar & 0x7FFFF;
            for (int i = 0; i < dma.Tcr * 2; i += 2)
                T2WriteWord(memory.Sh1Dram, destination + (uint)i, (ushort)((buffer[12 + i] << 8) | buffer[13 + i]));

            dma.Chcr |= 2;
            if ((dma.Chcr & 4) != 0)
                sh1.SendDmaChannel0Interrupt();
        }

        _drive.DiscFad++;
        if (_drive.DiscFad >= 150 && _drive.DiscFad < GetTrackStartFad(-1))
            CdInterface.ReadAheadFad(_drive.DiscFad);

        ygr.CdIrq(0x10);
    }

    private void MakeRingStatus()
    {
        uint fad = _drive.DiscFad + 4;
        Span<byte> msf = stackalloc byte[3];
        FadToMsfBcd(unchecked((int)_drive.DiscFad), msf);

        _drive.StateData[0] = (byte)StatusOperation.SeekSecurityRing2;
        _drive.StateData[1] = 0x44;
        _drive.StateData[2] = 0xF1;
        _drive.StateData[3] = (byte)(fad >> 16);
        _drive.StateData[4] = (byte)(fad >> 8);
        _drive.StateData[5] = (byte)fad;
        _drive.StateData[6] = 0x09;
        _drive.StateData[7] = 0x09;
        _drive.StateData[8] = 0x09;
        _drive.StateData[9] = 0x09;
        _drive.StateData[10] = msf[2];
        SetChecksum(_drive.StateData);
    }

    private int ContinueCommand()
    {
        switch ((StatusOperation)_drive.State.CurrentOperation)
        {
            case StatusOperation.Idle:
                _communication = CommunicationState.NoTransfer;
                _drive.DiscFad++;
                if (_drive.DiscFad > _drive.TargetFad + 5)
                    _drive.DiscFad = _drive.TargetFad;
                UpdateStatusInfo();
                MakeStatusData(_drive.State, _drive.StateData);
                return TimePeriodic / _drive.Speed;

            case StatusOperation.ReadingDataSectors:
            case StatusOperation.ReadingAudioData:
            {
                bool isAudio = _drive.DiscFad >= 150 &&
                    _drive.DiscFad < GetTrackStartFad(-1) &&
                    _drive.State.CurrentOperation == (byte)StatusOperation.ReadingAudioData;
                _communication = CommunicationState.NoTransfer;
                if (isAudio)
                {
                    Span<byte> buffer = stackalloc byte[2448];
                    CdInterface.ReadSectorFad(_drive.DiscFad, buffer);
                    receiveCdda(buffer);
                    _drive.DiscFad++;
                    CdInterface.ReadAheadFad(_drive.DiscFad);
                }
                else
                    DoDataRead();

                UpdateStatusInfo();
                _drive.State.CurrentOperation = (_drive.State.QSubcode & 0x40) != 0
                    ? (byte)StatusOperation.ReadingDataSectors
                    : (byte)StatusOperation.ReadingAudioData;
                MakeStatusData(_drive.State, _drive.StateData);
                return isAudio ? TimeAudioSector : TimeReadSector / _drive.Speed;
            }

            case StatusOperation.ReadToc:
                DoToc();
                return TimeReading;

            case StatusOperation.Seeking:
            case StatusOperation.SeekSecurityRing2:
                _drive.SeekTime++;
                UpdateSeekStatus();
                if (_drive.SeekTime > 9)
                    _drive.State.CurrentOperation = _drive.PostSeekState;
                _communication = CommunicationState.NoTransfer;
                return TimeReading;

            case StatusOperation.Stopped:
            default:
                _communication = CommunicationState.NoTransfer;
                return TimePeriodic / _drive.Speed;
        }
    }

    private static uint GetFadFromCommand(ReadOnlySpan<byte> command) =>
        ((uint)command[1] << 16) | ((uint)command[2] << 8) | command[3];

    private uint GetTrackStartFad(int trackNumber)
    {
        int index = trackNumber == -1 ? _drive.NumTracks : trackNumber - 1;
        SaturnCdInterface.Toc10Entry track = _drive.Tracks[index];
        return MsfBcdToFad(track.PointMinute, track.PointSecond, track.PointFrame);
    }

    private int GetTrackFad(int trackNumber, int fad, ref int index)
    {
        int trackFad = unchecked(fad - (int)GetTrackStartFad(trackNumber));
        if (trackFad < 0)
            index = 1;
        return trackFad;
    }

    private void UpdateSeekStatus()
    {
        UpdateStatusInfo();
        _drive.State.CurrentOperation = (byte)StatusOperation.Seeking;
        MakeStatusData(_drive.State, _drive.StateData);
        _communication = CommunicationState.NoTransfer;
    }

    private void DoSeekCommon(StatusOperation postSeekState)
    {
        _drive.DiscFad = unchecked(GetFadFromCommand(_drive.ReceivedData) - 4);
        _drive.TargetFad = _drive.DiscFad;
        _drive.SeekTime = 0;
        _drive.PostSeekState = (byte)postSeekState;
    }

    private void DoSeek()
    {
        DoSeekCommon(StatusOperation.Idle);
        UpdateSeekStatus();
    }

    private StatusOperation GetMediaOperation() => cdBlock.GetMediaDriveStatus() switch
    {
        3 => StatusOperation.LidOpen,
        2 => StatusOperation.NoDisc,
        _ => StatusOperation.Idle,
    };

    private int DoCommand()
    {
        _drive.Speed = _drive.ReceivedData[10] == 1 ? 1 : 2;

        StatusOperation mediaOperation = GetMediaOperation();
        StatusOperation previousOperation = (StatusOperation)_drive.State.CurrentOperation;
        if (mediaOperation != StatusOperation.Idle || previousOperation is StatusOperation.LidOpen or StatusOperation.NoDisc)
        {
            _drive.State = default;
            _drive.State.CurrentOperation = (byte)mediaOperation;
            _drive.TocEntry = _drive.NumTocEntries = _drive.NumTracks = 0;
            _drive.DiscFad = _drive.TargetFad = 0;
            MakeStatusData(_drive.State, _drive.StateData);
        }

        if (mediaOperation != StatusOperation.Idle)
        {
            _communication = CommunicationState.NoTransfer;
            return TimePeriodic / _drive.Speed;
        }

        switch (_drive.ReceivedData[0])
        {
            case 0x00:
                return ContinueCommand();

            case 0x02:
                _drive.State.CurrentOperation = (byte)StatusOperation.SeekSecurityRing2;
                DoSeekCommon(StatusOperation.Idle);
                MakeRingStatus();
                _communication = CommunicationState.NoTransfer;
                return TimeReading / _drive.Speed;

            case 0x03:
            {
                int maxTrack = 0;
                Span<SaturnCdInterface.Toc10Entry> toc = stackalloc SaturnCdInterface.Toc10Entry[102];
                _drive.TocEntry = 0;
                _drive.NumTocEntries = CdInterface.ReadToc10(toc);
                for (int i = 0; i < _drive.NumTocEntries; i++)
                {
                    ref SaturnCdInterface.Toc10Entry entry = ref _drive.Toc[i * 3];
                    entry.ControlAddress = toc[i].ControlAddress;
                    entry.TrackNumber = toc[i].TrackNumber;
                    entry.Point = toc[i].Point > 0x99 ? toc[i].Point : NumberToBcd(toc[i].Point);
                    entry.Minute = NumberToBcd(toc[i].Minute);
                    entry.Second = NumberToBcd(toc[i].Second);
                    entry.Frame = NumberToBcd(unchecked((byte)(i * 3)));
                    entry.Zero = 0;
                    entry.PointMinute = NumberToBcd(toc[i].PointMinute);
                    entry.PointSecond = NumberToBcd(toc[i].PointSecond);
                    entry.PointFrame = NumberToBcd(toc[i].PointFrame);
                    _drive.Toc[i * 3 + 1] = entry;
                    _drive.Toc[i * 3 + 1].Frame = NumberToBcd(unchecked((byte)(i * 3 + 1)));
                    _drive.Toc[i * 3 + 2] = entry;
                    _drive.Toc[i * 3 + 2].Frame = NumberToBcd(unchecked((byte)(i * 3 + 2)));

                    if (entry.Point <= 0x99)
                    {
                        int trackNumber = BcdToNumber(entry.Point);
                        if (trackNumber > maxTrack)
                            maxTrack = trackNumber;
                        _drive.Tracks[trackNumber - 1] = entry;
                    }
                }

                _drive.NumTocEntries *= 3;
                for (int i = 0; i < _drive.NumTocEntries; i++)
                {
                    if (_drive.Toc[i].Point == 0xA2)
                    {
                        _drive.Tracks[maxTrack] = _drive.Toc[i];
                        break;
                    }
                }
                _drive.NumTracks = maxTrack;
                DoToc();
                return TimeReading;
            }

            case 0x04:
                _drive.State.CurrentOperation = (byte)StatusOperation.Stopped;
                MakeStatusData(_drive.State, _drive.StateData);
                _communication = CommunicationState.NoTransfer;
                return TimePeriodic / _drive.Speed;

            case 0x05:
            case 0x0A:
            case 0x0B:
                _drive.State.CurrentOperation = (byte)StatusOperation.Idle;
                MakeStatusData(_drive.State, _drive.StateData);
                _communication = CommunicationState.NoTransfer;
                return TimePeriodic / _drive.Speed;

            case 0x06:
                DoSeekCommon(StatusOperation.ReadingDataSectors);
                UpdateSeekStatus();
                _communication = CommunicationState.NoTransfer;
                return TimeReadSector / _drive.Speed;

            case 0x08:
                _drive.State.CurrentOperation = (byte)StatusOperation.Idle;
                MakeStatusData(_drive.State, _drive.StateData);
                _communication = CommunicationState.NoTransfer;
                return TimePeriodic / _drive.Speed;

            case 0x09:
                DoSeek();
                return TimeReading;
        }

        Debug.Fail("Unknown Saturn CD drive command.");
        return TimePeriodic;
    }

    private int ExecuteCommandCycle()
    {
        if (_communication == CommunicationState.Reset)
        {
            _drive.State.CurrentOperation = (byte)GetMediaOperation();
            MakeStatusData(_drive.State, _drive.StateData);
            _communication = CommunicationState.NoTransfer;
            return TimePowerOn + TimeWaiting;
        }
        else if (_communication == CommunicationState.SendingFirstByte || _communication == CommunicationState.SendingByte)
            return TimeByte;
        else if (_communication == CommunicationState.NoTransfer)
        {
            _drive.BitCounter = 0;
            _drive.ByteCounter = 0;
            _communication = CommunicationState.SendingFirstByte;
            Array.Clear(_drive.ReceivedData);
            sh1.SetStart(true);
            sh1.SetOutputEnableFallingEdge();
            return TimeStart;
        }
        else if (_communication == CommunicationState.WaitToOeFirstByte)
        {
            sh1.SetOutputEnableFallingEdge();
            sh1.SetStart(false);
            _communication = CommunicationState.SendingByte;
            return TimeOe;
        }
        else if (_communication == CommunicationState.WaitToOe)
        {
            sh1.SetOutputEnableFallingEdge();
            _communication = CommunicationState.SendingByte;
            return TimeOe;
        }
        else if (_communication == CommunicationState.WaitToRxio)
            return DoCommand();

        Debug.Fail("Unknown Saturn CD drive communication state.");
        return 1;
    }

    public void Execute(int cycles)
    {
        int remainder = _drive.CyclesRemainder - cycles;
        while (remainder < 0)
            remainder += ExecuteCommandCycle();
        _drive.CyclesRemainder = remainder;
    }

    private static void SetChecksum(Span<byte> data)
    {
        byte parity = 0;
        for (int index = 0; index < 11; index++)
            parity = unchecked((byte)(parity + data[index]));
        data[11] = unchecked((byte)~parity);
        data[12] = 0;
    }

    private int Toc10GetTrack(int fad)
    {
        if (_drive.NumTocEntries == 0)
            return 1;

        for (int i = 0; i < _drive.NumTocEntries; i++)
        {
            int trackStart = (int)GetTrackStartFad(i + 1);
            int trackEnd = (int)GetTrackStartFad(i + 2);
            if (fad >= trackStart && fad < trackEnd)
                return i + 1;
            if (i == 0 && fad < trackStart)
                return 1;
        }

        return 0;
    }

    private static void StateSetMsfInfo(ref SaturnCdDriveState.Status state, int trackFad, int absoluteFad)
    {
        Span<byte> msf = stackalloc byte[3];
        FadToMsfBcd(trackFad, msf);
        state.Minutes = msf[0];
        state.Seconds = msf[1];
        state.Frame = msf[2];
        FadToMsfBcd(absoluteFad, msf);
        state.AbsoluteMinutes = msf[0];
        state.AbsoluteSeconds = msf[1];
        state.AbsoluteFrame = msf[2];
    }

    private static void MakeStatusData(in SaturnCdDriveState.Status state, Span<byte> data)
    {
        data[0] = state.CurrentOperation;
        data[1] = state.QSubcode;
        data[2] = state.TrackNumber;
        data[3] = state.IndexField;
        data[4] = state.Minutes;
        data[5] = state.Seconds;
        data[6] = state.Frame;
        data[7] = 4;
        data[8] = state.AbsoluteMinutes;
        data[9] = state.AbsoluteSeconds;
        data[10] = state.AbsoluteFrame;
        SetChecksum(data);
    }

    public void Reset()
    {
        _drive = new SaturnCdDriveState();
        _communication = CommunicationState.Reset;
    }
}
