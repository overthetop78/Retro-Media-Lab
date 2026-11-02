using System.Buffers.Binary;
using System.Numerics;
using C = RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsLayout;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;

public static partial class AcornAdfsFormatter
{
    /// <summary>ADFS D : secteurs de 1024 octets, ancienne carte en unités de 256 octets et répertoire Nick.</summary>
    public static byte[] CreateDFormatHeader()
    {
        var bytes = new byte[C.DFormatHeaderSize];
        var units = C.ImageBlockCount * C.BlockSize / C.FileCoreUnitSize;
        Write24(bytes, 0, C.DFormatFirstFreeUnit);
        Write24(bytes, C.OldMapFreeLengthsOffset, units - C.DFormatFirstFreeUnit);
        Write24(bytes, C.OldMapSizeOffset, units);
        bytes[C.OldMapFreeEndOffset] = C.OldMapEntryBytes;
        bytes.AsSpan(C.OldMapFirstNameOffset, C.OldMapNameHalfBytes).Fill((byte)' ');
        bytes.AsSpan(C.OldMapSecondNameOffset, C.OldMapNameHalfBytes).Fill((byte)' ');
        bytes[C.FileCoreUnitSize - 1] = Checksum(bytes.AsSpan(0, C.FileCoreUnitSize));
        bytes[C.FileCoreUnitSize * 2 - 1] = Checksum(bytes.AsSpan(C.FileCoreUnitSize, C.FileCoreUnitSize));

        var directory = bytes.AsSpan(C.DFormatRootOffset, C.DirectorySize);
        C.NickSignature.CopyTo(directory[C.HeaderSignatureOffset..]);
        C.NickSignature.CopyTo(directory[C.FooterSignatureOffset..]);
        directory[C.TitleOffset] = (byte)'$';
        directory[C.DirectoryNameOffset] = (byte)'$';
        Write24(bytes, C.DFormatRootOffset + C.ParentAddressOffset, C.DFormatRootAddress);
        directory[C.CheckByteOffset] = EmptyDirectoryCheckByte(directory);
        return bytes;
    }

    /// <summary>PRM FileCore : XOR après ROR 13, sur le début utilisé et la queue, hors dernier mot.</summary>
    private static byte EmptyDirectoryCheckByte(ReadOnlySpan<byte> directory)
    {
        uint value = 0;
        var offset = 0;
        while (offset + C.CheckWordBytes <= C.EntriesOffset)
        {
            value = BitOperations.RotateRight(value, C.CheckWordRotation)
                ^ BinaryPrimitives.ReadUInt32LittleEndian(directory[offset..]);
            offset += C.CheckWordBytes;
        }
        while (offset < C.EntriesOffset)
            value = BitOperations.RotateRight(value, C.CheckWordRotation) ^ directory[offset++];
        offset = C.TailOffset;
        while (offset % C.CheckWordBytes != 0)
            value = BitOperations.RotateRight(value, C.CheckWordRotation) ^ directory[offset++];
        while (offset < C.DirectorySize - C.CheckWordBytes)
        {
            value = BitOperations.RotateRight(value, C.CheckWordRotation)
                ^ BinaryPrimitives.ReadUInt32LittleEndian(directory[offset..]);
            offset += C.CheckWordBytes;
        }
        return (byte)(value ^ value >> C.ByteBits ^ value >> (C.ByteBits * 2) ^ value >> (C.ByteBits * 3));
    }
}
