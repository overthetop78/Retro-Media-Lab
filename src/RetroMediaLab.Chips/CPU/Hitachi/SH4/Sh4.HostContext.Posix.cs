using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe class Sh4PosixHostContext
{
    private static bool IsUnix =>
        OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() || OperatingSystem.IsFreeBSD()
        || OperatingSystem.IsOSPlatform("NETBSD") || OperatingSystem.IsOSPlatform("OPENBSD")
        || OperatingSystem.IsOSPlatform("DRAGONFLY") || OperatingSystem.IsOSPlatform("HAIKU");

    private static void CopyRegister<THost, TSignal>(ref THost host, ref TSignal signal, bool toSignal)
        where THost : unmanaged
        where TSignal : unmanaged
    {
        if (sizeof(THost) != sizeof(TSignal))
            throw new InvalidOperationException("Invalid assignment");

        if (toSignal)
            signal = Unsafe.As<THost, TSignal>(ref host);
        else
            host = Unsafe.As<TSignal, THost>(ref signal);
    }

    private static void CopyContext(ref Sh4HostContext host, void* signalContext, bool toSignal)
    {
        bool apple = OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()
            || OperatingSystem.IsMacCatalyst();
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.Arm:
                CopyArmContext(ref host, signalContext, toSignal, apple);
                break;
            case Architecture.Arm64:
                CopyArm64Context(ref host, signalContext, toSignal, apple);
                break;
            case Architecture.X86:
                CopyX86Context(ref host, signalContext, toSignal, apple);
                break;
            case Architecture.X64:
                CopyX64Context(ref host, signalContext, toSignal, apple);
                break;
            default:
                throw new PlatformNotSupportedException("Unsupported HOST_CPU");
        }
    }

    private static void CopyArmContext(ref Sh4HostContext host, void* signalContext, bool toSignal, bool apple)
    {
        if (OperatingSystem.IsFreeBSD())
        {
            var context = (Sh4FreeBsdArmUserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters.ProgramCounter, toSignal);
            for (int i = 0; i < 15; i++)
                CopyRegister(ref host.Arm32Registers[i], ref context->MachineContext.GeneralRegisters[i], toSignal);
        }
        else if (IsUnix)
        {
            var context = (Sh4UnixArmUserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.ArmPc, toSignal);
            for (int i = 0; i < 15; i++)
                CopyRegister(ref host.Arm32Registers[i], ref context->MachineContext.ArmRegisters[i], toSignal);
        }
        else if (apple)
        {
            var context = (Sh4DarwinArmUserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext->ThreadState.Pc, toSignal);
            for (int i = 0; i < 15; i++)
                CopyRegister(ref host.Arm32Registers[i], ref context->MachineContext->ThreadState.Registers[i], toSignal);
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }

    private static void CopyArm64Context(ref Sh4HostContext host, void* signalContext, bool toSignal, bool apple)
    {
        if (apple)
        {
            var context = (Sh4DarwinArm64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext->ThreadState.Pc, toSignal);
            CopyRegister(ref host.X0, ref context->MachineContext->ThreadState.Registers[0], toSignal);
        }
        else if (OperatingSystem.IsFreeBSD())
        {
            var context = (Sh4FreeBsdArm64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters.Elr, toSignal);
            CopyRegister(ref host.X0, ref context->MachineContext.GeneralRegisters.Registers[0], toSignal);
        }
        else if (OperatingSystem.IsOSPlatform("NETBSD"))
        {
            var context = (Sh4NetBsdArm64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdArm64Register.Elr], toSignal);
            CopyRegister(ref host.X0, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdArm64Register.X0], toSignal);
        }
        else if (OperatingSystem.IsOSPlatform("OPENBSD"))
        {
            var context = (Sh4OpenBsdArm64SignalContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->Elr, toSignal);
            CopyRegister(ref host.X0, ref context->Registers[0], toSignal);
        }
        else
        {
            var context = (Sh4UnixArm64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.Pc, toSignal);
            CopyRegister(ref host.X0, ref context->MachineContext.Registers[0], toSignal);
        }
    }

    private static void CopyX86Context(ref Sh4HostContext host, void* signalContext, bool toSignal, bool apple)
    {
        if (OperatingSystem.IsFreeBSD())
        {
            var context = (Sh4FreeBsdX86UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.Eip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.Esp, toSignal);
            CopyRegister(ref host.Eax, ref context->MachineContext.Eax, toSignal);
            CopyRegister(ref host.Ecx, ref context->MachineContext.Ecx, toSignal);
        }
        else if (IsUnix)
        {
            var context = (Sh4UnixX86UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX86Register.Eip], toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX86Register.Esp], toSignal);
            CopyRegister(ref host.Eax, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX86Register.Eax], toSignal);
            CopyRegister(ref host.Ecx, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX86Register.Ecx], toSignal);
        }
        else if (apple)
        {
            var context = (Sh4DarwinX86UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext->ThreadState.Eip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext->ThreadState.Esp, toSignal);
            CopyRegister(ref host.Eax, ref context->MachineContext->ThreadState.Eax, toSignal);
            CopyRegister(ref host.Ecx, ref context->MachineContext->ThreadState.Ecx, toSignal);
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }

    private static void CopyX64Context(ref Sh4HostContext host, void* signalContext, bool toSignal, bool apple)
    {
        if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOSPlatform("DRAGONFLY"))
        {
            var context = (Sh4FreeBsdX64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.Rip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.Rsp, toSignal);
            CopyRegister(ref host.R9, ref context->MachineContext.R9, toSignal);
            CopyRegister(ref host.Rdi, ref context->MachineContext.Rdi, toSignal);
        }
        else if (OperatingSystem.IsOSPlatform("OPENBSD"))
        {
            var context = (Sh4OpenBsdX64SignalContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->Rip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->Rsp, toSignal);
            CopyRegister(ref host.R9, ref context->R9, toSignal);
            CopyRegister(ref host.Rdi, ref context->Rdi, toSignal);
        }
        else if (OperatingSystem.IsOSPlatform("NETBSD"))
        {
            var context = (Sh4NetBsdX64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdX64Register.Rip], toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdX64Register.Rsp], toSignal);
            CopyRegister(ref host.R9, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdX64Register.R9], toSignal);
            CopyRegister(ref host.Rdi, ref context->MachineContext.GeneralRegisters[(int)Sh4NetBsdX64Register.Rdi], toSignal);
        }
        else if (OperatingSystem.IsOSPlatform("HAIKU"))
        {
            var context = (Sh4HaikuX64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.Rip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.Rsp, toSignal);
            CopyRegister(ref host.R9, ref context->MachineContext.R9, toSignal);
            CopyRegister(ref host.Rdi, ref context->MachineContext.Rdi, toSignal);
        }
        else if (IsUnix)
        {
            var context = (Sh4UnixX64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX64Register.Rip], toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX64Register.Rsp], toSignal);
            CopyRegister(ref host.R9, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX64Register.R9], toSignal);
            CopyRegister(ref host.Rdi, ref context->MachineContext.GeneralRegisters[(int)Sh4UnixX64Register.Rdi], toSignal);
        }
        else if (apple)
        {
            var context = (Sh4DarwinX64UserContext*)signalContext;
            CopyRegister(ref host.ProgramCounter, ref context->MachineContext->ThreadState.Rip, toSignal);
            CopyRegister(ref host.StackPointer, ref context->MachineContext->ThreadState.Rsp, toSignal);
            CopyRegister(ref host.R9, ref context->MachineContext->ThreadState.R9, toSignal);
            CopyRegister(ref host.Rdi, ref context->MachineContext->ThreadState.Rdi, toSignal);
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }

    internal static void FromSegfault(ref Sh4HostContext host, void* signalContext)
    {
        CopyContext(ref host, signalContext, toSignal: false);
    }

    internal static void ToSegfault(ref Sh4HostContext host, void* signalContext)
    {
        CopyContext(ref host, signalContext, toSignal: true);
    }
}
