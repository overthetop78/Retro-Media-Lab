using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
/// <summary>Contrat matériel du composant ElectronVideo ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IElectronVideo
{
    int Mode { get; }

    int Line { get; }

    int Clock { get; }

    long Frame { get; }

    byte[] CompletedFrame { get; }

    bool LowBandwidth { get; }

    bool DisplayFetching { get; }

    Action<int>? LineStarted { get; set; }

    void Reset();
    void SetMode(byte control);
    void WriteStart(bool high, byte value);
    void WritePalette(int register, byte value);
    bool Tick();
}
