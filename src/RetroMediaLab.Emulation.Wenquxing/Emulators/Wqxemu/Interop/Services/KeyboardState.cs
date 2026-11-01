using CommonControllerFunctions = RetroMediaLab.Emulation.Wenquxing.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Constants;
using RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Functions;

namespace RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Services;

internal sealed class KeyboardState
{
    private readonly IReadOnlyDictionary<EmulationKey, uint> _map;
    private HashSet<uint> _previous = [];
    internal KeyboardState(MachineModel model) => _map = KeyboardFunctions.CreateKeyMap(model);
    internal void Publish(IReadOnlySet<EmulationKey> keys, ExternalCoreApi.KeyboardEvent? callback)
    {
        if (callback is null) return;
        var current = keys.Where(_map.ContainsKey).Select(key => _map[key]).ToHashSet();
        CommonControllerFunctions.PublishCodeTransitions(current, ref _previous, callback, KeyboardConstants.NoModifiers,
                codes => codes,
                codes => codes,
                code => KeyboardConstants.NoCharacter);
        _previous = current;
    }
    internal void Release(ExternalCoreApi.KeyboardEvent? callback)
    {
        try { if (callback is not null) foreach (var code in _previous) callback(false, code, KeyboardConstants.NoCharacter, KeyboardConstants.NoModifiers); }
        finally { _previous.Clear(); }
    }
}