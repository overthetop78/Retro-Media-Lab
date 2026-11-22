using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.MediaEngine.Images.Formats;

namespace RetroMediaLab.App.Functions.DiskImages;

internal sealed class DiskClassificationChoices
{
    private DiskClassificationCatalog _catalog = new([]);
    private IReadOnlySet<string> _detectedFormatIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, string> _detectedFormatByMachine = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal void SetCatalog(IEnumerable<DiskFormat> formats) => _catalog = new(formats);
    internal DiskFormat? ResolveFormat(string? formatId) => _catalog.ResolveFormat(formatId);

    internal DiskFormat? ApplyDetection(string? preferredId, IEnumerable<string> detectedIds)
    {
        var detected = DiskClassificationDetection.Resolve(_catalog, preferredId, detectedIds);
        _detectedFormatIds = detected.FormatIds;
        _detectedFormatByMachine = detected.FormatByMachine;
        return detected.PreferredFormat;
    }

    internal string? DetectedFormatFor(string? machine) =>
        machine is null ? null : _detectedFormatByMachine.GetValueOrDefault(machine);

    internal (DiskMachineChoice[] Choices, DiskMachineChoice? Selected) Machines(string? selectedMachine)
    {
        var choices = _catalog.Machines.Select(machine => new DiskMachineChoice(machine, _detectedFormatByMachine.ContainsKey(machine))).ToArray();
        var selected = choices.FirstOrDefault(item => item.DisplayName.Equals(selectedMachine, StringComparison.OrdinalIgnoreCase));
        return (choices, selected);
    }

    internal (DiskFormatChoice[] Choices, DiskFormatChoice? Selected) Formats(string? machine, string? selectedId)
    {
        var choices = _catalog.FormatsFor(machine).Select(format => new DiskFormatChoice(format, _detectedFormatIds.Contains(format.Id))).ToArray();
        var selected = choices.FirstOrDefault(item => item.Format.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
            ?? choices.FirstOrDefault(item => item.Format.IsCommon) ?? choices.FirstOrDefault();
        return (choices, selected);
    }

    internal (DiskProtection[] Choices, DiskProtection? Selected) Protections(string? machine, string? formatId, string? selectedId)
    {
        var compatible = _catalog.ProtectionsFor(machine, formatId);
        if (compatible.Count == 0) return ([], null);
        DiskProtection[] choices = [new(string.Empty, machine ?? string.Empty, new HashSet<string>(), ControlVisualConstants.EmptyValue), .. compatible];
        var selected = choices.FirstOrDefault(item => item.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase)) ?? choices[0];
        return (choices, selected);
    }
}
