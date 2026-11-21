using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;

internal sealed class Sf7000Machine : Sc3000Machine
{
    internal const int IplFirmwareId = 0x7000;
    private readonly byte[] _ipl = new byte[8192];
    private readonly Sf7000DiskDrive _drive = new();
    private readonly Sf7000PeripheralPorts _ports;
    private Sf7000PrintFile? _printer;
    private ulong _frameTick;
    private bool _iplVisible;

    internal Sf7000Machine(MachineResources resources) : base(resources)
    {
        _ports = new Sf7000PeripheralPorts(null, ReadBoardPins, WriteBoardPins);
    }

    internal void LoadIpl(Func<int, Memory<byte>, int> read)
    {
        if (read(IplFirmwareId, _ipl) != _ipl.Length)
            throw new InvalidDataException("sf7000.rom");
    }

    protected override bool UsesComputerPpi => true;

    internal void SetDisk(string? path) => _drive.Insert(path);

    internal void ConnectPrinter(string? directory)
    {
        Sf7000PrintFile? previous = _printer;
        _printer = new Sf7000PrintFile(directory);
        _ports.Printer = _printer;
        previous?.Dispose();
    }

    protected override void DisposeMachineDevices()
    {
        _ports.Printer = null;
        _printer?.Dispose();
        _printer = null;
    }

    protected override void ResetMachineDevices()
    {
        base.ResetMachineDevices();
        _frameTick = 0;
        _drive.Reset();
        _ports.Reset();
        _iplVisible = true;
        MapFlatZ80Memory(_ipl);
    }

    protected override void FinishMachineAudioFrame(uint cycles)
    {
        base.FinishMachineAudioFrame(cycles);
        _frameTick += cycles;
        _drive.Advance(_frameTick, MasterClock);
    }

    protected override byte ReadPort(uint port)
    {
        int address = (byte)port;
        if (address is < 0xE0 or > 0xE9) return base.ReadPort(port);
        _drive.Advance(_frameTick + MachineZ80Cycles, MasterClock);
        return address switch
        {
            0xE0 => _drive.Controller.ReadMainStatus(),
            0xE1 => _drive.Controller.ReadFifo(),
            _ => _ports.TryRead(port, out byte value) ? value : byte.MaxValue
        };
    }

    protected override void WritePort(uint port, byte value)
    {
        int address = (byte)port;
        if (address is < 0xE0 or > 0xE9)
        {
            base.WritePort(port, value);
            return;
        }
        _drive.Advance(_frameTick + MachineZ80Cycles, MasterClock);
        if (address == 0xE1) _drive.Controller.WriteFifo(value);
        else _ports.TryWrite(port, value);
    }

    private byte ReadBoardPins() => (byte)((_drive.Controller.Irq ? 1 : 0) | (_drive.Index ? 4 : 0));

    private void WriteBoardPins(byte value, byte mask)
    {
        _drive.SetControl(value, mask);
        bool visible = (mask & value & 0x40) == 0;
        if (_iplVisible == visible) return;
        _iplVisible = visible;
        MapFlatZ80Memory(visible ? _ipl : null);
    }
}
