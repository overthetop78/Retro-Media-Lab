using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal partial class Sh4UnwindInfo
{
    internal void Start(nuint address)
    {
        if (UseWindowsUnwind)
            StartWindows(address);
        else if (UsePosixUnwind)
            StartPosix(address);
    }

    internal void PushRegister(uint offset, int register)
    {
        if (UseWindowsUnwind)
            PushRegisterWindows(offset, register);
        else if (UsePosixUnwind)
            PushRegisterPosix(offset, register);
    }

    internal void SaveRegister(uint offset, int register, int stackOffset)
    {
        if (UsePosixUnwind)
            SaveRegisterPosix(offset, register, stackOffset);
    }

    internal void SaveExtendedRegister(uint offset, int register, int stackOffset)
    {
        if (UsePosixUnwind)
            SaveExtendedRegisterPosix(offset, register, stackOffset);
    }

    internal void AllocateStack(uint offset, int size)
    {
        if (UseWindowsUnwind)
            AllocateStackWindows(offset, size);
        else if (UsePosixUnwind)
            AllocateStackPosix(offset, size);
    }

    internal void EndProlog(uint offset)
    {
        if (UseWindowsUnwind)
            EndPrologWindows(offset);
        else if (UsePosixUnwind)
            EndPrologPosix(offset);
    }

    internal nuint End(uint offset, nint writableToExecutableOffset = 0)
    {
        if (UseWindowsUnwind)
            return EndWindows(offset, writableToExecutableOffset);
        if (UsePosixUnwind)
            return EndPosix(offset, writableToExecutableOffset);
        return 0;
    }

    internal void Clear()
    {
        if (UseWindowsUnwind)
            ClearWindows();
        else if (UsePosixUnwind)
            ClearPosix();
    }

    internal void AllocateStackAt(nuint address, int size)
    {
        AllocateStack(unchecked((uint)(address - _startAddress)), size);
    }

    protected virtual void RegisterFrame(nuint frame)
    {
        if (UsePosixUnwind)
            RegisterFramePosix(frame);
    }

    protected virtual void DeregisterFrame(nuint frame)
    {
        if (UsePosixUnwind)
            DeregisterFramePosix(frame);
    }

    private readonly List<nuint> _tables = [];
    private readonly List<ushort> _codes = [];
    private nuint _startAddress;
    private int _stackOffset;
    private nuint _lastOffset;
    private readonly List<byte> _cieInstructions = [];
    private readonly List<byte> _fdeInstructions = [];
    private readonly List<nuint> _registeredFrames = [];

    private static bool UseWindowsUnwind =>
        OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;

    private static bool UsePosixUnwind =>
        !OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 or Architecture.X86 or Architecture.Arm64 => true,
            Architecture.Arm => OperatingSystem.IsAndroid(),
            _ => false
        };

    private partial void StartWindows(nuint address);
    private partial void PushRegisterWindows(uint offset, int register);
    private partial void AllocateStackWindows(uint offset, int size);
    private partial void EndPrologWindows(uint offset);
    private partial nuint EndWindows(uint offset, nint writableToExecutableOffset);
    private partial void ClearWindows();

    private partial void StartPosix(nuint address);
    private partial void PushRegisterPosix(uint offset, int register);
    private partial void SaveRegisterPosix(uint offset, int register, int stackOffset);
    private partial void SaveExtendedRegisterPosix(uint offset, int register, int stackOffset);
    private partial void AllocateStackPosix(uint offset, int size);
    private partial void EndPrologPosix(uint offset);
    private partial nuint EndPosix(uint offset, nint writableToExecutableOffset);
    private partial void ClearPosix();
    private partial void RegisterFramePosix(nuint frame);
    private partial void DeregisterFramePosix(nuint frame);
}
