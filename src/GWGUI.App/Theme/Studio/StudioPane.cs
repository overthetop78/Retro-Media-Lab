using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using GWGUI.App.Views.Controls.Common;

namespace GWGUI.App.Theme.Studio;

internal sealed class StudioPane : Border
{
    private readonly ContentControl body;
    private readonly Border caption;
    internal bool HasPlacement { get; set; }
    internal bool IsMaximized { get; set; }
    internal Rect RestoreBounds { get; set; }
    internal FrameworkElement View { get; }
    internal bool IsExclusive => View is GWGUI.App.Views.Controls.Emulation.Machine.EmulationSection;

    internal StudioPane(MainTabHeader header, FrameworkElement view, StudioWorkspace owner, int index)
    {
        View = view;
        ClipToBounds = true;
        SetResourceReference(StyleProperty, StudioConstants.PaneStyle);
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(StudioConstants.CaptionHeight) });
        layout.RowDefinitions.Add(new RowDefinition());
        var titleBar = new Grid();
        titleBar.ColumnDefinitions.Add(new ColumnDefinition());
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caption = new Border { Child = titleBar };
        caption.SetResourceReference(BackgroundProperty, StudioConstants.TitleBrush);
        var drag = new Thumb();
        drag.SetResourceReference(StyleProperty, StudioConstants.GripStyle);
        drag.DragDelta += (_, args) => owner.Move(index, args.HorizontalChange, args.VerticalChange);
        drag.MouseDoubleClick += (_, _) => owner.ToggleMaximize(index);
        drag.KeyDown += (_, args) =>
        {
            var horizontal = args.Key == Key.Left ? -StudioConstants.KeyboardStep : args.Key == Key.Right ? StudioConstants.KeyboardStep : 0;
            var vertical = args.Key == Key.Up ? -StudioConstants.KeyboardStep : args.Key == Key.Down ? StudioConstants.KeyboardStep : 0;
            if (horizontal == 0 && vertical == 0) return;
            owner.Move(index, horizontal, vertical);
            args.Handled = true;
        };
        titleBar.Children.Add(drag);
        var title = new TextBlock { Margin = new Thickness(12, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        title.SetBinding(TextBlock.TextProperty, new Binding(nameof(MainTabHeader.Text)) { Source = header });
        title.SetResourceReference(TextBlock.ForegroundProperty, StudioConstants.CaptionTextBrush);
        titleBar.Children.Add(title);
        var commands = new StackPanel { Orientation = Orientation.Horizontal };
        Grid.SetColumn(commands, 1);
        commands.Children.Add(StudioCaptionBar.Command(StudioConstants.MinimizeGlyph, StudioConstants.MinimizeResource, () => owner.Minimize(index)));
        var maximize = StudioCaptionBar.Command(StudioConstants.MaximizeGlyph, StudioConstants.MaximizeResource, () => owner.ToggleMaximize(index));
        maximize.IsEnabled = !IsExclusive;
        commands.Children.Add(maximize);
        commands.Children.Add(StudioCaptionBar.Command(StudioConstants.CloseGlyph, StudioConstants.CloseResource, () => owner.Minimize(index)));
        titleBar.Children.Add(commands);
        layout.Children.Add(caption);
        body = new ContentControl { Content = view, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        Grid.SetRow(body, 1);
        layout.Children.Add(body);
        var resize = new Thumb { Width = StudioConstants.GripSize, Height = StudioConstants.GripSize, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Visibility = IsExclusive ? Visibility.Collapsed : Visibility.Visible };
        resize.SetResourceReference(StyleProperty, StudioConstants.ResizeStyle);
        resize.DragDelta += (_, args) => owner.Resize(index, args.HorizontalChange, args.VerticalChange);
        Grid.SetRow(resize, 1);
        layout.Children.Add(resize);
        Child = layout;
        PreviewMouseDown += (_, _) => owner.Activate(index);
    }

    internal void SetActive(bool active) => caption.SetResourceReference(BackgroundProperty,
        active ? StudioConstants.ActiveTitleBrush : StudioConstants.TitleBrush);

    internal void Release()
    {
        try { body.Content = null; }
        finally { Child = null; }
    }
}
