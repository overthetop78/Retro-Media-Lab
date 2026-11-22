using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Controllers.Emulation.Storage;
using RetroMediaLab.App.Functions.Views.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;

namespace RetroMediaLab.App.Views.Dialogs.Emulation.Storage;

public sealed class CartridgeSlotConfigurationDialog : Window
{
    private readonly CheckBox _option = new();
    private readonly Dictionary<string, ComboBox> _options = new(StringComparer.Ordinal);
    private readonly CartridgeConfigurationSession _session;
    internal CheckBox PresentationOption => _option;
    internal string? PresentationHint { get; }
    internal string? PresentationModule { get; }
    internal IReadOnlyList<EmulationSettingsField> PresentationFields { get; private set; } = [];
    internal IReadOnlyDictionary<string, ComboBox> PresentationControls => _options;
    internal event EventHandler? PresentationFieldsChanged;

    public CartridgeSlotSettings Settings => _session.Capture(_option.IsChecked == true);

    public CartridgeSlotConfigurationDialog(string identifier, string machineName,
        CartridgeSlotSettings settings, string optionLabel, string? optionHint,
        string? moduleId = null, IReadOnlyList<EmulationSettingsField>? configurationFields = null,
        Func<IReadOnlyDictionary<string, string>, IReadOnlyList<EmulationSettingsField>>? describeOptions = null)
    {
        Title = $"{LocExtension.Get(EmulationResourceKeys.StorageDeviceConfigure)} {identifier}";
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = 720;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        _option.Content = optionLabel;
        _option.IsChecked = settings.OptionEnabled;
        _option.Margin = new Thickness(0, 4, 0, 4);
        PresentationHint = optionHint;
        PresentationModule = moduleId;
        _session = new CartridgeConfigurationSession(settings, configurationFields, describeOptions);

        var body = new StackPanel();
        if (!string.IsNullOrWhiteSpace(optionLabel)) body.Children.Add(_option);
        if (!string.IsNullOrWhiteSpace(optionHint)) body.Children.Add(StorageDialogUi.Info(optionHint));
        var selections = new StackPanel();
        body.Children.Add(selections);
        ShowFields();
        void ShowFields()
        {
            selections.Children.Clear();
            _options.Clear();
            var fields = _session.Describe();
            foreach (var (field, choice) in fields)
                AddField(selections, field, choice);
            PresentationFields = fields.Select(item => item.Field).ToArray();
            PresentationFieldsChanged?.Invoke(this, EventArgs.Empty);
        }

        void AddField(Panel parent, EmulationSettingsField field, EmulationSettingsChoice selection)
        {
            var choices = field.Choices!.Select(choice => new EmulationSettingsChoiceView(choice, choice.InvariantDisplayValue ?? LocExtension.GetForModule(moduleId ?? string.Empty, choice.DisplayResourceKey))).ToArray();
            var control = new ComboBox { ItemsSource = choices,
                SelectedItem = choices.First(choice => ReferenceEquals(choice.Choice, selection)) };
            _options[field.Id] = control;
            control.SelectionChanged += (_, _) =>
            {
                if (!_options.TryGetValue(field.Id, out var active) || !ReferenceEquals(active, control)
                    || control.SelectedItem is not EmulationSettingsChoiceView selected) return;
                _session.Select(field.Id, selected.Choice.Id);
                if (!_session.RefreshesOnSelection) return;
                ShowFields();
                if (_options.TryGetValue(field.Id, out var replacement)) replacement.Focus();
            };
            parent.Children.Add(EmulationSettingsLayout.BoundedForm(1, new EmulationSettingsControlField(
                LocExtension.GetForModule(moduleId ?? string.Empty, field.LabelResourceKey), control,
                field.ExplanationResourceKey is null ? null : LocExtension.GetForModule(moduleId ?? string.Empty, field.ExplanationResourceKey),
                field.DetailedExplanationResourceKey is null ? null : LocExtension.GetForModule(moduleId ?? string.Empty, field.DetailedExplanationResourceKey))));
        }
        Content = StorageDialogUi.DialogLayout(
            StorageDialogUi.DialogHeader(IconGlyphs.Controller, Title, $"{LocExtension.Get(EmulationResourceKeys.CartridgeDevice)} · {machineName}"),
            StorageDialogUi.Card(LocExtension.Get(EmulationResourceKeys.CartridgeDevice), body),
            StorageDialogUi.Footer(this, LocExtension.Get("Common.Save")));
    }
}
