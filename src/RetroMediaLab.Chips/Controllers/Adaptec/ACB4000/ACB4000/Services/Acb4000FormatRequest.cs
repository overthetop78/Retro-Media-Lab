using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using O = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000CommandOptionConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Options de la commande FORMAT UNIT, OEM 6.3.1.4, sans effet sur le support.</summary>
public readonly record struct Acb4000FormatRequest
{
    public int Unit { get; }
    public byte Fill { get; }
    public bool HasDefectList { get; }
    public Acb4000FormatGeometry Geometry { get; }

    private Acb4000FormatRequest(int unit, byte fill, bool defects, Acb4000FormatGeometry geometry)
    {
        Unit = unit;
        Fill = fill;
        HasDefectList = defects;
        Geometry = geometry;
    }

    public static bool TryDecode(ReadOnlySpan<byte> command, Acb4000DriveParameters parameters,
        out Acb4000FormatRequest request, bool expandedCommandSet = false)
    {
        request = default;
        var controlMask = expandedCommandSet ? O.LinkedControlMask : byte.MaxValue;
        if (command.Length != C.CommandLength || command[0] != C.Opcode || (command[C.ControlOffset] & controlMask) != 0)
            return false;
        var options = command[C.OptionsOffset] & C.OptionsMask;
        var defects = (options & C.DefectDataMask) != 0;
        var complete = (options & C.CompleteDefectsMask) != 0;
        var extended = (options & C.ExtendedOptionsMask) != 0;
        var uniqueFill = (options & C.UniqueFillMask) != 0;
        if ((options & C.DefectEncodingMask) != 0 || defects != complete
            || (defects || uniqueFill) && !extended) return false;
        if (!Acb4000FormatGeometry.TryCreate(parameters, command[C.InterleaveHighOffset],
            command[C.InterleaveLowOffset], out var geometry)) return false;
        request = new(command[C.OptionsOffset] >> C.UnitShift,
            uniqueFill ? command[C.FillOffset] : C.DefaultFill, defects, geometry);
        return true;
    }
}
