using S = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants.Tms9918StatusBits;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Registre de status interne, TI MP010A §2.3. Les mutations sont appelées
/// par le balayage et l'interface aux phases réellement acceptées dans le VDP.</summary>
internal sealed class Tms9918Status
{
    private byte _value;
    private byte _spriteCounter;
    internal Tms9918Status(byte initial, byte? spriteCounter = null)
    {
        if (spriteCounter > S.SpriteNumber) throw new ArgumentOutOfRangeException(nameof(spriteCounter));
        _value = initial;
        _spriteCounter = spriteCounter ?? (byte)(initial & S.SpriteNumber);
    }
    internal byte Value => _value;
    internal byte SpriteCounter => _spriteCounter;
    internal bool HasValidFifthSpriteNumber => (_value & S.FifthSprite) != 0;
    internal bool InterruptAsserted(bool enabled) => enabled && (_value & S.Frame) != 0;

    internal void EndOfActiveDisplay() => _value |= S.Frame;
    internal void SpriteCoincidence() => _value |= S.Coincidence;

    // CanonV20, relevé hap16avril2007 : le compteur continue lors des scans
    // suivants même si5S fige le numéro publié. F n'inhibe pas le compteur.
    internal void SpriteCounterAdvanced(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, S.SpriteNumber);
        _spriteCounter = (byte)number;
        if ((_value & S.FifthSprite) == 0)
            _value = (byte)((_value & ~S.SpriteNumber) | number);
    }

    internal void FifthSpriteDetected(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, S.SpriteNumber);
        // §2.3.3 : le cinquième sprite ne déclenche pas INT et ne se verrouille
        // que lorsque F vaut zéro. Le premier résultat reste jusqu'à l'acquittement.
        if ((_value & (S.Frame | S.FifthSprite)) == 0)
        {
            _spriteCounter = (byte)number;
            _value = (byte)((_value & S.Coincidence) | S.FifthSprite | number);
        }
    }

    internal byte ReadAccepted()
    {
        byte before = _value;
        ClearFlags();
        return before;
    }

    // L'acquittement déverrouille le numéro : une seconde lecture retrouve
    // le compteur vivant, y compris durant vblank où il ne progresse plus.
    internal void ClearFlags() => _value = _spriteCounter;
}
