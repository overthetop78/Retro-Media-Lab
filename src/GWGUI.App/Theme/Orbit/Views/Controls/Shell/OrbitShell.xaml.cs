using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.Orbit.Views;

/// <summary>Hosts borrowed presentations. Their owners retain their lifetime and functional controllers.</summary>
internal partial class OrbitShell : UserControl, IDisposable
{
    internal static readonly DependencyProperty AreaProperty = DependencyProperty.Register(nameof(Area), typeof(OrbitArea), typeof(OrbitShell),
        new PropertyMetadata(OrbitArea.Machines, (target, _) => ((OrbitShell)target).UpdateArea()), value => Enum.IsDefined((OrbitArea)value));
    internal static readonly DependencyProperty MachineContentProperty = ContentPropertyFor(nameof(MachineContent));
    internal static readonly DependencyProperty MediaContentProperty = ContentPropertyFor(nameof(MediaContent));
    internal static readonly DependencyProperty JournalContentProperty = ContentPropertyFor(nameof(JournalContent));
    internal static readonly DependencyProperty CommandContentProperty = ContentPropertyFor(nameof(CommandContent));
    internal static readonly DependencyProperty ContextContentProperty = ContentPropertyFor(nameof(ContextContent));
    internal static readonly DependencyProperty SessionContentProperty = ContentPropertyFor(nameof(SessionContent));
    internal static readonly DependencyProperty ContextTitleProperty = TextPropertyFor(nameof(ContextTitle));
    internal static readonly DependencyProperty StatusTextProperty = TextPropertyFor(nameof(StatusText));
    internal static readonly DependencyProperty HardwareTextProperty = TextPropertyFor(nameof(HardwareText));
    private OrbitResources? resources;
    private OrbitArea lastWorkArea = OrbitArea.Machines;
    private bool journalOpen;
    private bool contextOpen;
    private bool disposed;

    public OrbitArea Area { get => (OrbitArea)GetValue(AreaProperty); set => SetValue(AreaProperty, value); }
    public FrameworkElement? MachineContent { get => (FrameworkElement?)GetValue(MachineContentProperty); set => SetValue(MachineContentProperty, value); }
    public FrameworkElement? MediaContent { get => (FrameworkElement?)GetValue(MediaContentProperty); set => SetValue(MediaContentProperty, value); }
    public FrameworkElement? JournalContent { get => (FrameworkElement?)GetValue(JournalContentProperty); set => SetValue(JournalContentProperty, value); }
    public FrameworkElement? CommandContent { get => (FrameworkElement?)GetValue(CommandContentProperty); set => SetValue(CommandContentProperty, value); }
    public FrameworkElement? ContextContent { get => (FrameworkElement?)GetValue(ContextContentProperty); set => SetValue(ContextContentProperty, value); }
    public FrameworkElement? SessionContent { get => (FrameworkElement?)GetValue(SessionContentProperty); set => SetValue(SessionContentProperty, value); }
    public string ContextTitle { get => (string)GetValue(ContextTitleProperty); set => SetValue(ContextTitleProperty, value); }
    public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    public string HardwareText { get => (string)GetValue(HardwareTextProperty); set => SetValue(HardwareTextProperty, value); }
    internal event EventHandler? AreaChanged;

    internal OrbitShell(bool? dark = null)
    {
        try
        {
            resources = new OrbitResources(this, dark);
            InitializeComponent();
            PreviewKeyDown += PreviewKeys;
            UpdateArea();
        }
        catch { Dispose(); throw; }
    }

    private static DependencyProperty ContentPropertyFor(string name) => DependencyProperty.Register(name, typeof(FrameworkElement), typeof(OrbitShell),
        new PropertyMetadata(null, (target, _) => ((OrbitShell)target).UpdateArea()));
    private static DependencyProperty TextPropertyFor(string name) => DependencyProperty.Register(name, typeof(string), typeof(OrbitShell), new PropertyMetadata(string.Empty));

    private void UpdateArea()
    {
        if (!IsInitialized || disposed) return;
        if (Area == OrbitArea.Journal) journalOpen = true;
        else lastWorkArea = Area;
        Machines.IsChecked = lastWorkArea == OrbitArea.Machines;
        Media.IsChecked = lastWorkArea == OrbitArea.Media;
        Commands.IsChecked = lastWorkArea == OrbitArea.Commands;
        Journal.IsChecked = journalOpen;
        Work.Content = lastWorkArea switch
        {
            OrbitArea.Machines => MachineContent,
            OrbitArea.Media => MediaContent,
            OrbitArea.Commands => CommandContent,
            _ => null
        };
        var heading = lastWorkArea switch
        {
            OrbitArea.Machines => "Options.Themes.Orbit.Machines",
            OrbitArea.Media => "Options.Themes.Orbit.Media",
            _ => "Options.Themes.Orbit.Commands"
        };
        Heading.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(heading));
        EmptyMachine.Visibility = lastWorkArea == OrbitArea.Machines && Work.Content is null ? Visibility.Visible : Visibility.Collapsed;
        JournalPanel.Visibility = journalOpen ? Visibility.Visible : Visibility.Collapsed;
        Drawer.Visibility = contextOpen && ContextContent is not null ? Visibility.Visible : Visibility.Collapsed;
        ContextToggle.IsEnabled = ContextContent is not null;
        ContextToggle.Visibility = ContextContent is null ? Visibility.Collapsed : Visibility.Visible;
        NoSessions.Visibility = SessionContent is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Navigate(object sender, RoutedEventArgs args)
    {
        if (disposed || sender is not FrameworkElement { Tag: OrbitArea area }) return;
        if (area == OrbitArea.Journal)
        {
            journalOpen = !journalOpen;
            if (Area == OrbitArea.Journal) Area = lastWorkArea;
        }
        else Area = area;
        UpdateArea();
        AreaChanged?.Invoke(this, EventArgs.Empty);
    }
    private void CloseJournal(object sender, RoutedEventArgs args) { journalOpen = false; if (Area == OrbitArea.Journal) Area = lastWorkArea; UpdateArea(); }
    private void ToggleContext(object sender, RoutedEventArgs args) { contextOpen = !contextOpen; UpdateArea(); }
    private void PreviewKeys(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            args.Handled = true;
            Area = OrbitArea.Commands;
            AreaChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    internal void SetAppearance(bool dark) => resources?.SetAppearance(dark);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            PreviewKeyDown -= PreviewKeys;
            if (Machines is not null) Machines.Click -= Navigate;
            if (Media is not null) Media.Click -= Navigate;
            if (Journal is not null) Journal.Click -= Navigate;
            if (Commands is not null) Commands.Click -= Navigate;
            if (ContextToggle is not null) ContextToggle.Click -= ToggleContext;
            if (JournalClose is not null) JournalClose.Click -= CloseJournal;
            if (Work is not null) Work.Content = null;
            foreach (var property in new[] { MachineContentProperty, MediaContentProperty, JournalContentProperty,
                         CommandContentProperty, ContextContentProperty, SessionContentProperty }) ClearValue(property);
            AreaChanged = null;
            Content = null;
        }
        finally { try { resources?.Dispose(); } finally { resources = null; } }
    }
}
