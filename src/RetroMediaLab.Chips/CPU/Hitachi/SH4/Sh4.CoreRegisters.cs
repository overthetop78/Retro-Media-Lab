using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Context
{
#if !SH4_DYNAREC_NONE
    internal static unsafe nuint UpdateSrEntry =>
        (nuint)(delegate* unmanaged[Cdecl]<byte>)&NativeUpdateStatusRegister;

    internal static unsafe nuint UpdateFpscrEntry =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, void>)&NativeUpdateFpscrX86
            : (nuint)(delegate* unmanaged<nint, void>)&NativeUpdateFpscr;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte NativeUpdateStatusRegister() => Active.UpdateStatusRegister() ? (byte)1 : (byte)0;

    [UnmanagedCallersOnly]
    private static void NativeUpdateFpscr(nint contextAddress) => FromNative(contextAddress).UpdateFpscr();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void NativeUpdateFpscrX86(nint contextAddress) => FromNative(contextAddress).UpdateFpscr();
#endif

    internal bool UpdateStatusRegister()
    {
        if (StatusRegister.MD)
        {
            if (OldStatusRegister.Rb != StatusRegister.Rb)
                SwapGeneralRegisterBank();
        }
        else if (OldStatusRegister.Rb)
        {
            SwapGeneralRegisterBank();
        }

        OldStatusRegister.Status = StatusRegister.Status;
        OldStatusRegister.Rb &= StatusRegister.MD;
        return Interrupts.DecodeStatusRegister();
    }

    private void SwapGeneralRegisterBank()
    {
        Span<uint> registers = Registers;
        Span<uint> banked = BankedRegisters;
        for (int index = 0; index < 8; index++)
            (registers[index], banked[index]) = (banked[index], registers[index]);
    }

    internal void UpdateFpscr()
    {
        Fpscr.Full &= ~0x3F000u;
        if (Fpscr.Fr != OldFpscr.Fr)
        {
            Span<uint> registers = MemoryMarshal.Cast<float, uint>(FloatingRegisters);
            Span<uint> extended = MemoryMarshal.Cast<float, uint>(ExtendedFloatingRegisters);
            for (int index = 0; index < 16; index++)
                (registers[index], extended[index]) = (extended[index], registers[index]);
        }

        OldFpscr = Fpscr;
        SetHostRoundingMode(Fpscr.RoundingMode, Fpscr.Dn ? 1u : 0u);
    }

    internal void RestoreHostRoundingMode()
    {
        _hostRoundingMode = 0xFF;
        _hostFlushDenormals = 0xFF;
        SetHostRoundingMode(Fpscr.RoundingMode, Fpscr.Dn ? 1u : 0u);
    }

    internal static void SetDefaultRoundingMode() => SetHostRoundingMode(0, 0);

    private static uint _hostRoundingMode = 0xFF;
    private static uint _hostFlushDenormals = 0xFF;

    private static void SetHostRoundingMode(uint roundingMode, uint flushDenormals)
    {
        if (_hostRoundingMode == roundingMode && _hostFlushDenormals == flushDenormals)
            return;

        _hostRoundingMode = roundingMode;
        _hostFlushDenormals = flushDenormals;

#if !SH4_MINGW64
        if (OperatingSystem.IsWindows())
        {
            const uint roundingMask = 0x00000300;
            const uint roundChop = 0x00000300;
            const uint denormalMask = 0x03000000;
            const uint denormalFlush = 0x01000000;
            ControlFloatingPoint(roundingMode == 1 ? roundChop : 0, roundingMask);
            ControlFloatingPoint(flushDenormals == 1 ? denormalFlush : 0, denormalMask);
            return;
        }
#endif

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
                uint mxcsr = 0x1F80;
                if (roundingMode == 1)
                    mxcsr |= 3u << 13;
                if (flushDenormals == 1)
                    mxcsr |= 1u << 15;
                Sh4VirtualMemory.LoadPosixMxcsr(mxcsr);
                break;

            case Architecture.Arm:
                const uint armOffMask = 0x04086060;
                uint armOnMask = 0x02000000;
                if (roundingMode == 1)
                    armOnMask |= 3u << 22;
                if (flushDenormals == 1)
                    armOnMask |= 1u << 24;

                if (OperatingSystem.IsAndroid())
                {
                    GetFloatingEnvironment(out uint environment);
                    environment &= armOffMask;
                    environment |= armOnMask;
                    SetFloatingEnvironment(in environment);
                }
                else
                {
                    uint fpscr = Sh4VirtualMemory.ReadPosixFpscr();
                    fpscr &= armOffMask;
                    fpscr |= armOnMask;
                    Sh4VirtualMemory.WritePosixFpscr(fpscr);
                }
                break;

            case Architecture.Arm64:
                const ulong arm64OffMask = 0x04080000;
                ulong arm64OnMask = 0x02000000;
                if (roundingMode == 1)
                    arm64OnMask |= 3ul << 22;
                if (flushDenormals == 1)
                    arm64OnMask |= 1ul << 24;

                ulong fpcr = Sh4VirtualMemory.ReadPosixFpcr();
                fpcr &= arm64OffMask;
                fpcr |= arm64OnMask;
                Sh4VirtualMemory.WritePosixFpcr(fpcr);
                break;

            default:
                throw new PlatformNotSupportedException("SetFloatStatusReg: Unsupported platform");
        }
    }

    [DllImport("ucrtbase.dll", EntryPoint = "_controlfp", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint ControlFloatingPoint(uint value, uint mask);

    [DllImport("libm.so", EntryPoint = "fegetenv", ExactSpelling = true)]
    private static extern int GetFloatingEnvironment(out uint environment);

    [DllImport("libm.so", EntryPoint = "fesetenv", ExactSpelling = true)]
    private static extern int SetFloatingEnvironment(in uint environment);
}
