using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4AddressSpace
{
    private static Sh4SerialCommunicationRegisters SerialRegisters => Sh4SerialCommunicationRegisters.Instance;
    private static Sh4SerialFifoRegisters SerialFifoRegisters => Sh4SerialFifoRegisters.Instance;
    private static Sh4SerialFifoPort SerialFifoPort => Sh4SerialFifoPort.Instance;
}

internal sealed partial class Sh4SerialFifoPort
{
    private static readonly int[] ReceiveTriggerLevels = [1, 4, 8, 14];
    private static Sh4SerialFifoRegisters Registers => Sh4SerialFifoRegisters.Instance;
    private static Sh4Scheduler Scheduler => Sh4Context.Active.Scheduler;
    private static Sh4Interrupts Interrupts => Sh4Context.Active.Interrupts;

    private Sh4SerialFifoPort()
    {
    }

    private static class InstanceStorage
    {
        internal static readonly Sh4SerialFifoPort Value = new();
    }

    private static class PtyPipeStorage
    {
        internal static readonly ConsolePipe Value = new();
    }

    internal static Sh4SerialFifoPort Instance => InstanceStorage.Value;

    private void UpdateInterrupts()
    {
        uint status = Registers.Status;
        uint control = Registers.Control;
        bool receiveErrorEnabled = (control & 0x48) != 0;
        Interrupts.SetPending(Sh4InterruptId.ScifTxi, (status & 0x20) != 0);
        Interrupts.SetMask(Sh4InterruptId.ScifTxi, (control & 0x80) != 0);
        Interrupts.SetPending(Sh4InterruptId.ScifRxi, (status & 3) != 0);
        Interrupts.SetMask(Sh4InterruptId.ScifRxi, (control & 0x40) != 0);
        Interrupts.SetPending(Sh4InterruptId.ScifBri, (status & 0x10) != 0);
        Interrupts.SetMask(Sh4InterruptId.ScifBri, receiveErrorEnabled);
        Interrupts.SetPending(Sh4InterruptId.ScifEri, (status & 0x8C) != 0);
        Interrupts.SetMask(Sh4InterruptId.ScifEri, receiveErrorEnabled);
    }

    private int ScheduledCallback(int tag, int cycles, int lag, nint argument)
    {
        if (tag == 0)
        {
            bool reschedule = TransmitDone();
            ReceiveScheduled();
            if (reschedule || _pipe is not null)
                return _frameSize * _cyclesPerBit;
            return 0;
        }
        SendBreak();
        return 0;
    }

    private bool IsTransmitFifoEmpty() => _transmitFifo.Count <= 1 << (3 - (int)(Registers.FifoControl >> 4 & 3));

    private bool IsReceiveFifoFull() => _receiveFifo.Count >= ReceiveTriggerLevels[Registers.FifoControl >> 6 & 3];

    private bool TransmitDone()
    {
        if (!_transmitting || (Registers.FifoControl & 4) != 0)
            return false;
        if (_transmitFifo.Count == 0)
        {
            SetStatusBit(StatusBit.TransmitEnd);
            _transmitting = false;
            return false;
        }

        byte value = _transmitFifo.Dequeue();
        _pipe?.Write(value);
        if (IsTransmitFifoEmpty())
        {
            SetStatusBit(StatusBit.TransmitFifoDataEmpty);
            UpdateInterrupts();
        }

        return true;
    }

    private void ReceiveScheduled()
    {
        if (_pipe is null)
            return;
        if (_pipe.Available() > 0)
        {
            byte value = _pipe.Read();
            if ((Registers.Control & 0x10) == 0 || (Registers.FifoControl & 2) != 0)
                return;
            if (_receiveFifo.Count == 16)
            {
                Registers.LineStatus |= 1;
                UpdateInterrupts();
                Trace.TraceInformation("scif: Receive overrun");
            }
            else
            {
                _receiveFifo.Enqueue(value);
                if (IsReceiveFifoFull())
                {
                    SetStatusBit(StatusBit.ReceiveFifoDataFull);
                    UpdateInterrupts();
                }
            }
        }
        else if (_receiveFifo.Count != 0)
        {
            SetStatusBit(StatusBit.DataReady);
            UpdateInterrupts();
        }
    }

