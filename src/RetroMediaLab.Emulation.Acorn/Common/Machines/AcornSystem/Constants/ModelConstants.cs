namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants;

internal static class SystemModelConstants
{
    internal const string ResourcePrefix = "Emulation.Acorn.Model.";
    internal const int SystemOneClock = 1000000;
    internal const int StandardClock = 1000000;
    internal const int FastClock = 2000000;
    internal const int CpuRamBytes = 1024;
    internal const int RamCardBytes = 8192;
    internal const int SystemTwoRamBytes = CpuRamBytes + RamCardBytes;
    internal const int SystemThreeRamBytes = CpuRamBytes + RamCardBytes * 2;
    internal const int SystemFiveRamBytes = 50176;
    internal const int KeyboardPorts = 1;
    internal const string CpuOption = "acornemu.system.cpu";
    internal const string RomGroup = "Rom";
    internal const string CpuGroup = "Cpu";
    internal const string RamGroup = "Ram";
    internal const string CpuLabel = "Emulation.Acorn.System.Cpu";
    internal const string CpuResourcePrefix = "Emulation.Acorn.Cpu.";
    internal const string MemoryLabel = "Emulation.Acorn.System.Memory";
    internal const string MonitorLabel = "Emulation.Acorn.System.MonitorRom";
    internal const string BasicLabel = "Emulation.Firmware.Rom.Basic";
    internal const string CharactersLabel = "Emulation.Acorn.Firmware.CharacterGenerator";
    internal const string BreakLabel = "Emulation.Acorn.Key.Break";
    internal const int FrameRate = 50;
    internal const int PixelBytes = 4;
    internal const int PixelsWide = 480;
    internal const int PixelsHigh = 250;
    internal const int KeypadPixelsHigh = 160;
    internal const float ScreenAspect = 4f / 3f;
    internal const float KeypadAspect = 3f;
    internal const int BasicRomBytes = 4096;
    internal const int SmallMonitorBytes = 2048;
    internal const int MonitorBytes = 4096;
    internal const int FastMonitorBytes = 8192;
    internal const int LargestMonitorBytes = 16384;
    internal const int CharacterRomBytes = 960;
    internal const int MonochromeCharacterRomBytes = 2048;
    internal const int KeypadMonitorBytes = 512;
    internal const int TapeBaud = 300;
    internal const string MemorySuffix = " KiB";
    internal const string CpuSummaryFormat = "CPU {0}";
    internal const string MemorySummaryFormat = "RAM {0} KiB";
    internal const string CoreSummaryFormat = "Core {0}";
}
