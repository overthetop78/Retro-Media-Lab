namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private bool _transmitOutputsDirty;
    private bool _publishedRts;
    private bool _publishedTransmitting;
    public Action<bool, bool>? TransmitStateChanged { get; set; }

    private void PublishTransmitState()
    {
        _transmitOutputsDirty = false;
        var transmitting = Transmitting;
        if (_publishedRts == _rtsAsserted && _publishedTransmitting == transmitting) return;
        _publishedRts = _rtsAsserted;
        _publishedTransmitting = transmitting;
        TransmitStateChanged?.Invoke(_rtsAsserted, transmitting);
    }
}
