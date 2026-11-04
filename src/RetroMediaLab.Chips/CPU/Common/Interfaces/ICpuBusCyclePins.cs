namespace RetroMediaLab.Chips.CPU.Common.Interfaces;

/// <summary>Présentation facultative des sorties CPU avant l'avancement du cycle de bus.</summary>
/// <remarks>Le transfert reste effectué par ICpuBus après Tick. Une préparation ne garantit pas
/// sa réalisation : RESET peut l'inhiber, RDY répéter une lecture et DBE empêcher le pilotage
/// des données d'écriture. Un cycle interne sans transfert ne présente pas d'adresse fictive.</remarks>
public interface ICpuBusCyclePins
{
    void PrepareRead(ushort address);
    void PrepareWrite(ushort address, byte data);
}
