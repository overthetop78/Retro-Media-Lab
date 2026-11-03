namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums;

/// <summary>Phases du séquenceur de commandes, indépendantes de la machine propriétaire.</summary>
public enum Intel8271Phase
{
    Idle,
    Positioning,
    SeekSetup,
    StepHigh,
    StepWait,
    ExternalSeek,
    HeadLoad,
    Read,
    Write,
    Verify,
    Id,
    Format,
    Scan,
    ScanTail
}
