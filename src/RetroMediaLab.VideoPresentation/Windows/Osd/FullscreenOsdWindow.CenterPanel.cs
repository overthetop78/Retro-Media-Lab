using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed partial class FullscreenOsdWindow
{
    private const double MixerGroupGap = 13;
    private const double PreferredFaderWidth = 76;
    private static readonly Brush CenterAccent = Brushes.LightSkyBlue;
    private readonly Border _centerPanel = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        CornerRadius = new CornerRadius(18),
        BorderBrush = CenterAccent,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(24),
        Visibility = Visibility.Collapsed
    };

    private readonly List<(Border Card, Slider Slider, Setting Setting)> _faders = [];
    private readonly Dictionary<Setting, int> _pendingFaders = new();
    private Level? _centerLevel;
    private ScrollViewer? _centerScroll;

    private void FitCenterPanel()
    {
        if (!double.IsFinite(Width) || !double.IsFinite(Height)) return;
        bool mixer = _centerLevel?.Parent?.CenterPanel == CenterPanelKind.Mixer;
        double availableWidth = Math.Max(0, Width - (mixer ? 32 : 48));
        _centerPanel.MaxWidth = mixer ? availableWidth : Math.Min(1100, availableWidth);
        _centerPanel.Width = mixer
            ? Math.Min(availableWidth, 50 + _faders.Count * PreferredFaderWidth
                + (_centerLevel!.Entries.Select(entry => entry.Section).Distinct().Count() - 1) * MixerGroupGap)
            : double.NaN;
        _centerPanel.MaxHeight = Math.Max(0, Math.Min(720, Height - 48));
    }

    private void ShowCenterPanel()
    {
        if (ReferenceEquals(_centerLevel, Current)) return;
        _centerLevel = Current;
        _faders.Clear();
        var parent = Current.Parent!;
        var isMixer = parent.CenterPanel == CenterPanelKind.Mixer;
        var layout = new DockPanel();
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        AddGlyph(header, parent.Glyph, new Thickness(0, 0, 12, 0));
        header.Children.Add(CenterText(parent.Label, 24, Brushes.White));
        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);
        var separator = CreateSeparator(new Thickness(0, 0, 0, 14));
        DockPanel.SetDock(separator, Dock.Top);
        layout.Children.Add(separator);
        if (_hints is { } hints)
        {
            var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            if (isMixer)
            {
                actions.Children.Add(CreateEditHint(hints.HorizontalKeys, hints.NavigateLabel));
                actions.Children.Add(CreateEditHint(hints.VerticalKeys, hints.AdjustLabel));
            }
            AddEditAction(actions, hints.BackKeys, hints.BackLabel, Navigation.Cancel);
            DockPanel.SetDock(actions, Dock.Bottom);
            layout.Children.Add(actions);
        }

        Panel body = isMixer ? new Grid() : new StackPanel();
        foreach (var group in Current.Entries.GroupBy(entry => entry.Section))
        {
            if (body.Children.Count > 0)
                AddSection(new Border
                {
                    Background = CenterAccent,
                    Opacity = .35,
                    Width = isMixer ? 1 : double.NaN,
                    Height = isMixer ? double.NaN : 1,
                    Margin = isMixer ? new Thickness(6, 0, 6, 0) : new Thickness(0, 12, 0, 12)
                }, new GridLength(MixerGroupGap));
            var section = new StackPanel();
            var title = new DockPanel { Margin = new Thickness(0, 0, 0, 9) };
            if (!isMixer) AddGlyph(title, group.First().Glyph, new Thickness(0, 0, 8, 0));
            var sectionLabel = CenterText(group.Key, isMixer ? 14 : 16, CenterAccent);
            if (isMixer)
            {
                sectionLabel.TextWrapping = TextWrapping.NoWrap;
                sectionLabel.TextTrimming = TextTrimming.CharacterEllipsis;
                sectionLabel.ToolTip = group.Key;
            }
            title.Children.Add(sectionLabel);
            section.Children.Add(title);
            if (isMixer)
            {
                var faders = new UniformGrid { Rows = 1 };
                foreach (var entry in group) faders.Children.Add(CreateFader(entry));
                section.Children.Add(faders);
            }
            else
            {
                foreach (var entry in group)
                {
                    var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                    row.ColumnDefinitions.Add(new ColumnDefinition());
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                    var name = CenterText(entry.Label, 14, CenterAccent);
                    name.Margin = new Thickness(0, 0, 12, 0);
                    var value = CenterText(entry.Status?.Invoke(), 15, Brushes.White);
                    Grid.SetColumn(value, 1);
                    row.Children.Add(name);
                    row.Children.Add(value);
                    section.Children.Add(row);
                }
            }
            AddSection(section, new GridLength(group.Count(), GridUnitType.Star));
        }
        _centerScroll = new ScrollViewer
        {
            Focusable = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MinWidth = isMixer ? 0 : Math.Min(500, Math.Max(0, _centerPanel.MaxWidth - 60)),
            Content = body
        };
        layout.Children.Add(_centerScroll);
        _centerPanel.Background = TranslucentBackground(_mainBackground);
        _centerPanel.Child = layout;
        _centerPanel.Visibility = Visibility.Visible;
        FitCenterPanel();
        SelectFader(Current.Selected);

        void AddSection(FrameworkElement element, GridLength width)
        {
            if (body is Grid columns)
            {
                Grid.SetColumn(element, columns.ColumnDefinitions.Count);
                columns.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            }
            body.Children.Add(element);
        }
    }

    private Border CreateFader(Entry entry)
    {
        var setting = entry.Value!;
        var range = setting.Range!.Value;
        var initial = Math.Clamp(setting.Read(), range.Minimum, range.Maximum);
        var index = _faders.Count;
        var value = CenterText(setting.Format(initial), 14, CenterAccent);
        value.HorizontalAlignment = HorizontalAlignment.Center;
        var slider = new Slider
        {
            Orientation = Orientation.Vertical,
            Height = 124,
            Minimum = range.Minimum,
            Maximum = range.Maximum,
            Value = initial,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            IsMoveToPointEnabled = true,
            Focusable = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsEnabled = entry.IsEnabled?.Invoke() != false,
            Margin = new Thickness(0, 6, 0, 6)
        };
        System.Windows.Automation.AutomationProperties.SetName(slider, entry.Label);
        var label = CenterText(entry.Label, 12, Brushes.White);
        label.TextAlignment = TextAlignment.Center;
        label.TextTrimming = TextTrimming.CharacterEllipsis;
        label.MaxHeight = 52;
        var content = new StackPanel { Children = { value, slider, label } };
        var card = new Border
        {
            Margin = new Thickness(1),
            Padding = new Thickness(2, 8, 2, 6),
            ClipToBounds = true,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Background = Brushes.Transparent,
            Child = content,
            ToolTip = entry.Label
        };
        card.PreviewMouseLeftButtonDown += (_, _) => SelectFader(index, scroll: false);
        slider.ValueChanged += (_, args) =>
        {
            RestartIdleTimer();
            var draft = (int)args.NewValue;
            value.Text = setting.Format(draft);
            setting.Preview?.Invoke(draft);
            _pendingFaders[setting] = draft;
            if (_settingSave.IsCompleted) _settingSave = SaveFadersAsync();
        };
        _faders.Add((card, slider, setting));
        return card;
    }

    private async Task SaveFadersAsync()
    {
        await Task.Yield();
        while (_pendingFaders.Count != 0)
        {
            var pending = _pendingFaders.First();
            _pendingFaders.Remove(pending.Key);
            try { await pending.Key.Save(pending.Value); }
            catch (Exception error) { _showError?.Invoke(error); }
        }
    }

    private void SelectFader(int index, bool scroll = true)
    {
        if (_faders.Count == 0) return;
        Current.Selected = Math.Clamp(index, 0, _faders.Count - 1);
        for (var i = 0; i < _faders.Count; i++)
        {
            _faders[i].Card.BorderBrush = i == Current.Selected ? CenterAccent : Brushes.Transparent;
            _faders[i].Card.Background = i == Current.Selected
                ? new SolidColorBrush(Color.FromArgb(46, 65, 152, 236)) : Brushes.Transparent;
        }
        if (scroll) _faders[Current.Selected].Card.BringIntoView();
    }

    private void NavigateCenterPanel(CenterPanelKind panel, Navigation action)
    {
        if (action is Navigation.Cancel or Navigation.Confirm
            || (panel == CenterPanelKind.Information && action == Navigation.Left))
        {
            Back(closeAtRoot: false);
            return;
        }
        if (panel == CenterPanelKind.Information)
        {
            if (action == Navigation.Up) _centerScroll?.LineUp();
            if (action == Navigation.Down) _centerScroll?.LineDown();
            return;
        }
        if (_faders.Count == 0) return;
        if (action is Navigation.Left or Navigation.Right)
        {
            SelectFader(Current.Selected + (action == Navigation.Left ? -1 : 1));
            return;
        }
        var (_, slider, setting) = _faders[Current.Selected];
        if (slider.IsEnabled)
            slider.Value = setting.Step((int)slider.Value, action == Navigation.Up ? 1 : -1);
    }

    private void ClearCenterPanel()
    {
        _centerPanel.Child = null;
        _centerPanel.Visibility = Visibility.Collapsed;
        _centerLevel = null;
        _centerScroll = null;
        _faders.Clear();
        FitCenterPanel();
    }

    private static TextBlock CenterText(string? text, double size, Brush foreground) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = foreground,
        TextWrapping = TextWrapping.Wrap
    };
}
