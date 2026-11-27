using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal unsafe partial class Sh4UnwindInfo
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DynamicUnwindSections
    {
        internal nuint DsoBase;
        internal nuint DwarfSection;
        internal nuint DwarfSectionLength;
        internal nuint CompactUnwindSection;
        internal nuint CompactUnwindSectionLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MachHeader64
    {
        internal uint Magic;
        internal int CpuType;
        internal int CpuSubtype;
        internal uint FileType;
        internal uint CommandCount;
        internal uint CommandSize;
        internal uint Flags;
        internal uint Reserved;
    }

    private static class FakeMachHeader
    {
        internal static readonly MachHeader64[] Value = Initialize();

        private static MachHeader64[] Initialize()
        {
            var header = GC.AllocateArray<MachHeader64>(1, pinned: true);
            header[0] = new MachHeader64
            {
                Magic = 0xfeedfacf,
                CpuType = 0x0100000c,
                CpuSubtype = 0,
                FileType = 6,
                CommandCount = 0,
                CommandSize = 0,
                Flags = 0,
                Reserved = 0
            };
            return header;
        }
    }

    [UnmanagedCallersOnly]
    private static int FindDynamicUnwindSections(nuint address, DynamicUnwindSections* information)
    {
        fixed (MachHeader64* header = FakeMachHeader.Value)
            information->DsoBase = (nuint)header;
        information->DwarfSection = 0;
        information->DwarfSectionLength = 0;
        information->CompactUnwindSection = 0;
        information->CompactUnwindSectionLength = 0;
        return 1;
    }

    [ModuleInitializer]
    internal static void RegisterUnwindShim()
    {
        if (!IsAppleHost || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            return;

        _ = FakeMachHeader.Value;

        nuint function = LookupDarwinSymbol(unchecked((nint)(-2)), "__unw_add_find_dynamic_unwind_sections");
        if (function != 0)
        {
            var register = (delegate* unmanaged<delegate* unmanaged<nuint, DynamicUnwindSections*, int>, int>)function;
            register(&FindDynamicUnwindSections);
        }
    }

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlsym")]
    private static extern nuint LookupDarwinSymbol(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    private static class FrameRegistration
    {
        private static readonly nint Program = NativeLibrary.GetMainProgramHandle();

        internal static readonly delegate* unmanaged<nuint, void> Register =
            (delegate* unmanaged<nuint, void>)NativeLibrary.GetExport(Program, "__register_frame");

        internal static readonly delegate* unmanaged<nuint, void> Deregister =
            (delegate* unmanaged<nuint, void>)NativeLibrary.GetExport(Program, "__deregister_frame");
    }

    private static bool IsAppleHost =>
        OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst();

    private static ReadOnlySpan<int> DwarfX64Registers => [0, 2, 1, 3, 7, 6, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15];
    private const int DwarfX64XmmRegister = 17;

    private static int RegisterId(int register) =>
        RuntimeInformation.ProcessArchitecture == Architecture.X64 ? DwarfX64Registers[register] : register;

    private static int ReturnAddressRegister => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => 16,
        Architecture.X86 => 8,
        Architecture.Arm64 => 30,
        Architecture.Arm => 14,
        _ => throw new PlatformNotSupportedException()
    };

    private static int StackPointerRegister => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => DwarfX64Registers[4],
        Architecture.X86 => 4,
        Architecture.Arm64 => 31,
        Architecture.Arm => 13,
        _ => throw new PlatformNotSupportedException()
    };

    private static void WriteLength(List<byte> stream, uint position, uint value)
    {
        Unsafe.WriteUnaligned(ref CollectionsMarshal.AsSpan(stream)[(int)position], value);
    }

    private static void Write(List<byte> stream, ulong value, int size)
    {
        for (int i = 0; i < size; i++)
            stream.Add(unchecked((byte)(value >> (i * 8))));
    }

    private static void WriteUnsignedLeb128(List<byte> stream, uint value)
    {
        while (true)
        {
            if (value < 128)
            {
                Write(stream, value, sizeof(byte));
                break;
            }

            Write(stream, (value & 0x7f) | 0x80, sizeof(byte));
            value >>= 7;
        }
    }

    private static void WriteSignedLeb128(List<byte> stream, int value)
    {
        bool more;
        do
        {
            byte current = (byte)(value & 0x7f);
            value >>= 7;
            more = !((value == 0 && (current & 0x40) == 0) || (value == -1 && (current & 0x40) != 0));
            if (more)
                current |= 0x80;
            Write(stream, current, sizeof(byte));
        }
        while (more);
    }

    private static void WritePadding(List<byte> stream)
    {
        int padding = stream.Count % IntPtr.Size;
        if (padding != 0)
        {
            padding = IntPtr.Size - padding;
            for (int i = 0; i < padding; i++)
                Write(stream, 0, sizeof(byte));
        }
    }

    private static void WriteCie(List<byte> stream, List<byte> instructions, byte returnRegister)
    {
        uint lengthPosition = (uint)stream.Count;
        Write(stream, 0, sizeof(uint));
        Write(stream, 0, sizeof(uint));

        Write(stream, 1, sizeof(byte));
        Write(stream, 'z', sizeof(byte));
        Write(stream, 'R', sizeof(byte));
        Write(stream, 0, sizeof(byte));
        WriteUnsignedLeb128(stream, 1);
        WriteSignedLeb128(stream, -1);
        WriteUnsignedLeb128(stream, returnRegister);

        WriteUnsignedLeb128(stream, 1);
        Write(stream, 0, sizeof(byte));

        stream.AddRange(instructions);
        WritePadding(stream);
        WriteLength(stream, lengthPosition, (uint)stream.Count - lengthPosition - 4);
    }

    private static void WriteFde(List<byte> stream, List<byte> instructions, uint cieLocation, out uint functionStart)
    {
        uint lengthPosition = (uint)stream.Count;
        Write(stream, 0, sizeof(uint));
        uint cieOffset = (uint)stream.Count - cieLocation;
        Write(stream, cieOffset, sizeof(uint));

        functionStart = (uint)stream.Count;
        Write(stream, 0, IntPtr.Size);
        Write(stream, 0, IntPtr.Size);

        WriteUnsignedLeb128(stream, 0);

        stream.AddRange(instructions);
        WritePadding(stream);
        WriteLength(stream, lengthPosition, (uint)stream.Count - lengthPosition - 4);
    }

    private static void WriteAdvanceLocation(List<byte> instructions, nuint offset, ref nuint lastOffset)
    {
        nuint delta = unchecked(offset - lastOffset);
        if (delta == 0)
            return;

        if (delta < 1 << 6)
        {
            Write(instructions, (1u << 6) | (ulong)delta, sizeof(byte));
        }
        else if (delta < 1 << 8)
        {
            Write(instructions, 2, sizeof(byte));
            Write(instructions, delta, sizeof(byte));
        }
        else if (delta < 1 << 16)
        {
            Write(instructions, 3, sizeof(byte));
            Write(instructions, delta, sizeof(ushort));
        }
        else
        {
            Write(instructions, 4, sizeof(byte));
            Write(instructions, delta, sizeof(uint));
        }

        lastOffset = offset;
    }

    private static void WriteDefineCfa(List<byte> instructions, int register, int stackOffset)
    {
        Write(instructions, 0x0c, sizeof(byte));
        WriteUnsignedLeb128(instructions, unchecked((uint)register));
        WriteUnsignedLeb128(instructions, unchecked((uint)stackOffset));
    }

    private static void WriteDefineStackOffset(List<byte> instructions, int stackOffset)
    {
        Write(instructions, 0x0e, sizeof(byte));
        WriteUnsignedLeb128(instructions, unchecked((uint)stackOffset));
    }

    private static void WriteRegisterStackLocation(List<byte> instructions, int register, int stackLocation)
    {
        Write(instructions, unchecked((byte)((2 << 6) | register)), sizeof(byte));
        WriteUnsignedLeb128(instructions, unchecked((uint)stackLocation));
    }

    private static void WriteExtendedRegisterStackLocation(List<byte> instructions, int register, int stackLocation)
    {
        Write(instructions, 5, sizeof(byte));
        WriteUnsignedLeb128(instructions, unchecked((uint)register));
        WriteUnsignedLeb128(instructions, unchecked((uint)stackLocation));
    }

    private partial void StartPosix(nuint address)
    {
        _startAddress = address;
        _stackOffset = RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86 ? IntPtr.Size : 0;
        _lastOffset = 0;
        _cieInstructions.Clear();
        _fdeInstructions.Clear();
        WriteDefineCfa(_cieInstructions, StackPointerRegister, _stackOffset);
        if (_stackOffset > 0)
            WriteRegisterStackLocation(_cieInstructions, ReturnAddressRegister, _stackOffset);
    }

    private partial void PushRegisterPosix(uint offset, int register)
    {
        _stackOffset += IntPtr.Size;
        WriteAdvanceLocation(_fdeInstructions, offset, ref _lastOffset);
        WriteDefineStackOffset(_fdeInstructions, _stackOffset);
        WriteRegisterStackLocation(_fdeInstructions, RegisterId(register), _stackOffset);
    }

    private partial void SaveRegisterPosix(uint offset, int register, int stackOffset)
    {
        WriteAdvanceLocation(_fdeInstructions, offset, ref _lastOffset);
        WriteRegisterStackLocation(_fdeInstructions, RegisterId(register), stackOffset);
    }

    private partial void SaveExtendedRegisterPosix(uint offset, int register, int stackOffset)
    {
        WriteAdvanceLocation(_fdeInstructions, offset, ref _lastOffset);
        WriteExtendedRegisterStackLocation(_fdeInstructions, RegisterId(register), stackOffset);
    }

    private partial void AllocateStackPosix(uint offset, int size)
    {
        _stackOffset += size;
        WriteAdvanceLocation(_fdeInstructions, offset, ref _lastOffset);
        WriteDefineStackOffset(_fdeInstructions, _stackOffset);
    }

    private partial void EndPrologPosix(uint offset)
    {
    }

    private partial nuint EndPosix(uint offset, nint writableToExecutableOffset)
    {
        List<byte> unwindInfo = [];
        WriteCie(unwindInfo, _cieInstructions, (byte)ReturnAddressRegister);
        WriteFde(unwindInfo, _fdeInstructions, 0, out uint functionStart);
        Write(unwindInfo, 0, sizeof(uint));

        nuint endAddress = _startAddress + offset;
        if ((endAddress & 0xf) != 0)
            offset += 16 - (uint)(endAddress & 0xf);
        nuint destination = _startAddress + offset;
        CollectionsMarshal.AsSpan(unwindInfo).CopyTo(new Span<byte>((void*)destination, unwindInfo.Count));

        if (unwindInfo.Count != 0)
        {
            nuint function = destination + functionStart;
            Unsafe.WriteUnaligned((void*)function, unchecked(_startAddress + (nuint)writableToExecutableOffset));
            Unsafe.WriteUnaligned((void*)(function + (nuint)IntPtr.Size), unchecked((nint)(endAddress - _startAddress)));

#if __llvm__ && !SH4_ANDROID_NDK_BEFORE_23
            bool registerEachFde = true;
#elif __llvm__
            bool registerEachFde = RuntimeInformation.ProcessArchitecture == Architecture.Arm;
#else
            bool registerEachFde = false;
#endif
            if (registerEachFde)
            {
                nuint entry = destination;
                while (true)
                {
                    uint length = Unsafe.ReadUnaligned<uint>((void*)entry);
                    if (length == 0)
                        break;

                    if (length == uint.MaxValue)
                    {
                        ulong length64 = Unsafe.ReadUnaligned<ulong>((void*)(entry + 4));
                        if (length64 == 0)
                            break;

                        ulong entryOffset = Unsafe.ReadUnaligned<ulong>((void*)(entry + 12));
                        if (entryOffset != 0)
                        {
                            RegisterFrame(entry);
                            _registeredFrames.Add(entry);
                        }

                        entry += (nuint)(length64 + 12);
                    }
                    else
                    {
                        uint entryOffset = Unsafe.ReadUnaligned<uint>((void*)(entry + 4));
                        if (entryOffset != 0)
                        {
                            RegisterFrame(entry);
                            _registeredFrames.Add(entry);
                        }

                        entry += length + 4;
                    }
                }
            }
            else
            {
                RegisterFrame(destination);
                _registeredFrames.Add(destination);
            }
        }

        Debug.WriteLine($"RegisterFrame 0x{_startAddress:x} sz {(uint)(endAddress - _startAddress)} tables: {_registeredFrames.Count}");
        return destination + (nuint)unwindInfo.Count - endAddress;
    }

    private partial void ClearPosix()
    {
        Debug.WriteLine("UnwindInfo::clear");
        foreach (nuint frame in _registeredFrames)
            DeregisterFrame(frame);
        _registeredFrames.Clear();
    }

    private partial void RegisterFramePosix(nuint frame)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            return;
        FrameRegistration.Register(frame);
    }

    private partial void DeregisterFramePosix(nuint frame)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            return;
        FrameRegistration.Deregister(frame);
    }

#if SH4_HOST_ARM
    internal static bool FindDynarecFde(nuint targetAddress, ref nuint fde)
    {
        return Sh4ArmUnwindInfo.FindFde(targetAddress, ref fde);
    }
#endif
}
