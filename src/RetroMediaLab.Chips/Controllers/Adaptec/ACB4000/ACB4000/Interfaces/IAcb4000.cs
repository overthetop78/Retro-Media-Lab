namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;

/// <summary>Carte cible ACB-4000 ; les supports connectés restent sous la possession de l'appelant.</summary>
public interface IAcb4000
{
    IAcb4000Bus Bus { get; }
    /// <summary>Aucune commande ni recherche mécanique active ; le propriétaire traite séparément toute nouvelle sélection.</summary>
    bool IsIdle { get; }
    int Unit { get; }
    int RemainingBlocks { get; }
    /// <summary>Sélection physique de tête, indépendante de l'adresse logique ou du conteneur.</summary>
    int SelectedHead { get; }
    bool ExpandedCommandsEnabled { get; }
    void SetExpandedCommandSet(bool enabled);
    /// <summary>Fronts STEP réellement émis depuis RESET.</summary>
    long SeekCount(int unit);
    /// <summary>Champs DATA reçus, y compris les relectures pour contrôle ECC.</summary>
    long ReadBlocks(int unit);
    /// <summary>Champs DATA émis complètement ; un champ interrompu n'est pas compté comme terminé.</summary>
    long WrittenBlocks(int unit);
    void SetIndex(bool asserted);
    /// <summary>Fin de recherche fournie lorsque son échéance matérielle expire.</summary>
    void ExpireParameterSearch();
    /// <summary>Le canal attend les cellules horloge/données séparées de la tête sélectionnée.</summary>
    bool ReadGate { get; }
    /// <summary>Une cellule MFM, fournie à la cadence du séparateur ; aucun accès à un conteneur.</summary>
    void ClockReadCell(bool cell);
    /// <summary>Porte de sortie physique ; la cellule est valable après ClockWriteCell si cette porte est active.</summary>
    bool WriteGate { get; }
    int FormatCylinder { get; }
    int FormatHead { get; }
    /// <summary>Une horloge du canal d'écriture, avec le niveau INDEX du lecteur sélectionné.</summary>
    bool ClockWriteCell(bool index);
    /// <summary>Niveau emprunté de WRITE FAULT du lecteur sélectionné, indépendant de RESET du contrôleur.</summary>
    void SetWriteFault(bool active);
    /// <summary>Niveaux empruntés du lecteur ; leur état n'est pas réinitialisé par RESET SCSI.</summary>
    void SetDriveSignals(int unit, bool ready, bool seekComplete, bool trackZero);
    bool DriveStepIssued(int unit);
    bool DriveDirectionIn(int unit);
    /// <summary>Expiration de l'attente mécanique, fournie par son propriétaire.</summary>
    void ExpireDriveSeek(int unit);
    void SetInitiator(byte data, bool select, bool acknowledge, bool reset, bool attention = false);
    void Tick();
    /// <summary>Avancement cumulé uniquement lorsque IsIdle garantit l'absence d'événement à traiter.</summary>
    void AdvanceIdleTicks(uint ticks);
    void Reset();
}
