using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using System.IO;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using NativeStorage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions.StorageFunctions;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Possession et persistance des images disque ; les contrôleurs de la carte les empruntent seulement.</summary>
internal sealed partial class EmulatorCore
{
    private IEmulationDiskImage?[] _floppyImages = [];
    private FloppyDriveSettings?[] _floppySettings = [];
    private bool[] _floppyConnected = [];
    private IReadOnlyList<string> _diskExtensions = C.Extensions;

    private void ConfigureDisks(MachineConfiguration configuration)
    {
        var count = _machine!.FloppyDriveCount;
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _floppyImages = new IEmulationDiskImage?[count];
        _floppySettings = new FloppyDriveSettings?[count];
        _floppyConnected = new bool[count];
        _floppyMechanics = new IEmulationDriveMechanics?[count];
        _diskExtensions = NativeStorage.Extensions(configuration);
        for (var drive = 0; drive < _floppyImages.Length; drive++)
        {
            _floppyMechanics[drive] ??= RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions.MediaProcessing.CreateDriveMechanics(S.Floppy);
            _floppyConnected[drive] = NativeStorage.Connected(configuration, drive);
            var settings = _floppySettings[drive] = NativeStorage.Settings(configuration, drive);
            _machine!.ConfigureFloppySpeed(drive, int.Parse(settings.Speed, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private void InsertFloppy(MediaConfiguration media)
    {
        var drive = media.Slot.Index;
        if (media.Category != MediaCategory.Floppy || drive < 0 || drive >= _floppyImages.Length || !_floppyConnected[drive] || !_diskExtensions.Contains(Path.GetExtension(media.Path), StringComparer.OrdinalIgnoreCase)) throw new ArgumentException(nameof(media));
        var settings = _floppySettings[drive]!;
        var image = AcornDiskFunctions.Open(media.Path, !media.IsReadOnly && !settings.WriteProtected, mechanics: _floppyMechanics[drive]);
        var transferred = false;
        try
        {
            NativeStorage.ValidateMedia(image, settings);
            EjectFloppy(drive);
            _machine!.AttachFloppy(drive, image); _floppyImages[drive] = image; transferred = true;
            _floppyMechanics[drive]!.Insert();
        }
        finally { if (!transferred) image.Dispose(); }
    }

    private void SaveFloppy(int drive) { if (_floppyImages[drive] is { HasChanges: true } image) AcornDiskFunctions.Save(image); }
    private void EjectFloppy(int drive)
    {
        if (drive < 0 || drive >= _floppyImages.Length) throw new ArgumentOutOfRangeException(nameof(drive));
        SaveFloppy(drive);
        _machine?.AttachFloppy(drive, null);
        var image = _floppyImages[drive]; _floppyImages[drive] = null;
        try { image?.Dispose(); }
        finally { if (image is not null) _floppyMechanics[drive]?.Eject(); }
    }

    /// <summary>Libère toutes les images même si une publication échoue ; le propriétaire libère ensuite la carte.</summary>
    private void DisposeDisks()
    {
        var errors = new List<Exception>();
        for (var drive = 0; drive < _floppyImages.Length; drive++)
        {
            var image = _floppyImages[drive]; _floppyImages[drive] = null;
            try { _machine?.AttachFloppy(drive, null); }
            catch (Exception error) { errors.Add(error); }
            finally { try { image?.Dispose(); } catch (Exception error) { errors.Add(error); } finally { _floppyMechanics[drive]?.Reset(); _floppyMechanics[drive] = null; } }
        }
        if (errors.Count > 0) throw new AggregateException(errors);
    }

    private void SaveDisks() { for (var drive = 0; drive < _floppyImages.Length; drive++) SaveFloppy(drive); }
    private void FlushStoppedDisks() { for (var drive = 0; drive < _floppyImages.Length; drive++) if (_machine?.FloppyActive(drive) != true) SaveFloppy(drive); }
}
