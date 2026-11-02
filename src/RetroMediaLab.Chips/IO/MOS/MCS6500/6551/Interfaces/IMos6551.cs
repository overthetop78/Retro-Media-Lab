using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;
using System.Numerics;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Interfaces;
/// <summary>Contrat matériel du composant Mos6551 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IMos6551
{
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? TransmitChanged { get; set; }

    bool Irq { get; }

    bool Txd { get; }

    bool Dtr { get; }

    bool Rts { get; }

    byte Read(int register);
    void Write(int register, byte value);
    void SetRxd(bool high);
    void SetCts(bool high);
    void SetDsr(bool high);
    void SetDcd(bool high);
    void Reset();
    void Advance(int ticks);
    void AdvanceReceiveClock(int transitions);
}
