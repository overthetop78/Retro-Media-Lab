using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Interfaces;
/// <summary>Contrat matériel du composant Via6522 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IVia6522
{
    Func<byte>? ReadPortA { get; set; }

    Func<byte>? ReadPortB { get; set; }

    Action<byte>? WritePortA { get; set; }

    Action<byte>? WritePortB { get; set; }

    Action<byte>? PortBPinsChanged { get; set; }

    byte OutputA { get; }

    bool Irq { get; }

    void Reset();
    void Tick();
    byte Read(int register);
    void Write(int register, byte value);
    Action<bool>? Ca2PinsChanged { get; set; }

    Action<bool>? Cb2PinsChanged { get; set; }

    bool Ca2 { get; }

    bool Cb2 { get; }

    void SetPb6(bool level);
    void VSync();
    void SetCa1(bool level);
    void SetKeyboardLine(bool level);
    void SetCa2(bool level);
    void SetCb1(bool level);
    void SetCb2(bool level);
    Action<bool>? Cb1PinsChanged { get; set; }

    bool Cb1 { get; }
}
