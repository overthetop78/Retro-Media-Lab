namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationInput
{
    bool SupportsPointerCapture { get; }
    bool CapturePointerOnClick { get; }
    IReadOnlyDictionary<string, string> KeyboardBindings { get; }
    bool SupportsControllerPointerSwitch { get; }
    bool ControllerPointerMode { get; }
    void SetInput(EmulationInputSnapshot snapshot);
    /// <summary>Remplace les associations hôtes de la session sans réinitialiser le matériel émulé.</summary>
    void UpdateBindings(IEmulationConfiguration configuration);
    void SetControllerPortDevice(int port, EmulationPeripheralCategory peripheral);
    ValueTask<bool> SwitchControllerPointerAsync(CancellationToken cancellationToken = default);
}
