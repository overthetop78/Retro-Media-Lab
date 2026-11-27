using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class AicaDsp
{
    private static readonly DspState[] StateStorage = GC.AllocateArray<DspState>(1, pinned: true);

    internal static ref DspState State => ref StateStorage[0];
    internal static nuint StateAddress => (nuint)Unsafe.AsPointer(ref State);
    internal static nuint DataAddress => (nuint)AicaMemory.DspData;

    internal static ushort Pack(int value)
    {
        int sign = (value >> 23) & 1;
        uint temporary = unchecked((uint)(value ^ (value << 1))) & 0xffffff;
        int exponent = 0;
        for (int k = 0; k < 12; k++)
        {
            if ((temporary & 0x800000) != 0)
                break;
            temporary <<= 1;
            exponent += 1;
        }
        if (exponent < 12)
            value <<= exponent;
        else
            value <<= 11;
        value = (value >> 11) & 0x7ff;
        value |= sign << 15;
        value |= exponent << 11;
        return (ushort)value;
    }

    internal static int Unpack(ushort value)
    {
        int sign = (value >> 15) & 1;
        int exponent = (value >> 11) & 0xf;
        int mantissa = value & 0x7ff;
        int unpacked = mantissa << 11;
        unpacked |= sign << 22;
        if (exponent > 11)
            exponent = 11;
        else
            unpacked ^= 1 << 22;
        unpacked |= sign << 23;
        unpacked <<= 8;
        unpacked >>= 8;
        unpacked >>= exponent;
        return unpacked;
    }

    internal static void DecodeInstruction(uint* instruction, out Instruction decoded)
    {
        decoded.TRA = (byte)((instruction[0] >> 9) & 0x7f);
        decoded.TWT = (instruction[0] & 0x100) != 0;
        decoded.TWA = (byte)((instruction[0] >> 1) & 0x7f);

        decoded.XSEL = (instruction[1] & 0x8000) != 0;
        decoded.YSEL = (byte)((instruction[1] >> 13) & 3);
        decoded.IRA = (byte)((instruction[1] >> 7) & 0x3f);
        decoded.IWT = (instruction[1] & 0x40) != 0;
        decoded.IWA = (byte)((instruction[1] >> 1) & 0x1f);

        decoded.TABLE = (instruction[2] & 0x8000) != 0;
        decoded.MWT = (instruction[2] & 0x4000) != 0;
        decoded.MRD = (instruction[2] & 0x2000) != 0;
        decoded.EWT = (instruction[2] & 0x1000) != 0;
        decoded.EWA = (byte)((instruction[2] >> 8) & 0xf);
        decoded.ADRL = (instruction[2] & 0x80) != 0;
        decoded.FRCL = (instruction[2] & 0x40) != 0;
        decoded.SHIFT = (byte)((instruction[2] >> 4) & 3);
        decoded.YRL = (instruction[2] & 8) != 0;
        decoded.NEGB = (instruction[2] & 4) != 0;
        decoded.ZERO = (instruction[2] & 2) != 0;
        decoded.BSEL = (instruction[2] & 1) != 0;

        decoded.NOFL = (instruction[3] & 0x8000) != 0;
        decoded.MASA = (byte)((instruction[3] >> 9) & 0x3f);
        decoded.ADREB = (instruction[3] & 0x100) != 0;
        decoded.NXADR = (instruction[3] & 0x80) != 0;
    }

#if !AICA_DSP_DYNAREC_NONE && !ARM7_DYNAREC_NONE
    internal static nuint PackEntry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<int, ushort>)&PackX86
        : (nuint)(delegate* unmanaged<int, ushort>)&PackNative;

    internal static nuint UnpackEntry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<ushort, int>)&UnpackX86
        : (nuint)(delegate* unmanaged<ushort, int>)&UnpackNative;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort PackX86(int value) => Pack(value);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static int UnpackX86(ushort value) => Unpack(value);

    [UnmanagedCallersOnly]
    private static ushort PackNative(int value) => Pack(value);

    [UnmanagedCallersOnly]
    private static int UnpackNative(ushort value) => Unpack(value);

    private static void RecInit()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            InitializeX86();
        else
            InitializeX64();
    }

    private static void RecTerm()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            TerminateX86();
        else
            TerminateX64();
    }

    private static void Recompile()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            RecompileX86();
        else
            RecompileX64();
    }

    private static void RunStep()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            RunStepX86();
        else
            RunStepX64();
    }
#else
    private static void RecInit()
    {
    }

    private static void RecTerm()
    {
    }

    private static void Recompile()
    {
    }
#endif

    internal static void Init()
    {
        State = default;
        State.RBL = 0x8000 - 1;
        State.RBP = 0;
        State.MDEC_CT = 1;
        State.Dirty = true;
        RecInit();
    }

    internal static void WriteProg(uint address)
    {
        if (address >= 0x3400 && address < 0x3c00)
            State.Dirty = true;
    }

    internal static void Term()
    {
        State.Stopped = true;
        RecTerm();
    }

    internal static void Step()
    {
        if (State.Dirty)
        {
            State.Dirty = false;
            State.Stopped = true;
            uint* program = AicaMemory.DspData->MPRO;
            for (int index = 0; index < 128 * 4; index++)
            {
                if (program[index] != 0)
                {
                    State.Stopped = false;
                    break;
                }
            }
            if (!State.Stopped)
                Recompile();
        }
        if (State.Stopped)
            return;
        RunStep();
    }
}
