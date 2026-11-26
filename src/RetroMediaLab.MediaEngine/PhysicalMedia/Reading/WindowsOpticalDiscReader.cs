using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Reading;

/// <summary>Copies the ISO 9660 data volume accessible through a Windows optical drive.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsOpticalDiscReader
{
    private const int SectorSize = OpticalSectorConstants.Data2048Size;
    private const int FirstVolumeDescriptor = 16;
    private const int BufferSize = 64 * SectorSize;
    private const uint ReserveAndCommit = 0x3000;
    private const uint ReadWritePage = 0x04;
    private const uint ReleaseAllocation = 0x8000;
    private const uint AllowExtendedDasdIo = 0x00090083;
    private readonly IAtomicImageFileWriter imageWriter = new AtomicImageFileWriter();

    public IReadOnlyList<string> ListDrives() => DriveInfo.GetDrives()
        .Where(drive => drive.DriveType == DriveType.CDRom)
        .Select(drive => drive.Name)
        .ToArray();

    /// <summary>Copies the ISO volume, excluding any audio tracks and unaddressable areas of the disc.</summary>
    public Task ReadIsoVolumeAsync(string driveName, string destination, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        string drive = driveName.TrimEnd('\\');
        if (drive.Length != 2 || !char.IsAsciiLetter(drive[0]) || drive[1] != ':' ||
            !ListDrives().Contains(drive + "\\", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Select a Windows optical drive by its drive letter.", nameof(driveName));

        return imageWriter.WriteAsync(destination, (output, token) =>
            Task.Run(() => CopyVolume(drive, output, token), token), cancellationToken);
    }

    private static void CopyVolume(string drive, Stream output, CancellationToken cancellationToken)
    {
        using SafeFileHandle device = File.OpenHandle(@"\\.\" + drive, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite);
        if (!DeviceIoControl(device, AllowExtendedDasdIo, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
        {
            int error = Marshal.GetLastWin32Error();
            const int InvalidFunction = 1;
            const int NotSupported = 50;
            if (error is not (InvalidFunction or NotSupported))
                throw new Win32Exception(error);
        }
        // Direct volume reads require a suitably aligned buffer, even for some buffered handles.
        IntPtr aligned = VirtualAlloc(IntPtr.Zero, BufferSize, ReserveAndCommit, ReadWritePage);
        if (aligned == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            var buffer = new byte[BufferSize];
            Seek(device, FirstVolumeDescriptor * SectorSize);
            long volumeBytes = 0;
            for (int sector = FirstVolumeDescriptor; sector < FirstVolumeDescriptor + 256; sector++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadExact(device, aligned, buffer, SectorSize, sector);
                if (!buffer.AsSpan(1, 5).SequenceEqual("CD001"u8) || buffer[6] != 1 || buffer[0] == 255)
                    break;
                if (buffer[0] != 1)
                    continue;

                uint blockCount = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(80));
                if (blockCount <= sector || blockCount != BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(84)) ||
                    BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(128)) != SectorSize ||
                    BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(130)) != SectorSize)
                    throw new InvalidDataException("The ISO 9660 primary volume descriptor has an invalid block layout.");
                volumeBytes = (long)blockCount * SectorSize;
                break;
            }

            if (volumeBytes == 0)
                throw new InvalidDataException("The optical drive did not expose an ISO 9660 primary volume descriptor.");

            Seek(device, 0);
            for (long remaining = volumeBytes; remaining > 0;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int length = (int)Math.Min(remaining, BufferSize);
                ReadExact(device, aligned, buffer, length, (volumeBytes - remaining) / SectorSize);
                output.Write(buffer, 0, length);
                remaining -= length;
            }
        }
        finally
        {
            VirtualFree(aligned, 0, ReleaseAllocation);
        }
    }

    private static void Seek(SafeFileHandle device, long offset)
    {
        if (!SetFilePointerEx(device, offset, out _, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private static void ReadExact(SafeFileHandle device, IntPtr aligned, byte[] buffer, int length, long firstSector)
    {
        if (!ReadFile(device, aligned, length, out int read, IntPtr.Zero))
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            throw new IOException($"Could not read {length / SectorSize} optical sectors starting at {firstSector}: {error.Message}", error);
        }
        if (read != length)
            throw new EndOfStreamException("The optical drive returned fewer bytes than requested.");
        Marshal.Copy(aligned, buffer, 0, length);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr address, nuint size, uint allocationType, uint protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(IntPtr address, nuint size, uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFilePointerEx(SafeFileHandle file, long distance, out long position, uint origin);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadFile(SafeFileHandle file, IntPtr buffer, int length, out int read, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle file, uint controlCode, IntPtr input, uint inputSize,
        IntPtr output, uint outputSize, out uint bytesReturned, IntPtr overlapped);
}
