namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

/// <summary>Délais en nanosecondes ; le propriétaire choisit explicitement son profil matériel.</summary>
public sealed record Z80BusTiming(
    long Address, long Data, long AddressFloat, long DataFloat, long ControlFloat,
    long M1Assert, long M1Release, long MemoryAssert, long MemoryRelease,
    long ReadAssertRising, long ReadAssertFalling, long ReadRelease,
    long IoAssertRising, long IoAssertFalling, long IoRelease,
    long WriteAssertRising, long WriteAssertFalling, long WriteRelease,
    long RefreshAssert, long RefreshRelease,
    long MinimumPeriod, long MinimumHigh, long MinimumLow,
    long Halt, long BusAcknowledgeAssert, long BusAcknowledgeRelease,
    long ReadReleaseFalling, long IoReleaseFalling)
{
    /// <summary>Zilog 03-0027-02, mars 1978, page 6 : Z80 NMOS 2,5 MHz,
    /// +5 V ±5 %, 0–70 °C, charge 50 pF ; bornes maximales, pas des valeurs typiques.</summary>
    public static Z80BusTiming ZilogZ80Maximum { get; } = new(
        145, 230, 110, 90, 100, 130, 130, 100, 100, 100, 130, 100,
        90, 110, 100, 80, 90, 100, 180, 150, 400, 180, 180, 300, 120, 110, 110, 110);

    /// <summary>Même fiche Zilog, page 9 : grade Z80A NMOS 4 MHz, charge 50 pF.</summary>
    public static Z80BusTiming ZilogZ80AMaximum { get; } = new(
        110, 150, 90, 90, 80, 100, 100, 85, 85, 85, 95, 85,
        75, 85, 85, 65, 80, 80, 130, 120, 250, 110, 110, 300, 100, 100, 85, 85);

    internal long ValidateAndGetMaximumDelay()
    {
        long[] delays = [Address, Data, AddressFloat, DataFloat, ControlFloat, M1Assert, M1Release,
            MemoryAssert, MemoryRelease, ReadAssertRising, ReadAssertFalling, ReadRelease,
            IoAssertRising, IoAssertFalling, IoRelease, WriteAssertRising, WriteAssertFalling,
            WriteRelease, RefreshAssert, RefreshRelease, Halt, BusAcknowledgeAssert, BusAcknowledgeRelease,
            ReadReleaseFalling, IoReleaseFalling];
        long maximum = 0;
        foreach (long delay in delays)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(delay);
            maximum = Math.Max(maximum, delay);
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinimumPeriod);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinimumHigh);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinimumLow);
        if (MinimumHigh > MinimumPeriod || MinimumLow > MinimumPeriod)
            throw new ArgumentException("Clock pulse limits exceed the minimum period.");
        return maximum;
    }
}
