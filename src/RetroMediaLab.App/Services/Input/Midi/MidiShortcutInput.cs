using System.Globalization;
using NAudio;
using NAudio.Midi;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Services.Input.Midi;

internal sealed class MidiShortcutInput : IDisposable
{
    private const string DevicePrefix = "midi:";
    private const string BindingPrefix = "Controller:" + DevicePrefix;
    private static readonly object Gate = new();
    private static readonly List<Port> Ports = [];
    private static int _leases;
    private bool _disposed;

    internal static MidiShortcutInput Open()
    {
        lock (Gate)
        {
            var lease = new MidiShortcutInput();
            bool started = false;
            try
            {
                if (_leases++ == 0) OpenPorts();
                started = true;
                return lease;
            }
            finally { if (!started) lease.Dispose(); }
        }
    }

    internal static bool IsMidiBinding(string? binding) =>
        binding?.Split('+', StringSplitOptions.TrimEntries).Any(IsMidiSource) == true;

    internal static bool IsMidiSource(string source) =>
        source.StartsWith(BindingPrefix, StringComparison.OrdinalIgnoreCase);

    internal static string? DeviceName(string id)
    {
        if (!id.StartsWith(DevicePrefix, StringComparison.OrdinalIgnoreCase)) return null;
        int occurrence = id.LastIndexOf(':');
        if (occurrence <= DevicePrefix.Length) return null;
        string name = Uri.UnescapeDataString(id[DevicePrefix.Length..occurrence]);
        string number = id[(occurrence + 1)..];
        return number == "1" ? name : $"{name} ({number})";
    }

    internal IReadOnlyList<EmulationControllerState> ReadControllers()
    {
        lock (Gate)
            return _disposed ? [] : Ports.Select(port => port.Snapshot()).ToArray();
    }

    internal HashSet<string> ReadSources() => ReadControllers()
        .SelectMany(state => state.Controls.Keys.Select(note => InputBindingSyntax.Controller(state.DeviceId, note)))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public void Dispose()
    {
        lock (Gate)
        {
            if (_disposed) return;
            _disposed = true;
            if (--_leases != 0) return;
            try
            {
                foreach (var port in Ports)
                    try { port.Dispose(); }
                    catch (MmException) { }
            }
            finally { Ports.Clear(); }
        }
    }

    private static void OpenPorts()
    {
        var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < MidiIn.NumberOfDevices; index++)
        {
            try
            {
                var name = MidiIn.DeviceInfo(index).ProductName;
                var occurrence = occurrences.GetValueOrDefault(name) + 1;
                occurrences[name] = occurrence;
                var id = DevicePrefix + Uri.EscapeDataString(name) + ":" + occurrence.ToString(CultureInfo.InvariantCulture);
                Ports.Add(new Port(id, index));
            }
            catch (MmException) { } // Le port peut être utilisé par une autre application.
        }
    }

    private sealed class Port : IDisposable
    {
        private readonly MidiIn _input;
        private readonly HeldNotes _notes;

        internal Port(string id, int index)
        {
            _notes = new HeldNotes(id);
            _input = new MidiIn(index);
            bool started = false;
            try
            {
                _input.MessageReceived += MessageReceived;
                _input.Start();
                started = true;
            }
            finally { if (!started) Dispose(); }
        }

        private void MessageReceived(object? sender, MidiInMessageEventArgs args) => _notes.Receive(args.RawMessage);
        internal EmulationControllerState Snapshot() => _notes.Snapshot();

        public void Dispose()
        {
            try { _input.Stop(); }
            finally
            {
                _input.MessageReceived -= MessageReceived;
                _input.Dispose();
            }
        }
    }

    internal sealed class HeldNotes(string deviceId)
    {
        private readonly object _notesGate = new();
        private readonly HashSet<int> _notes = [];

        internal void Receive(int message)
        {
            int command = message & 0xf0;
            int channel = message & 0xf;
            int data = (message >> 8) & 0x7f;
            int value = (message >> 16) & 0x7f;
            int key = (channel << 7) | data;
            lock (_notesGate)
            {
                if (command == 0x90 && value != 0) _notes.Add(key);
                else if (command is 0x80 or 0x90) _notes.Remove(key);
                else if (command == 0xb0 && data is 120 or 123)
                    _notes.RemoveWhere(note => (note >> 7) == channel);
            }
        }

        internal EmulationControllerState Snapshot()
        {
            lock (_notesGate)
                return EmulationControllerState.Empty with
                {
                    DeviceId = deviceId,
                    Controls = new(_notes.Select(note =>
                        new KeyValuePair<string, float>(FormattableString.Invariant($"Ch{(note >> 7) + 1}-N{note & 0x7f}"), 1f)))
                };
        }

    }
}
