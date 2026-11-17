namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Optional peripheral-clock effects for a borrowed ROM compilation view.</summary>
public interface ICpu68000ObservedCodeBus : ICpu68000CodeBus
{
    /// <summary>Uniform over a returned window; changes belong to CodeMemoryVersion.</summary>
    bool RequiresCodeFetchObservation(uint address);
    /// <summary>Observe the instruction clock without interpreting an opcode.
    /// May advance peripherals; must not redirect the CPU or change its code mapping.</summary>
    void ObserveCodeFetch(uint address);
}
