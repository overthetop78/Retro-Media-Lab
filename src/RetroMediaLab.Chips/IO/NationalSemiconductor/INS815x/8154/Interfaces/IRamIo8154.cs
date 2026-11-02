namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Interfaces;
/// <summary>Contrat matériel du composant RamIo8154 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IRamIo8154
{
    Func<byte>? ReadPortA { get; set; }

    Func<byte>? ReadPortB { get; set; }

    Action<byte>? PortAPinsChanged { get; set; }

    Action<byte>? PortBPinsChanged { get; set; }

    byte Mode { get; }

    byte OutputA { get; }

    byte OutputB { get; }

    void Reset();
    byte ReadRam(int address);
    void WriteRam(int address, byte value);
    byte ReadIo(int address);
    void WriteIo(int address, byte value);
}
