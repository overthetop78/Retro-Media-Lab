using System.Text.Json.Serialization;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using C = RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Constants.Tmm23256Constants;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;

/// <summary>Immutable manufacturer profile for a parallel ROM peripheral; delays in nanoseconds.</summary>
public sealed record Tmm23256Timing
{
    public static Tmm23256Timing SpecifiedMaximum { get; } = new(C.MaximumAccess, C.MaximumOutputEnable, C.MaximumOutputDisable);
    public long Access { get; }
    public long OutputEnable { get; }
    public long OutputDisable { get; }
    public string Id { get; }
    public long Cycle { get; }
    public long CePulse { get; }
    public long CeOff { get; }
    public long AddressHold { get; }
    public long Startup { get; }
    public long Precharge { get; }
    public Tmm23256OutputEnableMode OeMode { get; }
    public bool LatchesAddress { get; }
    public long OutputHold { get; }
    /// <summary>Geometry of the shared mechanism; the named TMM23256P remains32KiB.</summary>
    public int Capacity { get; }
    /// <summary>False for a logical geometry without manufacturer timing evidence.</summary>
    public bool ElectricallyCharacterized { get; }
    public Tmm23256Timing(long access, long outputEnable, long outputDisable)
        : this("TMM23256P",access,outputEnable,outputDisable,C.Cycle,C.CePulse,C.CeOff,
            C.AddressHold,C.Startup,0,Tmm23256OutputEnableMode.ActiveLow)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(access, C.MaximumAccess);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(outputEnable, C.MaximumOutputEnable);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(outputDisable, C.MaximumOutputDisable);
    }

    /// <summary>Shared peripheral mechanism; manufacturer values belong to the selected hardware profile.</summary>
    [JsonConstructor]
    public Tmm23256Timing(string id,long access,long outputEnable,long outputDisable,long cycle,
        long cePulse,long ceOff,long addressHold,long startup,long precharge,Tmm23256OutputEnableMode oeMode,
        bool latchesAddress = true,long outputHold = 0,int capacity = C.Capacity,
        bool electricallyCharacterized = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        foreach (long value in new[] { access,outputEnable,outputDisable,cycle,cePulse,ceOff,addressHold,startup,precharge,outputHold })
            ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(outputHold,access);
        if (!Enum.IsDefined(oeMode)) throw new ArgumentOutOfRangeException(nameof(oeMode));
        if (capacity is not (8192 or 16384 or 32768)) throw new ArgumentOutOfRangeException(nameof(capacity));
        Id = id; Cycle = cycle; CePulse = cePulse; CeOff = ceOff; AddressHold = addressHold;
        Startup = startup; Precharge = precharge; OeMode = oeMode;
        LatchesAddress = latchesAddress; OutputHold = outputHold;
        Access = access; OutputEnable = outputEnable; OutputDisable = outputDisable;
        Capacity = capacity; ElectricallyCharacterized = electricallyCharacterized;
    }
}
