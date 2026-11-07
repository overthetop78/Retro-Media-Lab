using RetroMediaLab.App.Contracts.Emulation.Machine;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Constants.Emulation;

internal static class EmulationMachineTabConstants
{
    internal const double HorizontalPadding = 14;
    internal const double VerticalPadding = 9;
    internal const double OuterMargin = 8;
    internal const string RamGeometry = "M2,5 H22 V17 H2 Z M5,8 H9 V13 H5 Z M11,8 H15 V13 H11 Z M17,8 H20 V13 H17 Z M4,17 V20 M7,17 V20 M10,17 V20 M14,17 V20 M17,17 V20 M20,17 V20";
    internal const string RomGeometry = "M7,3 H10 C10,6 14,6 14,3 H17 V21 H7 Z M3,6 H7 M3,10 H7 M3,14 H7 M3,18 H7 M17,6 H21 M17,10 H21 M17,14 H21 M17,18 H21";

    internal static readonly IReadOnlyList<EmulationMachineTabDefinition> Definitions =
    [
        new(EmulationMachineTab.General, IconGlyphs.Settings, "Emulation.Tab.General"),
        new(EmulationMachineTab.Cpu, IconGlyphs.Processor, "Emulation.Tab.Cpu"),
        new(EmulationMachineTab.Ram, IconGlyphs.Memory, "Emulation.Tab.Ram"),
        new(EmulationMachineTab.Rom, IconGlyphs.File, "Emulation.Tab.Rom"),
        new(EmulationMachineTab.Video, IconGlyphs.Display, "Emulation.Tab.Video"),
        new(EmulationMachineTab.Audio, IconGlyphs.Audio, "Emulation.Tab.Audio"),
        new(EmulationMachineTab.Extensions, IconGlyphs.Connection, "Emulation.Tab.Extensions"),
        new(EmulationMachineTab.Storage, IconGlyphs.HardDisk, "Emulation.Tab.Storage"),
        new(EmulationMachineTab.Keyboard, EmulationInputSettingsConstants.KeyboardIcon, "Emulation.Tab.Keyboard"),
        new(EmulationMachineTab.Mouse, IconGlyphs.Mouse, "Emulation.Tab.Mouse"),
        new(EmulationMachineTab.Controllers, IconGlyphs.Controller, "Emulation.Tab.Controller"),
        new(EmulationMachineTab.Cheats, IconGlyphs.Settings, "Emulation.Tab.Cheats"),
        new(EmulationMachineTab.Ethernet, IconGlyphs.Connection, "Emulation.Tab.Ethernet")
    ];
}
