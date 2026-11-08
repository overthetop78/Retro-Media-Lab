using static RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Constants;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Enums;
using S = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.ChipSelect80186Constants;
using T = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Timings;
using I = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Interrupt80186Constants;
using D = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Dma80186Constants;
using P = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Peripheral80186Constants;
using R = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Timer80186Constants;
using Q = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Prefetch80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Interfaces;
/// <summary>Contrat matériel du composant Cpu80186 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICpu80186
{
    bool HoldInput { get; set; }

    bool HoldAcknowledged { get; }

    Action<bool>? HoldAcknowledgeChanged { get; set; }

    Action<long>? BusClocksAdvanced { get; set; }

    Cpu80186ChipSelect ActiveChipSelect { get; }

    Action<Cpu80186ChipSelect>? ChipSelectChanged { get; set; }

    bool LatchedAddress1 { get; }

    bool LatchedAddress2 { get; }

    Cpu80186ChipSelect SelectBusTargets(int address, bool memory);
    ushort IP { get; }

    long Cycles { get; }

    bool InterruptPending { get; set; }

    byte InterruptVector { get; set; }

    bool CoprocessorBusy { get; set; }

    bool Nmi { get; set; }

    ushort Register(int index);
    ushort Segment(int index);
    ushort Flags { get; }

    void Reset();
    void SetReset(bool asserted);
    bool ResetOutputHigh { get; }
    Action<bool>? ResetOutputChanged { get; set; }
    long ClockHalfCount { get; }
    void Step();
    void SetDmaRequest(int channel, bool asserted);
    Func<int, byte>? CascadeAcknowledge { get; set; }

    Func<byte>? ExternalInterruptAcknowledge { get; set; }

    void SetExternalInterrupt(int pin, bool asserted);
    byte ReadIoByte(ushort port);
    ushort ReadIoWord(ushort port);
    void WriteIoByte(ushort port, byte value);
    void WriteIoWord(ushort port, ushort value);
    bool IsInstructionFetch { get; }

    Action<int, bool>? TimerOutputChanged { get; set; }

    bool TimerOutput(int timer);
    void SetTimerInput(int timer, bool asserted);
}
