using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.Orbit.Views;

internal partial class FirmwareSelectionView : UserControl, IDisposable
{
    private readonly FirmwareSelectionDialog source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private OrbitResources? resourceScope;

    internal FirmwareSelectionView(FirmwareSelectionDialog source, bool? dark = null)
    {
        this.source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            InitializeComponent();
            Destination.Text = source.PresentationDestination;
            bindings.Action(source.PresentationSave, Save);
            bindings.Action(source.PresentationRefresh, Refresh);
            bindings.Action(source.PresentationOpen, Open);
            bindings.OneWay(source.PresentationPath, TextBlock.TextProperty, PathValue, TextBox.TextProperty);
            bindings.OneWay(source.PresentationPath, ToolTipProperty, PathValue, ToolTipProperty);
            bindings.OneWay(source.PresentationEmpty, TextBlock.TextProperty, Empty, TextBlock.TextProperty);
            bindings.OneWay(source.PresentationEmpty, VisibilityProperty, Empty, VisibilityProperty);
            source.PresentationChanged += OnChanged;
            Cancel.Click += OnCancel;
            Loaded += OnLoaded;
            Rebuild();
            bindings.TwoWay(source.PresentationList, Selector.SelectedIndexProperty, Candidates, Selector.SelectedIndexProperty);
        }
        catch { Dispose(); throw; }
    }

    private async void OnLoaded(object sender, RoutedEventArgs args) => await source.RefreshAsync();
    private void OnChanged(object? sender, EventArgs args) => Rebuild();
    private void OnCancel(object sender, RoutedEventArgs args) => source.RequestPresentationClose();

    private void Rebuild()
    {
        var selected = source.PresentationList.SelectedIndex;
        Candidates.ItemsSource = source.PresentationCandidates.Select(candidate => new Row(
            string.IsNullOrWhiteSpace(candidate.Version) || candidate.DisplayName.Contains(candidate.Version, StringComparison.OrdinalIgnoreCase)
                ? candidate.DisplayName : $"{candidate.DisplayName} — {candidate.Version}", Path.GetFileName(candidate.Path), candidate.Path,
            LocExtension.Get(candidate.Compatibility switch
            {
                EmulationFirmwareCompatibility.Official => "Emulation.Firmware.Official",
                EmulationFirmwareCompatibility.Compatible => "Emulation.Cpu.Compatibility.Compatible",
                EmulationFirmwareCompatibility.PartiallyCompatible => "Emulation.Firmware.PartiallyCompatible",
                EmulationFirmwareCompatibility.Incompatible => "Emulation.Firmware.Incompatible",
                _ => "Common.Unknown"
            }), source.DestinationLabel(candidate))).ToArray();
        Candidates.SelectedIndex = selected;
    }

    private sealed record Row(string Name, string File, string Path, string Compatibility, string Destination);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            source.PresentationChanged -= OnChanged;
            if (Cancel is not null) Cancel.Click -= OnCancel;
            Loaded -= OnLoaded;
            if (Candidates is not null) Candidates.ItemsSource = null;
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
