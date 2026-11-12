using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Interfaces;

public interface ISn74Ls32
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<int, LogicPinState>? OutputChanged { get; set; }
    LogicPinState GetOutput(int gate);
    void SetInputs(int gate, bool? a, bool? b);
    void SetInputs(byte aLevels, byte aKnown, byte bLevels, byte bKnown);
    void AdvanceTo(long nanoseconds);
    CombinationalState CaptureState();
    void RestoreState(CombinationalState state);
}
