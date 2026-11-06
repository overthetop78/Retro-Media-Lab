using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Interfaces;
using C = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;

/// <summary>Interface Tube asynchrone : les transferts et fronts raccordés sont ses seuls événements.</summary>
public sealed partial class TubeUla : ITubeUla
{
    private readonly Queue<byte>[] _toHost = CreateRegisters();
    private readonly Queue<byte>[] _toParasite = CreateRegisters();
    private readonly byte[] _lastHost = new byte[C.Channels];
    private readonly byte[] _lastParasite = new byte[C.Channels];
    private byte _control;
    private bool _hostReset;
    private bool _resetHeld;
    private bool _hostIrq;
    private bool _parasiteIrq;
    private bool _parasiteNmi;
    private bool _hostThreeAvailable;
    private bool _parasiteThreeAvailable;
    private bool _hostThreeSpace;
    private bool _parasiteThreeSpace;

    public Action? ResetParasite { get; set; }
    public Action<bool>? ParasiteResetChanged { get; set; }
    public Action<bool>? HostIrqChanged { get; set; }
    public Action<bool>? ParasiteIrqChanged { get; set; }
    public Action<bool>? ParasiteNmiChanged { get; set; }
    public Action<bool>? DmaRequestChanged { get; set; }
    public bool ResetHeld => _resetHeld;
    public bool HostIrq => _hostIrq;
    public bool ParasiteIrq => _parasiteIrq;
    public bool ParasiteNmi => _parasiteNmi;
    public bool DmaRequest => _parasiteNmi;
    private bool RegistersHeld => _hostReset || (_control & C.ResetRegisters) != 0;
    private int TransferBytes => (_control & C.TwoByteTransfers) != 0 ? C.TwoByteTransfer : C.SingleByteTransfer;

    public TubeUla() => Reset();

    private static Queue<byte>[] CreateRegisters()
    {
        var registers = new Queue<byte>[C.Channels];
        for (var channel = 0; channel < registers.Length; channel++)
            registers[channel] = new Queue<byte>(channel == 0 ? C.ChannelOneCapacity : C.ChannelThreeCapacity);
        return registers;
    }

    /// <summary>Réinitialisation de transaction ; SetHostReset expose le maintien de la broche HRST.</summary>
    public void Reset(bool clearControl = true)
    {
        if (clearControl) _control = 0;
        ResetRegisters();
        PublishInterrupts();
        PublishReset();
    }

    public void SetHostReset(bool asserted)
    {
        if (_hostReset == asserted) return;
        _hostReset = asserted;
        if (asserted)
        {
            _control = 0;
            ResetRegisters();
            CancelBusTransfers();
            PublishInterrupts();
        }
        PublishReset();
    }

    private void ResetRegisters()
    {
        for (var channel = 0; channel < C.Channels; channel++)
        {
            _toHost[channel].Clear();
            _toParasite[channel].Clear();
        }
        _toHost[C.ChannelThree].Enqueue(0);
        _hostThreeAvailable = _hostThreeSpace = true;
        _parasiteThreeAvailable = _parasiteThreeSpace = false;
    }

    private int Capacity(bool host, int channel) => channel == C.ChannelThree ? C.ChannelThreeCapacity
        : !host && channel == 0 ? C.ChannelOneCapacity : C.LatchCapacity;

    public byte HostRead(int register) => Read(true, register);
    public byte ParasiteRead(int register) => Read(false, register);

    private byte Read(bool host, int register, bool consume = true)
    {
        register &= C.RegisterMask;
        var channel = register >> 1;
        var receive = host ? _toHost[channel] : _toParasite[channel];
        var send = host ? _toParasite[channel] : _toHost[channel];
        if ((register & C.DataRegisterMask) == 0)
        {
            if (channel == C.ChannelThree)
                return (byte)(C.ControlMask | (host
                    ? (_hostThreeAvailable ? C.Available : 0) | (_hostThreeSpace ? C.Space : 0)
                    : (ThreeActionRequired ? C.Available : 0) | (_parasiteThreeSpace ? C.Space : 0)));
            return (byte)((channel == 0 ? _control & C.ControlMask : C.ControlMask)
                | (receive.Count != 0 ? C.Available : 0)
                | (send.Count < Capacity(host, channel) ? C.Space : 0));
        }

        var last = host ? _lastHost : _lastParasite;
        if (!consume) return receive.TryPeek(out var head) ? head : last[channel];
        if (RegistersHeld || !receive.TryDequeue(out var value)) return last[channel];
        last[channel] = value;
        if (channel == C.ChannelThree && receive.Count == 0)
        {
            if (host) { _hostThreeAvailable = false; _parasiteThreeSpace = true; }
            else { _parasiteThreeAvailable = false; _hostThreeSpace = true; }
        }
        PublishChannelInterrupts(channel);
        return value;
    }

