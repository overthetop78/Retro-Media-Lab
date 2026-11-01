using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;

/// <summary>Résout la RAM physique des BBC Micro A et B indépendamment de leur émulateur.</summary>
internal static class BbcMicroMemoryFunctions
{
    /// <summary>Conserve 16 Kio lorsque le réglage est absent ; seules les capacités matérielles 16/32 Kio sont acceptées.</summary>
    internal static int MainBytes(MachineConfiguration configuration)
    {
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
