using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronUlaConstants;
using V = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
/// <summary>Contrat matériel du composant ElectronUla ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IElectronUla
{
    int RomPage { get; }

    bool Motor { get; }

    bool CapsLock { get; }

    bool Irq { get; }

    short SoundSample { get; }

    void Reset(bool cold);
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
}
