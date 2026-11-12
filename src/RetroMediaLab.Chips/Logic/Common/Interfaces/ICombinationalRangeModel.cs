using RetroMediaLab.Chips.Logic.Common.Contracts;

namespace RetroMediaLab.Chips.Logic.Common.Interfaces;

/// <summary>The existing physical package supplies target/delay preparation.
/// Its inputs are committed only after the shared engine validates the dates.</summary>
internal interface ICombinationalRangeModel
{
    bool PrepareRangeInput(in TimedLogicInput input, Span<long> delays, out ushort targets);
    void CommitRangeInput(in TimedLogicInput input);
}
