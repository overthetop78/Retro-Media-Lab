using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Microprogramme 24×4 de l'AIC-010 ; une horloge par bit et cycles parallèles de tampon empruntés.</summary>
/// <remarks>Les horloges sont fournies par le propriétaire. Les adresses 18h–1Eh ne possèdent
/// pas de RAM documentée : StateIsDocumented les signale, aucun état de secours n'est inventé.</remarks>
public sealed class Acb4000Sequencer
{
    private readonly Acb4000EccRegisters ecc;
    private readonly IAcb4000SequencerBus buffer;
    private readonly byte[] program = new byte[C.StateCount * C.ColumnCount];
    private readonly byte[] stack = new byte[C.StackLength];
    private byte state = C.StopState;
    private byte branch;
    private byte operation;
    private byte events;
    private byte incoming;
    private byte outgoing;
    private byte reference;
    private byte currentNext;
    private byte currentControl;
    private byte currentCount;
    private byte currentData;
    private int bit;
    private int remaining;
    private int stackCount;
    private bool index;
    private bool sector;
    private bool input;
    private bool equal;
    private bool low;
    private bool eccError;
    private bool branchActive;
    private bool amActive;
    private bool carry;
    private bool eccEnabled;
    private bool writeGatePending;
    private bool resetWritePending;
    private bool waitingForReadSync;
    private bool clockedBufferTransfers;
    private bool bufferReadRequested;
    private bool bufferReadReady;
    private bool bufferWriteRequested;
    private byte bufferReadValue;
    private byte lastBufferValue;

    public Acb4000Sequencer(Acb4000EccRegisters ecc, IAcb4000SequencerBus buffer)
    {
        ArgumentNullException.ThrowIfNull(ecc);
        ArgumentNullException.ThrowIfNull(buffer);
        this.ecc = ecc;
        this.buffer = buffer;
    }

    public byte State => state;
    public bool StateIsDocumented => state < C.StateCount || state == C.StopState;
    public bool Stopped => state == C.StopState;
    public int RemainingBytes => remaining;
    public int BitRing => bit;
    public bool ReadGate { get; private set; }
    public bool WriteGate { get; private set; }
    public bool NrzOutput { get; private set; }
    public int OutputBitRing { get; private set; }
    public bool AddressMarkOutputWindow { get; private set; }
    public bool Output => !Stopped && StateIsDocumented && (Control & C.Output) != 0;
    public bool TransferEnabled => !Stopped && StateIsDocumented && (Control & C.Transfer) != 0;
    public bool WaitingForReadSync => waitingForReadSync;
    public bool ReadFieldCompleted { get; private set; }
    public bool ReadFieldEccValid { get; private set; }
    public ulong TransferByteCount { get; private set; }
    internal uint FieldDataByteCount { get; private set; }
    internal bool CarryInhibited => (operation & C.InhibitCarry) != 0;
    internal bool StackHasData => stackCount != 0;
    private byte Control => currentControl;
    private byte Count => currentCount;
    private byte Data => currentData;
    private bool Suppressed => (operation & C.SuppressTransfer) != 0;
    private bool EccField => !TransferEnabled && (Count & C.Ecc) != 0;
    private bool ReadsBuffer => TransferEnabled && !Suppressed
        && ((operation & C.Search) != 0 || WriteGate || writeGatePending);

    internal void ConfigureClockedBufferTransfers(bool enabled)
    {
        CancelBufferTransfers();
        buffer.ConfigureClockedTransfers(enabled);
        clockedBufferTransfers = enabled;
    }

    private void CancelBufferTransfers()
    {
        buffer.CancelBufferTransfers();
        bufferReadRequested = bufferReadReady = bufferWriteRequested = false;
    }

    internal void PollBufferTransfers()
    {
        if (bufferReadRequested && buffer.TryCompleteBufferRead(out bufferReadValue))
        {
            bufferReadRequested = false;
            bufferReadReady = true;
        }
        if (bufferWriteRequested && buffer.TryCompleteBufferWrite())
        {
            bufferWriteRequested = false;
            TransferByteCount++;
        }
    }

    private void RequestBufferRead()
    {
        if (bufferReadRequested || bufferReadReady) return;
        buffer.RequestBufferRead();
        bufferReadRequested = true;
        PollBufferTransfers();
    }

