using System.Buffers;
using System.IO;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Emulation.Interfaces;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Adapte l'image empruntée aux octets du contrôleur sans transférer sa possession à Chips.</summary>
internal sealed class WinchesterStorage(IEmulationDiskImage image) : IScsiStorage
{
    private readonly IEmulationDiskImage _image = image ?? throw new ArgumentNullException(nameof(image));
    internal IEmulationDiskImage Image => _image;
    internal int Cylinder { get; private set; }
    internal int Head { get; private set; }
    private bool physicalSignals;
    internal void UsePhysicalSignals() => physicalSignals = true;
    public long CapacityBytes => checked(_image.SectorCount * _image.SectorSize);
    public long Cylinders => _image.Cylinders;
    public int Heads => _image.Heads;
    public int SectorsPerTrack => _image.SectorsPerTrack;
    public int SectorSize => _image.SectorSize;
    public bool IsReadOnly => _image.IsReadOnly;

    public bool Read(long byteOffset, Span<byte> destination)
    {
        CheckRange(byteOffset, destination.Length);
        var valid = true;
        while (!destination.IsEmpty)
        {
            var index = byteOffset / SectorSize;
            var offset = (int)(byteOffset % SectorSize);
            var length = Math.Min(destination.Length, SectorSize - offset);
            var sector = _image.ReadSectorAsync(index).GetAwaiter().GetResult();
            valid &= sector.IntegrityValid != false;
            if (sector.Data.Length != SectorSize) throw new InvalidDataException();
            sector.Data.Span.Slice(offset, length).CopyTo(destination);
            destination = destination[length..];
            byteOffset += length;
        }
        return valid;
    }

    public void Write(long byteOffset, ReadOnlySpan<byte> source)
    {
        CheckRange(byteOffset, source.Length);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        byte[]? scratch = null;
        try
        {
            while (!source.IsEmpty)
            {
                var index = byteOffset / SectorSize;
                var offset = (int)(byteOffset % SectorSize);
                var length = Math.Min(source.Length, SectorSize - offset);
                if (offset == 0 && length == SectorSize)
                {
                    _image.WriteSector(index, source[..length]);
                }
                else
                {
                    var sector = _image.ReadSectorAsync(index).GetAwaiter().GetResult();
                    if (sector.Data.Length != SectorSize) throw new InvalidDataException();
                    scratch ??= ArrayPool<byte>.Shared.Rent(SectorSize);
                    var data = scratch.AsSpan(0, SectorSize);
                    sector.Data.Span.CopyTo(data);
                    source[..length].CopyTo(data[offset..]);
                    _image.WriteSector(index, data);
                }
                source = source[length..];
                byteOffset += length;
            }
        }
        finally
        {
            if (scratch is not null) ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    public long Seek(long byteOffset)
    {
        if (physicalSignals) return Cylinder;
        var sector = byteOffset / SectorSize;
        if (Heads <= 0 || SectorsPerTrack <= 0)
        {
            _image.Mechanics.Seek(0, 0, logicalBlock: sector);
            return sector;
        }
        var cylinder = sector / Heads / SectorsPerTrack;
        Head = (int)(sector / SectorsPerTrack % Heads);
        Cylinder = checked((int)cylinder);
        _image.Mechanics.Seek(Cylinder, Head, (int)(sector % SectorsPerTrack));
        return cylinder;
    }

    /// <summary>Front STEP physique, distinct de la sélection ultérieure de l'adresse de transfert.</summary>
    internal void Step(bool inward)
    {
        Cylinder = checked(Cylinder + (inward ? 1 : -1));
        // Le contrat sonore accepte seulement des pistes de données non négatives ; la position
        // physique reste signée pour les zones de parking extérieures à la piste zéro.
        _image.Mechanics.Seek(Math.Max(0, Cylinder), Head);
    }

    public byte[]? ReadParameters() => _image.ReadDeviceParameters();

    public void PrepareFormat(long cylinders, int heads, int sectorsPerTrack, int sectorSize)
        => _image.PrepareBlockFormat(new(cylinders, heads, sectorsPerTrack, sectorSize));

    public void WriteParameters(ReadOnlySpan<byte> parameters) => _image.WriteDeviceParameters(parameters);

    public void Flush() => _image.FlushAsync().GetAwaiter().GetResult();

    private void CheckRange(long byteOffset, int length)
    {
        if (SectorSize <= 0 || byteOffset < 0 || byteOffset > CapacityBytes || length > CapacityBytes - byteOffset)
            throw new ArgumentOutOfRangeException(nameof(byteOffset));
    }
}
