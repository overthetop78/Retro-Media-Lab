using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using Field = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    public void ClockReadCell(bool cell)
    {
        if (!ReadGate || !_bus.Busy) return;
        if (_stage == Acb4000Stage.Parameters)
        {
            if (!ReferenceEquals(Drive, _parameterStorage[Unit])) { _parameterChannel.Reset(); Complete(C.NotReady); return; }
            var parametersRead = _parameterChannel.ParametersRead;
            var field = _parameterChannel.ClockCell(cell);
            if (_parameterChannel.ParametersRead != parametersRead)
                _searchBudget.Begin(F.SmallBlockSequentialSectors);
            if (field != Acb4000FieldKind.None)
            {
                if (_parameterChannel.WaitingForIndex) BeginIndexSearch();
                else _searchDeadline.BeginFollowingFieldWait();
            }
            FinishParameterAcquisition();
            return;
        }
        if (_translating && (!CheckTranslationDrive() || !_translationIndexSeen)) return;
        if (_searchingData && !CheckSearchDataDrive()) return;
        var kind = _readChannel.Feed(cell);
        if (kind != Acb4000FieldKind.None) _searchDeadline.BeginFollowingFieldWait();
        if (kind == Acb4000FieldKind.Id)
        {
            var decoded = _readChannel.TryReadId(out var id);
            var validId = decoded && _readChannel.IntegrityValid;
            var missingLastData = _matchingReadId && _searchBudget.Remaining == 0;
            var exhausted = _searchBudget.ObserveId(missingLastData || validId);
            if (missingLastData && exhausted is { } missingDataError)
            {
                // The final matching ID may still supply DATA, but a new ID cannot
                // restart an exhausted search while that DATA is still absent.
                Complete(missingDataError, true);
                return;
            }
            // Never associate the following DATA with an old ID after an intervening ID.
            _matchingReadId = validId && !id.IsDefective && id.LogicalAddress == _address;
            if (_translating)
            {
                var ordinal = _translationOrdinal++;
                if (_matchingReadId) TranslatePhysicalId(ordinal);
                else if (exhausted is { } error) Complete(error, true);
                if (_stage == Acb4000Stage.Read) _readChannel.BeginIdRead();
                return;
            }
            if (!_matchingReadId && exhausted is { } searchError)
            { Complete(searchError, true); return; }
            if (_stage == Acb4000Stage.WriteSearch)
            {
                if (_matchingReadId && CheckWriteDrive())
                {
                    _readChannel.StopRead();
                    BeginNativeWrite();
                    _stage = Acb4000Stage.Write;
                }
                else if (_stage == Acb4000Stage.WriteSearch) _readChannel.BeginIdRead();
                return;
            }
            _readIdSequence = _readChannel.FieldSequence;
            if (_matchingReadId) BeginNativeDataRead();
            else _readChannel.BeginIdRead();
            return;
        }
        if (_stage == Acb4000Stage.WriteSearch || kind != Acb4000FieldKind.Data || !_matchingReadId)
        {
            if (kind == Acb4000FieldKind.Data) _readChannel.BeginIdRead();
            return;
        }
        if (_readChannel.FieldSequence != unchecked(_readIdSequence + 1))
        {
            _matchingReadId = false;
            _readChannel.BeginIdRead();
            return;
        }
        _matchingReadId = false;
        if (_readChannel.Field.Length != BlockSize + Field.DataOverhead
            || _readChannel.Field[Field.MarkOffset] != Field.DataMark) return;
        var valid = _readChannel.IntegrityValid;
        var transferComplete = _readChannel.TransferredBytes == (ulong)BlockSize;
        _receivedBlocks[Unit]++;

        if (_verifying)
        {
            // OEM 6.3.2.4: VERIFY checks ECC, without retrying/correcting the data.
            if (!valid) { Complete(C.VerifyDataCheck, true); return; }
            if (--RemainingBlocks == 0) { Complete(); return; }
            if (AdvanceAddress()) _stage = Acb4000Stage.Verify;
            return;
        }

        valid &= transferComplete;

        if (_searchingData) { SearchDataBlock(valid); return; }
        var policy = ReadErrorMode;
        if (!valid && (policy & Acb4000ReadErrorMode.DisableRetryAndCorrection) != 0)
        {
            Complete(C.DataCheckReported, true);
            return;
        }
        if (!valid && ++_readAttempts < C.DataReadAttempts)
        {
            // Firmware 1906 calls 15D1 for each reread, rearming the ID search.
            _searchBudget.Begin(Drive!.SectorsPerTrack);
            _readChannel.BeginIdRead();
            return;
        }
        var corrected = false;
        if (!valid && transferComplete)
        {
            Acb4000FieldCodec.TryReadData(_readChannel.Field, _buffer.AsSpan(0, BlockSize),
                correctErrors: true, out valid, out _, _eccRegisters.Polynomial);
            corrected = valid;
        }
        _readError = !valid ? C.UncorrectableData
            : corrected && (policy & Acb4000ReadErrorMode.ReportCorrections) != 0 ? C.DataCheckReported : C.NoSense;
        // OEM 6.3.1.5: even an uncorrectable block precedes CHECK/11 on the host bus.
        Reply(BlockSize);
    }
}
