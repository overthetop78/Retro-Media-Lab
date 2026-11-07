using RetroMediaLab.Emulation;
using CommonControllerFunctions = RetroMediaLab.Emulation.Acorn.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions;
internal static class InputSettingsFunctions
{
    internal static EmulationInputSettings Describe(MachineConfiguration configuration)
    {
        var input = configuration.Input;
        var adapter = new Engine().SelectedAdapter(configuration);
        var definitions = adapter?.GetKeyboardDefinitions(configuration) ?? BbcMicroInputSettingsFunctions.KeyboardDefinitions(configuration);
        var keyboard = new EmulationInputBindingSet(definitions, input.KeyboardBindings ?? EmulationSdk.InputProcessing.ToStrings(input.KeyboardMappings, StringComparer.Ordinal), EmulationInputSource.Keyboard | EmulationInputSource.Mouse | EmulationInputSource.Controller);
        var mouseDefinitions = adapter?.GetMouseDefinitions(configuration) ?? [];
        var mouse = mouseDefinitions.Count == 0 ? null : new EmulationInputBindingSet(mouseDefinitions,
            EmulationSdk.InputProcessing.ResolveBindings(mouseDefinitions, input.MouseBindings),
            EmulationInputSource.Mouse | EmulationInputSource.Keyboard | EmulationInputSource.Controller, true);
        var configured = input.Controllers ?? [];
        var choices = (adapter?.GetPeripherals(configuration) ?? CommonControllerFunctions.Peripherals(configuration.Model).ToArray()).Where(category => category != PeripheralCategory.Automatic).OrderBy(category => category).Select(Choice).ToArray();
        var ports = Enumerable.Range(ControllerPortConstants.MinimumControllerPort, CommonControllerFunctions.PortCount(configuration)).Select(index =>
        {
            var current = configured.FirstOrDefault(binding => binding.Port == index);
            var peripheral = current?.Peripheral ?? (configuration.Model == MachineModel.BbcMasterCompact ? PeripheralCategory.DigitalJoystick : PeripheralCategory.AnalogJoystick);
            if (peripheral == PeripheralCategory.Automatic) peripheral = configuration.Model == MachineModel.BbcMasterCompact ? PeripheralCategory.DigitalJoystick : PeripheralCategory.AnalogJoystick;
            return new EmulationControllerPort(
                index + BufferConstants.IndexIncrement,
                choices,
                peripheral.ToString(),
                current?.DeviceId,
                new EmulationInputBindingSet(ControllerDefinitions(peripheral), current?.Mappings ?? new Dictionary<string, string>(), EmulationInputSource.Keyboard | EmulationInputSource.Mouse | EmulationInputSource.Controller, true),
                current?.DeadZonePercent ?? ControllerConstants.DefaultDeadZonePercent,
                current?.VisualId
            ) { OutputBindings = current?.OutputBindings ?? new Dictionary<string, EmulationControllerOutputBinding>() };
        }).ToArray();
        return new EmulationInputSettings(keyboard, mouse, ports);
    }

    internal static MachineConfiguration Apply(MachineConfiguration configuration, EmulationInputSettings settings)
    {
        var allowedKeys = (new Engine().SelectedAdapter(configuration)?.GetKeyboardDefinitions(configuration) ?? BbcMicroInputSettingsFunctions.KeyboardDefinitions(configuration)).Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var keyboardBindings = settings.Keyboard is null 
            ? configuration.Input.KeyboardBindings ?? EmulationSdk.InputProcessing.ToStrings(configuration.Input.KeyboardMappings, StringComparer.Ordinal)
            : settings.Keyboard.Values.Where(item => allowedKeys.Contains(item.Key) && CommonControllerFunctions.IsValidBinding(item.Value)).ToDictionary(item => item.Key,
                item => string.IsNullOrWhiteSpace(item.Value) ? string.Empty : item.Value,
                StringComparer.Ordinal);
        var keyboard = EmulationSdk.InputProcessing.ToKeys(keyboardBindings);
        var mouseBindings = configuration.Input.MouseBindings;
        if (settings.Mouse is not null)
        {
            var mouseDefinitions = new Engine().SelectedAdapter(configuration)?.GetMouseDefinitions(configuration) ?? [];
            mouseBindings = EmulationSdk.InputProcessing.ResolveBindings(mouseDefinitions, settings.Mouse.Values);
            if (mouseBindings.Values.Any(value => !CommonControllerFunctions.IsValidBinding(value)))
                throw new ArgumentException(ErrorMessages.UnsupportedControllerDevice, nameof(settings));
        }
        var controllers = settings.ControllerPorts.Select(port =>
        {
            if (!Enum.TryParse<PeripheralCategory>(port.SelectedControllerId, true, out var peripheral) || !Enum.IsDefined(peripheral))
                throw new ArgumentException(ErrorMessages.UnsupportedControllerDevice, nameof(settings));
            return new ControllerBinding(port.Number - BufferConstants.IndexIncrement, peripheral, port.PhysicalDeviceId, port.Bindings.Values, port.DeadZonePercent, port.VisualId, OutputBindings: port.OutputBindings);
        }).ToArray();
        var result = configuration with
        {
            Input = configuration.Input with
            {
                KeyboardMappings = keyboard,
                KeyboardBindings = keyboardBindings,
                MouseBindings = mouseBindings,
                Controllers = controllers
            }
        };
        ConfigurationFunctions.Validate(result);
        return new Engine().SelectedAdapter(result)?.ApplyInputConfiguration(result) ?? result;
    }

