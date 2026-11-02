
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;

/// <summary>Construit un modèle CPCEMU réinscriptible depuis une image sectorielle reconstruite.</summary>
public static class CpcDskImageBuilder
{
    /// <summary>Regroupe les secteurs par adresse physique et synthétise uniquement les champs absents du flux sectoriel.</summary>
    public static CpcDskImage Build(SectorImage image, CpcDskContainerKind kind)
    {
        if (image.Cylinders > CpcDskLayout.MaximumCylinderCount || image.Heads > CpcDskLayout.MaximumHeadCount) throw CpcDskExceptions.InvalidContainer("the geometry exceeds CPCEMU limits");
        var tracks = new List<CpcDskTrack>(checked(image.Cylinders * image.Heads));
        for (var cylinder = 0; cylinder < image.Cylinders; cylinder++)
        {
            for (var head = 0; head < image.Heads; head++)
            {
                var blocks = image.AvailableBlocks.Where(block => block.Address.Cylinder == cylinder && block.Address.Head == head)
                    .OrderBy(block => block.RecordedTrackOrder ?? block.LogicalBlock).ToArray();
                var sectors = blocks.Select(BuildSector).ToArray();
                var defaultSizeCode = sectors.Length == 0 ? (byte)0 : sectors.GroupBy(sector => sector.SizeCode).OrderByDescending(group => group.Count()).First().Key;
                tracks.Add(new(tracks.Count, sectors.Length > 0, checked((byte)cylinder), checked((byte)head), defaultSizeCode, CpcDskFormat.DefaultGap3Length, CpcDskFormat.DefaultFillerByte, sectors));
            }
        }
        return new(kind, checked((byte)image.Cylinders), checked((byte)image.Heads), tracks, image);
    }

    private static CpcDskSector BuildSector(SectorBlock block)
    {
        var sizeCode = block.RecordedSizeCode ?? (block.FormatCode is { } declared && NominalSize(declared) == block.Data.Count ? declared : GetSizeCode(block.Data.Count));
        var dataError = block.DataIntegrityValid == false || block.IntegrityValid == false && block.HeaderIntegrityValid != false;
        var status1 = block.HeaderIntegrityValid == false || dataError ? checked((byte)CpcDskLayout.DataErrorMask) : (byte)0;
        var status2 = (byte)((dataError ? CpcDskLayout.DataErrorMask : 0)
            | (block.DeletedDataMark == true ? CpcDskLayout.DeletedDataMarkMask : 0));
        var recorded = block.RecordedAddress ?? block.Address;
        return new(checked((byte)recorded.Cylinder), checked((byte)recorded.Head), checked((byte)recorded.Number), sizeCode, status1, status2, block.Data.ToArray());
    }

    private static byte GetSizeCode(int size)
    {
        for (byte code = 0; code <= CpcDskLayout.SectorSizeCodeMask; code++) if (NominalSize(code) == size) return code;
        throw CpcDskExceptions.InvalidContainer($"sector size {size} is not representable by a CPCEMU size code");
    }

    private static int NominalSize(byte sizeCode) => CpcDskLayout.MinimumSectorSize << (sizeCode & CpcDskLayout.SectorSizeCodeMask);
}
