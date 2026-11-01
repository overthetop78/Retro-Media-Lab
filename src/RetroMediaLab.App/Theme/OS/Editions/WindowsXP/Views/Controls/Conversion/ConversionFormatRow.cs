using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.ViewModels.Conversion;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>A newly constructed list entry; the selected item and selected output are separate states.</summary>
internal sealed class ConversionFormatRow : ListBoxItem, IDisposable
{
    private readonly CheckBox output = new();
    private Action<ConversionFormatPresentation>? request;
    private bool disposed;

    internal ConversionFormatRow(ConversionFormatPresentation presentation, Action<ConversionFormatPresentation> request)
    {
        Presentation = presentation;
        this.request = request;
        try
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            Padding = new Thickness(4, 3, 4, 3);
            output.Content = presentation.IsReconstructedFlux
                ? string.Join(" — ", presentation.Format.DisplayName, LocExtension.Get("Conversion.ReconstructedFlux"))
                : presentation.Format.DisplayName;
            output.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
            output.IsChecked = presentation.IsSelected;
            output.IsEnabled = presentation.IsCompatible;
            output.ToolTip = presentation.IsCompatible
                ? LocExtension.Get("Conversion.DefaultExtensionTip", presentation.Format.Extensions.First(item => item.IsDefault).Extension.ToUpperInvariant())
                : LocExtension.Get("Conversion.Incompatible", presentation.Format.DisplayName);
            ToolTipService.SetShowOnDisabled(output, true);
            output.Checked += OutputChanged;
            output.Unchecked += OutputChanged;
            PreviewMouseDown += RowMouseDown;
            Content = output;
        }
        catch { Dispose(); throw; }
    }

    internal ConversionFormatPresentation Presentation { get; }
    internal bool SelectedForConversion => output.IsChecked == true;

    private void RowMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton == MouseButton.Left) SetCurrentValue(IsSelectedProperty, true);
    }

    private void OutputChanged(object sender, RoutedEventArgs args)
    {
        if (!disposed) request?.Invoke(Presentation with { IsSelected = SelectedForConversion });
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            output.Checked -= OutputChanged;
            output.Unchecked -= OutputChanged;
            PreviewMouseDown -= RowMouseDown;
        }
        finally
        {
            request = null;
            BindingOperations.ClearAllBindings(output);
            output.Content = null;
            Content = null;
        }
    }
}
