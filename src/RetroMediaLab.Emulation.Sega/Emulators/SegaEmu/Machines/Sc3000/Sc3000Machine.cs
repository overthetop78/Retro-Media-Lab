using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;
using Key = RetroMediaLab.Emulation.Enums.EmulationKey;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;

internal class Sc3000Machine(MachineResources resources) : SegaBoard(SystemHardware.Sg1000,resources)
{
    // SC-3000 Programmer's Manual, table XX20. Bits correspond to PA0–PA7 and PB0–PB3.
    private static readonly Key[][] s_keyboardRows =
    [
        [Key.D1, Key.Q, Key.A, Key.Z, Key.Escape, Key.Comma, Key.K, Key.I, Key.D8],
        [Key.D2, Key.W, Key.S, Key.X, Key.Space, Key.Period, Key.L, Key.O, Key.D9],
        [Key.D3, Key.E, Key.D, Key.C, Key.Insert, Key.Slash, Key.Semicolon, Key.P, Key.D0],
        [Key.D4, Key.R, Key.F, Key.V, Key.Delete, Key.Backquote, Key.Quote, Key.Equals, Key.Minus],
        [Key.D5, Key.T, Key.G, Key.B, Key.Unknown, Key.Down, Key.RightBracket, Key.LeftBracket, Key.Backquote],
        [Key.D6, Key.Y, Key.H, Key.N, Key.Unknown, Key.Left, Key.Return, Key.Unknown, Key.Backslash, Key.Unknown, Key.Unknown, Key.F1],
        [Key.D7, Key.U, Key.J, Key.M, Key.Unknown, Key.Right, Key.Up, Key.Unknown, Key.Break, Key.LeftAlt, Key.LeftControl, Key.LeftShift]
    ];

    private readonly Ppi8255 _ppi = new();
    private byte[]? _basicCartridge;
    internal Sc3000Cassette Cassette { get; } = new();
    internal bool HasBasicCartridge => _basicCartridge is not null;
    internal IReadOnlySet<Key> Keys { get; set; } = EmulationInputSnapshot.Empty.Keys;

    protected override void InitializeMachineBus()
    {
        InitializeEightBitMapping();
        _basicCartridge = ReadBasicCartridge();
    }
    protected override void ConfigureZ80Ports()
    {
        _ppi.ReadPortA = ReadPpiPortA;
        _ppi.ReadPortB = ReadPpiPortB;
        SetZ80Ports(ReadPort, WritePort);
    }
    protected override Action<int> CreateFrameScheduler() => CreateEightBitFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.Sg1000 or SystemHardware.Sg1000II or SystemHardware.Sg1000IIExtendedRam;

    protected override void ResetMachineDevices()
    {
        base.ResetMachineDevices();
        _ppi.Reset();
        if (_basicCartridge is not null)
            MapFlatZ80Memory(_basicCartridge, romWindow: 0x8000, ramStart: 0x8000);
    }

    protected override void FinishMachineAudioFrame(uint cycles)
    {
        base.FinishMachineAudioFrame(cycles);
        Cassette.EndFrame(cycles, MasterClock);
    }

    protected virtual bool UsesComputerPpi => _basicCartridge is not null || Cassette.State != EmulationTapeState.Empty;

    protected virtual byte ReadPort(uint port)
    {
        if ((port & 0xFC) == 0xDC && UsesComputerPpi)
            return _ppi.Read((int)port & 3);
        return ReadSg1000Port(port);
    }

    protected virtual void WritePort(uint port, byte value)
    {
        if ((port & 0xFC) == 0xDC && UsesComputerPpi)
            _ppi.Write((int)port & 3, value);
        else
            WriteSg1000Port(port, value);
    }

    private int KeyboardRow => _ppi.Read(2) & 7;

    private byte ReadPpiPortA() => KeyboardRow == 7 ? ReadSg1000Port(0xDC) : (byte)ReadKeyboardRow();

    private byte ReadPpiPortB()
    {
        int inputs = KeyboardRow == 7 ? ReadSg1000Port(0xDD) & 0x0F : (ReadKeyboardRow() >> 8) & 0x0F;
        return (byte)(inputs | 0x10 | (Cassette.ReadInput(MachineZ80Cycles, MasterClock) ? 0x80 : 0));
    }

    private int ReadKeyboardRow()
    {
        Key[] row = s_keyboardRows[KeyboardRow];
        int value = 0xFFF;
        for (int bit = 0; bit < row.Length; bit++)
        {
            Key key = row[bit];
            if (key != Key.Unknown && (Keys.Contains(key)
                || (key == Key.LeftShift && Keys.Contains(Key.RightShift))
                || (key == Key.LeftControl && Keys.Contains(Key.RightControl))))
                value &= ~(1 << bit);
        }
        return value;
    }
}
