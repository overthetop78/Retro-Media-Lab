using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal class Sh4HardwareRegister
{
    private Func<uint, byte> _read8;
    private Action<uint, byte> _write8;
    private Func<uint, ushort> _read16;
    private Action<uint, ushort> _write16;
    private Func<uint, uint> _read32;
    private Action<uint, uint> _write32;

    internal Sh4HardwareRegister()
    {
        _read8 = InvalidRead<byte>;
        _read16 = InvalidRead<ushort>;
        _read32 = InvalidRead<uint>;
        _write8 = InvalidWrite<byte>;
        _write16 = InvalidWrite<ushort>;
        _write32 = InvalidWrite<uint>;
    }

    internal T Read<T>(uint address) where T : unmanaged, IBinaryInteger<T> =>
        T.CreateTruncating(Read(address, Unsafe.SizeOf<T>()));

    internal uint Read(uint address, int size)
    {
        switch (size)
        {
            case 1:
                return _read8(address);
            case 2:
                return _read16(address);
            case 4:
                return _read32(address);
            default:
                throw new ArgumentOutOfRangeException(nameof(size), "invalid type size");
        }
    }

    internal void Write<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T> =>
        Write(address, Unsafe.SizeOf<T>(), uint.CreateTruncating(data));

    internal void Write(uint address, int size, uint data)
    {
        switch (size)
        {
            case 1:
                _write8(address, unchecked((byte)data));
                break;
            case 2:
                _write16(address, unchecked((ushort)data));
                break;
            case 4:
                _write32(address, data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), "invalid type size");
        }
    }

    internal void SetReadHandler<T>(Func<uint, T> readHandler) where T : unmanaged, IBinaryInteger<T> =>
        SetReadHandler(Unsafe.SizeOf<T>(), address => uint.CreateTruncating(readHandler(address)));

    internal void SetReadHandler(int size, Func<uint, uint> readHandler)
    {
        switch (size)
        {
            case 1:
                _read8 = address => unchecked((byte)readHandler(address));
                break;
            case 2:
                _read16 = address => unchecked((ushort)readHandler(address));
                break;
            case 4:
                _read32 = readHandler;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), "invalid type size");
        }
    }

    internal void SetWriteHandler<T>(Action<uint, T> writeHandler) where T : unmanaged, IBinaryInteger<T> =>
        SetWriteHandler(Unsafe.SizeOf<T>(), (address, value) => writeHandler(address, T.CreateTruncating(value)));

    internal void SetWriteHandler(int size, Action<uint, uint> writeHandler)
    {
        switch (size)
        {
            case 1:
                _write8 = (address, value) => writeHandler(address, value);
                break;
            case 2:
                _write16 = (address, value) => writeHandler(address, value);
                break;
            case 4:
                _write32 = writeHandler;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), "invalid type size");
        }
    }

    private static T InvalidRead<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        Trace.TraceInformation($"Invalid register read<{Unsafe.SizeOf<T>()}> {address:x}");
        return T.Zero;
    }

    private static void InvalidWrite<T>(uint address, T value) where T : unmanaged, IBinaryInteger<T>
    {
        Trace.TraceInformation($"Invalid register write<{Unsafe.SizeOf<T>()}> {address:x} = {int.CreateTruncating(value):x}");
    }
}

internal sealed class Sh4ModuleRegister(uint[] module, uint addressMask, uint baseAddress) : Sh4HardwareRegister
{
    internal void SetReadWrite(uint address, int size, uint mask, uint orMask)
    {
        SetReadHandler(size, _ => ReadModule(address));
        SetWriteHandler(size, (_, data) => WriteModule(address, data, mask, orMask));
    }

    internal void SetReadOnly(uint address, int size) =>
        SetReadHandler(size, _ => ReadModule(address));

    internal void SetWriteOnly(uint address, int size, uint mask, uint orMask) =>
        SetWriteHandler(size, (_, data) => WriteModule(address, data, mask, orMask));

    private uint ReadModule(uint address) => module[(unchecked(address - baseAddress) & addressMask) / sizeof(uint)];

    private void WriteModule(uint address, uint data, uint mask, uint orMask) =>
        module[(unchecked(address - baseAddress) & addressMask) / sizeof(uint)] = (data & mask) | orMask;
}

internal class Sh4RegisterBank
{
    private readonly Sh4ModuleRegister[] _registers;
    private readonly uint _addressMask;
    private readonly uint _baseAddress;

    internal uint[] Registers { get; }

    protected Sh4RegisterBank(int size, uint addressMask = 0xff, uint baseAddress = 0)
    {
        uint lastOffset = unchecked((uint)(size - 1) * sizeof(uint));
        if ((lastOffset & addressMask) != lastOffset)
            throw new ArgumentException("Size too big for address mask", nameof(size));

        _addressMask = addressMask;
        _baseAddress = baseAddress;
        Registers = new uint[size];
        _registers = new Sh4ModuleRegister[size];
        for (int index = 0; index < _registers.Length; index++)
            _registers[index] = new Sh4ModuleRegister(Registers, _addressMask, _baseAddress);
    }

    internal virtual void Initialize()
    {
        for (int index = 0; index < _registers.Length; index++)
            _registers[index] = new Sh4ModuleRegister(Registers, _addressMask, _baseAddress);
    }

    internal virtual void Reset() => Array.Clear(Registers);

    internal virtual void Terminate()
    {
    }

    internal Sh4ModuleRegister GetRegister(uint address)
    {
        uint index = (unchecked(address - _baseAddress) & _addressMask) / sizeof(uint);
        if (index >= _registers.Length)
            throw new ArgumentOutOfRangeException(nameof(address), "Out of bound register");

        return _registers[index];
    }