    private void PrepareFollowingBufferRead()
    {
        if (Suppressed || ((operation & C.Search) == 0 && !WriteGate && !writeGatePending)) return;
        if (TransferEnabled)
        {
            if (remaining > 1 || CarryInhibited) RequestBufferRead();
            return;
        }
        if (remaining != 1 || (currentNext & C.BranchMask) != 0) return;
        var next = currentNext & C.StateMask;
        if (next < C.StateCount && (program[C.StateCount * C.ControlColumn + next] & C.Transfer) != 0)
            RequestBufferRead();
    }

    public bool TryReadProgram(byte address, out byte value)
    {
        if (!TryProgramOffset(address, out var offset)) { value = 0; return false; }
        value = program[offset];
        return true;
    }

    public bool TryWriteProgram(byte address, byte value)
    {
        if (!TryProgramOffset(address, out var offset)) return false;
        program[offset] = value;
        if ((address & C.StateMask) == state)
        {
            switch (offset / C.StateCount)
            {
                case C.NextColumn: currentNext = value; break;
                case C.ControlColumn: currentControl = value; break;
                case C.CountColumn: currentCount = value; break;
                case C.DataColumn: currentData = value; break;
            }
        }
        return true;
    }

    private static bool TryProgramOffset(byte address, out int offset)
    {
        var relative = address - C.RamBase;
        var slot = relative & C.StateMask;
        if (relative < 0 || slot >= C.StateCount) { offset = 0; return false; }
        offset = relative / C.ColumnStride * C.StateCount + slot;
        return true;
    }

    public void WriteBranch(byte value) => branch = (byte)(value & C.StateMask);

    public byte ReadBranch()
    {
        var next = !Stopped && StateIsDocumented ? currentNext & C.StateMask : C.StopState;
        return (byte)(next | (carry ? C.AddressMark : 0) | (eccError ? C.Ecc : 0)
            | (bit == C.LastBit ? C.EnableBitRing : 0));
    }

    public byte ReadStatus()
    {
        var result = ecc.ResetAsserted ? (byte)0 : (byte)(
            (equal ? C.StatusEqual : 0) | (low ? C.StatusLow : 0) | (eccError ? C.StatusEccError : 0)
            | (Stopped ? C.StatusStopped : 0) | (branchActive ? C.StatusBranch : 0)
            | (TransferEnabled ? C.StatusTransfer : 0) | (amActive ? C.StatusAddressMark : 0));
        branchActive = false;
        return result;
    }

    public byte ReadOperation()
    {
        var result = (byte)(operation | events | C.OperationUnusedHigh
            | (index ? C.IndexPast : 0) | (sector ? C.SectorPast : 0));
        events = (byte)((index ? C.IndexPast : 0) | (sector ? C.SectorPast : 0));
        return result;
    }

    public void WriteOperation(byte value)
    {
        operation = (byte)(value & C.OperationWritable);
        events = (byte)((events & (C.IndexPast | C.SectorPast))
            | (index ? C.IndexPast : 0) | (sector ? C.SectorPast : 0));
    }

    public void SetIndex(bool active)
    {
        index = active;
        if (active) events |= C.IndexPast;
        CheckImmediateBranch();
    }

    public void SetSector(bool active)
    {
        sector = active;
        if (active) events |= C.SectorPast;
        CheckImmediateBranch();
    }

    public void SetInput(bool active)
    {
        input = active;
        CheckImmediateBranch();
    }

    public void Start(byte address)
    {
        if (ecc.ResetAsserted) return;
        Enter((byte)(address & C.StateMask));
    }

