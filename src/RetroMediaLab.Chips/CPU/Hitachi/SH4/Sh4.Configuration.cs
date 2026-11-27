namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed class Sh4Configuration
{
    internal bool DynarecEnabled { get; set; } = true;
    internal int Sh4Clock { get; set; } = 200;
    internal int Cable { get; set; } = 3;
    internal static bool SerialConsole { get; set; }
    internal static bool SerialPty { get; set; }
}
