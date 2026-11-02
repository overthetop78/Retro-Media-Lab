namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Enums;

/// <summary>Broches de contrôle variables effectivement raccordées par la carte ; IRQ et NMI sont toujours sondées.</summary>
[Flags]
internal enum CpuBusControlSignals
{
    None = 0,
    Ready = 1,
    Reset = 2,
    SetOverflow = 4,
    DataBusEnable = 8,
    Sync = 16,
    All = Ready | Reset | SetOverflow | DataBusEnable | Sync
}
