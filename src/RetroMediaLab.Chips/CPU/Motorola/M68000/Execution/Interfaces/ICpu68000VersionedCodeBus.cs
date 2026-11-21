namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>
/// Optional revision covering every window returned by TryGetCodeMemory,
/// including backing storage, base address and executable/handler policy.
/// Changes to the bytes themselves need not change this revision: native opcode
/// guards still check them. Owners serialize remapping with CPU execution.
/// Without this declaration the core resolves each entry and does not chain.
/// </summary>
public interface ICpu68000VersionedCodeBus : ICpu68000CodeBus
{
    long CodeMemoryVersion { get; }
}
