namespace RetroMediaLab.Chips.Controllers.Sega.CD;

/// <summary>Drive-visible track limits in 75 Hz sectors; type zero denotes CD-DA, one/two denotes data.</summary>
public sealed record CdTrack(int StartLba, int EndLba, int Type, int LoopMode = 0, int LoopOffset = 0);
