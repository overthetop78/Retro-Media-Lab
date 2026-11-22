using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed partial class FullscreenOsdWindow
{
    public enum Navigation { Up, Down, Left, Right, Confirm, Cancel }

    public sealed record Entry(string Label, Func<Task>? Command = null, IReadOnlyList<Entry>? Children = null,
        Func<bool>? IsEnabled = null, Setting? Value = null, Func<string>? Status = null,
        string? Glyph = null, bool SeparatorBefore = false, bool FullHeight = false, Func<bool>? IsVisible = null);

    public sealed record Setting(Func<int> Read, Func<int, string> Format, Func<int, int, int> Step,
        Action<int>? Preview, Func<int, Task> Save, Func<Action>? CaptureRestore = null);

    public sealed record FooterHints(IReadOnlyList<string> DirectionKeys, string NavigateLabel,
        string? ConfirmKey, string ConfirmLabel, string? BackKey, string BackLabel);

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
    private const double SubmenuWidth = 310;
    private readonly Decorator _submenu = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(PanelWidth, 0, 0, 0)
    };
    private readonly Border _shaderLoading;

    internal static Border CreateShaderLoadingBadge(string label, Thickness margin) => new()
    {
        Visibility = Visibility.Collapsed,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
        Margin = margin,
        Padding = new Thickness(14, 8, 14, 8),
        Background = new SolidColorBrush(Color.FromArgb(237, 12, 30, 58)),
        CornerRadius = new CornerRadius(6),
        Child = new TextBlock { Text = label, Foreground = Brushes.White }
    };
    private Action<Exception>? _showError;
    private Setting? _editing;
    private Action? _restorePreview;
    private int _original;
    private int _draft;
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

    public async Task NavigateAsync(Navigation action)
    {
        if (_closed || _commandRunning) return;
        if (_editing is not null)
        {
            switch (action)
            {
                case Navigation.Left: ChangeValue(-1); break;
                case Navigation.Right: ChangeValue(1); break;
                case Navigation.Confirm: await ActivateAsync(Current, Current.Selected); break;
                case Navigation.Cancel: CancelEdit(); RefreshMenu(); break;
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
        _draft = setting.Step(_draft, direction);
        setting.Preview?.Invoke(_draft);
        RefreshMenu();
    }

    private void CancelEdit()
    {
        var setting = _editing;
        _editing = null;
        var restore = _restorePreview;
        _restorePreview = null;
        if (restore is not null) restore();
        else setting?.Preview?.Invoke(_original);
    }

    private async Task ActivateAsync(Level level, int index)
    {
        if (_closed || _commandRunning) return;
        var item = level.Entries[index];
        if (item.IsEnabled?.Invoke() == false) return;
        _commandRunning = true;
        try
        {
            if (_editing is not null)
            {
                if (ReferenceEquals(level, Current) && index == level.Selected)
                {
                    if (_draft == _original)
                    {
                        CancelEdit();
                        return;
                    }
                    var setting = _editing;
                    var restore = _restorePreview;
                    _restorePreview = null;
                    _editing = null;
                    try { await setting.Save(_draft); }
                    catch
                    {
                        if (_closed)
                        {
                            if (restore is not null) restore();
                            else setting.Preview?.Invoke(_original);
                        }
                        else
                        {
                            _editing = setting;
                            _restorePreview = restore;
                        }
                        throw;
                    }
                    return;
                }
                CancelEdit();
            }

            var levelIndex = _levels.IndexOf(level);
            if (levelIndex < 0) return;
            _levels.RemoveRange(levelIndex + 1, _levels.Count - levelIndex - 1);
            level.Selected = index;
            if (item.Children is { } children) _levels.Add(new Level(children, item));
            else if (item.Value is { } setting)
            {
                _original = _draft = setting.Read();
                _restorePreview = setting.CaptureRestore?.Invoke();
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
        if (editedEntry is not null && (!ReferenceEquals(previousLevel, Current) || !Current.Entries.Contains(editedEntry)))
            CancelEdit();
        if (_closed) return;
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
            FontSize = 17,
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
            Background = new SolidColorBrush(Color.FromArgb(237, 12, 30, 58)),
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
            if (item.SeparatorBefore) rows.Children.Add(CreateSeparator(new Thickness(16, 5, 16, 5)));
            var enabled = item.IsEnabled?.Invoke() != false;
            var editing = ReferenceEquals(level, Current) && level.Selected == index && _editing is not null;
            var value = item.Value is { } setting ? setting.Format(editing ? _draft : setting.Read()) : item.Status?.Invoke();
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
            if (!editing) rows.Children.Add(button);
            else
            {
                var editRow = new DockPanel();
                AddAdjustmentButton(editRow, "‹", Dock.Left, -1);
                AddAdjustmentButton(editRow, "›", Dock.Right, 1);
                editRow.Children.Add(button);
                rows.Children.Add(editRow);
            }
        }
        return selectedRow;
    }

    private void AddAdjustmentButton(DockPanel row, string text, Dock dock, int direction)
    {
        var button = CreateMenuButton(new TextBlock { Text = text, Foreground = Brushes.White }, true, true);
        button.Padding = new Thickness(8, 6, 8, 6);
        DockPanel.SetDock(button, dock);
        button.Click += (_, _) =>
        {
            try { ChangeValue(direction); }
            catch (Exception error) { _showError?.Invoke(error); }
        };
        row.Children.Add(button);
    }

    private static Button CreateMenuButton(object content, bool selected, bool enabled)
    {
        var button = new Button
        {
            Content = content,
            Template = MenuButtonTemplate,
            Focusable = false,
            IsEnabled = enabled,
            FontSize = 14,
            Foreground = enabled ? Brushes.White : new SolidColorBrush(Color.FromRgb(161, 174, 197)),
            Background = selected ? new LinearGradientBrush(Color.FromRgb(7, 99, 232), Color.FromRgb(5, 66, 181), 90) : Brushes.Transparent,
            BorderBrush = selected ? new SolidColorBrush(Color.FromRgb(27, 176, 248)) : Brushes.Transparent,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 6, 12, 6),
            MinHeight = 35,
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
            FontSize = 15,
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
        Background = new SolidColorBrush(Color.FromArgb(155, 99, 189, 230)),
        Margin = margin
    };

    private static FrameworkElement CreateFooter(FooterHints? hints)
    {
        var footer = new StackPanel { Margin = new Thickness(16, 0, 16, 16) };
        footer.Children.Add(CreateSeparator(new Thickness(0, 0, 0, 7)));
        if (hints is null) return footer;
        var directions = new WrapPanel { Margin = new Thickness(0, 0, 0, 5) };
        foreach (var key in hints.DirectionKeys)
            AddShortcutKey(directions, key);
        if (directions.Children.Count > 0)
        {
            AddHint(directions, hints.NavigateLabel);
            footer.Children.Add(directions);
        }
        var commands = new WrapPanel();
        if (AddShortcutKey(commands, hints.ConfirmKey))
            AddHint(commands, hints.ConfirmLabel);
        if (AddShortcutKey(commands, hints.BackKey))
            AddHint(commands, hints.BackLabel);
        if (commands.Children.Count > 0) footer.Children.Add(commands);
        return footer;

        static bool AddShortcutKey(Panel row, string? cap)
        {
            if (cap is null) return false;
            row.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(155, 176, 211)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(0, 0, 3, 0),
                Child = new TextBlock { Text = cap, FontSize = 10, Foreground = Brushes.White }
            });
            return true;
        }

        static void AddHint(Panel row, string label) => row.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = Brushes.White,
            Margin = new Thickness(1, 0, 9, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
    }
}
