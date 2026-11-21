namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Optional side-effect-free data windows, in the bus's host word order.</summary>
public interface ICpu68000DirectMemoryBus
{
    /// <summary>Changes whenever a window or its read/write handler policy changes.</summary>
    long DirectMemoryVersion { get; }
    /// <summary>
    /// Borrows a complete 64 KiB bank. Empty means the operation must use the bus.
    /// Byte addresses use offset XOR 1; words use offset AND 65534 and are
    /// little-endian host words (including odd addresses when errors are disabled).
    /// Read and write eligibility are independent. Owners serialize map changes
    /// with block execution. All memory remains owned by the bus/machine.
    /// </summary>
    void GetDirectMemory(int page, out ReadOnlyMemory<byte> read, out Memory<byte> write);
}
