using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Enums;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Interfaces;

public interface ISn74Ls138
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    long? NextOutputEventNanoseconds { get; }
    Action<long?>? NextOutputEventNanosecondsChanged { get; set; }
    Action<int, bool?>? OutputChanged { get; set; }
    byte OutputLevels { get; }
    byte OutputKnown { get; }
    bool? GetOutput(int output);
    void SetObservedOutputs(byte mask);
    void SetInput(Sn74Ls138Input pin, bool? high);
    /// <summary>Simultaneous front, bits A/B/C/G1/G2A/G2B, high means physical high.
    /// Unknown pin values are ignored unless the corresponding known bit is set.</summary>
    void SetInputs(byte levels, byte known);
    void AdvanceTo(long nanoseconds);
}
