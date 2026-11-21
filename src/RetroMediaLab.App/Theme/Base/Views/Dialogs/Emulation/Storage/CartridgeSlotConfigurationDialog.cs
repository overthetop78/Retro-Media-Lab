using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
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
    internal CheckBox PresentationOption => _option;
    internal string? PresentationHint { get; }
    internal string? PresentationModule { get; }
    internal IReadOnlyList<EmulationSettingsField> PresentationFields { get; }
    internal IReadOnlyDictionary<string, ComboBox> PresentationControls => _options;

    public CartridgeSlotSettings Settings => new(_option.IsChecked == true, _options.ToDictionary(item => item.Key, item => ((EmulationSettingsChoiceView)item.Value.SelectedItem).Choice.Id, StringComparer.Ordinal));

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
        PresentationFields = configurationFields ?? [];

        var body = new StackPanel();
        if (!string.IsNullOrWhiteSpace(optionLabel)) body.Children.Add(_option);
        if (!string.IsNullOrWhiteSpace(optionHint)) body.Children.Add(StorageDialogUi.Info(optionHint));
        var selections = new StackPanel();
        body.Children.Add(selections);
        var values = new Dictionary<string, string>(settings.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        ShowFields();
        void ShowFields()
        {
            selections.Children.Clear();
            _options.Clear();
            var fields = describeOptions?.Invoke(values) ?? configurationFields ?? [];
            foreach (var field in fields)
                AddField(selections, field);
            foreach (var id in values.Keys.Where(id => !_options.ContainsKey(id)).ToArray())
                values.Remove(id);
        }

        void AddField(Panel parent, EmulationSettingsField field)
        {
            if (field.Editor != EmulationSettingsEditor.Selection || field.Choices is not { Count: > 0 })
                throw new ArgumentException(nameof(configurationFields));
            var choices = field.Choices.Select(choice => new EmulationSettingsChoiceView(choice, choice.InvariantDisplayValue ?? LocExtension.GetForModule(moduleId ?? string.Empty, choice.DisplayResourceKey))).ToArray();
            var value = values.TryGetValue(field.Id, out var configured) ? configured : field.Value;
            var control = new ComboBox { ItemsSource = choices,
                SelectedItem = choices.FirstOrDefault(choice => choice.Choice.Id == value) ?? choices[0] };
            _options[field.Id] = control;
            if (describeOptions is not null)
                control.SelectionChanged += (_, _) =>
                {
                    if (control.SelectedItem is not EmulationSettingsChoiceView selected) return;
                    values[field.Id] = selected.Choice.Id;
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
