using System.Globalization;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class PointerConstants
{
    internal const string SpeedKey = "segaemu_pointer_speed";
    internal const string Label = "Emulation.Option.segaemu.PointerSpeed";
    internal const string Description = Label + ".Description";
    internal const string Automatic = "auto";
    internal static readonly EmulationSettingsNumericRange Range = new(0.10, 8, 0.10)
    { FineStep = 0.01, ShowNumericInput = true };
    internal static string PortKey(int port) => $"segaemu_pointer_port{port + 1}_speed";
    internal static readonly IReadOnlyList<string> ControllerIds =
    [nameof(PeripheralCategory.SegaMegaMouse), nameof(PeripheralCategory.SegaMenacer), nameof(PeripheralCategory.SegaLightPhaser),
        nameof(PeripheralCategory.SegaSportsPad), nameof(PeripheralCategory.SegaPaddleControl)];
    internal static IReadOnlyList<CoreOption> Options { get; } =
    [
        new(SpeedKey, Label, Description, null, "2.00", "2.00", [],
            Tab: EmulationMachineTab.Mouse, NumericRange: Range),
        Port(0), Port(1)
    ];
    private static CoreOption Port(int port) => new(PortKey(port), Label, Description, null, Automatic, Automatic,
        [new(Automatic, "Visual.Automatic")], Tab: EmulationMachineTab.Controllers, NumericRange: Range);
    internal static int Percent(string value, int fallback) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var speed)
            && speed is >= 0.10m and <= 8m ? (int)(speed * 100) : fallback;
}
