using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationCrtVideoConfiguration(
    EmulationCrtMask Mask = EmulationCrtMask.None,
    EmulationCrtColorStandards ColorStandards = EmulationCrtColorStandards.PalSecamNtsc,
    EmulationCrtFieldRate FieldRate = EmulationCrtFieldRate.Hz50And60,
    EmulationCrtPhosphorColor PhosphorColor = EmulationCrtPhosphorColor.Gray,
    int BeamWidth = 0)
{
    public int Curvature { get; init; }
    public EmulationCrtConnection Connection { get; init; }
    public int ConnectionDegradation { get; init; }
    public EmulationCrtMonitorPalette MonitorPalette { get; init; }

    public bool CanDegradeConnection() => Connection is >= EmulationCrtConnection.Scart and <= EmulationCrtConnection.AnalogRf;
}
