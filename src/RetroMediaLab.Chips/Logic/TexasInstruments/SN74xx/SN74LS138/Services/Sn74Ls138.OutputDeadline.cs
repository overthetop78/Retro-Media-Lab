namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Services;

public sealed partial class Sn74Ls138
{
    private long _publishedOutputNext;
    private Action<long?>? _nextOutputChanged;

    /// <summary>Next delayed change on a pin in SetObservedOutputs' mask.
    /// Internal responses on every other pin remain pending in this package.
    /// Catch up to the transaction time before providing new inputs, observing
    /// other pins or adding a previously unconnected receiver.</summary>
    public long? NextOutputEventNanoseconds
    {
        get
        {
            var next = FindDeadline((byte)(_pending & _observed));
            return next == 0 ? null : next;
        }
    }

    /// <summary>Connection changes do not fabricate an initial event: the owner
    /// reads NextOutputEventNanoseconds when installing its listener.</summary>
    public Action<long?>? NextOutputEventNanosecondsChanged
    {
        get => _nextOutputChanged;
        set
        {
            _nextOutputChanged = value;
            _publishedOutputNext = FindDeadline((byte)(_pending & _observed));
        }
    }

    private void PublishOutputDeadline()
    {
        if (_nextOutputChanged is null) return;
        var next = FindDeadline((byte)(_pending & _observed));
        if (_publishedOutputNext == next) return;
        _publishedOutputNext = next;
        _nextOutputChanged.Invoke(next == 0 ? null : next);
    }
}