    private void UpdateBaudRate()
    {
        uint mode = Registers.Mode;
        _frameSize = 10 - (int)(mode >> 6 & 1) + (int)(mode >> 5 & 1) + (int)(mode >> 3 & 1);
        int baud = Sh4Context.MainClock / 4 / ((int)Registers.BitRate + 1) / 32 / (1 << (int)((mode & 3) * 2));
        _cyclesPerBit = Sh4Context.MainClock / baud;
        Trace.TraceInformation($"SCIF: Frame size {_frameSize} cycles/bit {_cyclesPerBit} ({baud} bauds) pipe {_pipe}");
        Scheduler.Request(_schedulerId, _frameSize * _cyclesPerBit);
    }

    internal void WriteTransmitData(byte data)
    {
#if DEBUG_SCIF
        Trace.TraceInformation($"serial out {data:x2} {(data == 0 ? ' ' : (char)data)} fifo_sz {_transmitFifo.Count}");
#endif
        if ((Registers.FifoControl & 4) != 0)
            return;
        if ((Registers.Mode & 0x40) != 0)
            data &= 0x7F;
        if (_transmitFifo.Count == 0 && !_transmitting && (Registers.Control & 0x20) != 0)
        {
            _pipe?.Write(data);
            _transmitting = true;
            Scheduler.Request(_schedulerId, _frameSize * _cyclesPerBit);
            SetStatusBit(StatusBit.TransmitFifoDataEmpty);
            UpdateInterrupts();
        }
        else if (_transmitFifo.Count < 16)
        {
            _transmitFifo.Enqueue(data);
        }
    }

    internal ushort ReadStatus() => _statusLastRead = (ushort)Registers.Status;

    private void SetStatusBit(StatusBit bit)
    {
        _statusLastRead &= (ushort)~(ushort)bit;
        Registers.Status |= (ushort)bit;
    }

    internal void WriteStatus(ushort data)
    {
        data = (ushort)(data | ~0xF3 | ~_statusLastRead);
        if (IsReceiveFifoFull())
            data |= (ushort)StatusBit.ReceiveFifoDataFull;
        if (IsTransmitFifoEmpty())
            data |= (ushort)StatusBit.TransmitFifoDataEmpty;
        if (_receiveFifo.Count != 0)
            data |= (ushort)StatusBit.DataReady;
#if DEBUG_SCIF
        Trace.TraceInformation("SCIF_SCFSR2.reset "
            + ((data & 0x80) == 0 ? "ER " : "")
            + ((data & 0x40) == 0 ? "TEND " : "")
            + ((data & 0x20) == 0 ? "TDFE " : "")
            + ((data & 0x10) == 0 ? "BRK " : "")
            + ((data & 0x08) == 0 ? "FER " : "")
            + ((data & 0x04) == 0 ? "PER " : "")
            + ((data & 0x02) == 0 ? "RDF " : "")
            + ((data & 0x01) == 0 ? "DR" : ""));
#endif
        Registers.Status &= data;
        _statusLastRead &= data;
        UpdateInterrupts();
    }

    internal ushort ReadFifoCount()
    {
        ushort value = (ushort)(_receiveFifo.Count | (_transmitFifo.Count << 8));
#if DEBUG_SCIF
        Trace.TraceInformation($"SCIF: fifo count rx {value & 0xff} tx {value >> 8}");
#endif
        return value;
    }

    internal byte ReadReceiveData()
    {
        if (_receiveFifo.Count == 0)
        {
            Trace.TraceInformation("Empty rx fifo read");
            return 0;
        }
        byte value = _receiveFifo.Dequeue();
#if DEBUG_SCIF
        Trace.TraceInformation($"serial in {value:x2} {(char)value}");
#endif
        return value;
    }