    public void HostWrite(int register, byte value)
    {
        register &= C.RegisterMask;
        if (register != 0) { Write(true, register, value); return; }
        if (_hostReset) return;
        var previous = _control;
        _control = (value & C.SetFlags) != 0
            ? (byte)(_control | (value & C.WritableControlMask))
            : (byte)(_control & ~(value & C.WritableControlMask));
        if (_control == previous) return;
        if ((_control & C.ResetRegisters) != 0)
        {
            ResetRegisters();
            CancelBusTransfers();
        }
        else if (((_control ^ previous) & C.TwoByteTransfers) != 0)
        {
            _hostThreeAvailable = _toHost[C.ChannelThree].Count >= TransferBytes;
            _parasiteThreeAvailable = _toParasite[C.ChannelThree].Count >= TransferBytes;
            _hostThreeSpace = _toParasite[C.ChannelThree].Count < TransferBytes;
            _parasiteThreeSpace = _toHost[C.ChannelThree].Count < TransferBytes;
        }
        PublishInterrupts();
        PublishReset();
    }

    public void ParasiteWrite(int register, byte value) => Write(false, register & C.RegisterMask, value);

    private void Write(bool host, int register, byte value)
    {
        if (RegistersHeld || (register & C.DataRegisterMask) == 0) return;
        var channel = register >> 1;
        var send = host ? _toParasite[channel] : _toHost[channel];
        if (send.Count >= Capacity(host, channel)) return;
        send.Enqueue(value);
        if (channel == C.ChannelThree && send.Count >= TransferBytes)
        {
            if (host) { _parasiteThreeAvailable = true; _hostThreeSpace = false; }
            else { _hostThreeAvailable = true; _parasiteThreeSpace = false; }
        }
        PublishChannelInterrupts(channel);
    }

    private bool ThreeActionRequired => _parasiteThreeAvailable || _toHost[C.ChannelThree].Count == 0;

    private void PublishChannelInterrupts(int channel)
    {
        if (channel == 0 || channel == C.ChannelFour) PublishIrq();
        else if (channel == C.ChannelThree) PublishNmi();
    }

    private void PublishInterrupts() { PublishIrq(); PublishNmi(); }

    private void PublishIrq()
    {
        var host = (_control & C.HostInterrupt) != 0 && _toHost[C.ChannelFour].Count != 0;
        var parasite = (_control & C.ParasiteInterruptOne) != 0 && _toParasite[0].Count != 0
            || (_control & C.ParasiteInterruptFour) != 0 && _toParasite[C.ChannelFour].Count != 0;
        if (_hostIrq != host) { _hostIrq = host; HostIrqChanged?.Invoke(host); }
        if (_parasiteIrq != parasite) { _parasiteIrq = parasite; ParasiteIrqChanged?.Invoke(parasite); }
    }

    private void PublishNmi()
    {
        var nmi = (_control & C.ParasiteNmi) != 0 && ThreeActionRequired;
        if (_parasiteNmi == nmi) return;
        _parasiteNmi = nmi;
        ParasiteNmiChanged?.Invoke(nmi);
        DmaRequestChanged?.Invoke(nmi);
    }

    private void PublishReset()
    {
        var reset = _hostReset || (_control & C.ParasiteReset) != 0;
        if (_resetHeld == reset) return;
        _resetHeld = reset;
        ParasiteResetChanged?.Invoke(reset);
        if (reset) ResetParasite?.Invoke();
    }
}
