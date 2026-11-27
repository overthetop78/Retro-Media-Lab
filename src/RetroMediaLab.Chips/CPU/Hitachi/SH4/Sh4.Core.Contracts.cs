namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed class Sh4InstructionException(uint programCounter, Sh4ExceptionEvent exceptionEvent) : Exception
{
    internal uint ProgramCounter { get; private set; } = programCounter;
    internal Sh4ExceptionEvent Event { get; private set; } = exceptionEvent;

    internal void AdjustForDelaySlot()
    {
        ProgramCounter = unchecked(ProgramCounter - 2);
        Event = Event switch
        {
            Sh4ExceptionEvent.FpuDisabled => Sh4ExceptionEvent.SlotFpuDisabled,
            Sh4ExceptionEvent.IllegalInstruction => Sh4ExceptionEvent.SlotIllegalInstruction,
            _ => Event
        };
    }
}

internal static class Sh4FloatingPoint
{
    internal static float FixNaN(float value)
    {
#if SH4_STRICT_MODE
        if (float.IsNaN(value))
            return BitConverter.UInt32BitsToSingle(0x7FBFFFFF);
#endif
        return value;
    }

    internal static double FixNaN64(double value)
    {
#if SH4_STRICT_MODE
        if (double.IsNaN(value))
            return BitConverter.UInt64BitsToDouble(0x7FF7FFFFFFFFFFFF);
#endif
        return value;
    }
}