    internal void WriteControl(ushort data)
    {
        Registers.Control = (uint)(data & 0xFA);
        if ((Registers.Control & 0x20) == 0)
        {
            SetStatusBit(StatusBit.TransmitEnd);
            SetBreak((Registers.Port & 3) == 2);
        }
        else
        {
            SetBreak(false);
        }

        UpdateInterrupts();
#if DEBUG_SCIF
        uint control = Registers.Control;
        Trace.TraceInformation("SCIF_SCSCR2= "
            + ((control & 0x80) != 0 ? "TIE " : "")
            + ((control & 0x40) != 0 ? "RIE " : "")
            + ((control & 0x20) != 0 ? "TE " : "")
            + ((control & 0x10) != 0 ? "RE " : "")
            + ((control & 0x08) != 0 ? "REIE" : ""));
#endif
    }

    internal ushort ReadPort()
    {
        uint port = Registers.Port;
#if DEBUG_SCIF
        Trace.TraceInformation($"SCIF_SCSPTR2.read {port:x}");
#endif
        return (ushort)(port & ~0x10u);
    }

    private void SetBreak(bool enabled)
    {
        if (enabled)
        {
            if (!Scheduler.IsScheduled(_breakSchedulerId))
                Scheduler.Request(_breakSchedulerId, _cyclesPerBit * _frameSize);
        }
        else
        {
            if (Scheduler.IsScheduled(_breakSchedulerId))
                Scheduler.Request(_breakSchedulerId, -1);
        }
    }

    internal void WritePort(ushort data)
    {
        Registers.Port = (uint)(data & 0xF3);
        if ((Registers.Port & 2) != 0)
            SetBreak((Registers.Port & 1) == 0 && (Registers.Control & 0x20) == 0);
        else
            SetBreak(false);
#if DEBUG_SCIF
        uint port = Registers.Port;
        Trace.TraceInformation("SCIF_SCSPTR2= "
            + ((port & 0x80) != 0 ? "RTSIO " : "")
            + ((port & 0x40) != 0 ? "RTSDT " : "")
            + ((port & 0x20) != 0 ? "CTSIO " : "")
            + ((port & 0x10) != 0 ? "CTSDT " : "")
            + ((port & 0x02) != 0 ? "SPB2IO " : "")
            + ((port & 0x01) != 0 ? "SPB2DT" : ""));
#endif
    }

    internal void WriteFifoControl(ushort data)
    {
        if ((Registers.FifoControl & 4) != 0 && (data & 4) == 0)
        {
            SetStatusBit(StatusBit.TransmitEnd);
            SetStatusBit(StatusBit.TransmitFifoDataEmpty);
            UpdateInterrupts();
        }

        Registers.FifoControl = (byte)data;
        if ((Registers.FifoControl & 4) != 0)
        {
            _transmitFifo.Clear();
            if (_pipe is null)
                Scheduler.Request(_schedulerId, -1);
            _transmitting = false;
        }
        if ((Registers.FifoControl & 2) != 0)
            _receiveFifo.Clear();
#if DEBUG_SCIF
        uint control = Registers.FifoControl;
        Trace.TraceInformation("SCIF_SCFCR2= "
            + ((control & 0x02) != 0 ? "RFRST " : "")
            + ((control & 0x04) != 0 ? "TFRST " : "")
            + ((control & 0x08) != 0 ? "MCE " : "")
            + $"TTRG {control >> 4 & 3} RTRG {control >> 6 & 3}");
#endif
    }

    internal static void WriteBitRate(uint address, byte data)
    {
        Registers.BitRate = data;
        Instance.UpdateBaudRate();
    }

    internal static void WriteMode(uint address, ushort data)
    {
        Registers.Mode = (uint)(data & 0x7B);
        Instance.UpdateBaudRate();
    }

    internal override void ReceiveBreak()
    {
#if DEBUG_SCIF
        Trace.TraceInformation("Break received");
#endif
        SetStatusBit(StatusBit.Break);
        UpdateInterrupts();
    }

    private void SendBreak() => _pipe?.SendBreak();

