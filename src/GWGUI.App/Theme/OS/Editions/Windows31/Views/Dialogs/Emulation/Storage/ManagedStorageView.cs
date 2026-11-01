using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation.Contracts;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal sealed class ManagedStorageView : BusinessDialogView
{
    private readonly ManagedStorageConfigurationDialog source;
    private readonly List<StorageFieldsView> forms = [];
    private readonly List<(EmulationSettingsBlock Block, FrameworkElement View)> groups = [];

    internal ManagedStorageView(ManagedStorageConfigurationDialog source) : base(source)
    {
        this.source = source;
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(Command("Common.Cancel", Cancel), save);
            var body = new StackPanel();
            if (!string.IsNullOrWhiteSpace(source.PresentationExplanation))
                body.Children.Add(new TextBlock { Text = source.PresentationExplanation,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
            foreach (var group in source.PresentationGroups)
            {
                var form = new StorageFieldsView();
                forms.Add(form);
                foreach (var field in group.Block.Fields.Where(field => field.IsVisible))
                    if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                        form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule, field.LabelResourceKey));
                var container = new GroupBox { Header = group.Title, Content = form };
                if (group.Block.IsCollapsible)
                {
                    var reveal = new CheckBox { Content = group.Title, IsChecked = false };
                    container.Header = reveal;
                    form.SetBinding(VisibilityProperty, new Binding(nameof(CheckBox.IsChecked))
                        { Source = reveal, Converter = new BooleanToVisibilityConverter() });
                }
                container.Margin = new Thickness(0, 0, 0, 8);
                groups.Add((group.Block, container));
                body.Children.Add(container);
            }
            Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
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
