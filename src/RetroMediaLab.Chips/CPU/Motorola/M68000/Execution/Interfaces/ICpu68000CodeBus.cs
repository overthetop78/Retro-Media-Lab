namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Optional direct instruction-read capability for the native backend.</summary>
public interface ICpu68000CodeBus
{
    /// <summary>
    /// Borrows a contiguous window whose little-endian host words equal ReadMapped16
    /// for even guest addresses. logicalBase is a 24-bit, even guest address.
    /// Reads must have no side effects, unless ICpu68000ObservedCodeBus supplies
    /// explicit peripheral-clock observation. Such windows cannot be projected
    /// as direct PC-relative data reads. The owner keeps the window mapped and
    /// prevents concurrent mutation for one block execution; code bytes may change
    /// between executions. A changed bank must be resolved again by the caller.
    /// No ownership is transferred. Return false for device/handler-backed code.
    /// </summary>
    bool TryGetCodeMemory(uint address, out ReadOnlyMemory<byte> memory, out uint logicalBase);
}
