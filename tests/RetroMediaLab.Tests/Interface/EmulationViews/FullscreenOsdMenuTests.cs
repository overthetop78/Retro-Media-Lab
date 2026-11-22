using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Infrastructure.Settings.Emulation;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Services;
using RetroMediaLab.VideoPresentation.Windows.Osd;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;
using OsdNavigation = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Navigation;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

[Collection("WPF")]
public sealed class FullscreenOsdMenuTests(StaExecutionScenarios sta)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task HeldDirectionsRepeatOnlyWhileEditing(bool useController) => sta.Run(() =>
    {
        using var view = new MachineView();
        var machineInput = ControlledDependencies.Simulate<IEmulationInput>((method, _) =>
            method.Name == "get_KeyboardBindings" ? new Dictionary<string, string>() : throw new InvalidOperationException(method.Name));
        var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) =>
            method.Name == "get_Input" ? machineInput : throw new InvalidOperationException(method.Name));
        var pointer = ControlledDependencies.Simulate<IRelativeMouseCapture>((method, _) => method.Name switch
        {
            "get_IsCaptured" => false,
            "Release" => null,
            _ => throw new InvalidOperationException(method.Name)
        });
        long now = 0;
        bool editing = true;
        uint controllerButtons = 0;
        const uint buttonA = 1u << 8;
        const uint buttonB = 1u;
        var controller = EmulationControllerState.Empty with { DeviceId = "osd-pad" };
        var direction = useController ? EmulationShortcutDefaults.OsdUp + EmulationShortcutDefaults.AlternativeSuffix
            : EmulationShortcutDefaults.OsdUp;
        var confirm = useController ? EmulationShortcutDefaults.OsdConfirm + EmulationShortcutDefaults.AlternativeSuffix
            : EmulationShortcutDefaults.OsdConfirm;
        var actions = new List<string>();
        using var input = new MachineInputController(view, new Border(), IntPtr.Zero, () => machine, [],
            action => { actions.Add(action); return Task.CompletedTask; }, () => true,
            pointerCapture: pointer, readPhysicalInput: () => new(EmulationInputSnapshot.Empty.Keys,
                EmulationInputSnapshot.Empty.Pointer, [controller with { Buttons = controllerButtons }]),
            automaticPolling: false, feedbackSource: ControlledDependencies.Reject<IGameInputControllerSource>(),
            osdShortcuts: EmulationShortcutMap.OsdShortcuts(new Dictionary<string, string>
            {
                [direction] = useController ? "Controller:osd-pad:A" : "Up",
                [confirm] = useController ? "Controller:osd-pad:B" : "Enter"
            }),
            isFullscreen: () => true, isOsdEditing: () => editing, inputClock: () => now);
        input.SetOsdFocus(true);

        SetDirection(true);
        input.Publish();
        now = 299;
        input.Publish();
        Assert.Equal([direction], actions);
        now = 300;
        input.Publish();
        now = 335;
        input.Publish();
        Assert.Equal(3, actions.Count);

        SetDirection(false);
        now = 1000;
        input.Publish();
        Assert.Equal(3, actions.Count);

        if (useController) controllerButtons = buttonB;
        else input.HandleKeyDown(Key.Enter, ModifierKeys.None);
        input.Publish();
        Assert.Equal(confirm, actions[3]);
        now = 2000;
        input.Publish();
        Assert.Equal(4, actions.Count);
        if (useController) controllerButtons = 0;
        else input.HandleKeyUp(Key.Enter, ModifierKeys.None);
        input.Publish();
        Assert.Equal(4, actions.Count);

        editing = false;
        SetDirection(true);
        input.Publish();
        now = 3000;
        input.Publish();
        Assert.Equal(5, actions.Count);

        void SetDirection(bool pressed)
        {
            if (useController) controllerButtons = pressed ? buttonA : 0;
            else if (pressed) input.HandleKeyDown(Key.Up, ModifierKeys.None);
            else input.HandleKeyUp(Key.Up, ModifierKeys.None);
        }
    });

    [Fact]
    public Task NestedSubmenusReplaceEachOtherBesideTheMainMenu() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 500, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            Entry[] entries =
            [
                new("Video", Children:
                [
                    new("Image", Children:
                    [
                        new("Colors", Children: [new("Brightness")], FullHeight: true)
                    ])
                ])
            ];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            menu.Show();
            var overlay = Assert.IsType<Grid>(menu.Content);
            var main = Assert.IsType<Border>(overlay.Children[0]);
            var submenu = Assert.IsType<Decorator>(overlay.Children[1]);
            Assert.Null(submenu.Child);

            await menu.NavigateAsync(OsdNavigation.Right);
            menu.UpdateLayout();
            var first = Assert.IsType<Border>(submenu.Child);
            var firstPosition = first.TranslatePoint(new Point(), overlay);
            var mainWidth = main.ActualWidth;

            await menu.NavigateAsync(OsdNavigation.Right);
            var second = Assert.IsType<Border>(submenu.Child);
            Assert.NotSame(first, second);
            Assert.Null(first.Parent);
            await menu.NavigateAsync(OsdNavigation.Right);
            menu.UpdateLayout();

            var third = Assert.IsType<Border>(submenu.Child);
            Assert.NotSame(second, third);
            Assert.Null(second.Parent);
            var thirdPosition = third.TranslatePoint(new Point(), overlay);
            Assert.Equal(mainWidth, main.ActualWidth);
            Assert.Equal(firstPosition, thirdPosition);
            Assert.InRange(Math.Abs(thirdPosition.X - mainWidth), 0, 1);
            Assert.True(firstPosition.Y > 0);
            Assert.InRange(Math.Abs(thirdPosition.X + third.ActualWidth - overlay.ActualWidth), 0, 1);

            await menu.NavigateAsync(OsdNavigation.Left);
            Assert.Same(second, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Same(first, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Null(submenu.Child);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task SliderEditsSaveWithoutConfirmationAndSurviveClosing() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 800, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            int saved = 10;
            int preview = saved;
            bool closeOnPreview = false;
            var setting = new FullscreenOsdWindow.Setting(
                () => saved, value => value.ToString(), (value, direction) => value + direction,
                value =>
                {
                    preview = value;
                    if (closeOnPreview)
                    {
                        closeOnPreview = false;
                        menu!.Close();
                    }
                },
                value => { saved = value; return Task.CompletedTask; }, Range: (0, 20));
            Entry[] entries = [new("Video", Children: [new("Image", Children: [new("Brightness", Value: setting)])])];
            owner.Show();
            var hints = new FullscreenOsdWindow.FooterHints(
                ["↑", "↓"], ["←", "→", "Pad Left"], ["↵", "Pad A"], ["Esc"], "Navigate", "Adjust", "Confirm", "Back");
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading", hints);
            menu.Show();
            var overlay = Assert.IsType<Grid>(menu.Content);
            var main = Assert.IsType<Border>(overlay.Children[0]);
            var submenu = Assert.IsType<Decorator>(overlay.Children[1]);
            var editor = Assert.IsType<Border>(overlay.Children[3]);

            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.NavigateAsync(OsdNavigation.Right);
            var imagePanel = Assert.IsType<Border>(submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Equal(Visibility.Hidden, main.Visibility);
            Assert.Equal(Visibility.Hidden, submenu.Visibility);
            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal(VerticalAlignment.Bottom, editor.VerticalAlignment);
            var content = Assert.IsType<StackPanel>(editor.Child);
            var slider = Assert.Single(content.Children.OfType<Slider>());
            var commands = Assert.Single(content.Children.OfType<WrapPanel>());
            var adjustment = Assert.IsType<WrapPanel>(commands.Children[0]);
            Assert.Contains(adjustment.Children.OfType<Border>(), cap => Assert.IsType<TextBlock>(cap.Child).Text == "Pad Left");
            Assert.Contains(adjustment.Children.OfType<TextBlock>(), label => label.Text == "Adjust");
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.PendingSettingSave;
            Assert.Equal(11d, slider.Value);
            Assert.Equal(11, preview);
            Assert.Equal(11, saved);
            slider.Value = 15;
            Assert.Equal(15, preview);

            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Equal(15, preview);
            Assert.Equal(15, saved);
            Assert.Same(imagePanel, submenu.Child);
            Assert.Equal(Visibility.Visible, main.Visibility);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);

            await menu.NavigateAsync(OsdNavigation.Confirm);
            await menu.NavigateAsync(OsdNavigation.Left);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Equal(14, saved);
            Assert.Same(imagePanel, submenu.Child);

            await menu.NavigateAsync(OsdNavigation.Confirm);
            closeOnPreview = true;
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.PendingSettingSave;
            Assert.Null(menu.Content);
            Assert.Equal(15, preview);
            Assert.Equal(15, saved);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task EditingAdjacentSettingsKeepsEachSavedValue() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 800, Height = 480 };
        FullscreenOsdWindow? menu = null;
        const string moduleId = "sega";
        var configurationId = Guid.NewGuid();
        var files = new MemoryVideoProfileFiles();
        var profiles = new VideoPresentationProfileStore("virtual-osd", fileSystem: files);
        var preview = new EmulationImageAdjustments { Contrast = 3 };
        var processing = new EmulationVideoProcessingConfiguration { Adjustments = preview };
        profiles.Set(moduleId, configurationId, new EmulationVideoPresentationProfile(Processing: processing));
        profiles.Save(moduleId, configurationId);

        FullscreenOsdWindow.Setting Setting(Func<EmulationImageAdjustments, int> read,
            Func<EmulationImageAdjustments, int, EmulationImageAdjustments> update) =>
            new(() => read(preview), value => value.ToString(),
                (value, direction) => value + direction, value => preview = update(preview, value),
                value => MachineController.SaveOsdVideoProfileAsync(profiles, moduleId, configurationId, profile => profile with
                {
                    Processing = (profile.Processing ?? processing) with
                    {
                        Adjustments = update((profile.Processing ?? processing).Adjustments, value)
                    }
                }), Range: (-10, 10));

        void AssertSaved(int brightness, int contrast)
        {
            var reopened = new VideoPresentationProfileStore("virtual-osd", fileSystem: files);
            var adjustments = reopened.Get(moduleId, configurationId).Processing!.Adjustments;
            Assert.Equal(brightness, adjustments.Brightness);
            Assert.Equal(contrast, adjustments.Contrast);
        }
        try
        {
            Entry[] entries = [new("Settings", Children:
            [
                new("Image", Children:
                [
                    new("Brightness", Value: Setting(image => image.Brightness, (image, value) => image with { Brightness = value })),
                    new("Read-only information"),
                    new("Contrast", Value: Setting(image => image.Contrast, (image, value) => image with { Contrast = value }))
                ])
            ])];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            menu.Show();
            var editor = Assert.IsType<Border>(Assert.IsType<Grid>(menu.Content).Children[3]);

            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            await menu.NavigateAsync(OsdNavigation.Right);
            Assert.Equal(1, preview.Brightness);

            await menu.NavigateAsync(OsdNavigation.Down);
            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal(3d, SliderValue());
            AssertSaved(1, 3);
            await menu.NavigateAsync(OsdNavigation.Left);
            Assert.Equal(2, preview.Contrast);

            await menu.NavigateAsync(OsdNavigation.Up);
            Assert.Equal(1d, SliderValue());
            AssertSaved(1, 2);
            await menu.NavigateAsync(OsdNavigation.Down);
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.PendingSettingSave;
            AssertSaved(1, 3);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);

            double SliderValue() => Assert.Single(Assert.IsType<StackPanel>(editor.Child).Children.OfType<Slider>()).Value;
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task VideoChoicesRevealRowsDirectlyBelowAndUseTheActiveSectionColor() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 800, Height = 480 };
        FullscreenOsdWindow? menu = null;
        var enabled = false;
        var detail = 0;
        var technology = new FullscreenOsdWindow.Setting(() => enabled ? 1 : 0, value => value.ToString(),
            (value, direction) => Math.Clamp(value + direction, 0, 1), Preview: null,
            Save: value => { enabled = value != 0; return Task.CompletedTask; });
        var amount = new FullscreenOsdWindow.Setting(() => detail, value => value.ToString(),
            (value, direction) => Math.Clamp(value + direction, 0, 10), Preview: null,
            Save: value => { detail = value; return Task.CompletedTask; }, Range: (0, 10));
        var rows = MachineController.EditableVideoRows(
        [
            new("Display", Children: [new("Technology", Value: technology), new("Chip", Status: () => "VDP")]),
            new("Effects", Children:
            [
                new("Amount", Value: amount),
                new("Unavailable", Value: amount, IsEnabled: () => false)
            ], IsVisible: () => enabled),
            new("Restoration", Children: [new("Sharpness", Value: amount)])
        ]);
        Assert.Equal(["Technology", "Amount", "Unavailable", "Sharpness"], rows.Select(row => row.Label));
        Assert.All(rows, row => Assert.Null(row.Children));
        Assert.Equal(["Technology", "Sharpness"], rows.Where(row => row.IsVisible!()).Select(row => row.Label));
        Assert.NotEqual(rows[0].Section, rows[1].Section);
        try
        {
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, [new("Video", Children: rows)],
                error => throw error, "Loading");
            menu.Show();
            await menu.NavigateAsync(OsdNavigation.Right);
            var overlay = Assert.IsType<Grid>(menu.Content);
            var submenu = Assert.IsType<Border>(Assert.IsType<Decorator>(overlay.Children[1]).Child);
            var editor = Assert.IsType<Border>(overlay.Children[3]);
            var displayColor = submenu.Background;
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Same(displayColor, editor.Background);
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.PendingSettingSave;
            Assert.True(enabled);
            Assert.True(menu.IsEditing);
            Assert.Equal(["Technology", "Amount", "Sharpness"], rows.Where(row => row.IsVisible!()).Select(row => row.Label));

            await menu.NavigateAsync(OsdNavigation.Down);
            var effectsColor = editor.Background;
            Assert.NotSame(displayColor, effectsColor);
            Assert.Single(Assert.IsType<StackPanel>(editor.Child).Children.OfType<Slider>());
            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Equal(1, detail);
            Assert.False(menu.IsEditing);
            Assert.Same(effectsColor, submenu.Background);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task IdleTimeoutClosesTheCurrentEditorAfterItsLastNavigation() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 800, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int saved = 0;
            var setting = new FullscreenOsdWindow.Setting(() => saved, value => value.ToString(),
                (value, direction) => value + direction, Preview: null,
                Save: value => { saved = value; return Task.CompletedTask; }, Range: (-20, 20));
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, [new("Brightness", Value: setting)],
                error => throw error, "Loading");
            menu.ContentRendered += (_, _) => rendered.TrySetResult();
            menu.Closed += (_, _) => closed.TrySetResult();
            menu.Show();
            await rendered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(TimeSpan.FromSeconds(1));
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.True(menu.IsEditing);
            var lastInput = System.Diagnostics.Stopwatch.StartNew();
            await menu.NavigateAsync(OsdNavigation.Right);

            await closed.Task.WaitAsync(TimeSpan.FromSeconds(19));
            Assert.True(lastInput.Elapsed >= TimeSpan.FromSeconds(14.5));
            await menu.PendingSettingSave;
            Assert.Equal(1, saved);
            Assert.Null(menu.Content);
        }
        finally
        {
            try { if (menu?.Content is not null) menu.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task CompleteChordsTriggerOnPressAcrossOsdFocusChanges() => sta.Run(() =>
    {
        using var view = new MachineView();
        var machineInput = ControlledDependencies.Simulate<IEmulationInput>((method, _) =>
            method.Name == "get_KeyboardBindings" ? new Dictionary<string, string>() : throw new InvalidOperationException(method.Name));
        var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) =>
            method.Name == "get_Input" ? machineInput : throw new InvalidOperationException(method.Name));
        var pointer = ControlledDependencies.Simulate<IRelativeMouseCapture>((method, _) => method.Name switch
        {
            "get_IsCaptured" => false,
            "Release" => null,
            _ => throw new InvalidOperationException(method.Name)
        });
        const ModifierKeys modifiers = ModifierKeys.Control | ModifierKeys.Shift;
        var actions = new List<string>();
        bool fullscreen = false;
        using var input = new MachineInputController(view, new Border(), IntPtr.Zero, () => machine,
            EmulationShortcutMap.GlobalShortcuts(new Dictionary<string, string>
            {
                [EmulationShortcutDefaults.ToggleFullscreen] = "Ctrl+Shift+F11",
                [EmulationShortcutDefaults.ReleaseMouse] = "Ctrl+Shift+F12"
            }),
            action => { actions.Add(action); return Task.CompletedTask; }, () => true,
            pointerCapture: pointer, focus: (_, _) => { }, automaticPolling: false,
            readPhysicalInput: () => new(EmulationInputSnapshot.Empty.Keys, EmulationInputSnapshot.Empty.Pointer, []),
            feedbackSource: ControlledDependencies.Reject<IGameInputControllerSource>(),
            osdShortcuts: EmulationShortcutMap.OsdShortcuts(new Dictionary<string, string>
            {
                [EmulationShortcutDefaults.OsdWithPause] = "Ctrl+Shift+Divide",
                [EmulationShortcutDefaults.OsdWithoutPause] = "Ctrl+Shift+Multiply",
                [EmulationShortcutDefaults.OsdStatistics] = EmulationShortcutDefaults.OsdValues[EmulationShortcutDefaults.OsdStatistics]
            }),
            isFullscreen: () => fullscreen);

        input.HandleKeyDown(Key.Subtract, modifiers);
        input.HandleKeyUp(Key.Subtract, modifiers);
        Assert.Empty(actions);
        fullscreen = true;

        input.HandleKeyDown(Key.LeftCtrl, ModifierKeys.Control);
        input.HandleKeyDown(Key.LeftShift, modifiers);
        Assert.Empty(actions);

        for (int count = 0; count < 3; count++)
        {
            input.HandleKeyDown(Key.Divide, modifiers);
            Assert.Equal(count + 1, actions.Count);
            input.SetOsdFocus(count % 2 == 0);
            input.HandleKeyDown(Key.Divide, modifiers, isRepeat: true);
            Assert.Equal(count + 1, actions.Count);
            // The old window does not deliver the Divide release.
        }
        Assert.All(actions, action => Assert.Equal(EmulationShortcutDefaults.OsdWithPause, action));

        // Neither a held Divide nor its lost release can arm a new Ctrl+Shift press.
        input.HandleKeyDown(Key.Divide, modifiers);
        Assert.Equal(4, actions.Count);
        input.SetOsdFocus(false);
        input.HandleKeyUp(Key.LeftShift, ModifierKeys.Control);
        input.HandleKeyDown(Key.LeftShift, modifiers);
        input.HandleKeyDown(Key.Divide, modifiers, isRepeat: true);
        input.HandleKeyUp(Key.LeftShift, ModifierKeys.Control);
        input.HandleKeyDown(Key.LeftShift, modifiers);
        input.HandleKeyUp(Key.Divide, modifiers);
        Assert.Equal(4, actions.Count);

        actions.Clear();
        for (int count = 0; count < 2; count++)
        {
            input.HandleKeyDown(Key.F11, modifiers);
            input.HandleKeyDown(Key.F11, modifiers, isRepeat: true);
            Assert.Equal(count + 1, actions.Count);
            input.HandleKeyUp(Key.F11, modifiers);
        }
        Assert.Equal(Enumerable.Repeat(EmulationShortcutDefaults.ToggleFullscreen, 2), actions);

        actions.Clear();
        input.HandleKeyDown(Key.Divide, modifiers);
        input.SetOsdFocus(true);
        input.HandleKeyUp(Key.Divide, modifiers);
        input.HandleKeyDown(Key.Subtract, modifiers | ModifierKeys.Alt);
        input.HandleKeyUp(Key.Subtract, modifiers);
        input.HandleKeyDown(Key.OemMinus, modifiers);
        input.HandleKeyUp(Key.OemMinus, modifiers);
        input.HandleKeyDown(Key.Subtract, modifiers);
        input.HandleKeyDown(Key.Subtract, modifiers, isRepeat: true);
        input.HandleKeyUp(Key.Subtract, modifiers);
        input.HandleKeyDown(Key.Multiply, modifiers);
        input.SetOsdFocus(false);
        input.HandleKeyUp(Key.Multiply, modifiers);
        input.HandleKeyDown(Key.Subtract, modifiers);
        input.HandleKeyUp(Key.Subtract, modifiers);
        input.HandleKeyDown(Key.F12, modifiers);
        input.HandleKeyUp(Key.F12, modifiers);
        input.HandleKeyDown(Key.F11, modifiers);
        input.HandleKeyUp(Key.F11, modifiers);
        Assert.Equal([
            EmulationShortcutDefaults.OsdWithPause,
            EmulationShortcutDefaults.OsdStatistics,
            EmulationShortcutDefaults.OsdWithoutPause,
            EmulationShortcutDefaults.OsdStatistics,
            EmulationShortcutDefaults.ReleaseMouse,
            EmulationShortcutDefaults.ToggleFullscreen], actions);
    });

    [Fact]
    public Task AHiddenParentClosesOnlyItsBranch() => sta.RunAsync(async () =>
    {
        bool visible = true;
        bool selectedSibling = false;
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 640, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            Entry[] entries =
            [
                new("Parent", FullHeight: true, Children:
                [
                    new("Branch", IsVisible: () => visible, FullHeight: true, Children:
                    [
                        new("Hide", Command: () => { visible = false; return Task.CompletedTask; })
                    ]),
                    new("Sibling", Command: () => { selectedSibling = true; return Task.CompletedTask; })
                ])
            ];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            var submenu = Assert.IsType<Decorator>(Assert.IsType<Grid>(menu.Content).Children[1]);
            await menu.NavigateAsync(OsdNavigation.Right);
            var firstPanel = Assert.IsType<Border>(submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Right);
            Assert.NotSame(firstPanel, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Same(firstPanel, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.True(selectedSibling);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });
}