    internal void SetReadWrite(uint address, int size, uint mask = uint.MaxValue, uint orMask = 0) =>
        GetRegister(address).SetReadWrite(address, size, mask, orMask);

    internal void SetHandlers<T>(uint address, Func<uint, T> readHandler, Action<uint, T> writeHandler)
        where T : unmanaged, IBinaryInteger<T>
    {
        Sh4ModuleRegister register = GetRegister(address);
        register.SetReadHandler(readHandler);
        register.SetWriteHandler(writeHandler);
    }

    internal void SetHandlers16(uint address, Func<uint, ushort> read, Action<uint, ushort> write) =>
        SetHandlers(address, read, write);

    internal void SetHandlers32(uint address, Func<uint, uint> read, Action<uint, uint> write) =>
        SetHandlers(address, read, write);

    internal void SetWriteHandler<T>(uint address, Action<uint, T> writeHandler) where T : unmanaged, IBinaryInteger<T>
    {
        Sh4ModuleRegister register = GetRegister(address);
        register.SetReadOnly(address, Unsafe.SizeOf<T>());
        register.SetWriteHandler(writeHandler);
    }

    internal void SetWriteHandler8(uint address, Action<uint, byte> write) => SetWriteHandler(address, write);

    internal void SetWriteHandler16(uint address, Action<uint, ushort> write) => SetWriteHandler(address, write);

    internal void SetWriteHandler32(uint address, Action<uint, uint> write) => SetWriteHandler(address, write);

    internal void SetWriteOnly<T>(uint address, Action<uint, T>? writeHandler = null,
        uint mask = uint.MaxValue, uint orMask = 0) where T : unmanaged, IBinaryInteger<T>
    {
        Sh4ModuleRegister register = GetRegister(address);
        if (writeHandler is not null)
            register.SetWriteHandler(writeHandler);
        else
            register.SetWriteOnly(address, Unsafe.SizeOf<T>(), mask, orMask);
    }

    internal void SetWriteOnly8(uint address, Action<uint, byte>? write = null, uint mask = uint.MaxValue, uint orMask = 0) =>
        SetWriteOnly(address, write, mask, orMask);

    internal void SetReadOnly<T>(uint address, Func<uint, T>? readHandler = null) where T : unmanaged, IBinaryInteger<T>
    {
        Sh4ModuleRegister register = GetRegister(address);
        if (readHandler is not null)
            register.SetReadHandler(readHandler);
        else
            register.SetReadOnly(address, Unsafe.SizeOf<T>());
    }

    internal void SetReadOnly(uint address, int size, Func<uint, uint>? read = null)
    {
        Sh4ModuleRegister register = GetRegister(address);
        if (read is not null)
            register.SetReadHandler(size, read);
        else
            register.SetReadOnly(address, size);
    }

    internal void SetReadOnly8(uint address, Func<uint, byte> read) => SetReadOnly(address, read);

    internal void SetReadOnly16(uint address, Func<uint, ushort> read) => SetReadOnly(address, read);

    internal void SetReadOnly32(uint address, Func<uint, uint> read) => SetReadOnly(address, read);

    internal uint GetRegisterIndex(uint address) => (unchecked(address - _baseAddress) & _addressMask) / sizeof(uint);

    protected uint GetValue(uint address) => Registers[GetRegisterIndex(address)];

    protected void SetValue(uint address, uint value) => Registers[GetRegisterIndex(address)] = value;

    internal T Read<T>(uint address) where T : unmanaged, IBinaryInteger<T> =>
        T.CreateTruncating(Read(address, Unsafe.SizeOf<T>()));

    internal uint Read(uint address, int size)
    {
        uint index = GetRegisterIndex(address);
        if (index >= _registers.Length)
        {
            Trace.TraceInformation($"Out of bound read @ {address:x}");
            return 0;
        }
        if ((address & 3) != 0)
        {
            Trace.TraceInformation($"Unaligned register read @ {address:x}");
            return 0;
        }

        return _registers[index].Read(address, size);
    }

    internal void Write<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T> =>
        Write(address, Unsafe.SizeOf<T>(), data);

    internal void Write<T>(uint address, int size, T data) where T : unmanaged, IBinaryInteger<T>
    {
        uint index = GetRegisterIndex(address);
        if (index >= _registers.Length)
            Trace.TraceInformation($"Out of bound write @ {address:x} = {int.CreateTruncating(data):x}");
        else if ((address & 3) != 0)
            Trace.TraceInformation($"Unaligned register write @ {address:x} = {int.CreateTruncating(data):x}");
        else
            _registers[index].Write(address, size, uint.CreateTruncating(data));
    }
}

internal static unsafe class Sh4HardwareRegisterMemory
{
    internal static T Read<T>(byte* array, uint address) where T : unmanaged =>
        *(T*)(array + (address & ~((uint)sizeof(T) - 1)));

    internal static void Write<T>(byte* array, uint address, T data) where T : unmanaged =>
        *(T*)(array + (address & ~((uint)sizeof(T) - 1))) = data;
}

internal class Sh4SerialPipe
{
    internal virtual void Write(byte data)
    {
    }

    internal virtual void SendBreak()
    {
    }

    internal virtual int Available() => 0;

    internal virtual byte Read() => 0;
}

internal abstract class Sh4SerialPort
{
    internal abstract void SetPipe(Sh4SerialPipe? pipe);

    internal abstract void UpdateStatus();

    internal virtual void ReceiveBreak() => Sh4HostRuntime.Die("unsupported");
}
