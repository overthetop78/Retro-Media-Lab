namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Interfaces;
/// <summary>Contrat matériel du composant Mos6551 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IMos6551
{
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? TransmitChanged { get; set; }

    Action<bool>? ReceiveClockChanged { get; set; }

    bool ReceiveClockIsOutput { get; }

    bool ReceiveClockOutput { get; }

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
    void SetReset(bool high);
    void Advance(int ticks);
    void AdvanceReceiveClock(int transitions);
    void SetReceiveClock(bool high);
    void SetTransmitClock(bool high);
}
