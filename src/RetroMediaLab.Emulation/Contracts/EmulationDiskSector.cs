namespace RetroMediaLab.Emulation.Contracts;

/// <summary>Secteur du média hôte, utilisable directement par les contrôleurs matériels partagés.</summary>
public sealed record EmulationDiskSector(long Index, ReadOnlyMemory<byte> Data, int Cylinder, int Head,
    int Number, bool? IntegrityValid = null, byte? DiagnosticCode = null)
    : RetroMediaLab.Chips.Controllers.Common.Contracts.DiskSector(Index, Data, Cylinder, Head, Number, IntegrityValid, DiagnosticCode);
