using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Controllers.Emulation.Input;
using GWGUI.App.Enums.Input;
using GWGUI.App.Services.Input.GameInput;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Emulation.Input;
using GWGUI.Emulation.Contracts;
using GWGUI.Emulation.Enums;
using GWGUI.Emulation.Interfaces;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ModuleInputTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task KeyboardAndMouseKeepTheirBindingsAndReleaseCaptureWhenThePageCloses() => sta.Run(() =>
    {
        var manager = new Manager();
        var controller = manager.CreateController();
        using var keyboard = View(EmulationMachineTab.Keyboard, controller);
        var editor = Assert.IsType<InputBindingsView>(((ContentControl)keyboard.FindName("EditorHost")).Content);
        var source = controller.PresentationKeyboard!;
        source.ConfigureCaptureSources(InputCaptureSources.Keyboard);
        editor.RequestCapture("action");
        Assert.True(source.CaptureKeyboardKeyDown(Key.F6, ModifierKeys.None));
        Assert.True(source.CaptureKeyboardKeyUp(Key.F6));
        controller.Apply(manager.Configuration);
        Assert.Equal("F6", manager.Applied!.Keyboard!.Values["action"]);
        editor.RequestCapture("action");
        keyboard.Dispose();
        Assert.False(source.CaptureKeyboardKeyDown(Key.F7, ModifierKeys.None));
        Assert.Null(((ListBox)editor.FindName("Rows")).ItemsSource);
        using var mouse = View(EmulationMachineTab.Mouse, controller);
        Assert.Same(controller.PresentationMouse!.Rows,
            ((ListBox)((InputBindingsView)((ContentControl)mouse.FindName("EditorHost")).Content).FindName("Rows")).ItemsSource);
        controller.PresentationMouse.RequestClear(controller.PresentationMouse.Rows.Single());
        controller.Apply(manager.Configuration);
        Assert.Empty(manager.Applied!.Mouse!.Values["action"]);
    });

    [Fact]
    public Task PortsChoicesAndScopedFieldsFollowTheExistingControllerAndPersistItsValues() => sta.Run(() =>
    {
        var manager = new Manager();
        var controller = manager.CreateController();
        var rate = new Slider { Minimum = 0, Maximum = 100, Value = 10 };
        var definition = new EmulationSettingsField("rate", EmulationMachineTab.Controllers, "controller", "Controllers.StickDeadZone",
            EmulationSettingsEditor.Slider, "10", AppliesToControllerIds: ["stick"], ControllerPortNumber: 2);
        using var view = new ModuleInputView(EmulationMachineTab.Controllers, controller, [definition],
            new Dictionary<string, FrameworkElement> { [definition.Id] = rate }, _ => "Rate", () => Task.CompletedTask);
        var ports = (ComboBox)view.FindName("Ports");
        Assert.Equal(2, ports.Items.Count);
        var first = (ControllerPortView)((ContentControl)view.FindName("EditorHost")).Content;
        Assert.Empty(((StackPanel)first.FindName("Settings")).Children);
        ports.SelectedIndex = 1;
        Assert.Null(first.Content);
        var second = (ControllerPortView)((ContentControl)view.FindName("EditorHost")).Content;
        Assert.Single(((StackPanel)second.FindName("Settings")).Children);
        var choices = (ComboBox)second.FindName("Type");
        choices.SelectedIndex = 0;
        Assert.Equal("none", controller.PresentationPorts[1].SelectedControllerChoice!.Id);
        Assert.Empty(((StackPanel)second.FindName("Settings")).Children);
        Assert.Equal(Visibility.Collapsed, ((InputBindingsView)((ContentControl)second.FindName("BindingsHost")).Content).Visibility);
        choices.SelectedIndex = 1;
        Assert.Single(((StackPanel)second.FindName("Settings")).Children);
        var bindings = (InputBindingsView)((ContentControl)second.FindName("BindingsHost")).Content;
        var source = controller.PresentationPorts[1].Bindings;
        source.ConfigureCaptureSources(InputCaptureSources.Keyboard);
        bindings.RequestCapture("action");
        Assert.True(source.CaptureKeyboardKeyDown(Key.F8, ModifierKeys.None));
        Assert.True(source.CaptureKeyboardKeyUp(Key.F8));
        controller.Apply(manager.Configuration);
        Assert.Equal("Keyboard:F8", manager.Applied!.ControllerPorts[1].Bindings.Values["action"]);
        Assert.Equal("stick", manager.Applied.ControllerPorts[1].SelectedControllerId);
        bindings.RequestCapture("action");
        view.Dispose();
        Assert.False(source.CaptureKeyboardKeyDown(Key.F9, ModifierKeys.None));
        Assert.Null(choices.ItemsSource);
        Assert.Null(ports.ItemsSource);
    });

    [Fact]
    public Task DrawingUsesExistingArtworkAndRestoresOwnershipWhenDetached() => sta.Run(() =>
    {
        var editor = new InputBindingEditor();
        var artwork = new System.Windows.Media.DrawingImage();
        var profile = new GWGUI.App.Contracts.Input.ControllerArtworkProfile("memory", artwork, []);
        var original = new GWGUI.App.Views.Controls.Options.ControllerVisualizer { ArtworkProfile = profile };
        using var view = new InputDrawingView(original, editor, _ => throw new InvalidOperationException());
        var drawing = Assert.IsType<GWGUI.App.Views.Controls.Options.ControllerVisualizer>(view.Content);
        Assert.Same(profile, drawing.ArtworkProfile);
        original.Visibility = Visibility.Collapsed;
        Assert.Equal(Visibility.Collapsed, view.Visibility);
        original.VisualCommandIds = new Dictionary<EmulationControllerVisualControl, string> { [EmulationControllerVisualControl.PrimaryAction] = "action" };
        Assert.Same(original.VisualCommandIds, drawing.VisualCommandIds);
        view.Dispose();
        Assert.Same(profile, original.ArtworkProfile);
        original.Visibility = Visibility.Visible;
        Assert.Equal(Visibility.Collapsed, view.Visibility);
        Assert.Null(drawing.ArtworkProfile);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task OutputRowsKeepExistingAssignmentsAndDetachAfterDefinitionsChange() => sta.Run(() =>
    {
        var reads = 0;
        var physical = ControlledDependencies.Simulate<IGameInputControllerSource>((method, _) => method.Name switch
        {
            "StartMonitoring" => null,
            "GetConnectedDevices" => Devices(),
            _ => throw new InvalidOperationException(method.Name)
        });
        IReadOnlyList<GameInputDeviceDescriptor> Devices() { reads++; return []; }
        var source = new EmulationControllerOutputEditor(physical);
        source.SetBindings(new Dictionary<string, EmulationControllerOutputBinding> { ["motor"] = new("memory-device", EmulationControllerMotor.LowFrequency) });
        source.SetDefinitions([new("motor", "Controllers.Rumble")], null);
        using var view = new ControllerOutputsView(source);
        var row = Assert.IsType<Grid>(Assert.Single(view.Children.Cast<UIElement>()));
        var selector = row.Children.OfType<ComboBox>().Single();
        Assert.Equal(2, selector.Items.Count);
        Assert.Single(source.Values);
        var endpoint = Assert.Single(source.PresentationRows);
        endpoint.Refresh();
        Assert.Equal(2, reads);
        selector.SelectedIndex = 0;
        Assert.Empty(source.Values);
        source.SetDefinitions([], null);
        Assert.Empty(view.Children);
        Assert.Null(selector.ItemsSource);
        Assert.Equal(Visibility.Collapsed, view.Visibility);
        view.Dispose();
        source.SetDefinitions([new("motor", "Controllers.Rumble")], null);
        Assert.Empty(view.Children);
    });

    private static ModuleInputView View(EmulationMachineTab tab, EmulationInputSettingsController source) => new(tab, source, [],
        new Dictionary<string, FrameworkElement>(), _ => throw new InvalidOperationException(), () => Task.CompletedTask);

    internal sealed class Manager
    {
        internal readonly Windows31EmulationViewTests.Configuration Configuration = new("input-view", Guid.NewGuid(), "machine");
        internal EmulationInputSettings? Applied;
        internal EmulationInputSettingsController CreateController(IReadOnlyList<string>? visuals = null)
        {
            InputBindingDefinition[] definitions = [new("action", "Common.Save", "F2")];
            var set = new EmulationInputBindingSet(definitions, new Dictionary<string, string> { ["action"] = "F2" }, EmulationInputSource.Keyboard);
            EmulationControllerChoice[] choices = [new("none", "Controllers.None", BindingDefinitions: [], CompatibleVisualIds: []),
                new("stick", "Controllers.Device", BindingDefinitions: definitions, CompatibleVisualIds: visuals ?? [])];
            var settings = new EmulationInputSettings(set, set, [new(1, choices, "stick", null, set), new(2, choices, "stick", null, set)]);
            var service = ControlledDependencies.Simulate<IEmulationInputSettingsManager>((method, args) => method.Name switch
            {
                "DescribeInputSettings" => settings,
                "ApplyInputSettings" => Apply((IEmulationConfiguration)args[0]!, (EmulationInputSettings)args[1]!),
                _ => throw new InvalidOperationException(method.Name)
            });
            var controller = new EmulationInputSettingsController(service, []);
            controller.CreateContent(EmulationMachineTab.Keyboard, Configuration, []);
            controller.CreateContent(EmulationMachineTab.Mouse, Configuration, []);
            controller.CreateContent(EmulationMachineTab.Controllers, Configuration, []);
            return controller;
        }
        private IEmulationConfiguration Apply(IEmulationConfiguration configuration, EmulationInputSettings settings)
        {
            Applied = settings;
            return configuration;
        }
    }
}
