namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Enums;

internal enum VidcRegister : uint
{
    PaletteFirst = 0x00,
    Border = 0x40,
    CursorFirst = 0x44,
    CursorLast = 0x4c,
    StereoFirst = 0x60,
    StereoLast = 0x7c,
    HorizontalCycle = 0x80,
    HorizontalSync = 0x84,
    HorizontalBorderStart = 0x88,
    HorizontalDisplayStart = 0x8c,
    HorizontalDisplayEnd = 0x90,
    HorizontalBorderEnd = 0x94,
    HorizontalCursor = 0x98,
    HorizontalInterlace = 0x9c,
    VerticalCycle = 0xa0,
    VerticalSync = 0xa4,
    VerticalBorderStart = 0xa8,
    VerticalDisplayStart = 0xac,
    VerticalDisplayEnd = 0xb0,
    VerticalBorderEnd = 0xb4,
    VerticalCursorStart = 0xb8,
    VerticalCursorEnd = 0xbc,
    SoundFrequency = 0xc0,
    Control = 0xe0
}
