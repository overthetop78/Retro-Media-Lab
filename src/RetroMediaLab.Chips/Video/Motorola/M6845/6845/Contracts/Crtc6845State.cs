using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Contracts;

/// <summary>État du balayage, des registres et du bus ; aucune mémoire vidéo propriétaire.</summary>
public sealed record Crtc6845State
{
    public required int FormatVersion { get; init; }
    public required Crtc6845Model Model { get; init; }
    public required byte[] Registers { get; init; }
    public required bool BusSelected { get; init; }
    public required bool BusRegister { get; init; }
    public required bool BusRead { get; init; }
    public required bool BusEnable { get; init; }
    public required bool BusAccessValid { get; init; }
    public required byte Selected { get; init; }
    public required int MemoryAddress { get; init; }
    public required int LineAddress { get; init; }
    public required int NextLineAddress { get; init; }
    public required int RasterAddress { get; init; }
    public required long BlinkFields { get; init; }
    public required bool HorizontalDisplay { get; init; }
    public required bool VerticalDisplay { get; init; }
    public required bool FirstScanline { get; init; }
    public required bool SuppressDisplay { get; init; }
    public required bool InterlaceSync { get; init; }
    public required bool InterlaceVideo { get; init; }
    public required bool Programmed { get; init; }
    public required int DisplaySkew { get; init; }
    public required int CursorSkew { get; init; }
    public required int CursorAddress { get; init; }
    public required int CursorStart { get; init; }
    public required int CursorEnd { get; init; }
    public required int CursorMode { get; init; }
    public required int HorizontalWidth { get; init; }
    public required int VerticalWidth { get; init; }
    public required int Column { get; init; }
    public required int Row { get; init; }
    public required int Scanline { get; init; }
    public required long Frame { get; init; }
    public required bool LightPenLevel { get; init; }
    public required bool LightPenPending { get; init; }
    public required bool CursorOnRaster { get; init; }
    public required int DisplayHistory { get; init; }
    public required int CursorHistory { get; init; }
    public required int AddressDelayOne { get; init; }
    public required int AddressDelayTwo { get; init; }
    public required bool DisplayEnabled { get; init; }
    public required bool Cursor { get; init; }
    public required bool VerticalOutputEnabled { get; init; }
    public required bool VerticalDisplayEndPending { get; init; }
    public required bool VerticalSyncEligible { get; init; }
    public required bool SyncOnEveryCharacter { get; init; }
    public required int HalfLineColumn { get; init; }
    public required int Address { get; init; }
    public required bool ResetPin { get; init; }
    public required bool ResetActive { get; init; }
    public required bool TestModeActive { get; init; }
    public required int TestLowCounter { get; init; }
    public required int TestHighCounter { get; init; }
    public required int AdjustCounter { get; init; }
    public required bool CheckAdjust { get; init; }
    public required bool EndMain { get; init; }
    public required bool EndAdjust { get; init; }
    public required bool Adjusting { get; init; }
    public required bool ExtraScanline { get; init; }
    public required bool SyncSeenInRow { get; init; }
    public required int HorizontalSyncCounter { get; init; }
    public required int VerticalSyncCounter { get; init; }
    public required bool VerticalSyncCounted { get; init; }
    public required bool PublishedVSync { get; init; }
    public required bool RestoreSyncOutput { get; init; }
}