    private void Enter(byte address)
    {
        state = address;
        incoming = 0;
        carry = false;
        if (Stopped || !StateIsDocumented)
        {
            ReadGate = WriteGate = writeGatePending = resetWritePending = amActive = waitingForReadSync = false;
            remaining = 0;
            currentNext = currentControl = currentCount = currentData = 0;
            return;
        }
        currentNext = program[state];
        currentControl = program[C.StateCount * C.ControlColumn + state];
        currentCount = program[C.StateCount * C.CountColumn + state];
        currentData = program[C.StateCount * C.DataColumn + state];
        remaining = ((Control & C.Transfer) != 0 ? Count : Count & C.StateMask) + 1;
        if ((Control & C.SetReadGate) != 0 && !WriteGate)
        {
            if (!ReadGate)
            {
                ecc.BeginField(); eccEnabled = false; equal = true; low = false;
                ReadFieldCompleted = ReadFieldEccValid = false;
            }
            ReadGate = true;
        }
        if ((Control & C.SetWriteGate) != 0 && !ReadGate) writeGatePending = true;
        if (!TransferEnabled && (Count & C.EnableBitRing) != 0)
        {
            ecc.BeginField();
            eccEnabled = true;
            equal = true;
            low = false;
            waitingForReadSync = ReadGate;
            FieldDataByteCount = 0;
            ReadFieldCompleted = ReadFieldEccValid = false;
        }
        if (!TransferEnabled && (Count & (C.AddressMark | C.EnableBitRing)) != 0) amActive = true;
        if (clockedBufferTransfers && ReadsBuffer) RequestBufferRead();
    }

    /// <summary>Front de l'horloge de bit sélectionnée ; les horloges système/référence ne sont pas des délais muraux.</summary>
    public void ClockBit(bool nrzInput)
    {
        AddressMarkOutputWindow = false;
        if (ecc.ResetAsserted) return;
        if (clockedBufferTransfers) PollBufferTransfers();
        CheckImmediateBranch();
        if (ReadGate && nrzInput) events |= C.NrzPast;
        if (waitingForReadSync) { NrzOutput = false; return; }
        if (bit == C.GateBit)
        {
            if (resetWritePending) { WriteGate = false; resetWritePending = false; }
            if (writeGatePending) { if (!ReadGate) WriteGate = true; writeGatePending = false; }
        }
        if (!Stopped && StateIsDocumented)
        {
            if (bit == 0)
            {
                if (ReadsBuffer)
                {
                    if (!clockedBufferTransfers) { reference = buffer.ReadBuffer(); TransferByteCount++; }
                    else
                    {
                        if (bufferReadReady)
                        {
                            lastBufferValue = bufferReadValue;
                            bufferReadReady = false;
                            TransferByteCount++;
                        }
                        reference = lastBufferValue;
                    }
                }
                else reference = Data;
                if (clockedBufferTransfers) PrepareFollowingBufferRead();
                outgoing = reference;
                incoming = 0;
            }
            NrzOutput = WriteGate && (EccField ? ecc.SerialOutput : (outgoing & (1 << (C.LastBit - bit))) != 0);
            OutputBitRing = bit;
            AddressMarkOutputWindow = WriteGate && !TransferEnabled
                && (Count & (C.AddressMark | C.EnableBitRing)) != 0;
            if (ReadGate)
            {
                incoming = (byte)((incoming << 1) | (nrzInput && (Control & C.InvalidNrz) == 0 ? 1 : 0));
            }
            if (eccEnabled && (Control & C.InvalidNrz) == 0 && (ReadGate || WriteGate))
                ecc.TryClockDataBit(ReadGate ? nrzInput : NrzOutput);
            if (bit == C.LastBit) FinishByte();
        }
        else NrzOutput = false;
        bit = (bit + 1) & C.LastBit;
    }

    private void FinishByte()
    {
        if (TransferEnabled) FieldDataByteCount++;
        if (ReadGate)
        {
            if ((Control & C.Compare) != 0 && equal && incoming != reference)
            {
                equal = false;
                low = incoming < reference;
            }
            if ((Control & C.Stack) != 0) Push(incoming);
            if (TransferEnabled && !Suppressed && (operation & C.Search) == 0)
            {
                if (!clockedBufferTransfers) { buffer.WriteBuffer(incoming); TransferByteCount++; }
                else
                {
                    buffer.RequestBufferWrite(incoming);
                    bufferWriteRequested = true;
                    PollBufferTransfers();
                }
            }
        }
        carry = --remaining == 0;
        if (!carry) return;
        if (TransferEnabled && (operation & C.InhibitCarry) != 0)
        {
            operation &= unchecked((byte)~C.InhibitCarry);
            remaining = byte.MaxValue + 1;
            return;
        }
        var readEcc = ReadGate && EccField;
        if (EccField)
        {
            eccError = !ecc.CheckPassed;
            if (readEcc)
            {
                ReadFieldCompleted = true;
                ReadFieldEccValid = !eccError;
            }
            eccEnabled = false;
            ReadGate = false;
            waitingForReadSync = false;
            amActive = false;
            if ((Control & C.InvalidNrz) == 0) resetWritePending = true;
        }
        if ((Control & C.ResetWriteGate) != 0) resetWritePending = true;
        if (!TryBranch(readEcc)) Enter((byte)(currentNext & C.StateMask));
    }

