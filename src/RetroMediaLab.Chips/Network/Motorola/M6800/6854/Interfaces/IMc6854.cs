namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Interfaces;
/// <summary>Contrat matériel du composant Mc6854 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IMc6854
{
    Action<ReadOnlyMemory<byte>>? FrameTransmitted { get; set; }

    bool Irq { get; }
    Action<bool>? IrqChanged { get; set; }

    bool Receiving { get; }

    bool Transmitting { get; }

    bool Rts { get; }
    Action<bool, bool>? TransmitStateChanged { get; set; }

    bool TransmitData { get; }
    bool FlagDetect { get; }
    bool DataTerminalReady { get; }
    bool ReceiveServiceRequest { get; }
    bool TransmitServiceRequest { get; }

    void SetReset(bool asserted);
    void SetClockPresent(bool present, bool idle = true);
    void SetClearToSend(bool allowed);
    void SetCarrierPresent(bool present);
    void SetReceiveData(bool level);
    void SetReceiveClock(bool level);
    void SetTransmitClock(bool level);
    byte? SetBus(bool enable, bool selected, bool read, int register, byte data = 0);
    void Reset();
    void SetLine(bool clockPresent, bool clearToSend = true, bool idle = true);
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
    bool ReceiveFrame(ReadOnlySpan<byte> frame, bool validFcs = true);
}
