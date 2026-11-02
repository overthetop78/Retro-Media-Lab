using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Interfaces;
/// <summary>Contrat matériel du composant Mc6854 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IMc6854
{
    Action<ReadOnlyMemory<byte>>? FrameTransmitted { get; set; }

    bool Irq { get; }

    bool Receiving { get; }

    bool Transmitting { get; }

    bool Rts { get; }

    void Reset();
    void SetLine(bool clockPresent, bool clearToSend = true, bool idle = true);
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
    bool ReceiveFrame(ReadOnlySpan<byte> frame, bool validFcs = true);
}
