namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Interfaces;

/// <summary>Transferts vus par l’ARM ; MEMC, IOC et les temps d’accès appartiennent à la carte.</summary>
/// <remarks>Chaque transfert fait avancer l’horloge de la carte. Un retour false signale
/// un abandon matériel, traité par le processeur sans exception hôte ni accès direct au système émulé.</remarks>
/// <remarks>Les adresses de données conservent A0/A1, même pour un mot. La RAM ignore ces bits ;
/// les registres de commande MEMC/CAM les décodent. LDR effectue ensuite sa rotation dans le CPU.</remarks>
internal interface IArmBus
{
    bool TryReadWord(uint address, bool sequential, bool userMode, bool instructionFetch, out uint value);
    bool TryReadByte(uint address, bool userMode, out byte value);
    bool TryWriteWord(uint address, uint value, bool sequential, bool userMode);
    bool TryWriteByte(uint address, byte value, bool userMode);
    /// <summary>Broche LOCK pendant SWP : le contrôleur ne peut intercaler un autre maître du bus.</summary>
    void SetBusLock(bool enabled);
    void InternalCycle();
    /// <summary>Dernier cycle interne pouvant être partagé avec le prochain accès d'instruction ARM2.</summary>
    void MergeInternalCycle() => InternalCycle();
    /// <summary>Itérations Booth ; la carte distingue les cycles internes et l'étirement MEMC1.</summary>
    void MultiplyCycles(uint nextFetchAddress, int cycles)
    {
        for (var cycle = 1; cycle < cycles; cycle++) InternalCycle();
        MergeInternalCycle();
    }
    bool Irq { get; }
    bool Fiq { get; }
}
