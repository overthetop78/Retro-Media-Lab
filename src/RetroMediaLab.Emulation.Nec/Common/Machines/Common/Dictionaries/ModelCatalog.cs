namespace RetroMediaLab.Emulation.Nec.Common.Machines.Common.Dictionaries;

using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Nec.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngine.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.CoreGrafx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.SuperGrafx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineDuo.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.TurboExpress.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineLt.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcFx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.LaserActive.Constants;

internal static class ModelCatalog
{
    internal static IReadOnlyList<Model> All { get; } =
    [
        PcEngineMachineConstants.Definition,
        CoreGrafxMachineConstants.Definition,
        SuperGrafxMachineConstants.Definition,
        PcEngineDuoMachineConstants.Definition,
        TurboExpressMachineConstants.Definition,
        PcEngineLtMachineConstants.Definition,
        PcFxMachineConstants.Definition,
        LaserActiveMachineConstants.Definition,
        Machines.Pc8001.Constants.MachineConstants.Definition,
        Machines.Pc8801.Constants.MachineConstants.Definition,
        Machines.Pc8801.Dictionaries.ModelCatalog.MkII,
        Machines.Pc8801.Dictionaries.ModelCatalog.MkIISr,
        Machines.Pc9801.Constants.MachineConstants.Definition,
        Machines.Pc9821.Constants.MachineConstants.Definition,
        Machines.PcEngineCd.Constants.MachineConstants.Definition,
        Machines.PcEngineSuperCd.Constants.MachineConstants.Definition,
        Machines.PcEngineArcadeCard.Constants.MachineConstants.Definition
    ];

    internal static Model Get(string id) => All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentOutOfRangeException(nameof(id), id, null);
}
