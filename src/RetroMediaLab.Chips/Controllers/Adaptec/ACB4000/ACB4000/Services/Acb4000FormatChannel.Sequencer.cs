using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatSequencerConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;
using L = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;
using E = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmWriteConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000FormatChannel
{
    private readonly Acb4000AicRegisters? aic;
    private readonly Acb4000MfmWriteEncoder encoder = new();
    private bool nativeGate;
    private bool nativeStarted;
    private bool nativeData;
    private byte nativePair;

    public Acb4000FormatChannel() { }

    internal Acb4000FormatChannel(Acb4000AicRegisters aic) => this.aic = aic;

    private void ResetNativeOutput()
    {
        var wasWriting = nativeGate;
        nativeGate = nativeStarted = nativeData = false;
        nativePair = 0;
        encoder.Reset();
        if (wasWriting) PublishWriteGate();
    }

    private void BeginNativeTrack(byte fill)
    {
        ResetNativeOutput();
        var sequencer = aic!.Sequencer;
        sequencer.TryWriteProgram((byte)(S.RamBase + C.IdEccState), C.IdContinuationState);
        sequencer.TryWriteProgram((byte)(S.RamBase + C.DataEccState), C.DataContinuation);
        sequencer.TryWriteProgram((byte)(S.RamBase + C.TailGapState), C.TailContinuation);
        sequencer.TryWriteProgram((byte)(S.RamBase + S.ColumnStride + C.DataState), C.DataControl);
        sequencer.TryWriteProgram((byte)(S.RamBase + S.CountColumn * S.ColumnStride + C.GapState), C.GapCount);
        sequencer.TryWriteProgram((byte)(S.RamBase + S.DataColumn * S.ColumnStride + C.DataState), fill);
        ProgramNativeId();
        sequencer.WriteOperation(S.SuppressTransfer);
        sequencer.WriteBranch(C.GapState);
        sequencer.Start(C.WaitForIndexState);
    }

    private void ProgramNativeId()
    {
        var entry = map[slot];
        var defective = entry == L.DefectiveSlot;
        if (defective) defectCount++;
        var lowFlags = slot < parameterFlagCount ? parameterFlags[slot] : defectCount;
        var flags = (byte)(lowFlags | ((entry & L.LastLogicalSectorMask) != 0 ? F.LastSectorMask : 0));
        var address = firstAddress + (uint)(entry & L.LogicalSectorMask);
        if (defective) address |= F.DefectiveAddressMask;
        for (var index = 0; index < F.AddressByteCount; index++)
            aic!.Sequencer.TryWriteProgram((byte)(S.RamBase + S.DataColumn * S.ColumnStride + index),
                (byte)(address >> ((F.AddressByteCount - 1 - index) * S.BitsPerByte)));
        aic!.Sequencer.TryWriteProgram((byte)(S.RamBase + S.DataColumn * S.ColumnStride + F.AddressByteCount), flags);
    }

    private bool ClockNativeCell(bool index)
    {
        if (Complete || phase == Acb4000FormatChannelPhase.Idle) return false;
        aic!.ClockReferenceCell();
        var sequencer = aic!.Sequencer;
        sequencer.SetIndex(index);
        if (phase == Acb4000FormatChannelPhase.WaitForIndex && sequencer.State == C.GapState)
        {
            // Firmware 14B7 changes the branch register after the initial INDEX.
            // Subsequent DATA fields pass through state 12's two postamble bytes.
            sequencer.WriteBranch(C.PostDataState);
            phase = Acb4000FormatChannelPhase.Gap;
        }
        if (cellsLeft != 0)
        {
            cellsLeft = 0;
            // An immediate INDEX branch can close WG between the two MFM cells.
            if (nativeGate && !sequencer.WriteGate)
            {
                nativeGate = false;
                PublishWriteGate();
            }
            return (nativePair & 1) != 0;
        }
        sequencer.ClockBit(false);
        nativePair = encoder.ClockBit(sequencer.NrzOutput, aic.WriteAddressMarkOutput);
        var gate = sequencer.WriteGate;
        if (nativeGate != gate)
        {
            nativeGate = gate;
            PublishWriteGate();
        }
        nativeStarted |= nativeGate;
        if (nativeData && sequencer.State != C.DataState)
        {
            nativeData = false;
            if (++slot == slots) sequencer.WriteBranch(C.TailState);
            else ProgramNativeId();
        }
        if (sequencer.State == C.DataState) nativeData = true;
        if (nativeStarted && sequencer.Stopped)
        {
            phase = Acb4000FormatChannelPhase.Complete;
            return false;
        }
        if (nativeStarted) phase = Acb4000FormatChannelPhase.Gap;
        cellsLeft = 1;
        return (nativePair & (1 << E.ClockCellShift)) != 0;
    }
}
