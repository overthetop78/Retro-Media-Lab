namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Champs de piste Adaptec ACB-4000, indépendants du conteneur DAT/DSC ou EMU.</summary>
public static class AdaptecMfmFormat
{
    /// <summary>Identifiant du codage des pistes, distinct du conteneur de fichier.</summary>
    public const string EncodingId = "adaptec.mfm";
    /// <summary>Cellule horloge/données à 10 MHz pour les données MFM à 5 Mbit/s.</summary>
    public const uint CellNanoseconds = 100;
    internal const int CellsPerByte = 16;
    internal const int BitsPerByte = 8;
    internal const ushort SyncWord = 0x4489;
    internal const int MinimumSyncBits = 64;
    internal const int CellsPerBit = 2;
    internal const byte SyncByte = 0xa1;
    internal const byte IdMark = 0xfe;
    internal const byte DataMark = 0xf8;
    internal const int IdLength = 10;
    internal const int IdContentLength = 6;
    internal const int AddressOffset = 2;
    internal const int AddressLength = 3;
    internal const int FlagsOffset = 5;
    internal const int DataOffset = 2;
    internal const int CheckLength = 4;
    internal const int DataOverhead = DataOffset + CheckLength;
    internal const int MinimumBlockSize = 256;
    internal const int MediumBlockSize = 512;
    internal const int MaximumBlockSize = 1024;
    internal const int MaximumSectors = 33;
    internal const uint AddressMask = 0x7fffff;
    internal const uint DefectiveMask = 0x800000;
    internal const byte DefectiveAddressByteMask = 0x80;
    internal const byte LastSectorFlag = 0x80;
    internal const byte DefectCountMask = 0x7f;
    internal const byte GapByte = 0x4e;
    internal const int FirstGapLength = 9;
    internal const int SequentialGapLength = 19;
    internal const int IdPreambleLength = 12;
    internal const int DataPreambleLength = 15;
    internal const int PostDataLength = 2;
    internal const int InterleavedRecordOverhead = 54;
    internal const int SequentialRecordOverhead = 64;
    internal const uint CheckPolynomial = 0x41044185;
    internal const uint CheckHighBit = 0x80000000;
    internal const int CheckHighByteShift = 24;
    internal const byte InvalidCheckMask = 1;
    internal const int DefaultInterleave = 2;
    internal const uint NominalIndexNanoseconds = 16_666_667;
    internal const int ParameterFlagCount = 9;
    internal const int ParameterWordShift = 7;
    internal const int ParameterAuxiliaryShift = 5;
    internal const int ParameterHeadUpperShift = 2;
    internal const byte ParameterWordHighMask = 0x1f;
    internal const byte ParameterAuxiliaryMask = 3;
    internal const int ParameterCylinderOffset = 2;
    internal const int ParameterHeadUpperOffset = 4;
    internal const int ParameterDerivedOffset = 6;
    internal const int ParameterLandingOffset = 8;
    internal const int MaximumReducedCurrentCylinder = 2047;
    internal const byte MaximumStepCode = 2;
    internal const byte LandingBeforeTrackZeroMask = 0x80;
    internal const byte LandingSignFlag = 0x40;
    internal const int MaximumCylinders = 2048;
    internal const int MaximumHeads = 16;
    internal const int SmallSequentialSectors = 32;
    internal const int SmallInterleavedSectors = 33;
    internal const int MediumSequentialSectors = 17;
    internal const int MediumInterleavedSectors = 18;
    internal const int LargeSectors = 9;
}
