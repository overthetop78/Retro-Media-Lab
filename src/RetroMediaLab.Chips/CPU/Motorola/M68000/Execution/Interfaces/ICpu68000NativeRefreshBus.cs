namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public enum Cpu68000NativeRefreshMode { Unsupported, OwnState, StableDuringNativeExecution }

/// <summary>
/// Optional declaration for translating IsRefreshWindowElapsed. OwnState means
/// cycles >= deadline. StableDuringNativeExecution allows sampling the existing bus predicate
/// before a native chain; the predicate's value must remain stable until the next managed return.
/// Undeclared predicates retain the interpreter path for affected instructions.
/// </summary>
public interface ICpu68000NativeRefreshBus
{
    Cpu68000NativeRefreshMode NativeRefreshMode { get; }
}
