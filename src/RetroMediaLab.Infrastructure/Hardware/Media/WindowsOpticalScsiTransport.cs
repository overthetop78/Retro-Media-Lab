using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.Infrastructure.Hardware.Media;

/// <summary>Sends synchronous, read-only SCSI commands to a Windows optical drive.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsOpticalScsiTransport : IOpticalCdTrackReader, IDisposable
{
    private const uint ScsiPassThrough = 0x0004D004;
    private const int SenseLength = 32;
    private const int MaxTransferLength = 16 * 1024;
    private static readonly int HeaderLength = Marshal.SizeOf<Request>();
    private static readonly int CdbOffset = (int)Marshal.OffsetOf<Request>(nameof(Request.Cdb));
    private readonly SafeFileHandle handle;

    private WindowsOpticalScsiTransport(SafeFileHandle handle) => this.handle = handle;

    public static WindowsOpticalScsiTransport Open(string driveName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveName);
        string drive = driveName.TrimEnd('\\');
        if (drive.Length != 2 || !char.IsAsciiLetter(drive[0]) || drive[1] != ':' ||
            !DriveInfo.GetDrives().Any(candidate => candidate.DriveType == DriveType.CDRom &&
                candidate.Name.Equals(drive + "\\", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Select a Windows optical drive by its drive letter.", nameof(driveName));

        // Windows requires both handle access flags for SCSI pass-through even for data-in commands.
        var handle = File.OpenHandle(@"\\.\" + drive, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        return new WindowsOpticalScsiTransport(handle);
    }

    public byte[] Inquiry() => Read([0x12, 0, 0, 0, 36, 0], 36);

    public byte[] ReadCapacity() => Read([0x25, 0, 0, 0, 0, 0, 0, 0, 0, 0], 8);

    public byte[] ReadTableOfContents() => Read([0x43, 0, 0, 0, 0, 0, 0, 3, 36, 0], 804);

    public byte[] ReadBlocks(uint firstBlock, ushort blockCount, int bytesPerBlock)
    {
        ArgumentOutOfRangeException.ThrowIfZero(blockCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerBlock);
        Span<byte> cdb = [0x28, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        BinaryPrimitives.WriteUInt32BigEndian(cdb[2..], firstBlock);
        BinaryPrimitives.WriteUInt16BigEndian(cdb[7..], blockCount);
        int length = checked(blockCount * bytesPerBlock);
        byte[] data = Read(cdb, length);
        if (data.Length != length)
            throw new EndOfStreamException($"The drive returned an incomplete block transfer at LBA {firstBlock}.");
        return data;
    }

    public byte[] ReadRawCdSectors(uint firstBlock, byte blockCount, bool audio)
    {
        ArgumentOutOfRangeException.ThrowIfZero(blockCount);
        const int rawSectorSize = 2352;
        // MMC READ CD selects CD-DA user bytes or all components of the detected data sector type.
        Span<byte> cdb = [0xBE, audio ? (byte)0x04 : (byte)0x00, 0, 0, 0, 0, 0, 0, blockCount,
            audio ? (byte)0x10 : (byte)0xF8, 0, 0];
        BinaryPrimitives.WriteUInt32BigEndian(cdb[2..], firstBlock);
        int length = blockCount * rawSectorSize;
        byte[] data = Read(cdb, length);
        if (data.Length != length)
            throw new EndOfStreamException($"The drive returned an incomplete raw CD transfer at LBA {firstBlock}.");
        return data;
    }

    private byte[] Read(ReadOnlySpan<byte> cdb, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, MaxTransferLength);
        int dataOffset = HeaderLength + SenseLength;
        var packet = new byte[dataOffset + length];
        var request = new Request
        {
            Length = (ushort)HeaderLength,
            CdbLength = (byte)cdb.Length,
            SenseLength = SenseLength,
            DataDirection = 1,
            TransferLength = (uint)length,
            TimeoutSeconds = 10,
            DataOffset = (nuint)dataOffset,
            SenseOffset = (uint)HeaderLength
        };
        MemoryMarshal.Write(packet, in request);
        cdb.CopyTo(packet.AsSpan(CdbOffset));

        if (!DeviceIoControl(handle, ScsiPassThrough, packet, packet.Length, packet, packet.Length,
            out int transferred, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        request = MemoryMarshal.Read<Request>(packet);
        if (request.Status != 0)
        {
            string sense = Convert.ToHexString(packet.AsSpan(HeaderLength, Math.Min((int)request.SenseLength, SenseLength)));
            throw new IOException($"SCSI command 0x{cdb[0]:X2} returned status 0x{request.Status:X2}, sense {sense}.");
        }
        if (request.TransferLength > length || transferred < dataOffset + request.TransferLength)
            throw new IOException("The optical driver returned an invalid SCSI transfer length.");
        return packet.AsSpan(dataOffset, (int)request.TransferLength).ToArray();
    }

    public void Dispose() => handle.Dispose();

    [StructLayout(LayoutKind.Sequential)]
    private struct Request
    {
        public ushort Length;
        public byte Status;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseLength;
        public byte DataDirection;
        public uint TransferLength;
        public uint TimeoutSeconds;
        public nuint DataOffset;
        public uint SenseOffset;
        public uint Cdb;
        public uint CdbWord2;
        public uint CdbWord3;
        public uint CdbWord4;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint controlCode, [In] byte[] input, int inputSize,
        [Out] byte[] output, int outputSize, out int bytesReturned, IntPtr overlapped);
}