    internal void Initialize()
    {
        if (_schedulerId == -1)
            _schedulerId = Scheduler.Register(0, ScheduledCallback);
        if (_breakSchedulerId == -1)
            _breakSchedulerId = Scheduler.Register(1, ScheduledCallback);
    }

    internal void Terminate()
    {
        try
        {
            if (_schedulerId != -1)
                Scheduler.Unregister(_schedulerId);
        }
        finally
        {
            _schedulerId = -1;
            try
            {
                if (_breakSchedulerId != -1)
                    Scheduler.Unregister(_breakSchedulerId);
            }
            finally
            {
                _breakSchedulerId = -1;
            }
        }
    }

    internal void Reset()
    {
        Scheduler.Request(_breakSchedulerId, -1);
        _transmitting = false;
        _statusLastRead = 0;
        _transmitFifo.Clear();
        _receiveFifo.Clear();
        UpdateBaudRate();
    }

    internal void Serialize(BinaryWriter writer)
    {
        Scheduler.Serialize(writer, _schedulerId);
        Scheduler.Serialize(writer, _breakSchedulerId);
        writer.Write(_statusLastRead);
        writer.Write(_transmitFifo.Count);
        foreach (byte value in _transmitFifo)
            writer.Write(value);
        writer.Write(_receiveFifo.Count);
        foreach (byte value in _receiveFifo)
            writer.Write(value);
        writer.Write(_transmitting);
    }

    internal void Deserialize(BinaryReader reader, int version)
    {
        _transmitFifo.Clear();
        _receiveFifo.Clear();
        const int saveVersion43 = 838;
        if (version >= saveVersion43)
        {
            Scheduler.Deserialize(reader, _schedulerId);
            Scheduler.Deserialize(reader, _breakSchedulerId);
            _statusLastRead = reader.ReadUInt16();
            int count = reader.ReadInt32();
            for (int index = 0; index < count; index++)
                _transmitFifo.Enqueue(reader.ReadByte());
            count = reader.ReadInt32();
            for (int index = 0; index < count; index++)
                _receiveFifo.Enqueue(reader.ReadByte());
            _transmitting = reader.ReadBoolean();
        }
        else
        {
            _statusLastRead = 0;
            _transmitting = false;
        }

        Registers.BitRate &= 0xFF;
    }

    internal static void SetUpPtyPipe()
    {
        ConsolePipe ptyPipe = PtyPipeStorage.Value;
        Sh4SerialFifoPort port = Instance;
        if (Sh4Configuration.SerialConsole || Sh4Configuration.SerialPty)
        {
            if (port.GetPipe() is null)
            {
                bool initialized = false;
                try
                {
                    ptyPipe.Initialize();
                    port.SetPipe(ptyPipe);
                    initialized = true;
                }
                finally
                {
                    if (!initialized)
                        ptyPipe.Dispose();
                }
            }
        }
        else if (ReferenceEquals(port.GetPipe(), ptyPipe))
        {
            try
            {
                ptyPipe.Dispose();
            }
            finally
            {
                port.SetPipe(null);
            }
        }
    }

    private sealed class ConsolePipe : Sh4SerialPipe, IDisposable
    {
        private bool _ownsConsole;
        private int _tty = 1;

