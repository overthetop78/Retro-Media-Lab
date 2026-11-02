using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Enums;
using C = RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Constants.ArchimedesIocConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Interfaces;
/// <summary>Contrat matériel du composant ArchimedesIoc ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IArchimedesIoc
{
    bool Irq { get; }

    bool Fiq { get; }

    byte StatusA { get; }

    byte StatusB { get; }

    byte StatusFiq { get; }

    bool KeyboardReceiveBusy { get; }

    byte ControlOutput { get; }

    Action<byte>? ControlWritten { get; set; }

    Action<byte>? KeyboardTransmitted { get; set; }

    Action<int>? BaudTransitions { get; set; }

    void Reset(bool cold);
    void SetControlInputs(byte pins);
    void SetFallingInput(bool high);
    void SetFlyback(bool high);
    void SetIrqA(byte mask, bool active);
    void SetIrqB(byte mask, bool active);
    void SetFiq(byte mask, bool active);
    byte Read(uint address);
    void Write(uint address, byte value);
    bool ReceiveKeyboardByte(byte value);
    void Advance(int ticks);
}
