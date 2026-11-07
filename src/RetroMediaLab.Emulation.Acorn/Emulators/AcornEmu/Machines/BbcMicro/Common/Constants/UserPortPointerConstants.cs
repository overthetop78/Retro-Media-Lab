namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Broches des connecteurs et adaptation des déplacements hôtes ; aucune fréquence de puce ajoutée.</summary>
internal static class UserPortPointerConstants
{
    internal const string ConnectedOption = "acornemu.userPort.pointer";
    internal const string Group = "Emulation.Tab.Mouse";
    internal const string EnabledLabel = "Emulation.Value.Enabled";
    // Fenêtre initiale de rééchantillonnage avant de disposer de deux instants d'entrée.
    // Les fenêtres suivantes suivent les cycles réellement écoulés entre les entrées hôtes.
    // Ce n'est ni une horloge physique de souris ni une limite de débit des fronts.
    internal const int InitialHostSampleCycles = 2048;
    internal const byte UserX = 0x01, UserY = 0x04;
    internal const byte UserLeft = 0x20, UserMiddle = 0x40, UserRight = 0x80;
    internal const byte CompactX = 0x08, CompactY = 0x10;
    internal const byte CompactLeft = 0x01, CompactMiddle = 0x02, CompactRight = 0x04;
}
