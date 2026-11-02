using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using System.Globalization;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces;
/// <summary>Contrat matériel du composant Cpu6809 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICpu6809
{
    byte A { get; }

    byte B { get; }

    byte DP { get; }

    byte CC { get; }

    ushort X { get; }

    ushort Y { get; }

    ushort U { get; }

    ushort S { get; }

    ushort PC { get; }

    long Cycles { get; }

    Func<bool>? Firq { get; set; }

    void Reset();
    void Step();
}
