using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Contracts;
using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Contracts;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

/// <summary>Balayage complet de la carte ; la RAM est détenue par la machine.</summary>
internal sealed record VideoState
{
    public required int FormatVersion { get; init; }
    public required int Height { get; init; }
    public required bool TeletextHighBankOnly { get; init; }
    public required int ScreenWrap { get; init; }
    public required Crtc6845State Crtc { get; init; }
    public required BbcVideoUlaState Ula { get; init; }
    public required TeletextSaa5050State Teletext { get; init; }
    public required uint[] Drawing { get; init; }
    public required uint[] Completed { get; init; }
    public required uint[] PreviousField { get; init; }
    public required bool CrtcVSync { get; init; }
    public required int CrtcRaster { get; init; }
    public required int CrtcMemoryAddress { get; init; }
    public required bool CrtcDisplayEnabled { get; init; }
    public required bool CrtcCursor { get; init; }
    public required int CrtcDisplayColumn { get; init; }
    public required int CrtcScanline { get; init; }
    public required bool CrtcProgrammed { get; init; }
    public required bool Monochrome { get; init; }
    public required int Phase { get; init; }
    public required bool CharacterPending { get; init; }
    public required int ScanoutX { get; init; }
    public required int ScanoutLine { get; init; }
    public required bool ScanoutVisible { get; init; }
    public required int LineMasterDot { get; init; }
    public required bool DrawingInterlaced { get; init; }
    public required bool CompletedInterlaced { get; init; }
    public required bool PreviousFieldValid { get; init; }
    public required int DrawingParity { get; init; }
    public required int CompletedParity { get; init; }
    public required byte TeletextLatchedData { get; init; }
    public required bool TeletextWindow { get; init; }
    public required int TeletextRasterParity { get; init; }
}