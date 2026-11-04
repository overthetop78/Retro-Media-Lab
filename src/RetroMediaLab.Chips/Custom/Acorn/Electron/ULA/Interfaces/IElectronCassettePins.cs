namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

public interface IElectronCassettePins
{
    bool InputPresent { get; }
    bool InputLevel { get; }
    void SetOutputLevel(bool value);
    void Tick(bool motor);
}
