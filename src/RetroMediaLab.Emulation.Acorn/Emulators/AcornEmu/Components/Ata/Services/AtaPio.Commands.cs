using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Constants.AtaConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Services;

internal sealed partial class AtaPio
{
    private void StartCommand(byte command)
    {
        var image = _disk(_unit);
        if (image is null && command != (byte)AtaCommand.Diagnostic) return;
        _command = (command & C.CommandGroupMask) switch
        {
            (byte)AtaCommand.Recalibrate => AtaCommand.Recalibrate,
            (byte)AtaCommand.Seek => AtaCommand.Seek,
            _ => (AtaCommand)command
        };
        Current.Interrupt = false;
        Current.Error = _transferError = 0;
        _dataOnly = false;
        _addressWrapped = false;
        _firstWrite = true;
        _remaining = Current.Count == 0 ? C.MaximumSectorCount : Current.Count;
        _offset = _length = _committed = 0;
        Schedule(AtaPhase.Command);
        UpdateInterrupt();
    }

    private void ExecuteEvent()
    {
        if (_phase == AtaPhase.Reset)
        {
            foreach (var drive in _drives) drive.Status = C.IdleStatus;
            _phase = AtaPhase.Idle;
            UpdateInterrupt();
            return;
        }
        if (_command != AtaCommand.Diagnostic && !ReferenceEquals(Current.Image, _disk(_unit)))
        {
            Current.Image = _disk(_unit);
            Finish(C.AbortError);
            return;
        }
        switch (_phase)
        {
            case AtaPhase.Reading: PrepareRead(); return;
            case AtaPhase.Writing: PrepareWrite(); return;
            case AtaPhase.Verifying: VerifySector(); return;
            case AtaPhase.Committing: CommitSector(); return;
        }
        switch (_command)
        {
            case AtaCommand.Identify:
                if (!ValidImage()) { Finish(C.AbortError); break; }
                Identify();
                SendBuffer(C.SectorBytes, dataOnly: true);
                break;
            case AtaCommand.ReadBuffer:
                SendBuffer(C.SectorBytes, dataOnly: true);
                break;
            case AtaCommand.WriteBuffer:
                _dataOnly = true;
                ReceiveBuffer(C.SectorBytes, interrupt: false);
                break;
            case AtaCommand.ReadSectors:
            case AtaCommand.ReadSectorsNoRetry:
                PrepareRead();
                break;
            case AtaCommand.WriteSectors:
            case AtaCommand.WriteSectorsNoRetry:
                PrepareWrite();
                break;
            case AtaCommand.ReadMultiple:
            case AtaCommand.WriteMultiple:
                if (Current.Multiple == 0) { Finish(C.AbortError); break; }
                if (_command == AtaCommand.ReadMultiple) PrepareRead();
                else PrepareWrite();
                break;
            case AtaCommand.VerifySectors:
            case AtaCommand.VerifySectorsNoRetry:
                VerifySector();
                break;
            case AtaCommand.SetMultiple:
                var count = Current.Count;
                var valid = count <= C.MaximumMultiple && (count & (count - 1)) == 0;
                Current.Multiple = valid ? count : 0;
                Finish(valid ? (byte)0 : C.AbortError);
                break;
            case AtaCommand.InitializeParameters:
                Current.Sectors = Current.Count;
                Current.Heads = (Current.Head & C.HeadMask) + 1;
                Finish();
                break;
            case AtaCommand.SetFeatures:
                SetFeatures();
                break;
            case AtaCommand.Recalibrate:
                Current.Image?.Mechanics.SetMotor(true);
                Current.Image?.Mechanics.Seek(0, Current.Head & C.HeadMask);
                if (Current.Track != 0) Current.Seeks++;
                Current.Track = 0;
                Finish();
                break;
            case AtaCommand.Seek:
                if (!TryAddress(out var block)) { Finish(C.IdNotFound); break; }
                Position(block, writing: false);
                Finish();
                break;
            case AtaCommand.Diagnostic:
                foreach (var drive in _drives)
                {
                    ResetRegisters(drive);
                    drive.Status = C.IdleStatus;
                }
                _unit = 0;
                _phase = AtaPhase.Idle;
                Current.Interrupt = true;
                UpdateInterrupt();
                break;
            default:
                Finish(C.AbortError);
                break;
        }
    }

