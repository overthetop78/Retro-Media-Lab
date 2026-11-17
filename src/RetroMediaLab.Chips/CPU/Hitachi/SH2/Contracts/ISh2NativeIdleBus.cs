namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

/// <summary>Optional certification for translating an already mapped idle loop.</summary>
/// <remarks>The two or three words must already be mapped. Fetching them again
/// must not change memory, cache, polling or device state. The returned total
/// includes instruction and wait clocks, and must remain valid under the code
/// epochs/versions used for the mapped words. No IO may be certified.</remarks>
public interface ISh2NativeIdleBus
{
    bool TryGetNativeIdleClocks(uint firstAddress, int words, out int clocks);
}