        internal void Initialize()
        {
            if (OperatingSystem.IsWindows())
            {
                if (Sh4Configuration.SerialConsole && _tty == 1)
                {
                    _ownsConsole = AllocConsole();
                    if (_ownsConsole)
                    {
                        SetConsoleTitle("Retro Media Lab Serial Output");
                        nint outputHandle = GetStdHandle(-11);
                        _tty = OpenOsFileHandle(outputHandle, 0x4000);
                        int standardOutput = FileNumber(GetStandardFile(1));
                        DuplicateDescriptor(_tty, standardOutput);
                        SetStdHandle(-11, GetOsFileHandle(standardOutput));
                    }
                    else
                    {
                        Trace.TraceError($"Cannot AllocConsole(): errno {Marshal.GetLastPInvokeError()}");
                    }
                }
            }
            else if (Sh4Configuration.SerialConsole && Sh4Configuration.SerialPty && _tty == 1
                     && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
            {
                int flags = OperatingSystem.IsMacOS() ? 0x20006 : 0x902;
                int descriptor = Open("/dev/ptmx", flags);
                if (descriptor < 0)
                {
                    Trace.TraceError($"Cannot open /dev/ptmx: errno {Marshal.GetLastPInvokeError()}");
                    return;
                }
                bool transferred = false;
                try
                {
                    GrantPty(descriptor);
                    UnlockPty(descriptor);
                    _tty = descriptor;
                    transferred = true;
                    Trace.TraceInformation($"Pseudoterminal is at {Marshal.PtrToStringAnsi(PtyName(descriptor))}");
                }
                finally
                {
                    if (!transferred)
                        Close(descriptor);
                }
            }
        }

        internal override void Write(byte data)
        {
            if (!Sh4Configuration.SerialConsole)
                return;
            if (OperatingSystem.IsWindows())
                WriteWindowsDescriptor(_tty, ref data, 1);
            else
                WriteDescriptor(_tty, ref data, 1);
        }

        internal override int Available()
        {
            int count = 0;
            if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && Sh4Configuration.SerialConsole && _tty != 1)
            {
                nuint request = OperatingSystem.IsMacOS() ? 0x4004667Fu : 0x541Bu;
                Ioctl(_tty, request, ref count);
            }
            return count;
        }

        internal override byte Read()
        {
            byte data = 0;
            if (_tty != 1)
            {
                if (OperatingSystem.IsWindows())
                    ReadWindowsDescriptor(_tty, ref data, 1);
                else
                    ReadDescriptor(_tty, ref data, 1);
            }
            return data;
        }

