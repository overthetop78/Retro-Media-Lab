using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GWGUI.App.Functions.Storage;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class HardDiskSettingsView : BusinessDialogView
{
    private readonly List<StorageFieldsView> forms = [];
    private readonly List<SettingsFieldView> standalone = [];

    internal HardDiskSettingsView(HardDiskDriveConfigurationDialog source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(Forward(source.PresentationRemove), Command("Common.Cancel", Cancel), save);
            var body = new TabControl();
            var device = Information(source.PresentationInterface);
            device.Margin = new Thickness(0, 0, 0, 8);
            SetDock(device, Dock.Top);
            Children.Add(device);
            var modes = new TabControl();
            var existing = new StackPanel { Margin = new Thickness(8) };
            existing.Children.Add(LabelText(source.PresentationImageLabel));
            var path = new DockPanel { Margin = new Thickness(0, 5, 0, 8) };
            var browse = Forward(source.PresentationBrowse);
            browse.Margin = ActionMargin;
            SetDock(browse, Dock.Right);
            path.Children.Add(browse);
            path.Children.Add(Field(source.PresentationExistingPath));
            existing.Children.Add(path);
            existing.Children.Add(LabelText(source.PresentationExistingHint));
            if (source.PresentationDelete is { } delete)
            {
                var command = Forward(delete);
                command.Margin = new Thickness(0, 8, 0, 0);
                command.HorizontalAlignment = HorizontalAlignment.Left;
                existing.Children.Add(command);
            }
            var create = new StackPanel { Margin = new Thickness(8) };
            var image = Form();
            foreach (var entry in source.PresentationCreateFields) Add(image, entry.Label, entry.Control);
            create.Children.Add(image);
            if (source.PresentationFixedSize is null)
            {
                var custom = Form();
                Add(custom, LocExtension.Get("Emulation.Storage.Geometry.CustomSize"), source.PresentationCustomSize);
                Bindings.OneWay(source.PresentationCustomSizeContainer, VisibilityProperty, custom, VisibilityProperty);
                create.Children.Add(custom);
                var allocation = Form();
                Add(allocation, string.Empty, source.PresentationPreallocate);
                create.Children.Add(allocation);
                var limits = Information(source.PresentationLimits);
                limits.Margin = new Thickness(0, 4, 0, 8);
                create.Children.Add(limits);
            }
            else create.Children.Add(LabelText(StorageSizeFormatter.FormatCapacity(source.PresentationFixedSize.Value)));
            var directory = Information(source.PresentationDirectory);
            directory.Margin = new Thickness(0, 8, 0, 0);
            create.Children.Add(Label("Emulation.Storage.File.DestinationFolder"));
            create.Children.Add(directory);
            var contents = new FrameworkElement[] { existing, create };
            for (var index = 0; index < contents.Length; index++)
            {
                var original = (TabItem)source.PresentationMode.Items[index];
                var tab = new TabItem { Content = contents[index] };
                Bindings.OneWay(original, HeaderedContentControl.HeaderProperty, tab, HeaderedContentControl.HeaderProperty);
                modes.Items.Add(tab);
            }
            Bindings.TwoWay(source.PresentationMode, Selector.SelectedIndexProperty, modes, Selector.SelectedIndexProperty);
            body.Items.Add(Page(LocExtension.Get("Emulation.Storage.Media.Associated"), modes));
            if (source.PresentationFixedSize is null)
            {
                var geometry = Form();
                Add(geometry, string.Empty, source.PresentationAutomaticGeometry);
                foreach (var entry in source.PresentationGeometryFields) Add(geometry, entry.Label, entry.Control);
                geometry.Children.Add(Information(source.PresentationCapacity));
                body.Items.Add(Page(LocExtension.Get("Emulation.Tab.Advanced"), geometry));
            }
            foreach (var group in source.PresentationFields.Where(field => field.IsVisible).GroupBy(field => field.BlockId))
            {
                var options = Form();
                foreach (var field in group)
                    if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                        options.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
                var title = LocExtension.GetForModule(source.PresentationModule ?? string.Empty, group.Key);
                if (title == group.Key) title = LocExtension.Get("Emulation.Input.Behavior");
                body.Items.Add(Page(title, options));
            }
            Children.Add(body);
        }
        catch { Dispose(); throw; }
    }

    private static TabItem Page(string title, FrameworkElement content) => new()
    {
        Header = title,
        Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }
    };

    private static TextBlock LabelText(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    private TextBlock Information(TextBlock source)
    {
        var text = LabelText(string.Empty);
        Bindings.OneWay(source, TextBlock.TextProperty, text, TextBlock.TextProperty);
        Bindings.OneWay(source, VisibilityProperty, text, VisibilityProperty);
        Bindings.OneWay(source, ToolTipProperty, text, ToolTipProperty);
        return text;
    }

    private static EmulationSettingsField Definition(FrameworkElement endpoint) => new(endpoint.Name,
        EmulationMachineTab.Storage, string.Empty, string.Empty,
        endpoint is CheckBox ? EmulationSettingsEditor.Toggle : endpoint is TextBox ? EmulationSettingsEditor.Text : EmulationSettingsEditor.Selection, null);

    private void Add(StorageFieldsView form, string label, FrameworkElement endpoint) => form.AddField(Definition(endpoint), endpoint, label);

    private SettingsFieldView Field(FrameworkElement endpoint)
    {
        var field = new SettingsFieldView(Definition(endpoint), endpoint, () => Task.CompletedTask);
        standalone.Add(field);
        return field;
    }

    private StorageFieldsView Form()
    {
        var form = new StorageFieldsView();
        forms.Add(form);
        return form;
    }

    protected override void Detach()
    {
        Exception? failure = null;
        try
        {
            foreach (var item in forms.Cast<IDisposable>().Concat(standalone))
                try { if (item is FrameworkElement element) BindingOperations.ClearAllBindings(element); item.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { forms.Clear(); standalone.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
