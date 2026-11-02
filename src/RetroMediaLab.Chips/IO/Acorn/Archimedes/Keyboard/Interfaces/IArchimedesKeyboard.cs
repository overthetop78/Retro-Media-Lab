using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Enums;
using C = RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Constants.ArchimedesKeyboardConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Interfaces;
/// <summary>Contrat matériel du composant ArchimedesKeyboard ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IArchimedesKeyboard
{
    bool ScanEnabled { get; }

    bool MouseEnabled { get; }

    bool Break { get; }

    byte Leds { get; }

    void Reset();
    void SetInput(ReadOnlySpan<int> keys, bool breakPressed, bool left, bool middle, bool right, int deltaX, int deltaY);
    void Write(byte command);
    bool TryTakeTransmit(out byte value);
    void Advance(int iocTicks);
}
