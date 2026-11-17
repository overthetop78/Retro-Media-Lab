using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private bool _useLiveX64Context;
    private int[]? _liveContextOffsets;
    private nint _activeLiveState;
    // Translation bookkeeping is not architectural state and is not serialized.
    private uint _liveError = 0, _liveFetchWait = 0, _liveEntry = 0, _liveChanged = 0, _liveExecuted = 0, _liveHops = 0;
    private uint _liveReserved0 = 0, _liveReserved1 = 0, _liveReserved2 = 0;

    /// <summary>Select outside Execute. Native payloads borrow pinned CPU fields, as the Z80 backend does.</summary>
    public bool UseLiveX64Context
    {
        get => _useLiveX64Context;
        set
        {
            if (value == _useLiveX64Context) return;
            if (value && (!RuntimeFeature.IsDynamicCodeSupported || !OperatingSystem.IsWindows()
                || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64))
                throw new PlatformNotSupportedException("The live SH-2 context requires Windows x64 dynamic code.");
            if (value) _liveContextOffsets ??= CreateLiveContextOffsets();
            ClearCodeCache(); _useLiveX64Context = value;
        }
    }

    private int[] CreateLiveContextOffsets()
    {
        string[] names = [nameof(SR), nameof(GBR), nameof(VBR), nameof(PR), nameof(MAC), nameof(MAC),
            nameof(PC), nameof(PPC), nameof(ID), nameof(_branch), "<PollingEpoch>k__BackingField",
            "<Cycles>k__BackingField", "<Cycles>k__BackingField", nameof(_executionEnd), nameof(_executionEnd),
            nameof(_busDeadline), nameof(_busDeadline), nameof(_pendingBusClocks), "<Sleeping>k__BackingField",
            nameof(_yield), nameof(_irqLevel), nameof(_irqVector), nameof(ET), nameof(_liveError), nameof(_liveFetchWait),
            nameof(_liveEntry), nameof(_liveChanged), nameof(_liveExecuted), nameof(_liveHops),
            nameof(_liveReserved0), nameof(_liveReserved1), nameof(_liveReserved2)];
        var offsets = new int[Sh2X64Context.Words];
        Type type = typeof(Sh2<TBus>);
        FieldInfo anchor = type.GetField(nameof(PC), BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo asBytes = typeof(Unsafe).GetMethods().Single(m => m.Name == nameof(Unsafe.As) && m.GetGenericArguments().Length == 2);
        MethodInfo distance = typeof(Unsafe).GetMethod(nameof(Unsafe.ByteOffset))!.MakeGenericMethod(typeof(byte));
        for (int i = 0; i < offsets.Length; i++)
        {
            FieldInfo field = type.GetField(names[i], BindingFlags.Instance | BindingFlags.NonPublic)!;
            // Keep managed interior references tracked through the ByteOffset
            // intrinsic, exactly as CpuZ80.NativeOffset does. No raw address
            // survives a possible relocation while computing the layout.
            var method = new DynamicMethod("SH2_FieldOffset", typeof(int), [type], type, true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldflda, anchor);
            il.Emit(OpCodes.Call, asBytes.MakeGenericMethod(anchor.FieldType, typeof(byte)));
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldflda, field);
            il.Emit(OpCodes.Call, asBytes.MakeGenericMethod(field.FieldType, typeof(byte)));
            il.Emit(OpCodes.Call, distance); il.Emit(OpCodes.Conv_I4); il.Emit(OpCodes.Ret);
            offsets[i] = method.CreateDelegate<Func<Sh2<TBus>, int>>()(this);
        }
        foreach (int high in new[] { Sh2X64Context.MacHigh, Sh2X64Context.CyclesHigh,
            Sh2X64Context.EndHigh, Sh2X64Context.BusDeadlineHigh }) offsets[high] += 4;
        return offsets;
    }

    private unsafe void ExecuteWithLiveX64Context()
    {
        _liveReserved0 = AccelerateIdleLoops ? 1u : 0;
        nint previous = _activeLiveState;
        // The CLR pins the whole object containing PC, including every field
        // addressed through the offsets. Its R array has its separate pin.
        fixed (uint* anchor = &PC)
        {
            _activeLiveState = (nint)anchor;
            try { ExecuteFullX64Frame((nint)anchor); }
            finally { _activeLiveState = previous; }
        }
    }

}

