namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

/// <summary>Politiques de lecture de SEND DIAGNOSTIC/65, manuel OEM section 6.3.1.</summary>
public enum Acb4000ReadErrorMode : byte
{
    CorrectSilently = 0,
    DisableRetryAndCorrection = 1,
    ReportCorrections = 2
}
