using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PhysicalParameterConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Encodage des paramètres dans les drapeaux ID, sans dépendance à un format d'image.</summary>
public static class Acb4000PhysicalParameters
{
    /// <summary>Décode les drapeaux relus en 0379–03CC ; les octets absents ne sont pas remplacés par une géométrie fictive.</summary>
    public static bool TryRead(ReadOnlySpan<byte> flags, Acb4000DriveParameters previous,
        out Acb4000DriveParameters parameters)
    {
        parameters = previous;
        if (flags.Length != C.FlagCount) return false;
        foreach (var flag in flags)
            if (flag > C.FlagMask) return false;
        var heads = ReadAuxiliary(flags[C.CylinderOffset..])
            | (ReadAuxiliary(flags[C.ReducedCurrentOffset..]) << C.HeadUpperShift);
        var landing = flags[C.LandingOffset]
            | ((flags[C.DerivedOffset + 1] & C.LandingSignFlag) != 0 ? P.LandingBeforeTrackZeroMask : 0);
        var candidate = previous with
        {
            BlockSize = (ushort)ReadWord(flags[C.BlockSizeOffset..]),
            StepRate = (byte)ReadAuxiliary(flags[C.BlockSizeOffset..]),
            Cylinders = (ushort)(ReadWord(flags[C.CylinderOffset..]) + 1),
            Heads = (byte)(heads + 1),
            ReducedWriteCurrent = (ushort)ReadWord(flags[C.ReducedCurrentOffset..]),
            LandingZone = (byte)landing
        };
        if (!candidate.IsValid) return false;
        parameters = candidate;
        return true;
    }

    /// <summary>Reproduit les neuf octets construits en 0B68–0BAF et l'empaquetage de 0E55.</summary>
    public static void Write(Span<byte> flags, Acb4000DriveParameters parameters)
    {
        if (!parameters.IsValid) throw new ArgumentOutOfRangeException(nameof(parameters));
        if (flags.Length < C.FlagCount) throw new ArgumentException(nameof(flags));
        var headIndex = parameters.Heads - 1;
        WritePair(flags[C.BlockSizeOffset..], parameters.BlockSize, parameters.StepRate);
        WritePair(flags[C.CylinderOffset..], parameters.Cylinders - 1, headIndex);
        WritePair(flags[C.ReducedCurrentOffset..], parameters.ReducedWriteCurrent, headIndex >> C.HeadUpperShift);
        // 0B98–0BA5 reuses the transformed HL from the preceding pair. It does not reload 80AC.
        flags[C.DerivedOffset] = flags[C.ReducedCurrentOffset];
        flags[C.DerivedOffset + 1] = (byte)((flags[C.ReducedCurrentOffset + 1] & C.WordHighMask)
            | ((parameters.LandingZone & P.LandingBeforeTrackZeroMask) != 0 ? C.LandingSignFlag : 0));
        flags[C.LandingOffset] = (byte)(parameters.LandingZone & C.FlagMask);
    }

    private static void WritePair(Span<byte> destination, int value, int auxiliary)
    {
        destination[0] = (byte)(value & C.FlagMask);
        destination[1] = (byte)(((value >> C.PackedWordShift) & C.WordHighMask)
            | ((auxiliary & C.AuxiliaryMask) << C.AuxiliaryShift));
    }

    private static int ReadWord(ReadOnlySpan<byte> pair) => pair[0]
        | ((pair[1] & C.WordHighMask) << C.PackedWordShift);

    private static int ReadAuxiliary(ReadOnlySpan<byte> pair) => pair[1] >> C.AuxiliaryShift;
}
