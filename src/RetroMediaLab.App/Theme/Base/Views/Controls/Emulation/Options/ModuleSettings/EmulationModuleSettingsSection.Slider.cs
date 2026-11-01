using System.Windows;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection
{
    private FrameworkElement CreateSlider(EmulationSettingsField field) =>
        EmulationSettingsFieldControlFunctions.CreateSlider(field, choice => ChoiceView(choice, field.ValueUnitResourceKey).DisplayName, ExecuteUserChangeAsync);
}
