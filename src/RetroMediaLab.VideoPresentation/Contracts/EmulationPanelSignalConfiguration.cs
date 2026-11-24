using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public abstract record EmulationPanelSignalConfiguration
{
    public EmulationCrtColorStandards ColorStandards { get; init; } = EmulationCrtColorStandards.PalSecamNtsc;
    public EmulationCrtFieldRate FieldRate { get; init; } = EmulationCrtFieldRate.Hz50And60;
    public EmulationLcdDefinition Definition { get; init; }
    public EmulationCrtConnection Connection { get; init; }
    public int ConnectionDegradation { get; init; }
    public EmulationCrtMonitorPalette MonitorPalette { get; init; }

    public bool CanDegradeConnection() => Connection is >= EmulationCrtConnection.Scart and <= EmulationCrtConnection.AnalogRf;
}
