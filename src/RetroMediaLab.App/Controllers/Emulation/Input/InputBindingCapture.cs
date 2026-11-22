using System.Windows.Input;
using RetroMediaLab.App.Constants.Input.Bindings;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Functions.Input.Keyboard;

namespace RetroMediaLab.App.Controllers.Emulation.Input;

internal sealed class InputBindingCapture(string actionId, InputCaptureSources sources, bool prefixKeyboardSource) : IDisposable
{
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly List<Key> _keyOrder = [];
    private readonly HashSet<string> _pressedSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _capturedSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly KeyboardModifierState _modifierState = new();
    private readonly DateTime _deadlineUtc = DateTime.UtcNow.AddSeconds(15);
    private readonly ControllerBindingCapture? _controller = sources.HasFlag(InputCaptureSources.Controller) ? new() : null;
    private readonly OsdMidiBindingCapture? _midi = OsdMidiBindingCapture.Start(actionId);
    private ModifierKeys _modifiers;

    internal TimeSpan Remaining => _deadlineUtc - DateTime.UtcNow;

    internal bool PressKey(Key key, ModifierKeys modifiers, int? timestamp)
    {
        if (KeyboardChordFunctions.IsWindowsReserved(new KeyboardChord(modifiers | KeyboardChordFunctions.ModifierForKey(key), [key])))
            return false;

        if (!_pressedKeys.Contains(key)) _modifierState.Press(key, timestamp, _pressedKeys);
        if (_modifierState.HasSyntheticLeftControl)
        {
            _keyOrder.Remove(Key.LeftCtrl);
            _modifiers &= ~ModifierKeys.Control;
        }
        modifiers = _modifierState.Normalize(modifiers, _pressedKeys);
        _modifiers |= modifiers | KeyboardChordFunctions.ModifierForKey(key);
        if (_pressedKeys.Add(key) && !_keyOrder.Contains(key)) _keyOrder.Add(key);
        return true;
    }

    internal void ReleaseKey(Key key)
    {
        _pressedKeys.Remove(key);
        _modifierState.Release(key);
    }

    internal bool PressSource(string source)
    {
        _capturedSources.Add(source);
        return _pressedSources.Add(source);
    }

    internal void ReleaseSource(string source) => _pressedSources.Remove(source);

    internal void PollControllers(Action<string> deviceCaptured) =>
        _controller?.Poll(_pressedSources, _capturedSources, PressSource, deviceCaptured);

    internal string? ReadCompletedBinding()
    {
        _midi?.Poll(_pressedSources, _capturedSources);
        if (_pressedKeys.Count != 0 || _pressedSources.Count != 0 || _keyOrder.Count + _capturedSources.Count == 0)
            return null;

        var keyboard = KeyboardChordFunctions.Format(_modifiers, _keyOrder);
        var parts = new List<string>();
        if (keyboard.Length > 0)
            parts.Add(prefixKeyboardSource || _capturedSources.Count > 0 ? InputBindingSyntax.Keyboard(keyboard) : keyboard);
        parts.AddRange(_capturedSources.OrderBy(source => source.StartsWith(InputBindingSyntaxConstants.MousePrefix, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(source => source, StringComparer.Ordinal));
        return string.Join("+", parts);
    }

    public void Dispose() => _midi?.Dispose();
}
