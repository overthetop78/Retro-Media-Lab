using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;
using V = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

internal sealed partial class AtomMachine
{
    // Témoin de comparaison du transport seulement ; aucune option ni variante matérielle.
    internal bool GroupVideoClocks { get; set; } = true;

    private void ClockVideo()
    {
        // Tick précède le transfert CPU : la RAM et les entrées de contrôle restent
        // stables pendant ces CLK fournis, avec les vraies prises DD/FS du récepteur.
        _videoPhase += V.CharacterClock;
        var clocks = _videoPhase / C.CpuClock;
        _videoPhase -= clocks * C.CpuClock;
        if (GroupVideoClocks) _video.AdvanceClocks(clocks);
        else for (var clock = 0; clock < clocks; clock++) _video.Tick();
    }

    private void WireCpuClock()
    {
        // Schéma202.000/C : PH2 du6502(IC22/39) vers6522(IC1/25).
        // Le CPU termine le transfert de bus avant de notifier son descendant.
        _cpu.PhaseOutputsChanged = (_, high) =>
        {
            if (high) _via.Phi2LeadingEdge();
            else _via.Phi2TrailingEdge();
        };
        // Niveau initial PH2 bas, avant les vrais fronts des cycles RESET.
        _via.Phi2TrailingEdge();
    }
}
