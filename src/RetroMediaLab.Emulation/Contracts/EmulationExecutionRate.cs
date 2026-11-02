namespace RetroMediaLab.Emulation.Contracts;

/// <summary>Progression mesurée de la machine ; les MHz sont calculés depuis son horloge principale annoncée.</summary>
public readonly record struct EmulationExecutionRate(double SpeedRatio, double? EffectiveMegahertz);
