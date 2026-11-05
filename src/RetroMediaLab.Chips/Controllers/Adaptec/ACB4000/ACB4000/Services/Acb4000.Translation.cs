using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using T = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000TranslateConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private bool _translating;
    private bool _translationIndexLevel;
    private bool _translationIndexSeen;
    private uint _translationOrdinal;
    private IScsiStorage? _translationDrive;

    private void BeginTranslation()
    {
        if (_command[C.ShortCountOffset] != 0) { Complete(C.BadArgument); return; }
        if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
        _translating = true;
        _translationIndexSeen = false;
        _translationOrdinal = 0;
        _translationDrive = Drive;
        BeginPositioning((long)_address * BlockSize, Acb4000PositioningPurpose.Read);
    }

    private void TranslationIndex(bool asserted)
    {
        var rising = asserted && !_translationIndexLevel;
        _translationIndexLevel = asserted;
        if (!_translating || _stage != Acb4000Stage.Read || !rising) return;
        AcceptTranslationIndex();
    }

    private void BeginTranslationRead()
    {
        BeginIndexSearch();
        // Firmware 15BF polls the level, including INDEX already asserted at entry.
        if (_translationIndexLevel) AcceptTranslationIndex();
    }

    private void AcceptTranslationIndex()
    {
        _translationIndexSeen = true;
        BeginFirstFieldSearch();
        _translationOrdinal = 0;
        _readChannel.Reset();
        _readChannel.BeginIdRead();
    }

    private bool CheckTranslationDrive()
    {
        if (ReferenceEquals(Drive, _translationDrive) && _driveReady[Unit]) return true;
        Complete(C.NotReady);
        return false;
    }

    private void TranslatePhysicalId(uint ordinal)
    {
        var overhead = _sequentialLayout[Unit] ? F.SequentialRecordOverhead : F.InterleavedRecordOverhead;
        var position = checked(T.FirstRecordBytePosition + ordinal * (uint)(BlockSize + overhead));
        // The OEM descriptor packs a 24-bit cylinder and an 8-bit head before the 32-bit byte position.
        BinaryPrimitives.WriteUInt32BigEndian(_buffer,
            checked(((uint)_driveCylinder[Unit] << F.CylinderHeadShift) | (uint)SelectedHead));
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(F.DefectBytePositionOffset), position);
        Reply(F.DefectEntryLength);
    }
}
