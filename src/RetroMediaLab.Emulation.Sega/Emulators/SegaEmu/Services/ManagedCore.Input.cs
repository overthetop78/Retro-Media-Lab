using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Services;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
    private readonly int[] _pointerX = Enumerable.Repeat(1, 8).ToArray(), _pointerY = Enumerable.Repeat(1, 8).ToArray();
    private readonly short[] _relativePointerX = new short[8], _relativePointerY = new short[8];
    private readonly long[] _relativeRemainderX = new long[8], _relativeRemainderY = new long[8];
    private readonly long[] _absoluteRemainderX = new long[8], _absoluteRemainderY = new long[8];
    private readonly bool[] _pointingUsesMouse = new bool[8];
    private static readonly IReadOnlyDictionary<uint, EmulationKey> KeyboardMap = RetroMediaLab.Emulation.Sega.Common.Functions.ControllerFunctions.CreateNativeKeyMap().ToDictionary(pair => pair.Value, pair => pair.Key);
    private ReadOnlySpan<byte> input_dev => _system!.Input.DeviceTypes;
    private ArrayPointer<ushort> input_pad => new(_system!.Input.DigitalInputs);
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
            int global = PointerConstants.Percent(GetOption("pointer_speed"), 200);
            for (int port = 0; port < _pointerX.Length; port++)
            {
                if (_polledInput.Pointer.DeltaX != 0 || _polledInput.Pointer.DeltaY != 0) _pointingUsesMouse[port] = true;
                else if (port < _polledInput.Controllers.Count &&
                    (_polledInput.Controllers[port].LeftX != 0 || _polledInput.Controllers[port].LeftY != 0)) _pointingUsesMouse[port] = false;
                int sensitivity = port < 2 ? PointerConstants.Percent(GetOption($"pointer_port{port + 1}_speed"), global) : global;
                _pointerX[port] = MoveAbsolutePointer(_pointerX[port], _polledInput.Pointer.DeltaX, sensitivity, ref _absoluteRemainderX[port]);
                _pointerY[port] = MoveAbsolutePointer(_pointerY[port], _polledInput.Pointer.DeltaY, sensitivity, ref _absoluteRemainderY[port]);
                _relativePointerX[port] = ScaleRelativePointer(_polledInput.Pointer.DeltaX, sensitivity, ref _relativeRemainderX[port]);
                _relativePointerY[port] = ScaleRelativePointer(_polledInput.Pointer.DeltaY, sensitivity, ref _relativeRemainderY[port]);
            }
            _pendingInput = _pendingInput with { Pointer = _pendingInput.Pointer with { DeltaX = 0, DeltaY = 0, Wheel = 0, HorizontalWheel = 0 } };
        }
        osd_input_update_internal_bitmasks();
        if (_system is Machines.Sc3000.Sc3000Machine computer)
            computer.Keys = _polledInput.Keys;
    }
    private static short ScaleRelativePointer(int delta, int sensitivity, ref long remainder)
    {
        long scaled = delta * (long)sensitivity + remainder;
        remainder = scaled % 100;
        return (short)Math.Clamp(scaled / 100, short.MinValue, short.MaxValue);
    }
    private static int MoveAbsolutePointer(int position, int delta, int sensitivity, ref long remainder)
    {
        long scaled = delta * 128L * sensitivity + remainder;
        long next = position + scaled / 100;
        remainder = next is >= -32767 and <= 32767 ? scaled % 100 : 0;
        return (int)Math.Clamp(next, -32767, 32767);
    }
    private short ReadPointingAxis(int player, uint axis) =>
        ReadHostInput((uint)player, _pointingUsesMouse[player] ? 6u : 5u, 0, axis);
    private short ReadHostInput(uint port, uint device, uint index, uint id)
    {
        static short Bool(bool value) => value ? (short)1 : (short)0;
        device &= 255;
        EmulationInputSnapshot snapshot = _polledInput;
        if (device == 3)
            return KeyboardMap.TryGetValue(id, out EmulationKey key) ? Bool(snapshot.Keys.Contains(key)) : (short)0;
        if (device == 2 && port < _pointerX.Length)
            return id switch
            {
                0 => _relativePointerX[port],
                1 => _relativePointerY[port],
                2 => Bool(snapshot.Pointer.Left), 3 => Bool(snapshot.Pointer.Right), 4 => Bool(snapshot.Pointer.Wheel > 0),
                5 => Bool(snapshot.Pointer.Wheel < 0), 6 => Bool(snapshot.Pointer.Middle), _ => 0
            };
        if (device == 4 && port < _pointerX.Length)
            return id switch { 13 => (short)_pointerX[port], 14 => (short)_pointerY[port], 2 => Bool(snapshot.Pointer.Left), 4 => Bool(snapshot.Pointer.Right), 3 => Bool(snapshot.Pointer.Middle), _ => 0 };
        if (device == 6 && port < _pointerX.Length)
            return id switch { 0 => (short)_pointerX[port], 1 => (short)_pointerY[port], 2 => Bool(snapshot.Pointer.Left), _ => 0 };
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
        if (_saturn is not null)
        {
            SetSaturnController(port, peripheral);
            return;
        }
        (byte system, byte type) = ControllerType(peripheral);
        _settings.PadTypes[port] = type;
        if (port < 2) _settings.PortSystems[port] = system;
        _system!.ConfigureInputPort(port, system, type);
    }
}
