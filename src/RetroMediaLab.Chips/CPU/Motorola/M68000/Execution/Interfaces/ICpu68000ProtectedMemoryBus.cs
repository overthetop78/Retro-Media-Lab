namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Optional native projection of RAM with an ignored-write prefix.</summary>
public interface ICpu68000ProtectedMemoryBus : ICpu68000DirectMemoryBus
{
    /// <summary>
    /// Borrows a complete 64 KiB bank, in the normal direct-memory word order.
    /// Writes with a guest bank offset less than minimumOffset are ignored;
    /// other writes store directly. Each word of a long is checked separately.
    /// Empty memory declines this projection. minimumOffset is in [0,65536].
    /// No read or write has other side effects. DirectMemoryVersion must change
    /// whenever the backing memory, minimum offset or eligibility changes.
    /// </summary>
    void GetProtectedWriteMemory(int page, out Memory<byte> memory, out int minimumOffset);
}
