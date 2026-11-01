using RetroMediaLab.Emulation.Acorn.Emulators.B2.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.B2.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.B2.Dictionaries;

namespace RetroMediaLab.Emulation.Acorn.Emulators.B2.Functions;

internal static class B2ModelFunctions
{
    internal static IReadOnlyList<B2ModelDefinition> GetModels(MachineModel model) => B2ModelCatalog.All.Where(definition => (definition.MachineModel == model || model is MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128 && definition.MachineModel is MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128)).ToArray();

    internal static string DefaultValue(MachineModel model) => model switch
    {
        MachineModel.BbcMicroModelB => OptionConstants.ModelBAcorn1770,
        MachineModel.BbcMicroModelBPlus => OptionConstants.ModelBPlus,
        MachineModel.BbcMicroModelBPlus128 => OptionConstants.ModelBPlus128,
        MachineModel.BbcMaster128 => OptionConstants.Master128,
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };

    internal static bool IsCompatible(MachineModel model, string nativeValue) => GetModels(model).Any(definition => string.Equals(definition.NativeValue, nativeValue, StringComparison.Ordinal));
}
