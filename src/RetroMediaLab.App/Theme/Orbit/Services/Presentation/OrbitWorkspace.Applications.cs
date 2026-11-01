using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.Orbit.Views;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Tools;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;
using RetroMediaLab.App.Views.Dialogs.Explorer;

namespace RetroMediaLab.App.Theme.Orbit;

internal sealed partial class OrbitWorkspace
{
    private const string ReadId = "Tab.Read";
    private const string WriteId = "Tab.Write";
    private const string ConversionId = "Tab.Convert";
    private const string VisualizationId = "Tab.Visualize";
    private const string ExplorerId = "Tab.Explorer";
    private const string ToolsId = "Tab.Tools";
    private const string EmulationId = "Tab.Emulation";
    private const string AdvancedTitle = "Common.Advanced";
    private const string InspectorTitle = "Visual.Inspector";
    private const string ClassificationTitle = "Classification.Format";
    private const string InformationTitle = "Menu.Information";
    private static readonly Size PropertiesSize = new(900, 650);
    private EmulationView? library;

    private void BuildApplications()
    {
        var read = Source<ReadTabSection>(ReadId);
        var write = Source<WriteTabSection>(WriteId);
        var conversion = Source<ConversionTabSection>(ConversionId);
        var visualization = Source<VisualizerTabSection>(VisualizationId);
        var explorer = Source<ExplorerSection>(ExplorerId);
        var tools = Source<ToolsTabSection>(ToolsId);
        machineSource = Source<EmulationSection>(EmulationId);
        var status = root.Children.OfType<ApplicationStatusBar>().Single();
        var terminal = root.Children.OfType<TerminalSection>().Single();
        Add(ReadId, new AcquisitionView(read, status,
            () => Properties(ReadId, AdvancedTitle, () => new AcquisitionOptionsView(read.AdvancedBlock)), ShowLibrary));
        Add(WriteId, new WriteView(write, status,
            () => Properties(WriteId, AdvancedTitle, () => new WriteOptionsView(write.AdvancedBlock)), ShowLibrary));
        Add(ConversionId, new ConversionView(conversion, status,
            () => Properties(ConversionId, AdvancedTitle, () => new ConversionOptionsView(conversion.AdvancedBlock)), ShowLibrary));
        Add(VisualizationId, new VisualizationView(visualization,
            () => Properties(VisualizationId, InspectorTitle, () => new InspectorView(visualization.FirstInspector, visualization.SecondInspector,
                () => CloseProperties(VisualizationId, InspectorTitle)), false),
            () => Properties(VisualizationId, ClassificationTitle, () => new ClassificationView(visualization.Header.ClassificationSelector,
                () => CloseProperties(VisualizationId, ClassificationTitle))), ShowLibrary));
        Add(ExplorerId, new ExplorerView(explorer,
            () => Properties(ExplorerId, InformationTitle, () => new ExplorerDetailsView(explorer.DetailsPanel), false),
            () => Properties(ExplorerId, ClassificationTitle, () => new ClassificationView(explorer.ClassificationSelector,
                () => CloseProperties(ExplorerId, ClassificationTitle))),
            () =>
            {
                var issues = new ExplorerIssuesWindow(explorer.CurrentIssues);
                try { TryShowWindow(issues); }
                finally { issues.Close(); }
            }, ShowLibrary));
        Add(ToolsId, new ToolsView(tools, status, ShowLibrary));
        library = new EmulationView(machineSource, ShowLibrary, ShowMachineError);
        documents.Add(library);
        shell.MachineContent = library;
        var journal = new TerminalView(terminal, () => shell.Area = OrbitArea.Machines);
        documents.Add(journal);
        shell.JournalContent = journal;
        var bindings = new ThemeBindingScope();
        documents.Add(bindings);
        bindings.OneWay(status.HardwareText, TextBlock.TextProperty, shell, OrbitShell.HardwareTextProperty);
        bindings.OneWay(status.OperationText, TextBlock.TextProperty, shell, OrbitShell.StatusTextProperty);
    }

    private void Add(string key, FrameworkElement document)
    {
        if (document is IDisposable lifetime) documents.Add(lifetime);
        var tab = new TabItem { Content = document };
        tab.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(key));
        mediaPages.Add(key, tab);
        media.Items.Add(tab);
    }

    private void Properties(string owner, string title, Func<FrameworkElement> create, bool modal = true)
    {
        var key = owner + title;
        if (propertyWindows.FirstOrDefault(window => Equals(window.Tag, key)) is { } existing) { existing.Activate(); return; }
        OrbitWindow? window = null;
        FrameworkElement? document = null;
        OrbitPropertyWindow? frame = null;
        var attached = false;
        try
        {
            document = create();
            frame = new OrbitPropertyWindow(document, () => window?.Close());
            window = new OrbitWindow { Document = frame, Width = PropertiesSize.Width, Height = PropertiesSize.Height,
                Tag = key, Owner = Window.GetWindow(this), ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            window.SetBinding(Window.TitleProperty, LocExtension.CreateBinding(title));
            var ownedDocument = document;
            var ownedFrame = frame;
            var ownedWindow = window;
            EventHandler? closed = null;
            closed = (_, _) =>
            {
                ownedWindow.Closed -= closed;
                propertyWindows.Remove(ownedWindow);
                try { ownedFrame.Dispose(); }
                finally { (ownedDocument as IDisposable)?.Dispose(); }
            };
            window.Closed += closed;
            propertyWindows.Add(window);
            attached = true;
            if (modal) window.ShowDialog(); else window.Show();
        }
        catch
        {
            window?.Close();
            throw;
        }
        finally
        {
            if (!attached)
            {
                try { window?.Close(); }
                finally { try { frame?.Dispose(); } finally { (document as IDisposable)?.Dispose(); } }
            }
        }
    }

    private void CloseProperties(string owner, string title) => propertyWindows.FirstOrDefault(window => Equals(window.Tag, owner + title))?.Close();
    private void ShowLibrary()
    {
        if (disposed) return;
        selectedMachine = null;
        shell.MachineContent = library;
        shell.Area = OrbitArea.Machines;
        shell.ContextTitle = LocExtension.Get(EmulationId);
    }
    private void ShowMachineError(Exception error) => ControlErrorPresenter.ShowEmulation(this, error,
        ControlErrorContexts.EmulationConfigurationOpening, LocExtension.Get(EmulationId));
}
