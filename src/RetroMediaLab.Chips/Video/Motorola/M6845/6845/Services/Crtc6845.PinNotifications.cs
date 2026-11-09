using static RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private Action<int>? _memoryAddressChanged;
    private Action<bool>? _displayEnabledChanged;
    private Action<bool>? _cursorChanged;
    private int _observedPins;
    private int _publishedMemoryAddress;
    private bool _publishedDisplayEnabled;
    private bool _publishedCursor;

    /// <summary>Changes on physical MA, independently of the renderer's skewed Address.</summary>
    public Action<int>? MemoryAddressChanged
    {
        get => _memoryAddressChanged;
        set => SetPinReceiver(ref _memoryAddressChanged, value, MemoryAddressObserver);
    }

    public Action<bool>? DisplayEnabledChanged
    {
        get => _displayEnabledChanged;
        set => SetPinReceiver(ref _displayEnabledChanged, value, DisplayEnableObserver);
    }

    public Action<bool>? CursorChanged
    {
        get => _cursorChanged;
        set => SetPinReceiver(ref _cursorChanged, value, CursorObserver);
    }

    private void SetPinReceiver<T>(ref Action<T>? target, Action<T>? receiver, int pin)
    {
        // Wiring does not create an electrical edge. The owner presents the
        // initial levels separately, as with HSYNC/VSYNC/RA.
        if (_observedPins == 0) StorePublishedPins();
        target = receiver;
        if (receiver is null) _observedPins &= ~pin;
        else _observedPins |= pin;
    }

    private void StorePublishedPins()
    {
        _publishedMemoryAddress = _memoryAddress;
        _publishedDisplayEnabled = _displayEnabled;
        _publishedCursor = _cursor;
    }

    private void PublishPins()
    {
        var addressChanged = _publishedMemoryAddress != _memoryAddress;
        var displayChanged = _publishedDisplayEnabled != _displayEnabled;
        var cursorChanged = _publishedCursor != _cursor;
        StorePublishedPins();
        // All equations of this edge have settled before any receiver runs.
        if (addressChanged) _memoryAddressChanged?.Invoke(_memoryAddress);
        if (displayChanged) _displayEnabledChanged?.Invoke(_displayEnabled);
        if (cursorChanged) _cursorChanged?.Invoke(_cursor);
    }
}
