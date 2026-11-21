using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

/// <summary>Experimental Windows x64 emitter for flag-neutral, bus-free SH-2 register runs.</summary>
/// <remarks>ModRM/REX encodings follow PicoDrive cpu/drc/emit_x86.c, revision
/// 9f46ee4e9680d108e548fb427e7c7e1aa1bb9d5a. This is not a complete SH-2 backend.</remarks>
internal sealed class Sh2X64RegisterBlock : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void RunRegisters(nint registers);
    private nint _memory;
    private readonly RunRegisters _run;
    public byte[] Code { get; }

    private Sh2X64RegisterBlock(byte[] code)
    {
        Code = code;
        bool initialized = false;
        try
        {
            _memory = VirtualAlloc(0, (nuint)code.Length, 0x3000, 0x04);
            if (_memory == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            Marshal.Copy(code, 0, _memory, code.Length);
            if (!VirtualProtect(_memory, (nuint)code.Length, 0x20, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!FlushInstructionCache(GetCurrentProcess(), _memory, (nuint)code.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            _run = Marshal.GetDelegateForFunctionPointer<RunRegisters>(_memory);
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    /// <summary>The caller owns and pins at least sixteen uint registers for the entire call.</summary>
    public void Execute(nint registers)
    {
        ObjectDisposedException.ThrowIf(_memory == 0, this);
        if (registers == 0) throw new ArgumentNullException(nameof(registers));
        _run(registers);
    }

    public unsafe void Execute(uint[] registers)
    {
        if (registers.Length < 16) throw new ArgumentException("Sixteen SH-2 registers are required.", nameof(registers));
        fixed (uint* pointer = registers) Execute((nint)pointer);
    }

    public static bool Supports(ushort opcode) => opcode == 0x0009
        || (opcode & 0xf000) is 0xe000 or 0x7000
        || (opcode & 0xf00f) is 0x6003 or 0x300c or 0x3008 or 0x2009 or 0x200a or 0x200b;

    public static Sh2X64RegisterBlock? TryCompile(ReadOnlySpan<ushort> instructions)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64 || instructions.IsEmpty)
            return null;
        var guests = new List<int>();
        foreach (ushort opcode in instructions)
        {
            if (opcode == 0x0009) continue;
            if (!Supports(opcode)) return null;
            int n = opcode >> 8 & 15, m = opcode >> 4 & 15;
            int operation = opcode & 0xf00f;
            if ((opcode & 0xf000) is 0xe000 or 0x7000)
            { if (!guests.Contains(n)) guests.Add(n); }
            else
            {
                if (!guests.Contains(n)) guests.Add(n);
                if (!guests.Contains(m)) guests.Add(m);
            }
        }
        if (guests.Count > 6) return null;
        // RCX holds the register file; only volatile Windows x64 registers are used.
        int[] hosts = [0, 2, 8, 9, 10, 11];
        var bytes = new List<byte>();
        void Rex(int r, int b) { int rex = 0x40 | ((r >> 3) << 2) | (b >> 3); if (rex != 0x40) bytes.Add((byte)rex); }
        void RegisterOperation(byte operation, int source, int destination)
        {
            Rex(source, destination); bytes.Add(operation);
            bytes.Add((byte)(0xc0 | ((source & 7) << 3) | (destination & 7)));
        }
        void Immediate(int value) => bytes.AddRange(BitConverter.GetBytes(value));
        for (int i = 0; i < guests.Count; i++)
        {
            Rex(hosts[i], 1); bytes.Add(0x8b);
            bytes.Add((byte)(0x41 | ((hosts[i] & 7) << 3))); bytes.Add((byte)(guests[i] * 4));
        }
        foreach (ushort opcode in instructions)
        {
            if (opcode == 0x0009) continue;
            int destination = hosts[guests.IndexOf(opcode >> 8 & 15)];
            if ((opcode & 0xf000) == 0xe000)
            {
                Rex(0, destination); bytes.Add((byte)(0xb8 | (destination & 7)));
                Immediate(unchecked((sbyte)opcode));
            }
            else if ((opcode & 0xf000) == 0x7000)
            {
                Rex(0, destination); bytes.Add(0x81); bytes.Add((byte)(0xc0 | (destination & 7)));
                Immediate(unchecked((sbyte)opcode));
            }
            else
            {
                int source = hosts[guests.IndexOf(opcode >> 4 & 15)];
                byte operation = (opcode & 0xf00f) switch
                { 0x6003 => 0x89, 0x300c => 0x01, 0x3008 => 0x29, 0x2009 => 0x21, 0x200a => 0x31, 0x200b => 0x09, _ => throw new UnreachableException() };
                RegisterOperation(operation, source, destination);
            }
        }
        for (int i = 0; i < guests.Count; i++)
        {
            Rex(hosts[i], 1); bytes.Add(0x89);
            bytes.Add((byte)(0x41 | ((hosts[i] & 7) << 3))); bytes.Add((byte)(guests[i] * 4));
        }
        bytes.Add(0xc3);
        return new(bytes.ToArray());
    }

    public void Dispose()
    {
        nint memory = _memory;
        if (memory == 0) return;
        if (!VirtualFree(memory, 0, 0x8000)) throw new Win32Exception(Marshal.GetLastWin32Error());
        _memory = 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool VirtualProtect(nint address, nuint size, uint protection, out uint oldProtection);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool VirtualFree(nint address, nuint size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool FlushInstructionCache(nint process, nint address, nuint size);
    [DllImport("kernel32.dll")] private static extern nint GetCurrentProcess();
}
