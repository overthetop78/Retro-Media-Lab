namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Interfaces;

/// <summary>Four independent combinatorial NAND gates, numbered zero through three.</summary>
public interface ISn74Ls00
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<int, bool?>? OutputChanged { get; set; }
    bool? GetOutput(int gate);
    void SetObservedOutputs(byte mask);
    void SetInputs(int gate, bool? a, bool? b);
    void SetInputs(int gate, bool a, bool b);
    void SetInputA(int gate, bool? high);
    void SetInputA(int gate, bool high);
    void SetInputB(int gate, bool? high);
    void SetInputB(int gate, bool high);
    /// <summary>Present both four-pin input banks at one instant; bit n selects gate n.
    /// Unknown input values are ignored unless their corresponding known bit is set.</summary>
    void SetInputLevels(byte aLevels, byte aKnown, byte bLevels, byte bKnown);
    void AdvanceTo(long nanoseconds);
}
