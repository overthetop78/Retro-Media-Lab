namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants;

/// <summary>Registres et codes du contrôleur Intel 8271, indépendants de son adresse sur une carte.</summary>
public static class Intel8271Constants
{
    public const byte Busy = 0x80, ResultFull = 0x10, Interrupt = 0x08, NonDma = 0x04;
    public const byte LateDma = 0x0a, DataCrc = 0x0e, NotReady = 0x10, WriteProtected = 0x12, NotFound = 0x18;
    public const byte ReadSingle = 0x12, ReadMultiple = 0x13, ReadIncludingDeletedSingle = 0x16, ReadIncludingDeletedMultiple = 0x17;
    public const byte WriteSingle = 0x0a, WriteMultiple = 0x0b, ReadId = 0x1b, VerifySingle = 0x1e, VerifyMultiple = 0x1f;
    public const byte Format = 0x23, Seek = 0x29, DriveStatus = 0x2c, Specify = 0x35, WriteSpecial = 0x3a, ReadSpecial = 0x3d;
    public const byte Track0 = 0x12, Track1 = 0x1a, Mode = 0x17, DriveOutput = 0x23;
    public const int Drive0Select = 0x40, Drive1Select = 0x80, SideSelect = 0x20;
    public const int ParameterCapacity = 5, FormatIdSize = 4, FmBytesPerSecond = 15625;
    public const byte FormatFill = 0xe5;
}