    private static EmulationControllerChoice Choice(PeripheralCategory category) => new(
        category.ToString(),
        category switch
        {
            PeripheralCategory.None => InputSettingsFunctionsConstants.ResourceControllerNone,
            PeripheralCategory.Automatic => InputSettingsFunctionsConstants.ResourceControllerAutomatic,
            PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick => "Controllers.Enum.Joystick",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        },
        BindingDefinitions: ControllerDefinitions(category),
        CompatibleVisualIds: category is PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick ? [EmulationControllerVisualIds.CompetitionPro5000] : [],
        DefaultVisualId: category is PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick ? EmulationControllerVisualIds.CompetitionPro5000 : null,
        VisualCommandIds: category is PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick ? new Dictionary<EmulationControllerVisualControl, string>
        {
            [EmulationControllerVisualControl.DirectionUp] = EmulationControllerCommandIds.Up,
            [EmulationControllerVisualControl.DirectionDown] = EmulationControllerCommandIds.Down,
            [EmulationControllerVisualControl.DirectionLeft] = EmulationControllerCommandIds.Left,
            [EmulationControllerVisualControl.DirectionRight] = EmulationControllerCommandIds.Right,
            [EmulationControllerVisualControl.PrimaryAction] = EmulationControllerCommandIds.Fire1,
            [EmulationControllerVisualControl.SecondaryAction] = EmulationControllerCommandIds.Fire1
        } : null,
        VisualBindingDefinitions: category is PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick ? new Dictionary<string, IReadOnlyList<InputBindingDefinition>>
        {
            [EmulationControllerVisualIds.CompetitionPro5000] = ControllerDefinitions(category)
        } : null
    ) { Category = category == PeripheralCategory.None ? EmulationControllerCategory.None : EmulationControllerCategory.Joystick };
    internal static IReadOnlyList<InputBindingDefinition> MouseDefinitions() =>
    [
        new(MouseInputConstants.LeftAction, MouseInputConstants.LeftResource, MouseInputConstants.LeftSource),
        new(MouseInputConstants.RightAction, MouseInputConstants.RightResource, MouseInputConstants.RightSource),
        new(MouseInputConstants.MiddleAction, MouseInputConstants.MiddleResource, MouseInputConstants.MiddleSource)
    ];
    private static IReadOnlyList<InputBindingDefinition> ControllerDefinitions(PeripheralCategory category) => category == PeripheralCategory.None 
        ? [] 
        : InputSettingsFunctionsConstants.ControllerActions.Select(action => new InputBindingDefinition(action, InputSettingsFunctionsConstants.ResourceActionPrefix + action, string.Empty)
        {
            OppositeDirectionId = category is PeripheralCategory.AnalogJoystick or PeripheralCategory.DigitalJoystick or PeripheralCategory.Automatic ? action switch
            {
                EmulationControllerCommandIds.Up => EmulationControllerCommandIds.Down,
                EmulationControllerCommandIds.Down => EmulationControllerCommandIds.Up,
                EmulationControllerCommandIds.Left => EmulationControllerCommandIds.Right,
                EmulationControllerCommandIds.Right => EmulationControllerCommandIds.Left,
                _ => null
            } : null
        }).ToArray();

    /// <summary>Décrit les touches du clavier principal selon les règles matérielles déjà utilisées.</summary>
    /// <param name="configuration">Profil et associations empruntés à consulter.</param>
    /// <returns>Définitions de clavier du profil ; liste vide sans clavier pris en charge.</returns>
    internal static IReadOnlyList<InputBindingDefinition> KeyboardDefinitions(MachineConfiguration configuration) { return BbcMicroInputSettingsFunctions.KeyboardDefinitions(configuration); }
}
