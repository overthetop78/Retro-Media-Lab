namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

public interface IElectronCassettePins
{
    bool InputPresent { get; }
    bool InputBit { get; }
    void SetOutputBit(bool value);
    void Tick(bool motor);
}
