using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private int[] _hardDiskSoundVolume = [];
    private bool[] _hardDiskSoundEnabled = [];
    private IEmulationDriveMechanics?[] _hardDiskMechanics = [];

    private void ConfigureHardDiskSound(int unit, bool allowSound = true)
    {
        _hardDiskMechanics[unit]?.Configure(
            RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StoragePresentationConstants.WinchesterSound,
            _hardDiskSoundVolume[unit], allowSound && _hardDiskConnected[unit] && _hardDiskSoundEnabled[unit]
                && _hardDiskSettings[unit].Speed == C.DefaultSpeedPercent);
    }

}
