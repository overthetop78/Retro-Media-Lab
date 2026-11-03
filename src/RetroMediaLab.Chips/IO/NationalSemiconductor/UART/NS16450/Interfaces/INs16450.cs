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

    Action<bool>? BaudOutChanged { get; set; }

    bool Irq { get; }

    bool Txd { get; }

    bool Dtr { get; }

    bool Rts { get; }

    bool Out1 { get; }

    bool Out2 { get; }

    bool BaudOutHigh { get; }

    bool ChipSelected { get; }
    bool DriverDisableHigh { get; }

    /// <summary>Physical CS0/CS1/CS2, ADS, RD and /RD, WR and /WR levels.
    /// Null denotes a high-impedance data bus. ADS low selects transparently;
    /// its rising edge latches the address and selection.</summary>
    byte? SetBus(bool cs0High, bool cs1High, bool cs2High, bool addressStrobeHigh,
        bool readHigh, bool readLowHigh, bool writeHigh, bool writeLowHigh, int register, byte input);

    void Reset();
    /// <summary>Active-high MR input. While asserted, transfers and register writes are inhibited.</summary>
    void SetReset(bool active);
    byte Read(int register);
    void Write(int register, byte value);
    void SetCts(bool high);
    void SetDsr(bool high);
    void SetRing(bool high);
    void SetDcd(bool high);
    void SetRxd(bool high);
    void Advance(int ticks);
    void AdvanceReceiveClock(int risingEdges);
    void SetReceiveClock(bool high);
}
