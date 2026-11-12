using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Interfaces;

public interface ISn74Ls257
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<int, LogicPinState>? OutputChanged { get; set; }
    LogicPinState GetOutput(int output);
    void SetInputs(byte aLevels, byte aKnown, byte bLevels, byte bKnown, bool? selectB, bool? disableHigh);
    /// <summary>Bits0..3=A,4..7=B,8=S,9=G. High means physical high.</summary>
    void SetInputs(ushort levels, ushort known);
    void AdvanceTo(long nanoseconds);
    CombinationalState CaptureState();
    void RestoreState(CombinationalState state);
}
