using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM23xx.TMM23256.Services;

// Immutable projection of the selected profile for the one execution engine.
// The original profile remains the identity serialized by the public contract.
internal readonly struct Tmm23256LocalTiming
{
    internal readonly long Access, OutputEnable, OutputDisable, Cycle, CePulse,
        CeOff, AddressHold, Startup, Precharge, OutputHold, MaximumDelay;
    internal readonly int Capacity;
    internal readonly bool LatchesAddress, AlwaysEnabled, ActiveHigh;

    internal Tmm23256LocalTiming(Tmm23256Timing p)
    {
        Access = p.Access; OutputEnable = p.OutputEnable; OutputDisable = p.OutputDisable;
        Cycle = p.Cycle; CePulse = p.CePulse; CeOff = p.CeOff;
        AddressHold = p.AddressHold; Startup = p.Startup; Precharge = p.Precharge;
        OutputHold = p.OutputHold; Capacity = p.Capacity; LatchesAddress = p.LatchesAddress;
        AlwaysEnabled = p.OeMode == Tmm23256OutputEnableMode.AlwaysEnabled;
        ActiveHigh = p.OeMode == Tmm23256OutputEnableMode.ActiveHigh;
        MaximumDelay = Math.Max(OutputHold, Math.Max(Access, Math.Max(OutputEnable, OutputDisable)));
    }
}
