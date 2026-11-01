using RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Constants;
using RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Contracts;
using RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Enums;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Dictionaries;

public static class AppleWinModelCatalog
{
    public static IReadOnlyList<AppleWinModelDefinition> All { get; } =
    [
        new(AppleWinModel.AppleII, MachineModel.AppleII, OptionConstants.ModelAppleII),
        new(AppleWinModel.AppleIIPlus, MachineModel.AppleIIPlus, OptionConstants.ModelAppleIIPlus),
        new(AppleWinModel.AppleIIJPlus, MachineModel.AppleIIJPlus, OptionConstants.ModelAppleIIJPlus),
        new(AppleWinModel.AppleIIe, MachineModel.AppleIIe, OptionConstants.ModelAppleIIe),
        new(AppleWinModel.AppleIIeEnhanced, MachineModel.AppleIIeEnhanced, OptionConstants.ModelAppleIIeEnhanced)
    ];

    public static string GetNativeValue(MachineModel model) => All.Single(entry => entry.MachineModel == model).NativeValue;
}
