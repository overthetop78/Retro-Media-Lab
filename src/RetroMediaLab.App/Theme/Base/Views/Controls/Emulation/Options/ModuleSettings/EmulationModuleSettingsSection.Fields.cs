using RetroMediaLab.App.Functions.Emulation.Settings;
using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Storage;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection
{
    private EmulationSettingsControlField CreateControlField(EmulationSettingsField field) =>
        new(
            LocExtension.GetForModule(_module, field.LabelResourceKey),
            CreateField(field),
            field.ExplanationResourceKey is null ? null : LocExtension.GetForModule(_module, field.ExplanationResourceKey),
            field.DetailedExplanationResourceKey is null ? null : LocExtension.GetForModule(_module, field.DetailedExplanationResourceKey),
            field);

    private FrameworkElement CreateField(EmulationSettingsField field)
    {
        FrameworkElement control = field.Editor switch
        {
            EmulationSettingsEditor.Selection => CreateSelection(field),
            EmulationSettingsEditor.Toggle => CreateToggle(field),
            EmulationSettingsEditor.Path => CreatePath(field),
            EmulationSettingsEditor.DirectoryPath => CreateDirectoryPath(field),
            EmulationSettingsEditor.OpenDirectory => CreateOpenDirectory(field),
            EmulationSettingsEditor.Slider => CreateSlider(field),
            EmulationSettingsEditor.Number => EmulationSettingsFieldControlFunctions.CreateNumber(field),
            EmulationSettingsEditor.IPv4 => EmulationSettingsFieldControlFunctions.CreateIpv4(field.Value ?? string.Empty),
            EmulationSettingsEditor.Information => new TextBlock
            {
                Text = EmulationSettingsValuePresentationFunctions.DisplayValue(field, _module),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            },
            _ => new TextBox
            {
                Text = field.Value ?? string.Empty, AcceptsReturn = field.IsMultiline,
                MinLines = field.IsMultiline ? 3 : 1, MaxLines = field.IsMultiline ? 5 : 1,
                TextWrapping = field.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                VerticalScrollBarVisibility = field.IsMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden
            }
        };
        control.IsEnabled = field.IsEnabled;
        if (field.Editor is (EmulationSettingsEditor.Text or EmulationSettingsEditor.Number
                or EmulationSettingsEditor.Percentage or EmulationSettingsEditor.IPv4) && control is TextBox input)
            input.LostKeyboardFocus += async (_, _) => await ExecuteUserChangeAsync();
        _fieldControls[field.Id] = control;
        return control;
    }

    private FrameworkElement CreateSelection(EmulationSettingsField field)
    {
        var control = EmulationSettingsFieldControlFunctions.CreateSelection(field, SelectionChoices(field));
        if (control is WrapPanel radios)
        {
            foreach (var radio in radios.Children.OfType<RadioButton>())
                radio.Checked += async (_, _) =>
                {
                    if (_applyingSettingsRule) return;
                    if (field.RefreshSettingsOnChange) RefreshSettingsDescription();
                    await ExecuteUserChangeAsync();
                };
            return radios;
        }
        var selection = (ComboBox)control;
        _userChangeHandlers[selection] = async () =>
        {
            if (field.RefreshSettingsOnChange)
            {
                CaptureEditorValues();
                RebuildEditor();
            }
            await ExecuteUserChangeAsync();
        };
        return selection;
    }

    private IReadOnlyList<EmulationSettingsChoiceView> SelectionChoices(EmulationSettingsField field) =>
        EmulationSettingsChoiceFunctions.GetChoices(field).Select(choice => ChoiceView(choice, field.ValueUnitResourceKey)).ToArray();

    private EmulationSettingsChoiceView ChoiceView(EmulationSettingsChoice choice, string? unitResourceKey = null) => new(choice,
        EmulationSettingsValuePresentationFunctions.WithUnit(choice.InvariantDisplayValue ?? LocExtension.GetForModule(_module, choice.DisplayResourceKey), unitResourceKey));

    private CheckBox CreateToggle(EmulationSettingsField field)
    {
        var toggle = EmulationSettingsFieldControlFunctions.CreateToggle(field);
        _userChangeHandlers[toggle] = async () =>
        {
            if (field.RefreshSettingsOnChange) RefreshSettingsDescription();
            await ExecuteUserChangeAsync();
        };
        return toggle;
    }

    private void RefreshSettingsDescription()
    {
        CaptureEditorValues();
        RebuildEditor();
    }

    private FrameworkElement CreatePath(EmulationSettingsField field)
    {
        if ((field.Tab == EmulationMachineTab.Rom || field.DefaultFolderCategory == EmulationDefaultFolderCategory.Firmware) && _firmwareManagement is not null)
            return _firmwareManagement.CreateField(field, () => ExecuteAsync(() => SelectFirmwareAsync(field)), ExecuteUserChangeAsync);
        var path = new TextBox { Text = field.Value ?? string.Empty };
        path.LostKeyboardFocus += async (_, _) => await ExecuteUserChangeAsync();
        var browse = new Button { Content = LocExtension.Get(ControlVisualConstants.BrowseResource), MinWidth = 90 };
        browse.Click += async (_, _) =>
        {
            var initial = FileSelectionFiles.InitialLocation(path.Text);
            var dialog = new OpenFileDialog
            {
                InitialDirectory = initial.Directory ?? DefaultFolder(field.DefaultFolderCategory),
                FileName = initial.FileName
            };
            if (WpfFileDialogService.ShowFileDialog(dialog, Window.GetWindow(this)) == true)
            {
                path.Text = dialog.FileName;
                await ExecuteUserChangeAsync();
            }
        };
        return PathRow(path, browse);
    }

    private async Task SelectFirmwareAsync(EmulationSettingsField field)
    {
        if (_firmwareManagement is null || _disposed) return;
        CaptureEditorValues();
        var selected = await _firmwareManagement.SelectAsync(Window.GetWindow(this), field, _showError);
        if (selected is null || _disposed) return;
        var previous = _configuration;
        SetConfiguration(selected);
        try { await ApplyUserChangeAsync(); }
        catch
        {
            SetConfiguration(previous);
            throw;
        }
    }

    private FrameworkElement CreateDirectoryPath(EmulationSettingsField field)
    {
        var path = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(field.Value) ? DefaultFolder(field.DefaultFolderCategory) : field.Value
        };
        path.LostKeyboardFocus += async (_, _) => await ExecuteUserChangeAsync();
        var browse = new Button { Content = LocExtension.Get(ControlVisualConstants.BrowseResource), MinWidth = 90 };
        browse.Click += async (_, _) =>
        {
            var dialog = new OpenFolderDialog { InitialDirectory = path.Text };
            if (WpfFileDialogService.ShowFolderDialog(dialog, Window.GetWindow(this)) == true)
            {
                path.Text = dialog.FolderName;
                await ExecuteUserChangeAsync();
            }
        };
        return PathRow(path, browse);
    }

    private FrameworkElement CreateOpenDirectory(EmulationSettingsField field)
    {
        var directory = DefaultFolder(field.DefaultFolderCategory);
        StoragePaths.EnsureDirectory(directory);
        var open = new Button
        {
            Content = LocExtension.Get("Common.OpenFolder"), Tag = directory,
            HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 140
        };
        open.Click += async (_, _) => await ExecuteAsync(() =>
        {
            StoragePaths.OpenDirectoryInExplorer(directory);
            return Task.CompletedTask;
        });
        return open;
    }

    private static Grid PathRow(TextBox path, Button browse)
    {
        var row = new Grid { Tag = path };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(path);
        Grid.SetColumn(browse, 1);
        row.Children.Add(browse);
        return row;
    }

    private string DefaultFolder(EmulationDefaultFolderCategory? category) =>
        StoragePaths.EmulationModuleDirectory(_module.Id, _configuration.MachineId, category,
            category == EmulationDefaultFolderCategory.Firmware ? _firmwareManagement?.GetFirmwareDirectory() : null);
}
