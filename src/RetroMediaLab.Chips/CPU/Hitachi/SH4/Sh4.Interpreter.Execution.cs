using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal partial class Sh4Interpreter
{
    private void ExecuteOpcode(ushort opcode)
    {
        if (Context.StatusRegister.Fd && OpcodeTable[opcode].IsFloatingPoint)
            throw new Sh4InstructionException(Context.ProgramCounter - 2, Sh4ExceptionEvent.FpuDisabled);
        _handlers[opcode](Context, opcode);
        _cycles.ExecuteCycles(opcode);
    }

    private ushort ReadNextOpcode()
    {
        uint address = Context.ProgramCounter;
        if (!Mmu.Enabled && (address & 1) != 0)
            throw new Sh4InstructionException(address, Sh4ExceptionEvent.AddressErrorRead);

        Context.ProgramCounter = address + 2;
        return ReadInstruction16(address);
    }

    internal override void Run()
    {
        Sh4Context? previousContext = Sh4Context.ExchangeActive(Context);
        Instance = this;
        try
        {
            Context.RestoreHostRoundingMode();
            do
            {
                try
                {
                    do
                    {
                        ExecuteOpcode(ReadNextOpcode());
                    } while (Context.CycleCounter > 0);
                    Context.CycleCounter += Sh4Scheduler.TimeSlice;
                    UpdateSystemInterrupts();
                }
                catch (Sh4InstructionException exception)
                {
                    RaiseException(exception.ProgramCounter, exception.Event);
                    _cycles.AddCycles(5 * InterpreterCpuRatio);
                }
            } while (Context.CpuRunning);
        }
        catch (Sh4DebuggerStopException)
        {
        }
        finally
        {
            Context.CpuRunning = false;
            Instance = null;
            Sh4Context.ExchangeActive(previousContext);
        }
    }

    internal override void Start() => Context.CpuRunning = true;

    internal override void Stop()
    {
        Context.CpuRunning = false;
        Context.CycleCounter = 0;
    }

    internal override void Step()
    {
        Sh4Context? previousContext = Sh4Context.ExchangeActive(Context);
        Instance = this;
        try
        {
            Context.RestoreHostRoundingMode();
            ExecuteOpcode(ReadNextOpcode());
        }
        catch (Sh4InstructionException exception)
        {
            RaiseException(exception.ProgramCounter, exception.Event);
            _cycles.AddCycles(5 * InterpreterCpuRatio);
        }
        catch (Sh4DebuggerStopException)
        {
        }
        finally
        {
            Instance = null;
            Sh4Context.ExchangeActive(previousContext);
        }
    }

    internal override void Reset(bool hard)
    {
        Context = _addressSpace.RecompilerContext;
        Sh4HostRuntime.Verify(!Context.CpuRunning);

        if (hard)
        {
            _cycles.Initialize(Context);
            _instructionCache.Initialize(Context, _addressSpace.CacheRegisters, _addressSpace, Mmu);
            _operandCache.Initialize(Context, _addressSpace.CacheRegisters, _addressSpace, Mmu);
            int scheduledNext = Context.SchedulerNext;
            Context.Clear();
            Context.SchedulerNext = scheduledNext;
        }

        Context.ProgramCounter = 0xA0000000;
        Context.Registers.Clear();
        Context.BankedRegisters.Clear();
        Context.Gbr = Context.Ssr = Context.Spc = Context.Sgr = Context.Dbr = Context.Vbr = 0;
        Context.Mac = Context.ProcedureRegister = Context.Fpul = 0;

        Context.StatusRegister.SetFull(0x700000F0);
        Context.OldStatusRegister.Status = Context.StatusRegister.Status;
        Context.UpdateStatusRegister();
        Context.Fpscr.Full = 0x00040001;
        Context.OldFpscr = Context.Fpscr;

        _instructionCache.Reset(hard);
        _operandCache.Reset(hard);
        _cycles.Reset();
        Context.CycleCounter = Sh4Scheduler.TimeSlice;
        Trace.TraceInformation("Sh4 Reset");
    }

    internal override bool IsCpuRunning => Context.CpuRunning;

    internal void ExecuteDelaySlot()
    {
        try
        {
            ExecuteOpcode(ReadNextOpcode());
        }
        catch (Sh4InstructionException exception)
        {
            exception.AdjustForDelaySlot();
            throw;
        }
        catch (Sh4DebuggerStopException)
        {
            Context.ProgramCounter -= 2;
            throw;
        }
    }

    internal void ExecuteReturnFromExceptionDelaySlot()
    {
        try
        {
            ushort opcode = ReadNextOpcode();
            Context.StatusRegister.SetFull(Context.Ssr);
            ExecuteOpcode(opcode);
        }
        catch (Sh4InstructionException)
        {
            throw new InvalidOperationException("Fatal: SH4 exception in RTE delay slot");
        }
        catch (Sh4DebuggerStopException)
        {
            Context.ProgramCounter -= 2;
            throw;
        }
    }

    internal int UpdateSystemInterrupts()
    {
        Context.SchedulerNext -= Sh4Scheduler.TimeSlice;
        if (Context.SchedulerNext < 0)
            Scheduler.Tick(Sh4Scheduler.TimeSlice);
        return Context.InterruptPending ? _interrupts.Update() : 0;
    }

    internal override void Initialize()
    {
        Context = _addressSpace.RecompilerContext;
        Context.Clear();
        _cycles.Initialize(Context);
        _instructionCache.Initialize(Context, _addressSpace.CacheRegisters, _addressSpace, Mmu);
        _operandCache.Initialize(Context, _addressSpace.CacheRegisters, _addressSpace, Mmu);
    }

    internal override void Terminate()
    {
        Stop();
        Trace.TraceInformation("Sh4 Term");
    }

    internal static Sh4Executor GetSh4Interpreter(Sh4AddressSpace addressSpace, Sh4Debugger debugger) =>
        new Sh4Interpreter(addressSpace, debugger);
}
