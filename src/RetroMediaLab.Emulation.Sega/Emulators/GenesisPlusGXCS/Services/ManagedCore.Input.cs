using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Services;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Services;

internal sealed partial class ManagedCore
{
    private int _pointerX = 1, _pointerY = 1;
    private static readonly IReadOnlyDictionary<uint, EmulationKey> KeyboardMap = RetroMediaLab.Emulation.Sega.Common.Functions.ControllerFunctions.CreateNativeKeyMap().ToDictionary(pair => pair.Value, pair => pair.Key);
    private ReadOnlySpan<byte> input_dev => _system!.Input.DeviceTypes;
    private CPointer<ushort> input_pad => new(_system!.Input.DigitalInputs);
    private Span<short> input_analog => _system!.Input.AnalogInputs;
    private int viewport_w => _system!.Video.ViewportWidth;
    private int viewport_h => _system!.Video.ViewportHeight;
    private byte pico_current { get => _system!.PicoPage; set => _system!.PicoPage = value; }
    private int retro_gun_mode => GetOption("gun_input") == "lightgun" ? 1 : 0;
    private int invert_mouse => GetOption("invert_mouse") == "enabled" ? 1 : 0;
    private static int abs(int value) => Math.Abs(value);

    private void PollInputs()
    {
        lock (_inputGate)
        {
            _polledInput = _pendingInput;
            _pointerX = (int)Math.Clamp((long)_pointerX + _polledInput.Pointer.DeltaX * 128L, -32767, 32767);
            _pointerY = (int)Math.Clamp((long)_pointerY + _polledInput.Pointer.DeltaY * 128L, -32767, 32767);
            _pendingInput = _pendingInput with { Pointer = _pendingInput.Pointer with { DeltaX = 0, DeltaY = 0, Wheel = 0, HorizontalWheel = 0 } };
        }
        osd_input_update_internal_bitmasks();
    }
    private short ReadHostInput(uint port, uint device, uint index, uint id)
    {
        static short Bool(bool value) => value ? (short)1 : (short)0;
        device &= 255;
        EmulationInputSnapshot snapshot = _polledInput;
        if (device == 3)
            return KeyboardMap.TryGetValue(id, out EmulationKey key) ? Bool(snapshot.Keys.Contains(key)) : (short)0;
        if (device == 2 && port == 0)
            return id switch
            {
                0 => (short)Math.Clamp(snapshot.Pointer.DeltaX, short.MinValue, short.MaxValue),
                1 => (short)Math.Clamp(snapshot.Pointer.DeltaY, short.MinValue, short.MaxValue),
                2 => Bool(snapshot.Pointer.Left), 3 => Bool(snapshot.Pointer.Right), 4 => Bool(snapshot.Pointer.Wheel > 0),
                5 => Bool(snapshot.Pointer.Wheel < 0), 6 => Bool(snapshot.Pointer.Middle), _ => 0
            };
        if (device == 4 && port == 0)
            return id switch { 13 => (short)_pointerX, 14 => (short)_pointerY, 2 => Bool(snapshot.Pointer.Left), 4 => Bool(snapshot.Pointer.Right), 3 => Bool(snapshot.Pointer.Middle), _ => 0 };
        if (device == 6 && port == 0)
            return id switch { 0 => (short)_pointerX, 1 => (short)_pointerY, 2 => Bool(snapshot.Pointer.Left), _ => 0 };
        return port < snapshot.Controllers.Count ? ExternalHostCallbacks.ReadControllerState(snapshot.Controllers[(int)port], device, index, id) : (short)0;
    }

    private static (byte System, byte PadType) ControllerType(PeripheralCategory peripheral) => peripheral switch
    {
        PeripheralCategory.None => (0, 3),
        PeripheralCategory.SegaMegaMouse => (2, 3), PeripheralCategory.SegaMenacer => (3, 3),
        PeripheralCategory.SegaXe1Ap => (5, 3), PeripheralCategory.SegaActivator => (6, 3),
        PeripheralCategory.SegaLightPhaser => (7, 3), PeripheralCategory.SegaPaddleControl => (8, 3),
        PeripheralCategory.SegaSportsPad => (9, 3),
        PeripheralCategory.SegaMegaDriveSixButton or PeripheralCategory.SegaArcadePowerStickSixButton => (1, 1),
        PeripheralCategory.SegaMegaDriveThreeButton or PeripheralCategory.SegaArcadePowerStick => (1, 0),
        PeripheralCategory.SegaSg1000Joystick or PeripheralCategory.SegaSg1000IiJoypad or PeripheralCategory.SegaMasterSystemController or PeripheralCategory.SegaGameGearController or PeripheralCategory.SegaControlStick => (1, 2),
        _ => (1, 3)
    };
    public void SetControllerPortDevice(int port, PeripheralCategory peripheral)
    {
        if ((uint)port >= 8) throw new ArgumentOutOfRangeException(nameof(port));
        (byte system, byte type) = ControllerType(peripheral);
        _settings.PadTypes[port] = type;
        if (port < 2) _settings.PortSystems[port] = system;
        _system!.ConfigureInputPort(port, system, type);
    }
}
