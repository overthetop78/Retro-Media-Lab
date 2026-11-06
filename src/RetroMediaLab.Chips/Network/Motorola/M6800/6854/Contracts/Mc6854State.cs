using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Contracts;

public readonly record struct Mc6854ReceiveByteState(byte Value, bool Address, bool Last, bool Valid);
public readonly record struct Mc6854TransmitByteState(byte Value, bool Last);

/// <summary>État de l'ADLC et de ses données en vol, sans socket ni transport hôte.</summary>
public sealed record Mc6854State
{
    public required int FormatVersion { get; init; }
    public required int CyclesPerByte { get; init; }
    public required Mc6854ReceiveByteState[] Receive { get; init; }
    public required Mc6854TransmitByteState[] Transmit { get; init; }
    public required Mc6854TransmitByteState Prefetched { get; init; }
    public required byte[] Outgoing { get; init; }
    public required bool[]? PacketBits { get; init; }
    public required bool BusEnable { get; init; }
    public required bool BusSelected { get; init; }
    public required bool BusRead { get; init; }
    public required byte? BusData { get; init; }
    public required int BusRegister { get; init; }
    public required bool ReceiveDmaSuppressed { get; init; }
    public required bool TransmitDmaSuppressed { get; init; }
    public required byte Control1 { get; init; }
    public required byte Control2 { get; init; }
    public required byte Control3 { get; init; }
    public required byte Control4 { get; init; }
    public required byte ReceiveStatus { get; init; }
    public required byte ReadReceiveStatus { get; init; }
    public required byte ReadTransmitStatus { get; init; }
    public required byte AcknowledgedReceive { get; init; }
    public required byte AcknowledgedTransmit { get; init; }
    public required long ClockFraction { get; init; }
    public required bool StableIdle { get; init; }
    public required bool ClockPresent { get; init; }
    public required bool ClearToSend { get; init; }
    public required bool CarrierPresent { get; init; }
    public required bool CtsRaised { get; init; }
    public required bool DcdRaised { get; init; }
    public required bool ResetAsserted { get; init; }
    public required bool LineIdle { get; init; }
    public required bool RtsAsserted { get; init; }
    public required int RtsReleaseBits { get; init; }
    public required bool LoopOnline { get; init; }
    public required bool LoopActive { get; init; }
    public required bool LoopPrefixZero { get; init; }
    public required bool LoopWaitingForData { get; init; }
    public required bool LoopDelay { get; init; }
    public required int LoopOnes { get; init; }
    public required bool TransmitOutputsDirty { get; init; }
    public required bool PublishedRts { get; init; }
    public required bool PublishedTransmitting { get; init; }
    public required int PacketOffset { get; init; }
    public required int PacketLength { get; init; }
    public required bool PacketLineLevel { get; init; }
    public required int FillBit { get; init; }
    public required int ReceiveHead { get; init; }
    public required int ReceiveCount { get; init; }
    public required AdlcFrameField ReceiveField { get; init; }
    public required bool FlagDetected { get; init; }
    public required bool FlagRead { get; init; }
    public required bool FlagOutput { get; init; }
    public required bool RxClock { get; init; }
    public required bool RxData { get; init; }
    public required bool RxLastLevel { get; init; }
    public required bool RxInFrame { get; init; }
    public required bool RxDiscard { get; init; }
    public required byte RawWindow { get; init; }
    public required int RawCount { get; init; }
    public required int RawOnes { get; init; }
    public required int DataOnes { get; init; }
    public required uint ReceiveShift { get; init; }
    public required int ReceiveBits { get; init; }
    public required int RxTotalBits { get; init; }
    public required ushort ReceiveCrc { get; init; }
    public required bool StatusDirty { get; init; }
    public required byte CachedStatusOne { get; init; }
    public required byte CachedStatusTwo { get; init; }
    public required bool IrqOutput { get; init; }
    public required int TransmitHead { get; init; }
    public required int TransmitCount { get; init; }
    public required bool PrefetchedValid { get; init; }
    public required AdlcFrameField TransmitField { get; init; }
    public required AdlcTransmitPhase TransmitPhase { get; init; }
    public required AdlcTransmitPhase QueuedPhase { get; init; }
    public required bool Transmitting { get; init; }
    public required bool TransmitComplete { get; init; }
    public required bool Underrun { get; init; }
    public required bool FinishPending { get; init; }
    public required bool AbortRequested { get; init; }
    public required bool TxClock { get; init; }
    public required bool TxData { get; init; }
    public required bool NrziLevel { get; init; }
    public required uint TransmitShift { get; init; }
    public required int TransmitBits { get; init; }
    public required int TransmitOnes { get; init; }
    public required ushort TransmitCrc { get; init; }
    public required bool LastFlagAvailable { get; init; }
    public required bool MarkIdlePrefix { get; init; }
}