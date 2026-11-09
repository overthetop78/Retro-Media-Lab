namespace RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Interfaces;

/// <summary>Static mask ROM: the device owns its propagation deadlines, without a clock pin.</summary>
public interface IMm52116
{
    long TimeNanoseconds { get; }
    long AddressAccessNanoseconds { get; }
    long SelectAccessNanoseconds { get; }
    long OutputDisableNanoseconds { get; }
    byte ActiveSelectLevels { get; }
    ushort Address { get; }
    byte ChipSelectLevels { get; }
    bool ChipSelected { get; }
    bool OutputsDriven { get; }
    byte? OutputData { get; }
    long? NextEventNanoseconds { get; }
    Action<byte?, bool>? OutputsChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void SetAddress(ushort address);
    void SetChipSelects(byte levels);
    void AdvanceTo(long nanoseconds);
}
