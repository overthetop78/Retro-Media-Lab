using System.Buffers.Binary;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Constants.AtaConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Services;

internal sealed partial class AtaPio
{
    private void Identify()
    {
        var data = Current.Buffer.AsSpan(0, C.SectorBytes);
        data.Clear();
        var image = Current.Image!;
        var defaultHeads = image.Heads is > 0 and <= C.MaximumHeads ? image.Heads : 0;
        var trackBytes = (long)image.SectorsPerTrack * image.SectorSize;
        var defaultSectors = trackBytes > 0 && trackBytes % C.SectorBytes == 0
            && trackBytes / C.SectorBytes <= C.MaximumSectorsPerTrack ? (int)(trackBytes / C.SectorBytes) : 0;
        var defaultCylinders = defaultHeads > 0 && defaultSectors > 0
            ? Math.Min(C.MaximumCylinders, Capacity / defaultHeads / defaultSectors) : 0;
        PutWord(data, C.ConfigurationWord, C.FixedDrive);
        PutWord(data, C.CylindersWord, (ushort)defaultCylinders);
        PutWord(data, C.HeadsWord, (ushort)defaultHeads);
        PutWord(data, C.SectorsPerTrackWord, (ushort)defaultSectors);
        PutWord(data, C.BufferSizeWord, C.MaximumMultiple);
        PutWord(data, C.MultipleMaximumWord, C.MaximumMultiple);
        PutWord(data, C.CapabilitiesWord, C.LbaSupported);
        PutWord(data, C.MultipleCurrentWord, (ushort)(C.MultipleValid | Current.Multiple));
        PutCapacity(data, C.LbaCapacityWord, Capacity);
        if (Current.Heads > 0 && Current.Sectors > 0)
        {
            var cylinders = Math.Min(C.MaximumCylinders, Capacity / Current.Heads / Current.Sectors);
            PutWord(data, C.ValidGeometryWord, C.GeometryValid);
            PutWord(data, C.CurrentCylindersWord, (ushort)cylinders);
            PutWord(data, C.CurrentHeadsWord, (ushort)Current.Heads);
            PutWord(data, C.CurrentSectorsWord, (ushort)Current.Sectors);
            PutCapacity(data, C.CurrentCapacityWord, cylinders * Current.Heads * Current.Sectors);
        }
        PutText(data, C.FirmwareWord, C.FirmwareCharacters, C.FirmwareRevision);
        PutText(data, C.ModelWord, C.ModelCharacters, C.ModelName);
    }

    private static void PutWord(Span<byte> data, int word, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data[(word * C.WordBytes)..], value);

    private static void PutCapacity(Span<byte> data, int word, long sectors)
    {
        PutWord(data, word, (ushort)sectors);
        PutWord(data, word + 1, (ushort)(sectors >> C.WordBits));
    }

    private static void PutText(Span<byte> data, int word, int characters, string value)
    {
        var destination = data.Slice(word * C.WordBytes, characters);
        destination.Fill(C.Space);
        for (var index = 0; index < Math.Min(characters, value.Length); index++)
            destination[index ^ 1] = (byte)value[index];
    }
}
