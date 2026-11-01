using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;

/// <summary>Résout la RAM physique des BBC Micro A et B indépendamment de leur émulateur.</summary>
internal static class BbcMicroMemoryFunctions
{
    /// <summary>Conserve 16 Kio lorsque le réglage est absent ; seules les capacités matérielles 16/32 Kio sont acceptées.</summary>
    internal static int MainBytes(MachineConfiguration configuration)
    {
        if (configuration.Model is MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128)
        {
            var total = configuration.Options.GetValueOrDefault(SettingsConstants.MainMemory,
                configuration.Model == MachineModel.BbcMicroModelBPlus128 ? BbcMicroModelConstants.OneHundredTwentyEightKibibytesValue : BbcMicroModelConstants.SixtyFourKibibytesValue);
            if (total is not (BbcMicroModelConstants.SixtyFourKibibytesValue or BbcMicroModelConstants.OneHundredTwentyEightKibibytesValue)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
            return (int)BbcMicroModelConstants.ThirtyTwoKibibytes;
        }
        var value = configuration.Options.GetValueOrDefault(SettingsConstants.MainMemory,
            configuration.Model == MachineModel.BbcMicroModelB ? BbcMicroModelConstants.ThirtyTwoKibibytesValue : BbcMicroModelAConstants.StandardMemoryKib);
        if (configuration.Model == MachineModel.BbcMicroModelB && value != BbcMicroModelConstants.ThirtyTwoKibibytesValue)
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        return value switch
        {
            BbcMicroModelConstants.SixteenKibibytesValue => (int)BbcMicroModelConstants.SixteenKibibytes,
            BbcMicroModelConstants.ThirtyTwoKibibytesValue => (int)BbcMicroModelConstants.ThirtyTwoKibibytes,
            _ => throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration))
        };
    }
}
