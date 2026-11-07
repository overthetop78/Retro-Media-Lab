namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

public interface IElectronTapePins
{
    bool InputPresent { get; }
    bool InputLevel { get; }
    void SetOutputLevel(bool value);
    void Tick(bool motor);
}
