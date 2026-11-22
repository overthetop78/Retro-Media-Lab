namespace RetroMediaLab.App.Dictionaries.Tools;

internal static class GwToolParameterCatalog
{
    internal enum ParameterKind { Required, Optional, Check }
    internal sealed record Parameter(string Key, string LabelResourceKey, ParameterKind Kind, string DefaultValue = "");

    internal static IReadOnlyList<Parameter> For(string verb) => verb switch
    {
        "rpm" =>
        [
            new("nr", "Tool.Field.Measurements", ParameterKind.Required, "1"),
        ],
        "seek" =>
        [
            new("cylinder", "Tool.Field.Cylinder", ParameterKind.Required, "0"),
            new("force", "Tool.Field.Extreme", ParameterKind.Check),
            new("motor-on", "Tool.Field.MotorOn", ParameterKind.Check),
        ],
        "pin" =>
        [
            new("pin", "Tool.Field.Pin", ParameterKind.Required, "26"),
            new("set", "Tool.Field.SetPin", ParameterKind.Check),
            new("high", "Tool.Field.High", ParameterKind.Check),
        ],
        "delays" =>
        [
            new("select", "Tool.Field.Select", ParameterKind.Optional, "10"),
            new("step", "Tool.Field.Step", ParameterKind.Optional, "3000"),
            new("settle", "Tool.Field.Settle", ParameterKind.Optional, "15"),
            new("motor", "Tool.Field.Motor", ParameterKind.Optional, "750"),
            new("watchdog", "Tool.Field.Watchdog", ParameterKind.Optional, "10000"),
            new("pre-write", "Tool.Field.PreWrite", ParameterKind.Optional, "15"),
            new("post-write", "Tool.Field.PostWrite", ParameterKind.Optional, "15"),
            new("index-mask", "Tool.Field.IndexMask", ParameterKind.Optional, "15"),
        ],
        "align" =>
        [
            new("tracks", "Tool.Field.AlignTracks", ParameterKind.Required, "c=40:h=0-1"),
            new("revs", "Read.Revolutions", ParameterKind.Required, "3"),
            new("reads", "Tool.Field.AlignReads", ParameterKind.Required, "10"),
            new("format", "Tool.Field.AlignFormat", ParameterKind.Optional, "ibm.720"),
            new("diskdefs", "Advanced.DiskDefs", ParameterKind.Optional, "diskdefs.cfg"),
            new("raw", "Tool.Field.AlignRaw", ParameterKind.Check),
            new("fake-index", "Advanced.FakeIndex", ParameterKind.Optional, "300rpm"),
            new("hard-sectors", "Advanced.HardSectors", ParameterKind.Check),
            new("adjust-speed", "Advanced.AdjustSpeed", ParameterKind.Optional, "300rpm"),
            new("pll", "Advanced.Pll", ParameterKind.Optional, "period=5:phase=60"),
            new("densel", "Advanced.DensityPin", ParameterKind.Optional, "H"),
            new("gen-tg43", "Advanced.Tg43", ParameterKind.Check),
            new("reverse", "Advanced.Reverse", ParameterKind.Check),
        ],
        "update" =>
        [
            new("bootloader", "Tool.Field.Bootloader", ParameterKind.Check),
        ],
        _ => []
    };
}
