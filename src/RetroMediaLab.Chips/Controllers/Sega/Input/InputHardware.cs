namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public interface IInputBus
{
    uint MainCycles { get; }
    uint Z80Cycles { get; }
    byte Region { get; }
    int ViewportWidth { get; }
    int ViewportHeight { get; }
    ushort VerticalCounter { get; }
    uint HvcLatch { get; set; }
    byte VersionPins { get; }

    bool BootRomMapped { get; }
    void SetPsgPanning(uint cycles, uint panning);
    void SwitchSmsCartridge(byte mode);
    ReadOnlyMemory<byte> VdpRegisters { get; }
    ReadOnlyMemory<byte> HorizontalCounterTable { get; }
    void UpdateMainIrq(uint level);
}

public sealed partial class InputHardware
{
    private readonly IInputBus _bus;
    private readonly byte[] configuredPadTypes = new byte[8];
    private byte system_hw;
    private InputDeviceConfiguration _deviceConfiguration = new();
    private uint mainCycles => _bus.MainCycles;
    private uint z80Cycles => _bus.Z80Cycles;
    private byte region_code => _bus.Region;
    private int viewportWidth => _bus.ViewportWidth;
    private int viewportHeight => _bus.ViewportHeight;
    private ushort v_counter => _bus.VerticalCounter;
    private uint hvc_latch { get => _bus.HvcLatch; set => _bus.HvcLatch = value; }
    private ReadOnlySpan<byte> reg => _bus.VdpRegisters.Span;
    private readonly byte[] io_reg = new byte[16];
    private ReadOnlySpan<byte> hctab => _bus.HorizontalCounterTable.Span;
    private void m68k_update_irq(uint level) => _bus.UpdateMainIrq(level);
    private static int abs(int value) => Math.Abs(value);

    public Span<byte> PortSystems => input.system;
    public Span<byte> ConfiguredPadTypes => configuredPadTypes;
    public ReadOnlySpan<byte> DeviceTypes => input.dev;
    public Span<ushort> DigitalInputs => input.pad;
    public Span<short> AnalogInputs => input.analog;
    public Span<int> OriginalPortSystems => old_system;
    public Memory<byte> IoRegisters => io_reg;
    public int GunOffsetX { get => input.x_offset; set => input.x_offset = value; }
    public int GunOffsetY { get => input.y_offset; set => input.y_offset = value; }

    public InputHardware(IInputBus bus)
    {
        _bus = bus;
        Array.Fill(configuredPadTypes, (byte)3);
    }

    public void Initialize(byte hardware, InputDeviceConfiguration configuration)
    {
        Configure(hardware, configuration);
        input_init();
    }
    public void Configure(byte hardware, InputDeviceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        system_hw = hardware;
        _deviceConfiguration = configuration;
    }
    public void Reset() => input_reset();
    public void Refresh() => input_refresh();
    public void EndFrame(uint cycles) => input_end_frame(cycles);
}
