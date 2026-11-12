namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Capture locale à CSW montant. Qualified porte les garanties connues
/// au front ; une violation ultérieure de maintien reste signalée dans l'état.</summary>
internal readonly record struct Tms9918CpuWrite(long TimeNanoseconds, bool Mode,
    byte Data, bool Qualified);
