using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.AtomPpiConstants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Interfaces;
/// <summary>Contrat matériel du composant AtomPpi8255 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAtomPpi8255
{
    Func<byte>? ReadPortA { get; set; }

    Func<byte>? ReadPortB { get; set; }

    Func<byte>? ReadPortC { get; set; }

    Action<byte>? PortAPinsChanged { get; set; }

    Action<byte>? PortBPinsChanged { get; set; }

    Action<byte>? PortCPinsChanged { get; set; }

    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
}
