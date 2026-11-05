namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Bornes de calibration du firmware 400108-00, 0A7B–0ADC.</summary>
public static class Acb4000PositioningConstants
{
    public const int CalibrationExcursion = 16;
    public const int MaximumCalibrationSteps = 2048;
    public const byte NoSeekComplete = 0x02;
    public const byte NoTrackZero = 0x06;
    public const byte StartUnitMask = 0x01;
}
