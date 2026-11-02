using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.AtomPpiConstants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

/// <summary>Ports statiques du 8255 d’Atom : clavier, vidéo, cassette et haut-parleur.</summary>
/// <remarks>Le mode 0 utilisé par la carte est pris en charge. Les modes avec
/// acquittement ne sont pas simulés par des ports statiques. Les rappels sont
/// empruntés à la carte mère ; ce composant ne possède aucune ressource native.</remarks>
public sealed class AtomPpi8255
:RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Interfaces.IAtomPpi8255{
    private byte _control = C.ResetControl;
    private byte _portA;
    private byte _portB;
    private byte _portC;

    public Func<byte>? ReadPortA { get; set; }
    public Func<byte>? ReadPortB { get; set; }
    public Func<byte>? ReadPortC { get; set; }
    public Action<byte>? PortAPinsChanged { get; set; }
    public Action<byte>? PortBPinsChanged { get; set; }
    public Action<byte>? PortCPinsChanged { get; set; }

    private byte InputMaskC => (byte)(((_control & C.InputUpperC) != 0 ? C.UpperC : 0)
        | ((_control & C.InputLowerC) != 0 ? C.LowerC : 0));

    public void Reset()
    {
        _control = C.ResetControl;
        _portA = _portB = _portC = 0;
        PublishPins();
    }

    public byte Read(int register) => (register & C.RegisterMask) switch
    {
        C.PortA => (_control & C.InputA) != 0 ? ReadPortA?.Invoke() ?? byte.MaxValue : _portA,
        C.PortB => (_control & C.InputB) != 0 ? ReadPortB?.Invoke() ?? byte.MaxValue : _portB,
        C.PortC => (byte)((_portC & ~InputMaskC) | ((ReadPortC?.Invoke() ?? byte.MaxValue) & InputMaskC)),
        _ => byte.MaxValue
    };

    public void Write(int register, byte value)
    {
        switch (register & C.RegisterMask)
        {
            case C.PortA:
                _portA = value;
                PublishA();
                break;
            case C.PortB:
                _portB = value;
                PublishB();
                break;
            case C.PortC:
                _portC = value;
                PublishC();
                break;
            case C.Control when (value & C.ModeSet) != 0:
                if ((value & C.UnsupportedHandshakeModes) != 0) throw new NotSupportedException();
                _control = value;
                _portA = _portB = _portC = 0;
                PublishPins();
                break;
            case C.Control:
                var bit = (byte)(1 << ((value >> C.BitSelectionShift) & C.BitSelectionMask));
                _portC = (value & C.BitSet) != 0 ? (byte)(_portC | bit) : (byte)(_portC & ~bit);
                PublishC();
                break;
        }
    }

    private void PublishPins()
    {
        PublishA();
        PublishB();
        PublishC();
    }

    private void PublishA() => PortAPinsChanged?.Invoke((_control & C.InputA) != 0 ? byte.MaxValue : _portA);
    private void PublishB() => PortBPinsChanged?.Invoke((_control & C.InputB) != 0 ? byte.MaxValue : _portB);
    private void PublishC() => PortCPinsChanged?.Invoke((byte)(_portC | InputMaskC));
}
