using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Contracts;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Interfaces;

/// <summary>Broches TTL, après les buffers de ligne externes. Tick représente un cycle de la carte appelante.</summary>
public interface IBbcSerialUla
{
    BbcSerialUlaState CaptureState();
    void RestoreState(BbcSerialUlaState state);
    byte Control { get; }
    bool Motor { get; }
    bool Rs423Selected { get; }
    bool Rs423ReceiveData { get; set; }
    bool Rs423ClearToSendHigh { get; set; }
    bool Rs423TransmitData { get; }
    bool Rs423RequestToSendHigh { get; }
    bool ReceiveData { get; }
    bool ClearToSendHigh { get; }
    bool DataCarrierDetectHigh { get; }
    bool TransmitClockEdge { get; }
    bool ReceiveClockEdge { get; }
    short TapeOutput { get; }
    Action<bool, bool, bool, bool>? SerialPinsChanged { get; set; }
    Action<bool, bool, bool>? SerialClocksRaised { get; set; }
    long SourceClockPhase { get; }
    void SetTransmitPins(bool data, bool requestToSendHigh);
    void Tick();
    void Write(byte value);
    byte Read();
    void SetTapeInput(bool? level);
    void Tick(bool transmitData, bool requestToSendHigh);
    void PowerOn();
}
