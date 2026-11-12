using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Décodage interne MODE=1, TI MP010A §2.1.2–2.1.5, table 2-1.
/// Le VDP appelle ce bloc lorsque le transfert physique est effectivement capturé.
/// Il reste propriétaire de CSR/CSW, des délais, de l'adresse, du status et de la VRAM.</summary>
internal sealed class Tms9918CpuProtocol(Tms9918ControlLatchState initial)
{
    private byte _firstByte = initial.FirstByte;
    private bool _awaitingSecondByte = initial.AwaitingSecondByte;

    internal Tms9918ControlLatchState Capture() => new(_firstByte, _awaitingSecondByte);

    internal Tms9918ControlCommand AcceptControlByte(byte value)
    {
        if (!_awaitingSecondByte)
        {
            _firstByte = value;
            _awaitingSecondByte = true;
            return new(Tms9918ControlCommandKind.FirstByteLatched, value, 0, true);
        }
        _awaitingSecondByte = false;
        if ((value & 0x80) != 0)
        {
            // Les quatre bits entre le MSB et le numéro du registre doivent être
            // zéro. Conserver l'octet hors format et sa qualification : le cœur
            // ne doit pas le confondre silencieusement avec une opération validée.
            return new(Tms9918ControlCommandKind.WriteRegister, _firstByte, value, (value & 0x78) == 0);
        }
        return new((value & 0x40) != 0 ? Tms9918ControlCommandKind.SetWriteAddress
            : Tms9918ControlCommandKind.SetReadAddress, _firstByte, value, true);
    }

    // §2.1.2 : la lecture du status remet la logique d'interface dans l'état
    // qui attend le premier octet. La valeur historique du verrou n'est pas
    // artificiellement effacée ; le prochain premier octet la remplacera.
    internal void StatusReadAccepted() => _awaitingSecondByte = false;
}
