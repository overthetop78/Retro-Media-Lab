using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Enums;
using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;
using System.Numerics;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Interfaces;
/// <summary>Contrat matériel du composant Ns16450 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface INs16450
{
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? TransmitChanged { get; set; }

    Action<bool>? DtrChanged { get; set; }

    Action<bool>? RtsChanged { get; set; }

    Action<bool>? Out1Changed { get; set; }

    Action<bool>? Out2Changed { get; set; }

    bool Irq { get; }

    bool Txd { get; }

    bool Dtr { get; }

    bool Rts { get; }

    bool Out1 { get; }

    bool Out2 { get; }

    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
    void SetCts(bool high);
    void SetDsr(bool high);
    void SetRing(bool high);
    void SetDcd(bool high);
    void SetRxd(bool high);
    void Advance(int ticks);
    void AdvanceReceiveClock(int risingEdges);
}
