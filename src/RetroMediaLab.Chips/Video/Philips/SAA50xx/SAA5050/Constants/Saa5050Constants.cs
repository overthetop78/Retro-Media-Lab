namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants;

public static class Saa5050Constants
{
    public const int CharacterRomSize = 960;
    public const int FirstCharacter = 32;
    public const int CharacterCount = 96;
    public const int CharacterRows = 10;
    public const int RasterParities = 2;
    public const int CharacterDataMask = 127;
    public const int ColourMask = 7;
    public const int White = 7;
    public const int Black = 0;
    public const int GraphicsCharacterBit = 32;
    public const int FlashFields = 64;
    public const int FlashVisibleFields = 48;
    public const int DoubleHeightRasterDivisor = 4;
    public const int HalfCharacterRows = 5;
    public const int GlyphColumns = 5;
    public const int FirstMosaicBoundary = 3;
    public const int SecondMosaicBoundary = 7;
    public const int FirstSeparatorRow = 2;
    public const int SecondSeparatorRow = 6;
    public const int LastSeparatorRow = 9;
    public const int MosaicBottomRightBit = 6;
    public const int MosaicLeft = 0xfc0;
    public const int MosaicRight = 0x03f;
    public const int SeparatedMosaicLeft = 0x3c0;
    public const int SeparatedMosaicRight = 0x00f;
    public const int RemoteTextBit = 0x40;
    public const int RemoteViewdataMask = 0x60;
    public const int RemoteSuperimpose = 0x5e;
    public const int RemoteTeletext = 0x5f;
    public const int RemoteRevealMask = 0x5f;
    public const int RemoteRevealTimed = 0x46;
    public const int RemoteRevealSet = 0x47;
    public const int RevealCharacterClocks = 160000;
    public const int HalfDotsPerCharacter = 12;
    public const int PipelineCapacity = 8;
    public const int PipelineIndexMask = PipelineCapacity - 1;
    public const int GraphicsDelayHalfClocks = 32;
    public const int RemoteCommandBits = 7;
    public const int RemoteFirstBit = 6;
    public const int RedBit = 1;
    public const int GreenBit = 2;
    public const int BlueBit = 4;
}
