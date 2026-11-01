using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class ManagedStorageView : BusinessDialogView
{
    private readonly ManagedStorageConfigurationDialog source;
    private readonly List<StorageFieldsView> forms = [];
    private readonly List<(EmulationSettingsBlock Block, FrameworkElement View)> groups = [];

    internal ManagedStorageView(ManagedStorageConfigurationDialog source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        this.source = source;
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(save, Command("Common.Cancel", Cancel));
            var body = new TabControl();
            if (!string.IsNullOrWhiteSpace(source.PresentationExplanation))
            {
                var explanation = new TextBlock { Text = source.PresentationExplanation,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
                SetDock(explanation, Dock.Top);
                Children.Add(explanation);
            }
            foreach (var group in source.PresentationGroups)
            {
                var form = new StorageFieldsView();
                forms.Add(form);
                foreach (var field in group.Block.Fields.Where(field => field.IsVisible))
                    if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                        form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule, field.LabelResourceKey));
                var container = new TabItem { Header = group.Title, Content = new ScrollViewer
                    { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
                groups.Add((group.Block, container));
                body.Items.Add(container);
            }
            Children.Add(body);
            source.PresentationChanged += Refresh;
            Refresh(this, EventArgs.Empty);
        }
        catch { Dispose(); throw; }
    }

    private void Refresh(object? sender, EventArgs args)
    {
        foreach (var group in groups)
            group.View.Visibility = source.PresentationVisibleGroups.Contains(group.Block) ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void Detach()
    {
        source.PresentationChanged -= Refresh;
        Exception? failure = null;
        try
        {
            foreach (var form in forms)
                try { BindingOperations.ClearAllBindings(form); form.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { forms.Clear(); groups.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
