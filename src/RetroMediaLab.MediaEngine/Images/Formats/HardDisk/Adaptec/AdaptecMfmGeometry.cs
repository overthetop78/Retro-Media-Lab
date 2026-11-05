using RetroMediaLab.MediaEngine.Contracts;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Géométrie logique enregistrée dans les identités physiques Adaptec, indépendante du conteneur.</summary>
internal readonly record struct AdaptecMfmGeometry(int SectorSize, int Cylinders, int Heads, int SectorsPerTrack)
{
    internal void WriteInitialParameters(Span<byte> flags, ushort reducedWriteCurrent = 0,
        byte landingZone = 0, byte stepCode = 0)
    {
        if (flags.Length < C.ParameterFlagCount) throw new ArgumentException(nameof(flags));
        if (Cylinders is < 1 or > C.MaximumCylinders || Heads is < 1 or > C.MaximumHeads
            || SectorSize is not (C.MinimumBlockSize or C.MediumBlockSize or C.MaximumBlockSize)
            || reducedWriteCurrent > C.MaximumReducedCurrentCylinder || stepCode > C.MaximumStepCode)
            throw new ArgumentOutOfRangeException();
        flags[..C.ParameterFlagCount].Clear();
        flags[0] = (byte)(SectorSize & C.DefectCountMask);
        flags[1] = (byte)(((SectorSize >> C.ParameterWordShift) & C.ParameterWordHighMask)
            | (stepCode << C.ParameterAuxiliaryShift));
        var lastCylinder = Cylinders - 1;
        var lastHead = Heads - 1;
        flags[C.ParameterCylinderOffset] = (byte)(lastCylinder & C.DefectCountMask);
        flags[C.ParameterCylinderOffset + 1] = (byte)((lastCylinder >> C.ParameterWordShift)
            | ((lastHead & C.ParameterAuxiliaryMask) << C.ParameterAuxiliaryShift));
        flags[C.ParameterHeadUpperOffset] = (byte)(reducedWriteCurrent & C.DefectCountMask);
        flags[C.ParameterHeadUpperOffset + 1] = (byte)((reducedWriteCurrent >> C.ParameterWordShift)
            | ((lastHead >> C.ParameterHeadUpperShift) << C.ParameterAuxiliaryShift));
        // The original firmware reuses the transformed reduced-current word for this pair.
        flags[C.ParameterDerivedOffset] = flags[C.ParameterHeadUpperOffset];
        flags[C.ParameterDerivedOffset + 1] = (byte)((flags[C.ParameterHeadUpperOffset + 1] & C.ParameterWordHighMask)
            | ((landingZone & C.LandingBeforeTrackZeroMask) != 0 ? C.LandingSignFlag : 0));
        flags[C.ParameterLandingOffset] = (byte)(landingZone & C.DefectCountMask);
    }

    internal static bool TryRead(IReadOnlyList<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)> fields,
        out AdaptecMfmGeometry geometry)
    {
        geometry = default;
        if (fields.Count is < C.ParameterFlagCount or > C.MaximumSectors) return false;
        var first = -1;
        ulong addresses = 0;
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.Id.Length != C.IdLength || field.Sector.HeaderIntegrityValid != true
                || (field.Id.Span[C.AddressOffset] & C.DefectiveAddressByteMask) != 0
                || field.Sector.Number < 0 || field.Sector.Number >= fields.Count) return false;
            var mask = 1UL << field.Sector.Number;
            if ((addresses & mask) != 0) return false;
            addresses |= mask;
            if (((field.Id.Span[C.FlagsOffset] & C.LastSectorFlag) != 0)
                != (field.Sector.Number == fields.Count - 1)) return false;
            if (field.Sector.Number == 0) first = index;
        }
        if (first < 0) return false;
        Span<byte> flags = stackalloc byte[C.ParameterFlagCount];
        for (var index = 0; index < flags.Length; index++)
            flags[index] = (byte)(fields[(first + index) % fields.Count].Id.Span[C.FlagsOffset] & C.DefectCountMask);
        var size = ReadWord(flags);
        var cylinders = ReadWord(flags[C.ParameterCylinderOffset..]) + 1;
        var heads = (flags[C.ParameterCylinderOffset + 1] >> C.ParameterAuxiliaryShift)
            | ((flags[C.ParameterHeadUpperOffset + 1] >> C.ParameterAuxiliaryShift) << C.ParameterHeadUpperShift);
        heads++;
        if (size is not (C.MinimumBlockSize or C.MediumBlockSize or C.MaximumBlockSize)
            || cylinders > C.MaximumCylinders || heads > C.MaximumHeads) return false;
        var validCount = size switch
        {
            C.MinimumBlockSize => fields.Count is C.SmallSequentialSectors or C.SmallInterleavedSectors,
            C.MediumBlockSize => fields.Count is C.MediumSequentialSectors or C.MediumInterleavedSectors,
            C.MaximumBlockSize => fields.Count == C.LargeSectors,
            _ => false
        };
        if (!validCount) return false;
        geometry = new(size, cylinders, heads, fields.Count);
        return true;
    }

    private static int ReadWord(ReadOnlySpan<byte> pair) => pair[0]
        | ((pair[1] & C.ParameterWordHighMask) << C.ParameterWordShift);
}
