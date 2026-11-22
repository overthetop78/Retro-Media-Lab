using RetroMediaLab.App.Services.Input.Midi;
using RetroMediaLab.Infrastructure.Settings.Emulation;

namespace RetroMediaLab.App.Controllers.Emulation.Input;

internal sealed class OsdMidiBindingCapture : IDisposable
{
    private readonly MidiShortcutInput _input;
    private HashSet<string> _baseline;

    private OsdMidiBindingCapture(MidiShortcutInput input, HashSet<string> baseline)
    {
        _input = input;
        _baseline = baseline;
    }

    internal static OsdMidiBindingCapture? Start(string actionId)
    {
        if (!EmulationShortcutDefaults.OsdValues.ContainsKey(actionId)) return null;
        var input = MidiShortcutInput.Open();
        bool started = false;
        try
        {
            var capture = new OsdMidiBindingCapture(input, input.ReadSources());
            started = true;
            return capture;
        }
        finally { if (!started) input.Dispose(); }
    }

    internal void Poll(HashSet<string> pressed, HashSet<string> captured)
    {
        var current = _input.ReadSources();
        pressed.RemoveWhere(source => MidiShortcutInput.IsMidiSource(source) && !current.Contains(source));
        foreach (var source in current.Where(source => !_baseline.Contains(source)))
        {
            pressed.Add(source);
            captured.Add(source);
        }
        _baseline = current;
    }

    public void Dispose() => _input.Dispose();
}
