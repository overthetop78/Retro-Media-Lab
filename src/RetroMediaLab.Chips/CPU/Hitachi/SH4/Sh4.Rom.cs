namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4FscaTable
{
    private static readonly Sh4FscaPair[] SineTable = GC.AllocateArray<Sh4FscaPair>(0x10000, pinned: true);

    internal static unsafe nuint NativeAddress
    {
        get
        {
            fixed (Sh4FscaPair* table = SineTable)
                return (nuint)table;
        }
    }

    static Sh4FscaTable()
    {
        ReadOnlySpan<uint> coefficients = Coefficients;
        for (int phase = 0; phase < 0x10000; phase++)
        {
            if (phase < 0x8000)
                SineTable[phase].Sine = BitConverter.UInt32BitsToSingle(coefficients[phase]);
            else
                SineTable[phase].Sine = -BitConverter.UInt32BitsToSingle(coefficients[phase - 0x8000]);
        }

        Sh4HostRuntime.Verify(SineTable[0x8000].Sine == 0, "sin_table[0x8000].u[0]==0");

        for (int phase = 0; phase < 0x10000; phase++)
            SineTable[phase].Cosine = SineTable[(phase + 0x4000) & 0xFFFF].Sine;
    }

    internal static (float Sine, float Cosine) GetPair(int phase)
    {
        Sh4FscaPair value = SineTable[phase];
        return (value.Sine, value.Cosine);
    }
}