    private void CheckImmediateBranch()
    {
        if (Stopped || !StateIsDocumented || (currentNext & C.BranchMask) == 0
            || ecc.ResetAsserted || ReadGate && EccField) return;
        TryBranch(false);
    }

    private bool TryBranch(bool readEcc)
    {
        var condition = (Acb4000SequencerBranch)(currentNext >> C.BranchShift);
        var indexOrSector = index || ecc.SectorBranchEnabled && sector;
        var take = readEcc ? condition switch
        {
            Acb4000SequencerBranch.StopOnInputOrEccError or Acb4000SequencerBranch.BranchOnInputOrEccError => eccError,
            Acb4000SequencerBranch.StopOnIndexOrMismatch or Acb4000SequencerBranch.BranchOnIndexOrMismatch => !equal,
            Acb4000SequencerBranch.StopOnMismatchOrReadError or Acb4000SequencerBranch.BranchOnMismatchOrReadError => !equal || eccError,
            Acb4000SequencerBranch.BranchOnCarryOrGoodRead => equal && !eccError,
            _ => false
        } : condition switch
        {
            Acb4000SequencerBranch.StopOnInputOrEccError or Acb4000SequencerBranch.BranchOnInputOrEccError => input,
            Acb4000SequencerBranch.StopOnIndexOrMismatch or Acb4000SequencerBranch.BranchOnIndexOrMismatch => indexOrSector,
            Acb4000SequencerBranch.StopOnMismatchOrReadError or Acb4000SequencerBranch.BranchOnMismatchOrReadError => !equal,
            Acb4000SequencerBranch.BranchOnCarryOrGoodRead => carry,
            _ => false
        };
        if (!take) return false;
        branchActive = true;
        Enter((byte)condition <= (byte)Acb4000SequencerBranch.StopOnMismatchOrReadError ? C.StopState : branch);
        return true;
    }

    private void Push(byte value)
    {
        if (stackCount == C.StackLength)
        {
            stack.AsSpan(1).CopyTo(stack);
            stackCount--;
        }
        stack[stackCount++] = value;
    }

    public bool TryPop(out byte value)
    {
        if (stackCount == 0) { value = 0; return false; }
        value = stack[--stackCount];
        return true;
    }

    public void Reset()
    {
        ecc.Reset();
        ResetEngine();
    }

    internal void ResetEngine()
    {
        state = C.StopState;
        branch = operation = events = incoming = outgoing = reference = 0;
        currentNext = currentControl = currentCount = currentData = 0;
        bit = remaining = stackCount = 0;
        equal = low = eccError = branchActive = amActive = carry = eccEnabled = false;
        ReadGate = WriteGate = writeGatePending = resetWritePending = NrzOutput = false;
        waitingForReadSync = ReadFieldCompleted = ReadFieldEccValid = false;
        OutputBitRing = 0;
        AddressMarkOutputWindow = false;
        TransferByteCount = 0;
        FieldDataByteCount = 0;
        CancelBufferTransfers();
        bufferReadValue = lastBufferValue = 0;
    }

    /// <summary>Octet déjà présent dans le registre de décalage NRZ lors de la reconnaissance AMD.</summary>
    /// <remarks>La reconnaissance aligne le bit ring ; les octets suivants arrivent sur ClockBit.</remarks>
    public bool AcceptReadAddressMark(byte value)
    {
        if (!waitingForReadSync || !ReadGate || ecc.ResetAsserted) return false;
        waitingForReadSync = false;
        bit = 0;
        for (var index = C.LastBit; index >= 0; index--) ClockBit((value & (1 << index)) != 0);
        return true;
    }
}
