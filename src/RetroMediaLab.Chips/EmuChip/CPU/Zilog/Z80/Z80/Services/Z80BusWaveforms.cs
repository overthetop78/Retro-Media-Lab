using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using C = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80PinEncoding;
using L = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80BusLines;
using A = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums.Z80BusStepAction;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

/// <summary>Données constantes des chronogrammes Zilog 03-0027-02 p.3–5,
/// NEC p.4-10–4-12. La phase et l'exécution restent au séquenceur unique.</summary>
internal sealed class Z80BusWaveforms
{
    internal readonly Z80BusStep[][] Cycles;
    internal readonly byte[][] QuietPhases;

    internal Z80BusWaveforms(Z80BusTiming t)
    {
        Cycles = new Z80BusStep[6][];
        QuietPhases = new byte[6][];
        foreach (var cycle in Enum.GetValues<Z80BusCycle>())
        {
            bool fetch = cycle == Z80BusCycle.OpcodeFetch;
            bool ack = cycle == Z80BusCycle.InterruptAcknowledge;
            bool io = cycle is Z80BusCycle.IoRead or Z80BusCycle.IoWrite;
            bool write = cycle is Z80BusCycle.MemoryWrite or Z80BusCycle.IoWrite;
            var steps = new Z80BusStep[ack ? 13 : fetch || io ? 9 : 7];
            Cycles[(int)cycle] = steps;
            steps[0] = Step(A.BeginAddress, (L.Address, 0, t.Address),
                (L.M1, fetch || ack ? 0 : 1, fetch || ack ? t.M1Assert : t.M1Release));
            if (ack)
            {
                steps[5] = Step(A.None, (L.Io, 0, t.IoAssertFalling));
                steps[7] = Step(A.Wait);
                steps[8] = Step(A.Sample | A.RefreshAddress, (L.Io, 1, t.IoRelease),
                    (L.M1, 1, t.M1Release), (L.Address, 0, t.Address), (L.Refresh, 0, t.RefreshAssert));
                steps[9] = Step(A.None, (L.Memory, 0, t.MemoryAssert));
                steps[11] = Step(A.None, (L.Memory, 1, t.MemoryRelease));
                steps[12] = Step(A.Complete, (L.Refresh, 1, t.RefreshRelease));
            }
            else if (io)
            {
                if (write) steps[1] = Step(A.WriteData, (L.Data, 0, t.Data));
                steps[2] = Step(A.None, (L.Io, 0, t.IoAssertRising),
                    (write ? L.Write : L.Read, 0, write ? t.WriteAssertRising : t.ReadAssertRising));
                steps[5] = Step(A.Wait);
                steps[7] = Step(write ? A.None : A.Sample, (L.Io, 1, t.IoReleaseFalling),
                    (write ? L.Write : L.Read, 1, write ? t.WriteRelease : t.ReadReleaseFalling));
                steps[8] = write ? Step(A.CompleteWrite, (L.Data, -1, t.DataFloat)) : Step(A.Complete);
            }
            else
            {
                steps[1] = write
                    ? Step(A.WriteData, (L.Memory, 0, t.MemoryAssert), (L.Data, 0, t.Data))
                    : Step(A.None, (L.Memory, 0, t.MemoryAssert), (L.Read, 0, t.ReadAssertFalling));
                steps[3] = write ? Step(A.Wait, (L.Write, 0, t.WriteAssertFalling)) : Step(A.Wait);
                if (fetch)
                {
                    steps[4] = Step(A.Sample | A.RefreshAddress, (L.Read, 1, t.ReadRelease),
                        (L.Memory, 1, t.MemoryRelease), (L.M1, 1, t.M1Release),
                        (L.Address, 0, t.Address), (L.Refresh, 0, t.RefreshAssert));
                    steps[5] = Step(A.None, (L.Memory, 0, t.MemoryAssert));
                    steps[7] = Step(A.None, (L.Memory, 1, t.MemoryRelease));
                    steps[8] = Step(A.Complete, (L.Refresh, 1, t.RefreshRelease));
                }
                else
                {
                    steps[5] = Step(write ? A.None : A.Sample, (L.Memory, 1, t.MemoryRelease),
                        (write ? L.Write : L.Read, 1, write ? t.WriteRelease : t.ReadReleaseFalling));
                    steps[6] = write ? Step(A.CompleteWrite, (L.Data, -1, t.DataFloat)) : Step(A.Complete);
                }
            }
            // Compile maximal phase portions with no bus action. The CPU's
            // control-sampling phase is always a fence even if its bus step
            // has no output. Phase zero and completion remain explicit.
            var quiet = new byte[steps.Length];
            int control = ack ? 10 : cycle is Z80BusCycle.MemoryRead or Z80BusCycle.MemoryWrite ? 4 : 6;
            for (int phase = steps.Length - 2; phase >= 0; phase--)
            {
                ref readonly var following = ref steps[phase + 1];
                if (phase + 1 != control && following.Outputs == 0 && following.Action == A.None)
                    quiet[phase] = (byte)(1 + quiet[phase + 1]);
            }
            QuietPhases[(int)cycle] = quiet;
        }
    }

    private static Z80BusStep Step(A action, params (int Line, int Value, long Delay)[] outputs)
    {
        ulong mask = 0, targets = 0;
        uint lines = 0;
        var delays = new long[L.Count];
        foreach (var (line, value, delay) in outputs)
        {
            ulong bits = line == L.Address ? (ulong)(ushort)value | C.AddressDriven
                : line == L.Data ? value < 0 ? 0 : ((ulong)(byte)value << C.DataShift) | C.DataDriven
                : value != 0 ? C.Masks[line] : C.DrivenMasks[line];
            mask |= C.Masks[line]; targets |= bits; lines |= 1u << line; delays[line] = delay;
        }
        return new(mask, targets, lines, delays, action);
    }
}
