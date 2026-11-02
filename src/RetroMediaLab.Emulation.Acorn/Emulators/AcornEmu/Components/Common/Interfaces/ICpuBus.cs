namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Interfaces;

/// <summary>Broches vues par le 6502 ; le CPU ignore le modèle de machine et le SDK de l'application.</summary>
/// <remarks>Chaque lecture ou écriture du processeur avance d'abord l'horloge par Tick. Le bus appartient à la machine.</remarks>
internal interface ICpuBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
    void Tick();
    bool Irq { get; }
    bool Nmi { get; }
    /// <summary>RDY haut : la lecture peut se terminer. Les écritures NMOS continuent même quand RDY est bas.</summary>
    bool Ready => true;
    /// <summary>RESET actif logiquement : inhibe les transferts jusqu'à sa libération.</summary>
    bool ResetRequested => false;
    /// <summary>DBE haut pendant le transfert : le 6502 peut piloter ses sorties de données.</summary>
    bool DataBusEnabled => true;
    /// <summary>Cycle d'écriture dont les données CPU sont en haute impédance ; le bus détermine sa réponse électrique.</summary>
    void WriteUndriven(ushort address) { }
    /// <summary>SO actif : un front inactif vers actif positionne V sur le 6502.</summary>
    bool SetOverflow => false;
    /// <summary>SYNC est actif pendant la lecture d'un opcode, y compris une lecture abandonnée pour interruption.</summary>
    void SetSync(bool asserted) { }
}
