using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Functions.DiskImages;
using System.Windows;
using System.Windows.Controls;


namespace RetroMediaLab.App.Views.Controls.Common;

public partial class DiskClassificationSelector : UserControl
{
    private readonly DiskClassificationChoices _choices = new();
    private bool _updating;

    public DiskClassificationSelector() => InitializeComponent();

    public event EventHandler? ValueChanged;
    public bool ShowAutomatic
    {
        get => Automatic.Visibility == Visibility.Visible;
        set => Automatic.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }
    public bool AutomaticDetection => Automatic.IsChecked == true;
    public string? SelectedMachine => (Machine.SelectedItem as DiskMachineChoice)?.DisplayName;
    public string? SelectedFormatId => (Format.SelectedItem as DiskFormatChoice)?.Format.Id;
    public string? SelectedProtectionId => Protection.SelectedItem is DiskProtection protection && !string.IsNullOrWhiteSpace(protection.Id) ? protection.Id : null;

    public void SetAutomaticDetection(bool enabled)
    {
        if (Automatic.IsChecked == enabled) return;
        _updating = true;
        Automatic.IsChecked = enabled;
        _updating = false;
    }

    public void SetCatalog(IEnumerable<DiskFormat> formats)
    {
        var machine = SelectedMachine;
        var format = SelectedFormatId;
        var protection = SelectedProtectionId;
        _choices.SetCatalog(formats);
        _updating = true;
        RefreshMachines(machine);
        RefreshFormats(format);
        RefreshProtections(protection);
        _updating = false;
    }

    public void SelectFormat(string formatId)
    {
        var format = _choices.ResolveFormat(formatId);
        if (format is null) return;
        _updating = true;
        try
        {
            RefreshMachines(format.Family);
            RefreshFormats(format.Id);
            RefreshProtections();
        }
        finally
        {
            _updating = false;
        }
    }

    public void ApplyDetection(string? detectedFormatId, string? detectedProtectionId) => ApplyDetection(detectedFormatId, detectedProtectionId, detectedFormatId is null ? [] : [detectedFormatId]);

    public void ApplyDetection(string? detectedFormatId, string? detectedProtectionId, IEnumerable<string> detectedFormatIds)
    {
        var format = _choices.ApplyDetection(detectedFormatId, detectedFormatIds);
        if (!AutomaticDetection)
        {
            _updating = true;
            RefreshMachines(SelectedMachine);
            RefreshFormats(SelectedFormatId);
            _updating = false;
            return;
        }

        _updating = true;
        RefreshMachines(format?.Family);
        if (format is null)
        {
            Format.ItemsSource = Array.Empty<DiskFormat>();
            Format.SelectedItem = null;
            Protection.ItemsSource = Array.Empty<DiskProtection>();
            Protection.SelectedItem = null;
            ProtectionPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            RefreshFormats(format.Id);
            RefreshProtections(detectedProtectionId);
        }
        _updating = false;
    }

    private void RefreshFormats(string? selectedId = null)
    {
        var (formats, selected) = _choices.Formats(SelectedMachine, selectedId);
        Format.ItemsSource = formats;
        Format.SelectedItem = selected;
    }

    private void RefreshMachines(string? selectedMachine = null)
    {
        var (machines, selected) = _choices.Machines(selectedMachine);
        Machine.ItemsSource = machines;
        Machine.SelectedItem = selected;
    }

    private void RefreshProtections(string? selectedId = null)
    {
        var (protections, selected) = _choices.Protections(SelectedMachine, SelectedFormatId, selectedId);
        Protection.ItemsSource = protections;
        Protection.SelectedItem = selected;
        ProtectionPanel.Visibility = protections.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Automatic_Changed(object sender, RoutedEventArgs e) { if (!_updating) ValueChanged?.Invoke(this, EventArgs.Empty); }
    private void Machine_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        var detected = _choices.DetectedFormatFor(SelectedMachine);
        _updating = true; RefreshFormats(detected); RefreshProtections(); _updating = false;
        if (SelectedFormatId is not null) ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Format_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        _updating = true; RefreshProtections(); _updating = false;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Protection_Changed(object sender, SelectionChangedEventArgs e) { if (!_updating) ValueChanged?.Invoke(this, EventArgs.Empty); }
}
