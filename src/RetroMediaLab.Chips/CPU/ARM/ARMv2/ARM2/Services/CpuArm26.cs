using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Interfaces;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

/// <summary>ARM 26 bits : pipeline matériel, instructions conditionnelles et exceptions de la carte.</summary>
/// <remarks>Le bus emprunté possède la mémoire et ses temps d’accès. Aucun appel système
/// du programme invité n’est remplacé par une opération de l’hôte.</remarks>
public sealed partial class CpuArm26(IArmBus bus, Arm26Model model = Arm26Model.Arm2)
{
    private readonly uint[] _registers = new uint[C.RegisterCount];
    private readonly uint[,] _banks = new uint[C.ModeCount, C.FiqBankedRegisters];
    private readonly uint[] _pipeline = new uint[C.PipelineWords];
    private readonly bool[] _prefetchAborts = new bool[C.PipelineWords];
    private uint _address;
    private uint _status;
    private bool _pipelineReady;
    private bool _nextFetchSequential;
    private Arm26Mode _operandMode;
    private Arm26Mode? _deferredOperandMode;

    public uint PC => _address;
    public uint Status => _status;
    public Arm26Mode Mode => (Arm26Mode)(_status & C.ModeMask);
    public uint InstructionAddress { get; private set; }
    public long Cycles { get; private set; }
    public long Instructions { get; private set; }

    public void Reset()
    {
        Array.Clear(_registers);
        Array.Clear(_banks);
        _status = C.IrqDisable | C.FiqDisable | (uint)Arm26Mode.Supervisor;
        _address = C.ResetVector;
        _operandMode = Mode;
        _deferredOperandMode = null;
        _pipelineReady = false;
        ResetCache();
        Instructions = Cycles = 0;
        InternalCycle();
        RefillPipeline();
    }

    public void Step()
    {
        if (!_pipelineReady) RefillPipeline();
        if (bus.Fiq && (_status & C.FiqDisable) == 0)
        {
            EnterException(C.FiqVector, Arm26Mode.Fiq, _address + C.ExceptionReturnOffset, maskFiq: true);
            return;
        }
        if (bus.Irq && (_status & C.IrqDisable) == 0)
        {
            EnterException(C.IrqVector, Arm26Mode.Irq, _address + C.ExceptionReturnOffset);
            return;
        }
        InstructionAddress = _address;
        if (_prefetchAborts[0])
        {
            EnterException(C.PrefetchAbortVector, Arm26Mode.Supervisor, InstructionAddress + C.ExceptionReturnOffset);
            return;
        }
        var instruction = _pipeline[0];
        _operandMode = _deferredOperandMode ?? Mode;
        _deferredOperandMode = null;
        _pipeline[0] = _pipeline[1];
        _prefetchAborts[0] = _prefetchAborts[1];
        _prefetchAborts[1] = !ReadWord((_address + C.OperandPcOffset) & C.AddressMask,
            _nextFetchSequential, Mode == Arm26Mode.User, true, out _pipeline[1]);
        _nextFetchSequential = true;
        _address = (_address + C.WordBytes) & C.AddressMask;
        Instructions++;
        if (!ConditionPassed((ArmCondition)(instruction >> C.ConditionShift))) return;
        Execute(instruction);
    }

    private void RefillPipeline()
    {
        _address &= C.AddressMask;
        _prefetchAborts[0] = !ReadWord(_address, false, Mode == Arm26Mode.User, true, out _pipeline[0]);
        _prefetchAborts[1] = !ReadWord((_address + C.WordBytes) & C.AddressMask,
            true, Mode == Arm26Mode.User, true, out _pipeline[1]);
        _pipelineReady = true;
        _nextFetchSequential = true;
    }

    private void InternalCycle()
    {
        if (_cacheFillLine != C.NoPendingCacheLine) { ReadPendingCacheWord(); return; }
        Cycles++;
        bus.InternalCycle();
    }

    private void MergeInternalCycle()
    {
        if (model == Arm26Model.Arm3) { InternalCycle(); return; }
        Cycles++;
        bus.MergeInternalCycle();
        _nextFetchSequential = false;
    }

    private bool ReadWord(uint address, bool sequential, bool userMode, bool instruction, out uint value)
    {
        if (model == Arm26Model.Arm3 && !_busLocked && (_cacheControl & C.CacheEnable) != 0)
            return ReadCachedWord(address, userMode, instruction, sequential, out value);
        if (_cacheFillLine != C.NoPendingCacheLine) CompleteCacheFill();
        Cycles++;
        return bus.TryReadWord(address, sequential, userMode, instruction, out value);
    }

    private bool ReadByte(uint address, bool userMode, out byte value)
    {
        if (model == Arm26Model.Arm3 && !_busLocked && (_cacheControl & C.CacheEnable) != 0 && CachedByteAddress(address, userMode))
        {
            var success = ReadCachedWord(address & ~C.WordAlignmentMask, userMode, false, false, out var word);
            value = (byte)(word >> ((int)(address & C.WordAlignmentMask) * C.ByteBits));
            return success;
        }
        if (_cacheFillLine != C.NoPendingCacheLine) CompleteCacheFill();
        Cycles++;
        return bus.TryReadByte(address, userMode, out value);
    }

    private bool WriteWord(uint address, uint value, bool sequential, bool userMode)
    {
        if (_cacheFillLine != C.NoPendingCacheLine) CompleteCacheFill();
        Cycles++;
        var success = bus.TryWriteWord(address, value, sequential, userMode);
        if (success && model == Arm26Model.Arm3) UpdateCache(address, value, false, userMode);
        return success;
    }

    private bool WriteByte(uint address, byte value, bool userMode)
    {
        if (_cacheFillLine != C.NoPendingCacheLine) CompleteCacheFill();
        Cycles++;
        var success = bus.TryWriteByte(address, value, userMode);
        if (success && model == Arm26Model.Arm3) UpdateCache(address, value, true, userMode);
        return success;
    }

    private bool ConditionPassed(ArmCondition condition)
    {
        var zero = (_status & C.Zero) != 0;
        var carry = (_status & C.Carry) != 0;
        var negative = (_status & C.Negative) != 0;
        var overflow = (_status & C.Overflow) != 0;
        return condition switch
        {
            ArmCondition.Equal => zero,
            ArmCondition.NotEqual => !zero,
            ArmCondition.CarrySet => carry,
            ArmCondition.CarryClear => !carry,
            ArmCondition.Negative => negative,
            ArmCondition.Positive => !negative,
            ArmCondition.Overflow => overflow,
            ArmCondition.NoOverflow => !overflow,
            ArmCondition.Higher => carry && !zero,
            ArmCondition.LowerOrSame => !carry || zero,
            ArmCondition.GreaterOrEqual => negative == overflow,
            ArmCondition.LessThan => negative != overflow,
            ArmCondition.GreaterThan => !zero && negative == overflow,
            ArmCondition.LessOrEqual => zero || negative != overflow,
            ArmCondition.Always => true,
            _ => false
        };
    }
}
