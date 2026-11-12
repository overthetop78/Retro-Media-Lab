namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants;

internal static class Tms9918StatusBits
{
    internal const byte Frame = 0x80, FifthSprite = 0x40, Coincidence = 0x20;
    internal const byte SpriteNumber = 0x1f;
}
