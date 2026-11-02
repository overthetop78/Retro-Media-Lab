using RetroMediaLab.Emulation.HardDisks;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using System.IO;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using NativeStorage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions.StorageFunctions;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Possède les unités Winchester et publie leurs données par le même relais que les autres lecteurs.</summary>
internal sealed partial class EmulatorCore
{
    private IEmulationDiskImage?[] _hardDisks = [];
    private bool[] _hardDiskConnected = [];
    private (bool Protected, int ReadLatency, int WriteLatency, int Speed)[] _hardDiskSettings = [];

    private void ConfigureHardDisks(MachineConfiguration configuration)
    {
        var count = _machine!.HardDiskDriveCount;
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _hardDisks = new IEmulationDiskImage?[count];
        _hardDiskConnected = new bool[count];
        _hardDiskSettings = new (bool, int, int, int)[count];
        _hardDiskSoundVolume = new int[count];
        _hardDiskSoundEnabled = new bool[count];
        _hardDiskMechanics = new IEmulationDriveMechanics?[count];
        for (var unit = 0; unit < _hardDisks.Length; unit++)
        {
            _hardDiskConnected[unit] = NativeStorage.HardDiskConnected(configuration, unit);
            if (!_hardDiskConnected[unit]) continue;
            _hardDiskMechanics[unit] ??= RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions.MediaProcessing.CreateDriveMechanics(S.HardDisk);
            var settings = _hardDiskSettings[unit] = NativeStorage.HardDiskSettings(configuration, unit);
            _hardDiskSoundEnabled[unit] = bool.Parse(configuration.Options.GetValueOrDefault(C.HardDiskKey(unit, C.MechanicalSoundOption), bool.TrueString));
            _hardDiskSoundVolume[unit] = EmulationMechanicalSoundSettings.ReadVolume(configuration.Options, C.HardDiskKey(unit, C.SoundVolumeOption));
            _machine!.ConfigureHardDisk(unit, settings.ReadLatency, settings.Speed, settings.WriteLatency);
        }
    }

    private void InsertHardDisk(MediaConfiguration media)
    {
        var unit = media.Slot.Index;
        if (media.Category != MediaCategory.HardDisk || unit < 0 || unit >= _hardDisks.Length || !_hardDiskConnected[unit] || !C.HardDiskExtensions.Contains(Path.GetExtension(media.Path), StringComparer.OrdinalIgnoreCase)) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(media));
        var settings = _hardDiskSettings[unit];
        var image = AcornDiskFunctions.Open(media.Path, !media.IsReadOnly && !settings.Protected, mechanics: _hardDiskMechanics[unit]);
        var transferred = false;
        try
        {
            var supported = ArchimedesFunctions.IsModel(_definition.Model)
                ? ArchimedesDriveModels.SupportsHardDisk(image)
                : image.FormatId == C.HardDiskFormat && RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.HardDiskFormats.All.Any(format => HardDiskImageValidation.IsSupportedCapacity(image.SectorCount * image.SectorSize, format));
            if (!supported) throw new InvalidDataException(ErrorMessages.IncompatibleMedia);
            EjectHardDisk(unit); _machine!.AttachHardDisk(unit, image); _hardDisks[unit] = image; transferred = true;
        }
        finally { if (!transferred) image.Dispose(); }
    }

    private void SaveHardDisk(int unit) { if (_hardDisks[unit] is { HasChanges: true } image) AcornDiskFunctions.Save(image); }
    private void SaveHardDisks() { for (var unit = 0; unit < _hardDisks.Length; unit++) SaveHardDisk(unit); }
    private void FlushStoppedHardDisks() { for (var unit = 0; unit < _hardDisks.Length; unit++) if (_machine?.HardDiskActive(unit) != true) SaveHardDisk(unit); }
    private void EjectHardDisk(int unit)
    {
        if (unit < 0 || unit >= _hardDisks.Length) throw new ArgumentOutOfRangeException(nameof(unit));
        SaveHardDisk(unit); _machine?.AttachHardDisk(unit, null);
        var image = _hardDisks[unit]; _hardDisks[unit] = null;
        try { image?.Dispose(); } finally { _hardDiskMechanics[unit]?.SetMotor(false); }
    }
    private void DisposeHardDisks()
    {
        var errors = new List<Exception>();
        for (var unit = 0; unit < _hardDisks.Length; unit++)
        {
            var image = _hardDisks[unit]; _hardDisks[unit] = null;
            try { _machine?.AttachHardDisk(unit, null); }
            catch (Exception error) { errors.Add(error); }
            finally { try { image?.Dispose(); } catch (Exception error) { errors.Add(error); } finally { _hardDiskMechanics[unit]?.Reset(); _hardDiskMechanics[unit] = null; } }
        }
        if (errors.Count > 0) throw new AggregateException(errors);
    }
}
