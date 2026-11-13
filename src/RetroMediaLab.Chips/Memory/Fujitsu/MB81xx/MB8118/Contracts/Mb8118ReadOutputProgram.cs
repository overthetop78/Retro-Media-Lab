using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

/// <summary>Finite DOUT transitions established by the memory receiving a
/// complete read waveform. Negative dates denote absent transitions. This
/// describes wires only; cells and the interruptible input phases stay owned
/// by the memory. Replacing an input withdraws the unconsumed suffix.</summary>
public readonly record struct Mb8118ReadOutputProgram
{
    public readonly LogicPinState Initial, Previous, Read;
    public readonly long PreviousTime, ReadTime, ReleaseTime;

    public Mb8118ReadOutputProgram(LogicPinState initial, long previousTime, LogicPinState previous,
        long readTime, LogicPinState read, long releaseTime)
    {
        Initial = initial; PreviousTime = previousTime; Previous = previous;
        ReadTime = readTime; Read = read; ReleaseTime = releaseTime;
    }
    public long TimeAt(int index) => index switch
    {
        0 => PreviousTime, 1 => ReadTime, 2 => ReleaseTime,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public LogicPinState ValueAt(int index) => index switch
    {
        0 => Previous, 1 => Read, 2 => LogicPinState.HighImpedance,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