        public void Dispose()
        {
            try
            {
                if (_tty != 1)
                {
                    if (OperatingSystem.IsWindows())
                        CloseWindowsDescriptor(_tty);
                    else
                        Close(_tty);
                }
            }
            finally
            {
                _tty = 1;
                if (_ownsConsole)
                    FreeConsole();
                _ownsConsole = false;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
        [DllImport("kernel32.dll", EntryPoint = "SetConsoleTitleW", CharSet = CharSet.Unicode)]
        private static extern bool SetConsoleTitle(string title);
        [DllImport("kernel32.dll")]
        private static extern nint GetStdHandle(int id);
        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(int id, nint handle);
        [DllImport("ucrtbase.dll", EntryPoint = "_open_osfhandle", CallingConvention = CallingConvention.Cdecl)]
        private static extern int OpenOsFileHandle(nint handle, int flags);
        [DllImport("ucrtbase.dll", EntryPoint = "__acrt_iob_func", CallingConvention = CallingConvention.Cdecl)]
        private static extern nint GetStandardFile(uint index);
        [DllImport("ucrtbase.dll", EntryPoint = "_fileno", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FileNumber(nint file);
        [DllImport("ucrtbase.dll", EntryPoint = "_dup2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DuplicateDescriptor(int source, int destination);
        [DllImport("ucrtbase.dll", EntryPoint = "_get_osfhandle", CallingConvention = CallingConvention.Cdecl)]
        private static extern nint GetOsFileHandle(int descriptor);
        [DllImport("ucrtbase.dll", EntryPoint = "_close", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CloseWindowsDescriptor(int descriptor);
        [DllImport("ucrtbase.dll", EntryPoint = "_read", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ReadWindowsDescriptor(int descriptor, ref byte data, uint count);
        [DllImport("ucrtbase.dll", EntryPoint = "_write", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WriteWindowsDescriptor(int descriptor, ref byte data, uint count);
        [DllImport("libc", EntryPoint = "open", SetLastError = true)]
        private static extern int Open(string path, int flags);
        [DllImport("libc", EntryPoint = "grantpt")]
        private static extern int GrantPty(int descriptor);
        [DllImport("libc", EntryPoint = "unlockpt")]
        private static extern int UnlockPty(int descriptor);
        [DllImport("libc", EntryPoint = "ptsname")]
        private static extern nint PtyName(int descriptor);
        [DllImport("libc", EntryPoint = "close")]
        private static extern int Close(int descriptor);
        [DllImport("libc", EntryPoint = "read")]
        private static extern nint ReadDescriptor(int descriptor, ref byte data, nuint count);
        [DllImport("libc", EntryPoint = "write")]
        private static extern nint WriteDescriptor(int descriptor, ref byte data, nuint count);
        [DllImport("libc", EntryPoint = "ioctl")]
        private static extern int Ioctl(int descriptor, nuint request, ref int count);
    }
}

internal sealed partial class Sh4SerialFifoRegisters
{
    private const uint Base = 0x1FE80000;
    internal static Sh4SerialFifoRegisters Instance { get; } = new();

    internal uint Mode { get => GetValue(Base); set => SetValue(Base, value); }
    internal uint BitRate { get => GetValue(Base + 4); set => SetValue(Base + 4, value); }
    internal uint Control { get => GetValue(Base + 8); set => SetValue(Base + 8, value); }
    internal uint Status { get => GetValue(Base + 16); set => SetValue(Base + 16, value); }
    internal uint FifoControl { get => GetValue(Base + 24); set => SetValue(Base + 24, value); }
    internal uint Port { get => GetValue(Base + 32); set => SetValue(Base + 32, value); }
    internal uint LineStatus { get => GetValue(Base + 36); set => SetValue(Base + 36, value); }

    internal override void Initialize()
    {
        base.Initialize();
        SetWriteHandler16(Base, Sh4SerialFifoPort.WriteMode);
        SetWriteHandler8(Base + 4, Sh4SerialFifoPort.WriteBitRate);
        SetHandlers16(Base + 8, _ => (ushort)Control, (_, value) => Sh4SerialFifoPort.Instance.WriteControl(value));
        SetWriteOnly8(Base + 12, (_, value) => Sh4SerialFifoPort.Instance.WriteTransmitData(value));
        SetHandlers16(Base + 16, _ => Sh4SerialFifoPort.Instance.ReadStatus(), (_, value) => Sh4SerialFifoPort.Instance.WriteStatus(value));
        SetReadOnly8(Base + 20, _ => Sh4SerialFifoPort.Instance.ReadReceiveData());
        SetHandlers16(Base + 24, _ => (ushort)FifoControl, (_, value) => Sh4SerialFifoPort.Instance.WriteFifoControl(value));
        SetReadOnly16(Base + 28, _ => Sh4SerialFifoPort.Instance.ReadFifoCount());
        SetHandlers16(Base + 32, _ => Sh4SerialFifoPort.Instance.ReadPort(), (_, value) => Sh4SerialFifoPort.Instance.WritePort(value));
        SetReadWrite(Base + 36, 2, 1);
        Sh4SerialFifoPort.Instance.Initialize();
        Reset(hard: true);
    }

    internal void Reset(bool hard)
    {
        base.Reset();
        BitRate = 0xFF;
        Status = 0x60;
        if (hard)
            Sh4SerialFifoPort.Instance.SetPipe(null);
        Sh4SerialFifoPort.Instance.Reset();
    }

    internal override void Terminate()
    {
        try
        {
            Sh4SerialFifoPort.Instance.Terminate();
        }
        finally
        {
            base.Terminate();
        }
    }
}

internal sealed partial class Sh4SerialCommunicationRegisters
{
    private const uint Base = 0x1FE00000;
    internal static Sh4SerialCommunicationRegisters Instance { get; } = new();

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(Base, 1);
        SetReadWrite(Base + 4, 1);
        SetReadWrite(Base + 8, 1);
        SetReadWrite(Base + 12, 1);
        SetReadWrite(Base + 16, 1, 0xF9);
        SetReadOnly(Base + 20, 1);
        SetReadWrite(Base + 28, 1, 0x8F);
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
        SetValue(Base + 4, 0xFF);
        SetValue(Base + 12, 0xFF);
        SetValue(Base + 16, 0x84);
    }
}
