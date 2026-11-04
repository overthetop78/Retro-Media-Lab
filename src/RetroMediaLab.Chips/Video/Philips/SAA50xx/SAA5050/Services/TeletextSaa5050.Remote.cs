using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private bool _textMode;
    private bool _superimpose;
    private bool _reveal;
    private int _revealClocks;

    /// <summary>Mode texte distant, ou entrée SI maintenue au niveau bas par la carte.</summary>
    public bool TextMode => _textMode || !SuperimposeInputHigh;
    public bool Reveal => _reveal;
    public bool Superimpose => _superimpose;

    // Entrée décodée du récepteur série ; les bits sont dans l'ordre du tableau 2,
    // et non dans leur ordre de transmission b7, b1, b2, b3, b4, b5, b6.
    internal void ReceiveRemoteCommand(byte data)
    {
        data &= C.CharacterDataMask;
        _textMode = (data & C.RemoteTextBit) != 0;
        if (!_textMode || (data & C.RemoteViewdataMask) == C.RemoteViewdataMask)
            _superimpose = false;
        else if (data == C.RemoteSuperimpose)
            _superimpose = true;
        else if (data == C.RemoteTeletext)
            _superimpose = false;

        var reveal = data & C.RemoteRevealMask;
        _reveal = reveal is C.RemoteRevealTimed or C.RemoteRevealSet;
        _revealClocks = reveal == C.RemoteRevealTimed ? C.RevealCharacterClocks : 0;
        UpdateSerialOutputs();
    }

    // Horloge F1 ; une commande Reveal répétée réarme les 160 ms du tableau 2.
    internal void ClockRemoteTimer()
    {
        if (_revealClocks > 0 && --_revealClocks == 0) _reveal = false;
    }

    private void ResetRemoteControl()
    {
        _textMode = false;
        _superimpose = false;
        _reveal = false;
        _revealClocks = 0;
    }
}
