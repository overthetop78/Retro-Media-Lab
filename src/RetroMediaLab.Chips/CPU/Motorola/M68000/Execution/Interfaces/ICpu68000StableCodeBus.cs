namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Optional owner guarantee for code whose bytes are stable until its version changes.</summary>
/// <remarks>
/// The owner must change CodeMemoryVersion for every content, mapping or write-policy change,
/// and serialize mutation with execution. Ordinary writable RAM must never opt in. A callback
/// changing the version must return to the runner before any further guest instruction runs.
/// </remarks>
public interface ICpu68000StableCodeBus : ICpu68000VersionedCodeBus
{
    /// <summary>Certifies the complete borrowed window, not just the first opcode.</summary>
    bool IsCodeContentStable(uint address, ReadOnlyMemory<byte> memory, uint logicalBase);

    /// <summary>
    /// Revision of this window's bytes. Owners may separate it from unrelated
    /// bank changes; CodeMemoryVersion must still announce every mutation.
    /// The default conservatively invalidates on every overall revision.
    /// </summary>
    long GetStableCodeContentVersion(uint address, ReadOnlyMemory<byte> memory, uint logicalBase) => CodeMemoryVersion;
}
