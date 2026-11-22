using RetroMediaLab.App.Services.Audio;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Functions.Emulation.Settings;

internal static class EmulationSettingsChoiceFunctions
{
    internal static IReadOnlyList<EmulationSettingsChoice> GetChoices(EmulationSettingsField field)
    {
        IReadOnlyList<EmulationSettingsChoice> declared = field.Choices ?? [];
        if (field.ChoiceSource != EmulationSettingsChoiceSource.AudioOutputDevices) return declared;

        return declared.Concat(WasapiAudioOutput.GetOutputDevices()
                .Select(device => new EmulationSettingsChoice(device.Id, string.Empty, device.Name)))
            .DistinctBy(choice => choice.Id).ToArray();
    }
}
