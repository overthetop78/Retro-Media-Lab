using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Capacité choisie par FORMAT ; indépendante des conteneurs et de leur géométrie antérieure.</summary>
public readonly record struct Acb4000FormatGeometry
{
    public int SectorsPerTrack { get; }
    public byte Interleave { get; }
    public long BlockCount { get; }
    public long CapacityBytes { get; }

    private Acb4000FormatGeometry(Acb4000DriveParameters parameters, int sectorsPerTrack, byte interleave)
    {
        SectorsPerTrack = sectorsPerTrack;
        Interleave = interleave;
        BlockCount = (long)parameters.Cylinders * parameters.Heads * sectorsPerTrack;
        CapacityBytes = BlockCount * parameters.BlockSize;
    }

    public static bool TryCreate(Acb4000DriveParameters parameters, byte interleaveHigh, byte interleaveLow,
        out Acb4000FormatGeometry geometry)
    {
        geometry = default;
        if (!parameters.IsValid || interleaveHigh != C.RequiredInterleaveHighByte) return false;
        var interleave = interleaveLow == 0 ? C.DefaultInterleave : interleaveLow;
        var sequential = interleave == C.SequentialInterleave;
        var sectors = parameters.BlockSize switch
        {
            P.MinimumBlockSize => sequential ? C.SmallBlockSequentialSectors : C.SmallBlockInterleavedSectors,
            P.MediumBlockSize => sequential ? C.MediumBlockSequentialSectors : C.MediumBlockInterleavedSectors,
            P.MaximumBlockSize => C.LargeBlockSectors,
            _ => 0
        };
        if (interleave >= sectors) return false;
        geometry = new(parameters, sectors, interleave);
        return true;
    }
}
