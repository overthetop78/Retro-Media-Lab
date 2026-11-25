using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed partial class FullscreenOsdWindow
{
    public enum Navigation { Up, Down, Left, Right, Confirm, Cancel }
    public enum CenterPanelKind { Mixer, Information }

    public sealed record Entry(string Label, Func<Task>? Command = null, IReadOnlyList<Entry>? Children = null,
        Func<bool>? IsEnabled = null, Setting? Value = null, Func<string>? Status = null,
        string? Glyph = null, bool SeparatorBefore = false, bool FullHeight = false, Func<bool>? IsVisible = null,
        string? Section = null, CenterPanelKind? CenterPanel = null);

    public sealed record Setting(Func<int> Read, Func<int, string> Format, Func<int, int, int> Step,
        Action<int>? Preview, Func<int, Task> Save, (int Minimum, int Maximum)? Range = null);

    public sealed record FooterHints(IReadOnlyList<string> VerticalKeys, IReadOnlyList<string> HorizontalKeys,
        IReadOnlyList<string> ConfirmKeys, IReadOnlyList<string> BackKeys, string NavigateLabel,
        string AdjustLabel, string ConfirmLabel, string BackLabel);

    private sealed class Level
    {
        private readonly IReadOnlyList<Entry> _entries;

        internal Level(IReadOnlyList<Entry> entries, Entry? parent = null)
        {
            _entries = entries;
            Entries = entries;
            Parent = parent;
        }

        internal IReadOnlyList<Entry> Entries { get; private set; }
        internal Entry? Parent { get; }
        internal int Selected { get; set; }
        internal StackPanel Rows { get; } = new();
        internal Border? Panel { get; set; }

        internal Brush? ActiveSectionBackground()
        {
            if (Selected >= Entries.Count || Entries[Selected].Section is null) return null;
            int group = -1;
            string? previous = null;
            for (int index = 0; index <= Selected; index++)
            {
                var section = Entries[index].Section;
                if (section is not null && section != previous) group++;
                previous = section;
            }
            return SectionBackgrounds[group % SectionBackgrounds.Length];
        }

        internal void RefreshVisibility()
        {
            if (!_entries.Any(entry => entry.IsVisible is not null)) return;
            var selectedEntry = Selected < Entries.Count ? Entries[Selected] : null;
            var visible = _entries.Where(entry => entry.IsVisible?.Invoke() != false).ToArray();
            var index = Array.IndexOf(visible, selectedEntry);
            Entries = visible;
            Selected = index >= 0 ? index : Math.Min(Selected, Math.Max(0, visible.Length - 1));
        }
    }

    private readonly List<Level> _levels = [];
    private readonly StackPanel _mainRows = new();
    private static readonly Brush[] SectionBackgrounds =
    [
        SectionBackground(18, 38, 72),
        SectionBackground(13, 56, 57),
        SectionBackground(47, 30, 74),
        SectionBackground(64, 43, 24),
        SectionBackground(61, 29, 53),
        SectionBackground(34, 56, 35)
    ];
    private static readonly Brush DefaultSubmenuBackground = SectionBackground(11, 25, 52);
    private static readonly Brush DefaultEditorBackground = CreateDefaultEditorBackground();

    private static Brush SectionBackground(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static Brush CreateDefaultEditorBackground()
    {
        var brush = new LinearGradientBrush(Color.FromRgb(14, 24, 44), Color.FromRgb(20, 44, 61), 90);
        brush.Freeze();
        return brush;
    }

    private const double SubmenuWidth = 310;
    private readonly Decorator _submenu = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(PanelWidth, 0, 0, 0)
    };
    private readonly Border _shaderLoading;
    private readonly Border _editBar = new()
    {
        Visibility = Visibility.Collapsed,
        VerticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(16, 0, 16, 28),
        Padding = new Thickness(24, 16, 24, 12),
        Background = DefaultEditorBackground,
        BorderBrush = new SolidColorBrush(Color.FromRgb(91, 187, 232)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(16)
    };
    private static readonly ControlTemplate EditorSliderTemplate = (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         TargetType="{x:Type Slider}">
            <Grid Background="Transparent" Height="22">
                <Track x:Name="PART_Track" VerticalAlignment="Stretch"
                       Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}"
                       Value="{TemplateBinding Value}">
                    <Track.DecreaseRepeatButton>
                        <RepeatButton Command="{x:Static Slider.DecreaseLarge}" Focusable="False">
                            <RepeatButton.Template>
                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                    <Grid Background="Transparent">
                                        <Border Background="#58CDE8" Height="6" CornerRadius="3"/>
                                    </Grid>
                                </ControlTemplate>
                            </RepeatButton.Template>
                        </RepeatButton>
                    </Track.DecreaseRepeatButton>
                    <Track.IncreaseRepeatButton>
                        <RepeatButton Command="{x:Static Slider.IncreaseLarge}" Focusable="False">
                            <RepeatButton.Template>
                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                    <Grid Background="Transparent">
                                        <Border Background="#526D87" Height="6" CornerRadius="3"/>
                                    </Grid>
                                </ControlTemplate>
                            </RepeatButton.Template>
                        </RepeatButton>
                    </Track.IncreaseRepeatButton>
                    <Track.Thumb>
                        <Thumb Width="16" Height="16" VerticalAlignment="Center" Focusable="False">
                            <Thumb.Template>
                                <ControlTemplate TargetType="{x:Type Thumb}">
                                    <Border Background="#F5FAFF" BorderBrush="#63D8EE"
                                            BorderThickness="2" CornerRadius="8"/>
                                </ControlTemplate>
                            </Thumb.Template>
                        </Thumb>
                    </Track.Thumb>
                </Track>
            </Grid>
        </ControlTemplate>
        """);

    private TextBlock? _editValue;
    private Slider? _editSlider;

    internal static Border CreateShaderLoadingBadge(string label)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = label,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });
        content.Children.Add(new ProgressBar
        {
            Height = 4,
            Margin = new Thickness(0, 16, 0, 0),
            IsIndeterminate = true,
            Foreground = new SolidColorBrush(Color.FromRgb(27, 176, 248)),
            Background = new SolidColorBrush(Color.FromRgb(24, 56, 91)),
            BorderThickness = new Thickness(0)
        });
        return new Border
        {
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            MaxWidth = 420,
            Padding = new Thickness(34, 24, 34, 24),
            Background = new LinearGradientBrush(Color.FromRgb(11, 28, 64), Color.FromRgb(15, 42, 65), 90),
            BorderBrush = new SolidColorBrush(Color.FromRgb(77, 169, 219)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Child = content
        };
    }
    private Action<Exception>? _showError;
    private Setting? _editing;
    private int _original;
    private int _draft;
    private int? _pendingValue;
    private Task _settingSave = Task.CompletedTask;
    private bool _commandRunning;
    private bool _closed;

    private Level Current => _levels[^1];
    private static readonly ControlTemplate MenuButtonTemplate = CreateButtonTemplate();

    private void InitializeMenu(IReadOnlyList<Entry> entries, Action<Exception> showError)
    {
        _showError = showError;
        _levels.Add(new Level(entries));
        RefreshMenu();
    }

    public void SetShaderLoading(bool loading) =>
        _shaderLoading.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEditing => !_closed && (_editing is not null || Current.Parent?.CenterPanel == CenterPanelKind.Mixer);
    public Task PendingSettingSave => _settingSave;

    public async Task NavigateAsync(Navigation action)
    {
        RestartIdleTimer();
        if (_closed || _commandRunning) return;
        if (Current.Parent?.CenterPanel is { } panel)
        {
            NavigateCenterPanel(panel, action);
            return;
        }
        if (_editing is not null)
        {
            switch (action)
            {
                case Navigation.Left: ChangeValue(-1); break;
                case Navigation.Right: ChangeValue(1); break;
                case Navigation.Up or Navigation.Down:
                    await EditAdjacentSettingAsync(action == Navigation.Up ? -1 : 1);
                    break;
                case Navigation.Confirm or Navigation.Cancel: await LeaveEditAsync(); break;
            }
            return;
        }

        switch (action)
        {
            case Navigation.Up: Move(-1); break;
            case Navigation.Down: Move(1); break;
            case Navigation.Left: Back(closeAtRoot: false); break;
            case Navigation.Cancel: Back(closeAtRoot: true); break;
            case Navigation.Right:
                if (Current.Entries.Count != 0 && Current.Entries[Current.Selected].Children is not null)
                    await ActivateAsync(Current, Current.Selected);
                break;
            case Navigation.Confirm:
                if (Current.Entries.Count != 0) await ActivateAsync(Current, Current.Selected);
                break;
        }
    }

    private void Move(int direction)
    {
        if (Current.Entries.Count == 0) return;
        Current.Selected = Math.Clamp(Current.Selected + direction, 0, Current.Entries.Count - 1);
        RefreshMenu();
    }

    private void Back(bool closeAtRoot)
    {
        if (_levels.Count > 1)
        {
            _levels.RemoveAt(_levels.Count - 1);
            RefreshMenu();
        }
        else if (closeAtRoot) Close();
    }

    private void ChangeValue(int direction)
    {
        if (_editing is not { } setting) return;
        SetDraft(setting.Step(_draft, direction));
    }

    private async Task EditAdjacentSettingAsync(int direction)
    {
        await _settingSave;
        if (_closed || _commandRunning || _editing is null) return;
        var level = Current;
        for (var index = level.Selected + direction; index >= 0 && index < level.Entries.Count; index += direction)
        {
            var entry = level.Entries[index];
            if (entry.Value is null || entry.IsEnabled?.Invoke() == false) continue;
            await ActivateAsync(level, index);
            return;
        }
    }

    private void SetDraft(int value)
    {
        if (_closed || _commandRunning || _editing is not { } setting || _draft == value) return;
        _draft = value;
        _pendingValue = value;
        if (_settingSave.IsCompleted) _settingSave = SaveChangesAsync(setting);
        setting.Preview?.Invoke(value);
        RefreshMenu();
    }

    private async Task SaveChangesAsync(Setting setting)
    {
        await Task.Yield();
        while (_pendingValue is int value)
        {
            _pendingValue = null;
            try
            {
                await setting.Save(value);
                _original = value;
            }
            catch (Exception error)
            {
                if (!_pendingValue.HasValue)
                {
                    _draft = _original;
                    setting.Preview?.Invoke(_original);
                }
                _showError?.Invoke(error);
            }
        }
        if (!_closed) RefreshMenu();
    }

    private async Task LeaveEditAsync()
    {
        _commandRunning = true;
        try
        {
            await _settingSave;
            _editing = null;
        }
        finally
        {
            _commandRunning = false;
            if (!_closed) RefreshMenu();
        }
    }

    private async Task ActivateAsync(Level level, int index)
    {
        if (_closed || _commandRunning) return;
        var item = level.Entries[index];
        if (item.IsEnabled?.Invoke() == false) return;
        _commandRunning = true;
        try
        {
            await _settingSave;
            if (_closed || !ReferenceEquals(level.Entries.ElementAtOrDefault(index), item)) return;
            if (_editing is not null)
            {
                _editing = null;
                ClearEditBar();
            }

            var levelIndex = _levels.IndexOf(level);
            if (levelIndex < 0) return;
            _levels.RemoveRange(levelIndex + 1, _levels.Count - levelIndex - 1);
            level.Selected = index;
            if (item.Children is { } children) _levels.Add(new Level(children, item));
            else if (item.Value is { } setting)
            {
                _original = _draft = setting.Read();
                _editing = setting;
            }
            else if (item.Command is { } command) await command();
        }
        finally
        {
            _commandRunning = false;
            if (!_closed) RefreshMenu();
        }
    }

    private void RefreshMenu()
    {
        if (_closed) return;
        var previousLevel = Current;
        var editedEntry = _editing is not null ? previousLevel.Entries[previousLevel.Selected] : null;
        for (var index = 0; index < _levels.Count; index++)
        {
            var level = _levels[index];
            level.RefreshVisibility();
            if (index + 1 == _levels.Count || level.Entries.Contains(_levels[index + 1].Parent!)) continue;
            _levels.RemoveRange(index + 1, _levels.Count - index - 1);
        }
        if (Current.Parent?.CenterPanel is not null)
        {
            _mainPanel.Visibility = Visibility.Hidden;
            _submenu.Visibility = Visibility.Hidden;
            ClearEditBar();
            ShowCenterPanel();
            return;
        }
        ClearCenterPanel();
        if (editedEntry is not null && (!ReferenceEquals(previousLevel, Current) || !Current.Entries.Contains(editedEntry)))
            _editing = null;
        if (_closed) return;
        if (_editing is { } setting)
        {
            _mainPanel.Visibility = Visibility.Hidden;
            _submenu.Visibility = Visibility.Hidden;
            ShowEditBar(Current.Entries[Current.Selected], setting);
            return;
        }
        ClearEditBar();
        _mainPanel.Visibility = Visibility.Visible;
        _submenu.Visibility = Visibility.Visible;
        _mainRows.Children.Clear();
        var parentRow = AppendRows(_mainRows, _levels[0]);

        UpdatePanelWidths();
        if (_levels.Count == 1)
        {
            _submenu.Child = null;
            parentRow?.BringIntoView();
            return;
        }

        var current = Current;
        if (current.Panel is null)
        {
            var firstSubmenu = _levels[1];
            var top = firstSubmenu.Panel?.Margin.Top ?? 0;
            if (ReferenceEquals(current, firstSubmenu) && !firstSubmenu.Parent!.FullHeight && parentRow is not null)
            {
                parentRow.BringIntoView();
                ((Grid)Content).UpdateLayout();
                top = parentRow.TranslatePoint(new Point(), (Grid)Content).Y;
            }
            current.Panel = CreateSubmenu(current, top);
        }

        if (!ReferenceEquals(_submenu.Child, current.Panel))
            _submenu.Child = current.Panel;
        current.Panel.Background = TranslucentBackground(current.ActiveSectionBackground() ?? DefaultSubmenuBackground);
        current.Rows.Children.Clear();
        AppendRows(current.Rows, current)?.BringIntoView();
    }

    private void UpdatePanelWidths()
    {
        var preferredWidth = _levels.Count > 1 ? PanelWidth + SubmenuWidth : PanelWidth;
        var scale = double.IsFinite(Width) ? Math.Min(1, Width / preferredWidth) : 1;
        _mainPanel.Width = PanelWidth * scale;
        _submenu.Margin = new Thickness(_mainPanel.Width, 0, 0, 0);
        _submenu.Width = _levels.Count > 1 ? SubmenuWidth * scale : 0;
        _editBar.Width = double.IsFinite(Width) ? Math.Clamp(Width - 32, 0, 680) : 680;
    }

    private void ShowEditBar(Entry item, Setting setting)
    {
        _editBar.Background = TranslucentBackground(Current.ActiveSectionBackground() ?? DefaultEditorBackground);
        if (_editValue is null)
        {
            var content = new StackPanel();
            var header = new DockPanel();
            var titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text = item.Section ?? Current.Parent?.Label,
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(162, 213, 236)),
                Margin = new Thickness(0, 0, 0, 3), TextWrapping = TextWrapping.Wrap
            });
            _editValue = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(171, 233, 255)),
                FontSize = 20, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            var valueBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(21, 53, 79)),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(9), Padding = new Thickness(13, 6, 13, 6),
                Margin = new Thickness(16, 0, 0, 0), MinWidth = 70, Child = _editValue
            };
            DockPanel.SetDock(valueBadge, Dock.Right);
            header.Children.Add(valueBadge);
            titles.Children.Add(new TextBlock
            {
                Text = item.Label, Foreground = Brushes.White, FontSize = 24, FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            header.Children.Add(titles);
            content.Children.Add(header);
            if (setting.Range is { } range)
            {
                _editSlider = new Slider
                {
                    Minimum = range.Minimum, Maximum = range.Maximum, Value = _draft,
                    TickFrequency = 1, IsSnapToTickEnabled = true, IsMoveToPointEnabled = true,
                    Focusable = false, Margin = new Thickness(0, 14, 0, 0),
                    Template = EditorSliderTemplate
                };
                _editSlider.ValueChanged += EditSliderValueChanged;
                content.Children.Add(_editSlider);
            }
            content.MouseWheel += (_, args) =>
            {
                args.Handled = true;
                try { ChangeValue(Math.Sign(args.Delta)); }
                catch (Exception error) { _showError?.Invoke(error); }
            };
            if (_hints is { } hints)
            {
                content.Children.Add(CreateSeparator(new Thickness(0, 11, 0, 8)));
                var actions = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                AddEditHint(actions, hints.HorizontalKeys, hints.AdjustLabel);
                if (Current.Entries.Count(entry => entry.Value is not null && entry.IsEnabled?.Invoke() != false) > 1)
                    AddEditHint(actions, hints.VerticalKeys, hints.NavigateLabel);
                AddEditAction(actions, hints.ConfirmKeys.Concat(hints.BackKeys).Distinct().ToArray(), hints.BackLabel,
                    Navigation.Cancel);
                content.Children.Add(actions);
            }
            _editBar.Child = content;
        }
        _editValue.Text = setting.Format(_draft);
        if (_editSlider is not null) _editSlider.Value = _draft;
        _editBar.Visibility = Visibility.Visible;
    }

    private void EditSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
    {
        try { SetDraft((int)args.NewValue); }
        catch (Exception error) { _showError?.Invoke(error); }
    }

    private static void AddEditHint(Panel actions, IReadOnlyList<string> keys, string label)
    {
        if (keys.Count > 0) actions.Children.Add(CreateEditHint(keys, label));
    }

    private static WrapPanel CreateEditHint(IReadOnlyList<string> keys, string label)
    {
        var hint = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var key in keys) AddShortcutKey(hint, key);
        AddHint(hint, label);
        hint.Margin = new Thickness(4, 3, 4, 3);
        return hint;
    }

    private void AddEditAction(Panel actions, IReadOnlyList<string> keys, string label, Navigation navigation)
    {
        var button = CreateMenuButton(label, selected: false, enabled: true);
        button.Padding = new Thickness(3, 0, 3, 0);
        button.Content = CreateEditHint(keys, label);
        button.Click += async (_, _) =>
        {
            try { await NavigateAsync(navigation); }
            catch (Exception error) { _showError?.Invoke(error); }
        };
        actions.Children.Add(button);
    }

    private void ClearEditBar()
    {
        if (_editSlider is not null) _editSlider.ValueChanged -= EditSliderValueChanged;
        _editSlider = null;
        _editValue = null;
        _editBar.Child = null;
        _editBar.Visibility = Visibility.Collapsed;
    }

    private Border CreateSubmenu(Level level, double top)
    {
        var parent = level.Parent!;
        var rows = new StackPanel();
        var title = new DockPanel { Margin = new Thickness(6, 0, 6, 12) };
        AddGlyph(title, parent.Glyph, new Thickness(0, 0, 12, 0));
        title.Children.Add(new TextBlock
        {
            Text = parent.Label,
            Foreground = Brushes.White,
            FontSize = 19,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        rows.Children.Add(title);
        rows.Children.Add(CreateSeparator(new Thickness(0, 0, 0, 10)));
        rows.Children.Add(level.Rows);
        return new Border
        {
            Margin = new Thickness(0, top, 0, 0),
            Padding = parent.FullHeight ? new Thickness(16, 24, 16, 14) : new Thickness(10, 12, 10, 10),
            VerticalAlignment = parent.FullHeight ? VerticalAlignment.Stretch : VerticalAlignment.Top,
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, 77, 169, 219)),
            BorderThickness = new Thickness(0, 0, 1, parent.FullHeight ? 0 : 1),
            CornerRadius = parent.FullHeight ? new CornerRadius(0) : new CornerRadius(0, 7, 7, 0),
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = rows
            }
        };
    }

    private FrameworkElement? AppendRows(StackPanel rows, Level level)
    {
        FrameworkElement? selectedRow = null;
        for (int index = 0; index < level.Entries.Count; index++)
        {
            var itemIndex = index;
            var item = level.Entries[index];
            var startsSection = item.Section is not null && (index == 0 || item.Section != level.Entries[index - 1].Section);
            if (item.SeparatorBefore || startsSection && index > 0)
                rows.Children.Add(CreateSeparator(new Thickness(16, 5, 16, 5)));
            if (startsSection && level.Parent is not null)
                rows.Children.Add(new TextBlock
                {
                    Text = item.Section, FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(162, 213, 236)),
                    Margin = new Thickness(10, 8, 8, 4), TextWrapping = TextWrapping.Wrap
                });
            var enabled = item.IsEnabled?.Invoke() != false;
            var value = item.Value is { } setting ? setting.Format(setting.Read()) : item.Status?.Invoke();
            var label = new DockPanel { LastChildFill = true };
            AddGlyph(label, item.Glyph, new Thickness(0, 0, 12, 0));
            var caption = new TextBlock
            {
                Text = item.Label,
                Foreground = enabled ? Brushes.White : new SolidColorBrush(Color.FromRgb(161, 174, 197)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (value is not null)
            {
                var columns = new Grid();
                columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var detail = new TextBlock
                {
                    Text = value,
                    Foreground = Brushes.White,
                    Margin = new Thickness(8, 0, 0, 0),
                    TextAlignment = TextAlignment.Right,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(detail, 1);
                columns.Children.Add(caption);
                columns.Children.Add(detail);
                columns.SizeChanged += (_, args) =>
                {
                    if (!args.WidthChanged) return;
                    bool stacked = args.NewSize.Width < 180;
                    Grid.SetColumnSpan(caption, stacked ? 2 : 1);
                    Grid.SetColumnSpan(detail, stacked ? 2 : 1);
                    Grid.SetColumn(detail, stacked ? 0 : 1);
                    Grid.SetRow(detail, stacked ? 1 : 0);
                    detail.Margin = stacked ? new Thickness(0, 3, 0, 0) : new Thickness(8, 0, 0, 0);
                };
                label.Children.Add(columns);
            }
            else
            {
                if (item.Children is not null)
                {
                    var arrow = new TextBlock
                    {
                        Text = "›",
                        Foreground = Brushes.White,
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    DockPanel.SetDock(arrow, Dock.Right);
                    label.Children.Add(arrow);
                }
                label.Children.Add(caption);
            }
            var button = CreateMenuButton(label, level.Selected == index, enabled);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Click += async (_, _) =>
            {
                try { await ActivateAsync(level, itemIndex); }
                catch (Exception error) { _showError?.Invoke(error); }
            };
            if (level.Selected == index) selectedRow = button;
            rows.Children.Add(button);
        }
        return selectedRow;
    }

    private static Button CreateMenuButton(object content, bool selected, bool enabled)
    {
        var button = new Button
        {
            Content = content,
            Template = MenuButtonTemplate,
            Focusable = false,
            IsEnabled = enabled,
            FontSize = 16,
            Foreground = enabled ? Brushes.White : new SolidColorBrush(Color.FromRgb(161, 174, 197)),
            Background = selected ? new LinearGradientBrush(Color.FromRgb(9, 110, 238), Color.FromRgb(5, 69, 184), 90) : Brushes.Transparent,
            BorderBrush = selected ? new SolidColorBrush(Color.FromRgb(61, 204, 250)) : Brushes.Transparent,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(15, 7, 10, 7),
            MinHeight = 40,
            Margin = new Thickness(1, 1, 1, 1)
        };
        return button;
    }

    private static ControlTemplate CreateButtonTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        Bind(border, Border.BackgroundProperty, nameof(Control.Background));
        Bind(border, Border.BorderBrushProperty, nameof(Control.BorderBrush));
        Bind(border, Border.BorderThicknessProperty, nameof(Control.BorderThickness));
        Bind(border, Border.PaddingProperty, nameof(Control.Padding));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        return new ControlTemplate(typeof(Button)) { VisualTree = border };

        static void Bind(FrameworkElementFactory element, DependencyProperty property, string source) =>
            element.SetBinding(property, new Binding(source) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
    }

    private static void AddGlyph(DockPanel row, string? glyph, Thickness margin)
    {
        if (glyph is null) return;
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 16,
            Foreground = Brushes.White,
            Width = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = margin
        };
        DockPanel.SetDock(icon, Dock.Left);
        row.Children.Add(icon);
    }

    private static Border CreateSeparator(Thickness margin) => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromArgb(204, 99, 189, 230)),
        Margin = margin
    };

    private static FrameworkElement CreateFooter(FooterHints? hints)
    {
        var footer = new StackPanel { Margin = new Thickness(16, 0, 16, 16) };
        footer.Children.Add(CreateSeparator(new Thickness(0, 0, 0, 7)));
        if (hints is null) return footer;
        var directions = new WrapPanel { Margin = new Thickness(0, 0, 0, 5) };
        foreach (var key in hints.VerticalKeys.Concat(hints.HorizontalKeys))
            AddShortcutKey(directions, key);
        if (directions.Children.Count > 0)
        {
            AddHint(directions, hints.NavigateLabel);
            footer.Children.Add(directions);
        }
        var commands = new WrapPanel();
        foreach (var key in hints.ConfirmKeys) AddShortcutKey(commands, key);
        if (hints.ConfirmKeys.Count > 0)
            AddHint(commands, hints.ConfirmLabel);
        foreach (var key in hints.BackKeys) AddShortcutKey(commands, key);
        if (hints.BackKeys.Count > 0)
            AddHint(commands, hints.BackLabel);
        if (commands.Children.Count > 0) footer.Children.Add(commands);
        return footer;

    }

    private static void AddShortcutKey(Panel row, string cap)
    {
            row.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(155, 176, 211)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(0, 0, 3, 0),
                Child = new TextBlock { Text = cap, FontSize = 12, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap }
            });
    }

    private static void AddHint(Panel row, string label) => row.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = Brushes.White,
            Margin = new Thickness(1, 0, 9, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
}
