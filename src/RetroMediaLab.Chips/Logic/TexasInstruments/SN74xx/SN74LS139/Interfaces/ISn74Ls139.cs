using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Interfaces;

public interface ISn74Ls139
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<int, LogicPinState>? OutputChanged { get; set; }
    LogicPinState GetOutput(int output);
    void SetInputs(int decoder, bool? a, bool? b, bool? enableHigh);
    /// <summary>Physical high levels; A/B/G for decoder 0 in bits0/1/2, decoder1 in3/4/5.</summary>
    void SetInputs(byte levels, byte known);
    void AdvanceTo(long nanoseconds);
    CombinationalState CaptureState();
    void RestoreState(CombinationalState state);
}