    private void SetFeatures()
    {
        switch (Current.Features)
        {
            case C.EnableEightBit: Current.EightBit = true; break;
            case C.DisableEightBit: Current.EightBit = false; break;
            case C.PreserveSettings: Current.PreserveSettings = true; break;
            case C.RevertSettings: Current.PreserveSettings = false; break;
            default: Finish(C.AbortError); return;
        }
        Finish();
    }

    private bool ValidImage()
    {
        var image = Current.Image;
        return image is { SectorSize: > 0, SectorCount: > 0 }
            && (image.SectorSize % C.SectorBytes == 0 || C.SectorBytes % image.SectorSize == 0)
            && image.SectorCount <= long.MaxValue / image.SectorSize
            && image.SectorCount * image.SectorSize % C.SectorBytes == 0;
    }

    private long Capacity => ValidImage()
        ? Math.Min(C.MaximumSectors, Current.Image!.SectorCount * Current.Image.SectorSize / C.SectorBytes) : 0;

    private bool TryAddress(out long block)
    {
        block = 0;
        if (!ValidImage() || _addressWrapped) return false;
        if ((Current.Head & C.LbaMode) != 0)
            block = Current.Sector | (long)Current.CylinderLow << C.LbaCylinderLowShift
                | (long)Current.CylinderHigh << C.LbaCylinderHighShift
                | (long)(Current.Head & C.HeadMask) << C.LbaHeadShift;
        else
        {
            var head = Current.Head & C.HeadMask;
            if (Current.Heads == 0 || Current.Sectors == 0 || head >= Current.Heads
                || Current.Sector == 0 || Current.Sector > Current.Sectors) return false;
            var cylinder = Current.CylinderLow | Current.CylinderHigh << C.ByteBits;
            block = ((long)cylinder * Current.Heads + head) * Current.Sectors + Current.Sector - 1;
        }
        return block < Capacity;
    }

    private void AdvanceAddress()
    {
        if ((Current.Head & C.LbaMode) != 0)
        {
            var block = Current.Sector | (long)Current.CylinderLow << C.LbaCylinderLowShift
                | (long)Current.CylinderHigh << C.LbaCylinderHighShift
                | (long)(Current.Head & C.HeadMask) << C.LbaHeadShift;
            block++;
            _addressWrapped = block >= C.MaximumSectors;
            Current.Sector = (byte)block;
            Current.CylinderLow = (byte)(block >> C.LbaCylinderLowShift);
            Current.CylinderHigh = (byte)(block >> C.LbaCylinderHighShift);
            Current.Head = (byte)((Current.Head & ~C.HeadMask) | ((int)(block >> C.LbaHeadShift) & C.HeadMask));
            return;
        }
        if (Current.Sector < Current.Sectors) { Current.Sector++; return; }
        Current.Sector = 1;
        var head = (Current.Head & C.HeadMask) + 1;
        if (head < Current.Heads) Current.Head = (byte)((Current.Head & ~C.HeadMask) | head);
        else
        {
            Current.Head &= unchecked((byte)~C.HeadMask);
            var cylinder = (Current.CylinderLow | Current.CylinderHigh << C.ByteBits) + 1;
            _addressWrapped = cylinder > C.MaximumCylinders;
            Current.CylinderLow = (byte)cylinder;
            Current.CylinderHigh = (byte)(cylinder >> C.ByteBits);
        }
    }

    private void CompleteSector()
    {
        _remaining--;
        Current.Count = (byte)_remaining;
        if (_remaining > 0 && (_phase != AtaPhase.Sending || _transferError == 0)) AdvanceAddress();
    }

    private void Position(long block, bool writing)
    {
        var image = Current.Image!;
        var native = block * C.SectorBytes / image.SectorSize;
        var cylinder = image.Heads > 0 && image.SectorsPerTrack > 0
            ? (int)(native / image.SectorsPerTrack / image.Heads) : 0;
        var head = image.Heads > 0 && image.SectorsPerTrack > 0
            ? (int)(native / image.SectorsPerTrack % image.Heads) : 0;
        image.Mechanics.SetMotor(true);
        image.Mechanics.SetActivity(!writing, writing);
        image.Mechanics.Seek(cylinder, head, logicalBlock: block);
        if (Current.Track != cylinder) Current.Seeks++;
        Current.Track = cylinder;
    }
}
