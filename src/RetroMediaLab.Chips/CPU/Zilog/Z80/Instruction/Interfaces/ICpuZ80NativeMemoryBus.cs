namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

/// <summary>Optional side-effect-free data windows for the native Z80 backend.</summary>
public interface ICpuZ80NativeMemoryBus : ICpuZ80Bus
{
    /// <summary>Declines versioning when changes must instead be detected by comparing windows.</summary>
    bool TryGetNativeMemoryVersion(out long version);
    /// <summary>Independent read/write 1 KiB windows; empty means use the original bus.</summary>
    void GetNativeMemory(int page, out ReadOnlyMemory<byte> read, out Memory<byte> write);
}
