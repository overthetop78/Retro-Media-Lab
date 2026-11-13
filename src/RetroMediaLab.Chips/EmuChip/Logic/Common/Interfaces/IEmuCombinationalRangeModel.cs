namespace RetroMediaLab.Chips.EmuChip.Logic.Common.Interfaces;

internal interface IEmuCombinationalRangeModel
{
    bool PrepareRangeInput(ulong input, Span<long> delays, out ushort targets);
    void CommitRangeInput(ulong input);
}
