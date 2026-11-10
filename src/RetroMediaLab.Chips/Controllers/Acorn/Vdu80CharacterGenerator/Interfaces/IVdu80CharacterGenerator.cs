namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Interfaces;

public interface IVdu80CharacterGenerator
{
    long TimeNanoseconds { get; }
    long CharacterClockCount { get; }
    long DotClockCount { get; }
    bool? VideoWhite { get; }
    Action<bool?>? VideoChanged { get; set; }
    void SetCharacterData(byte data);
    void SetRasterAddress(int address);
    void SetDisplayEnabled(bool high);
    /// <summary>Receive CU; the local R12/R13/C6 response determines the later XOR level.</summary>
    void SetCursor(bool high);
    void SetInverted(bool high);
    void SetCharacterClock(bool high);
    bool? SetDotClock(bool high);
    int AdvanceStableDotClocks(int maximumClocks);
    bool TryAdvanceSettledCharacterPeriods(int periods, out bool white);
    bool TryAdvanceCharacterPeriod(out byte values, out byte known);
    bool StableClockCandidate { get; }
    Action<bool>? StableClockCandidateChanged { get; set; }
}
