using C = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;

/// <summary>Quatre canaux Tube bidirectionnels, contrôle de flux et interruptions ; aucun transport hôte.</summary>
public sealed class TubeUla
:RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Interfaces.ITubeUla{
    private readonly Queue<byte>[] _toHost = Enumerable.Range(0, C.Channels).Select(_ => new Queue<byte>()).ToArray();
    private readonly Queue<byte>[] _toParasite = Enumerable.Range(0, C.Channels).Select(_ => new Queue<byte>()).ToArray();
    private readonly byte[] _lastHost = new byte[C.Channels];
    private readonly byte[] _lastParasite = new byte[C.Channels];
    private byte _control;
    private byte _hostThreeStatus;
    private byte _parasiteThreeStatus;
    private bool _hostIrq;
    private bool _parasiteIrq;
    private bool _parasiteNmi;
    public Action? ResetParasite { get; set; }
    public Action<bool>? HostIrqChanged { get; set; }
    public Action<bool>? ParasiteIrqChanged { get; set; }
    public Action<bool>? ParasiteNmiChanged { get; set; }
    public bool ResetHeld => (_control & C.ParasiteReset) != 0;
    public bool HostIrq => _hostIrq;
    public bool ParasiteIrq => _parasiteIrq;
    public bool ParasiteNmi => _parasiteNmi;
    private int TransferBytes => (_control & C.TwoByteTransfers) != 0 ? 2 : 1;
    public TubeUla() => Reset();
    public void Reset(bool clearControl = true)
    {
        foreach (var queue in _toHost.Concat(_toParasite)) queue.Clear();
        _toHost[C.ChannelThree].Enqueue(0);
        if (clearControl) _control = 0;
        _hostThreeStatus = C.Available | C.Space;
        _parasiteThreeStatus = 0;
        UpdateInterrupts();
    }
    private int Capacity(bool host, int channel) => channel == C.ChannelThree ? TransferBytes : !host && channel == 0 ? C.ChannelOneCapacity : 1;
    public byte HostRead(int register) => Read(true, register);
    public byte ParasiteRead(int register) => Read(false, register);
    private byte Read(bool host, int register)
    {
        register &= C.RegisterMask; var channel = register >> 1;
        var receive = host ? _toHost[channel] : _toParasite[channel];
        var send = host ? _toParasite[channel] : _toHost[channel];
        if ((register & 1) == 0)
        {
            if (channel == C.ChannelThree) return (byte)(C.ControlMask | (host ? _hostThreeStatus : _parasiteThreeStatus));
            return (byte)((channel == 0 ? _control : C.ControlMask)
                | (receive.Count >= (channel == C.ChannelThree ? TransferBytes : 1) ? C.Available : 0)
                | (send.Count < Capacity(host, channel) ? C.Space : 0));
        }
        var last = host ? _lastHost : _lastParasite;
        if (receive.TryDequeue(out var value)) last[channel] = value;
        if (channel == C.ChannelThree)
        {
            if (receive.Count == 0)
            {
                if (host) { _hostThreeStatus &= unchecked((byte)~C.Available); _parasiteThreeStatus |= C.Available | C.Space; }
                else { _parasiteThreeStatus &= unchecked((byte)~C.Available); _hostThreeStatus |= C.Space; }
            }
        }
        UpdateInterrupts();
        return last[channel];
    }
    public void HostWrite(int register, byte value)
    {
        register &= C.RegisterMask;
        if (register == 0)
        {
            var previousReset = ResetHeld;
            _control = (value & C.SetFlags) != 0 ? (byte)(_control | (value & C.ControlMask)) : (byte)(_control & ~(value & C.ControlMask));
            if ((value & C.ResetRegisters) != 0) Reset(false);
            UpdateInterrupts();
            if (!previousReset && ResetHeld) ResetParasite?.Invoke();
            return;
        }
        Write(true, register, value);
    }
    public void ParasiteWrite(int register, byte value) => Write(false, register & C.RegisterMask, value);
    private void Write(bool host, int register, byte value)
    {
        if ((register & 1) == 0) return;
        var channel = register >> 1;
        var send = host ? _toParasite[channel] : _toHost[channel];
        if (channel == C.ChannelThree)
        {
            if (send.Count < C.ChannelThreeCapacity) send.Enqueue(value);
            if (send.Count >= TransferBytes)
            {
                if (host) { _parasiteThreeStatus |= C.Available; _hostThreeStatus &= unchecked((byte)~C.Space); }
                else { _hostThreeStatus |= C.Available; _parasiteThreeStatus &= unchecked((byte)~(C.Available | C.Space)); }
            }
        }
        else if (send.Count < Capacity(host, channel)) send.Enqueue(value);
        UpdateInterrupts();
    }

    private void UpdateInterrupts()
    {
        var host = (_control & C.HostInterrupt) != 0 && _toHost[C.ChannelFour].Count != 0;
        var parasite = (_control & C.ParasiteInterruptOne) != 0 && _toParasite[0].Count != 0
            || (_control & C.ParasiteInterruptFour) != 0 && _toParasite[C.ChannelFour].Count != 0;
        var nmi = (_control & C.ParasiteNmi) != 0 && (_parasiteThreeStatus & C.Available) != 0;
        if (_hostIrq != host) { _hostIrq = host; HostIrqChanged?.Invoke(host); }
        if (_parasiteIrq != parasite) { _parasiteIrq = parasite; ParasiteIrqChanged?.Invoke(parasite); }
        if (_parasiteNmi != nmi) { _parasiteNmi = nmi; ParasiteNmiChanged?.Invoke(nmi); }
    }
}
