using RetroMediaLab.MediaEngine.Constants;
using System.Collections.Frozen;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;

/// <summary>Disposition des images Winchester Acorn : secteurs DAT et descripteur SCSI DSC séparé.</summary>
public static class AcornWinchesterFormat
{
    internal const int SectorSize = 256;
    internal const int SectorsPerTrack = 33;
    internal const int DescriptorSize = 33;
    internal const int MinimumDescriptorSize = 22;
    internal const int BlockSizeOffset = 9;
    internal const int BlockSizeShift = 8;
    internal const int CylinderOffset = 13;
    internal const int HeadOffset = 15;
    internal const int ReducedWriteCurrentOffset = 16;
    internal const int LandingZoneOffset = 20;
    internal const int StepRateOffset = 21;
    internal const int ExtentLengthOffset = 3;
    internal const byte ExtentLength = 8;
    internal const int DriveListOffset = 12;
    internal const byte DriveListCode = 1;
    internal const int MediumSectorSize = 512;
    internal const int LargeSectorSize = 1024;
    internal const int SmallSequentialSectors = 32;
    internal const int MediumSequentialSectors = 17;
    internal const int MediumInterleavedSectors = 18;
    internal const int LargeSectors = 9;
    internal const int MaximumCylinders = 65535;
    internal const int MaximumHeads = 255;
    /// <summary>Clé du descripteur DSC binaire, encodé en Base64 dans les métadonnées de l'image.</summary>
    public const string DescriptorMetadataKey = "acorn.dsc";
    internal static readonly IReadOnlySet<string> Extensions = new[] { DiskImageFileExtensions.Dat }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    internal static readonly IReadOnlySet<string> AssociatedExtensions = new[] { DiskImageFileExtensions.Dsc }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Lit la géométrie DSC ; sa taille de bloc nulle historique signifie 256 octets.</summary>
    /// <remarks>L'interleave n'est pas conservé dans DSC. Une longueur DAT exactement séquentielle
    /// distingue cette disposition ; une image partielle conserve la disposition interleavée usuelle.</remarks>
    internal static HardDiskGeometry? Geometry(ReadOnlySpan<byte> descriptor, long? dataLength = null)
    {
        if (descriptor.Length < MinimumDescriptorSize) return null;
        var cylinders = BinaryPrimitives.ReadUInt16BigEndian(descriptor[CylinderOffset..]);
        var heads = descriptor[HeadOffset];
        var blockSize = BinaryPrimitives.ReadUInt32BigEndian(descriptor[BlockSizeOffset..]) >> BlockSizeShift;
        if (blockSize == 0) blockSize = SectorSize;
        var sectors = DefaultSectorsPerTrack((int)blockSize);
        if (cylinders == 0 || heads == 0 || sectors == 0) return null;
        var sequential = SequentialSectorsPerTrack((int)blockSize);
        if (dataLength == (long)cylinders * heads * sequential * blockSize) sectors = sequential;
        return new(cylinders, heads, sectors, (int)blockSize);
    }

    internal static bool SupportsGeometry(HardDiskGeometry geometry)
    {
        if (geometry.Cylinders > MaximumCylinders || geometry.Heads > MaximumHeads) return false;
        var sectors = DefaultSectorsPerTrack(geometry.SectorSize);
        return sectors != 0 && (geometry.SectorsPerTrack == sectors
            || geometry.SectorsPerTrack == SequentialSectorsPerTrack(geometry.SectorSize));
    }

    /// <summary>Projette les paramètres existants du DSC dans les premiers ID physiques Adaptec.</summary>
    internal static bool TryWritePhysicalParameters(ReadOnlySpan<byte> descriptor, long dataLength, Span<byte> flags)
    {
        if (flags.Length < AdaptecMfmFormat.ParameterFlagCount) throw new ArgumentException(nameof(flags));
        var geometry = Geometry(descriptor, dataLength);
        if (geometry is null || geometry.Cylinders > AdaptecMfmFormat.MaximumCylinders
            || geometry.Heads > AdaptecMfmFormat.MaximumHeads) return false;
        var current = BinaryPrimitives.ReadUInt16BigEndian(descriptor[ReducedWriteCurrentOffset..]);
        var step = descriptor[StepRateOffset];
        if (current > AdaptecMfmFormat.MaximumReducedCurrentCylinder || step > AdaptecMfmFormat.MaximumStepCode)
            return false;
        new AdaptecMfmGeometry(geometry.SectorSize, checked((int)geometry.Cylinders), geometry.Heads, geometry.SectorsPerTrack)
            .WriteInitialParameters(flags, current, descriptor[LandingZoneOffset], step);
        return true;
    }

    internal static void UpdateGeometryIfChanged(Span<byte> descriptor, HardDiskGeometry geometry, long dataLength)
    {
        var original = Geometry(descriptor, dataLength);
        if (original is not null && original.Cylinders == geometry.Cylinders && original.Heads == geometry.Heads
            && original.SectorsPerTrack == geometry.SectorsPerTrack && original.SectorSize == geometry.SectorSize)
            return;
        WriteGeometry(descriptor, geometry);
    }

    internal static void WriteGeometry(Span<byte> descriptor, HardDiskGeometry geometry)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(descriptor.Length, MinimumDescriptorSize);
        if (!SupportsGeometry(geometry)) throw new ArgumentOutOfRangeException(nameof(geometry));
        descriptor[ExtentLengthOffset] = ExtentLength;
        BinaryPrimitives.WriteUInt32BigEndian(descriptor[BlockSizeOffset..], (uint)geometry.SectorSize << BlockSizeShift | descriptor[DriveListOffset]);
        descriptor[DriveListOffset] = DriveListCode;
        BinaryPrimitives.WriteUInt16BigEndian(descriptor[CylinderOffset..], (ushort)geometry.Cylinders);
        descriptor[HeadOffset] = (byte)geometry.Heads;
    }

    private static int DefaultSectorsPerTrack(int blockSize) => blockSize switch
    {
        SectorSize => SectorsPerTrack,
        MediumSectorSize => MediumInterleavedSectors,
        LargeSectorSize => LargeSectors,
        _ => 0
    };

    private static int SequentialSectorsPerTrack(int blockSize) => blockSize switch
    {
        SectorSize => SmallSequentialSectors,
        MediumSectorSize => MediumSequentialSectors,
        LargeSectorSize => LargeSectors,
        _ => 0
    };
}
