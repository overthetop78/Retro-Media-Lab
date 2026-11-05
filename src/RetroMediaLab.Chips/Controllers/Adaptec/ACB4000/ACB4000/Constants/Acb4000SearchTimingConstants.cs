namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Boucles 15BF et 15EE du firmware 400108-00 ; mesures en T-states 8085 sans attente externe.</summary>
public static class Acb4000SearchTimingConstants
{
    public const int RevisionBSequencerClock = 2_500_000;
    public const int IndexSetupTStates = 10;
    public const int FirstFieldSetupTStates = 51;
    public const byte NoIndex = 0x01;

    public const int IndexIterations = 0x2100;
    public const int IndexIterationTStates = 51;
    public const int FirstFieldIterations = 0x10;
    public const int FollowingFieldIterations = 256;
    public const int FieldIterationTStates = 39;
    public const int FinalJumpSavingTStates = 3;
    public const int IndexWaitTStates = IndexIterations * IndexIterationTStates - FinalJumpSavingTStates;
    public const int FirstFieldWaitTStates = FirstFieldIterations * FieldIterationTStates - FinalJumpSavingTStates;
    public const int FollowingFieldWaitTStates = FollowingFieldIterations * FieldIterationTStates - FinalJumpSavingTStates;
}
