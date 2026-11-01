using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Storage;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal sealed class MediaCreationView : BusinessDialogView
{
    private readonly MediaCreationDialog source;
    private IDisposable? configuration;

    internal MediaCreationView(MediaCreationDialog source) : base(source)
    {
        this.source = source;
        try
        {
            var mount = Forward(source.PresentationMount);
            mount.IsDefault = true;
            Footer(mount, Forward(source.PresentationCreate), Forward(source.PresentationCancel));
            FrameworkElement editor;
            if (source.PresentationConfiguration is FloppyCreationSettingsView floppy)
            {
                var view = new FloppyCreationView(floppy);
                configuration = view;
                editor = view;
            }
            else if (source.PresentationConfiguration is TextBox path)
            {
                var view = new SettingsFieldView(new EmulationSettingsField(nameof(source.PresentationConfiguration),
                    EmulationMachineTab.Storage, string.Empty, string.Empty, EmulationSettingsEditor.Path, null), path, () => Task.CompletedTask);
                configuration = view;
                var body = new StackPanel();
                body.Children.Add(Label("Emulation.Storage.File.Destination"));
                view.Margin = new Thickness(0, 5, 0, 0);
                body.Children.Add(view);
                editor = body;
            }
            else throw new NotSupportedException(source.PresentationConfiguration.GetType().FullName);
            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            Bindings.OneWay(source.PresentationStatus, TextBlock.TextProperty, status, TextBlock.TextProperty);
            Bindings.OneWay(source.PresentationStatus, VisibilityProperty, status, VisibilityProperty);
            var content = new StackPanel();
            content.Children.Add(editor);
            content.Children.Add(status);
            Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    protected override void Cancel()
    {
        if (!source.PresentationBusy) base.Cancel();
    }

    protected override void Detach()
    {
        try { configuration?.Dispose(); }
        finally { configuration = null; }
    }
}
