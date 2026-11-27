namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal enum Sh4PlatformSystem
{
    Dreamcast = 0,
    DevelopmentUnit = 1,
    Naomi = 2,
    Naomi2 = 3,
    Atomiswave = 4,
    SystemSp = 5
}

internal sealed class Sh4Platform(
    Sh4Configuration configuration,
    Action<ushort> writeBoardId,
    Func<ushort> readBoardId)
{
    private static string _userDataDirectory = string.Empty;

    internal Sh4PlatformSystem System { get; set; }
    internal uint RamSize { get; set; }
    internal uint RamMask { get; set; }
    internal uint VramSize { get; set; }
    internal uint VramMask { get; set; }
    internal uint AramSize { get; set; }
    internal uint AramMask { get; set; }
    internal uint BiosSize { get; set; }
    internal uint FlashSize { get; set; }

    internal bool IsNaomi1 => System == Sh4PlatformSystem.Naomi;
    internal bool IsNaomi2 => System == Sh4PlatformSystem.Naomi2;
    internal bool IsNaomi => IsNaomi1 || IsNaomi2;
    internal bool IsAtomiswave => System == Sh4PlatformSystem.Atomiswave;
    internal bool IsArcade => !IsConsole;
    internal bool IsConsole => System == Sh4PlatformSystem.Dreamcast;
    internal bool IsSystemSp => System == Sh4PlatformSystem.SystemSp;

    internal int Cable => configuration.Cable;

    internal void WriteBoardId(ushort value) => writeBoardId(value);

    internal ushort ReadBoardId() => readBoardId();

    internal static void SetUserDataDirectory(string directory)
    {
        _userDataDirectory = directory;
        if (OperatingSystem.IsWindows())
        {
            if (_userDataDirectory.Length == 0 || _userDataDirectory[^1] is not ('/' or '\\'))
                _userDataDirectory += '\\';
        }
        else if (_userDataDirectory.Length == 0 || _userDataDirectory[^1] != '/')
        {
            _userDataDirectory += '/';
        }
    }

    internal static string GetWritableDataPath(string name) => _userDataDirectory + name;
}
