using CommonControllerFunctions = RetroMediaLab.Emulation.Epoch.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Epoch.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Epoch.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Epoch.Emulators.Common.Interop.Services;

internal sealed class KeyboardState
{
    private static readonly IReadOnlyDictionary<EmulationKey, uint> KeyMap = KeyboardFunctions.CreateKeyMap();
    private IReadOnlySet<EmulationKey> _previous = new HashSet<EmulationKey>();

    internal void Publish(IReadOnlySet<EmulationKey> keys, ExternalCoreApi.KeyboardEvent? keyboardEvent)
    {
        if (keyboardEvent is null) return;
        var modifiers = KeyboardFunctions.Modifiers(keys);
        CommonControllerFunctions.PublishTransitions(keys, KeyMap, modifiers,
            ref _previous, ref keyboardEvent, KeyboardFunctions.IsModifier, KeyboardFunctions.Character);
    }

    internal static IReadOnlyDictionary<EmulationKey, uint> Mappings => KeyMap;
}
