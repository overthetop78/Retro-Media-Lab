using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Tools;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;
using RetroMediaLab.App.Views.Dialogs.Explorer;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Constants.Emulation.Errors;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal sealed partial class WindowsXPWorkspace
{
    private const string ReadId = "Tab.Read";
    private const string WriteId = "Tab.Write";
    private const string ConversionId = "Tab.Convert";
    private const string VisualizationId = "Tab.Visualize";
    private const string ExplorerId = "Tab.Explorer";
    private const string ToolsId = "Tab.Tools";
    private const string EmulationId = "Tab.Emulation";
    private const string TerminalId = "Common.Console";
    private const string AdvancedTitle = "Common.Advanced";
    private const string InspectorTitle = "Visual.Inspector";
    private const string ClassificationTitle = "Classification.Format";
    private const string InformationTitle = "Menu.Information";
    private static readonly Size ReadSize = new(760, 500);
    private static readonly Size WriteSize = new(720, 440);
    private static readonly Size ConversionSize = new(900, 600);
    private static readonly Size VisualizationSize = new(1000, 660);
    private static readonly Size ExplorerSize = new(1000, 610);
    private static readonly Size ToolsSize = new(660, 400);
    private static readonly Size EmulationSize = new(700, 400);
    private static readonly Size TerminalSize = new(760, 380);
    private static readonly Size PropertiesSize = new(760, 540);
    private static readonly Size InspectorSize = new(560, 460);
    private static readonly Size ClassificationSize = new(480, 350);

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
        Add(ReadId, WindowsXPProgramGroup.Media, ReadSize, close => new AcquisitionView(read, status,
            () => Properties(ReadId, AdvancedTitle, PropertiesSize, () => new AcquisitionOptionsView(read.AdvancedBlock, scheme: Scheme)), close, scheme: Scheme));
        Add(WriteId, WindowsXPProgramGroup.Media, WriteSize, close => new WriteView(write, status,
            () => Properties(WriteId, AdvancedTitle, PropertiesSize, () => new WriteOptionsView(write.AdvancedBlock, scheme: Scheme)), close, scheme: Scheme));
        Add(ConversionId, WindowsXPProgramGroup.Media, ConversionSize, close => new ConversionView(conversion, status,
            () => Properties(ConversionId, AdvancedTitle, PropertiesSize, () => new ConversionOptionsView(conversion.AdvancedBlock, scheme: Scheme)), close, scheme: Scheme));
        Add(VisualizationId, WindowsXPProgramGroup.Media, VisualizationSize, close => new VisualizationView(visualization,
            () => Properties(VisualizationId, InspectorTitle, InspectorSize,
                () => new InspectorView(visualization.FirstInspector, visualization.SecondInspector, () => CloseProperties(VisualizationId, InspectorTitle), scheme: Scheme), false),
            () => Properties(VisualizationId, ClassificationTitle, ClassificationSize,
                () => new ClassificationView(visualization.Header.ClassificationSelector, () => CloseProperties(VisualizationId, ClassificationTitle), scheme: Scheme)), close, scheme: Scheme));
        Add(ExplorerId, WindowsXPProgramGroup.Media, ExplorerSize, close => new ExplorerView(explorer,
            () => Properties(ExplorerId, InformationTitle, InspectorSize, () => new ExplorerDetailsView(explorer.DetailsPanel, scheme: Scheme), false),
            () => Properties(ExplorerId, ClassificationTitle, ClassificationSize,
                () => new ClassificationView(explorer.ClassificationSelector, () => CloseProperties(ExplorerId, ClassificationTitle), scheme: Scheme)),
            () =>
            {
                var issues = new ExplorerIssuesWindow(explorer.CurrentIssues);
                try { TryShowWindow(issues); }
                finally { issues.Close(); }
            }, close, scheme: Scheme));
        Add(ToolsId, WindowsXPProgramGroup.System, ToolsSize, close => new ToolsView(tools, status, close, scheme: Scheme));
        Add(EmulationId, WindowsXPProgramGroup.Emulation, EmulationSize, close => new EmulationView(machineSource, close, ShowMachineError, scheme: Scheme));
        Add(TerminalId, WindowsXPProgramGroup.System, TerminalSize, close => new TerminalView(terminal, close, scheme: Scheme));
    }

    private void Add(string id, WindowsXPProgramGroup group, Size size, Func<Action, FrameworkElement> create) =>
        launchers.Add(new(id, group, LocExtension.Get(id), WindowsXPApplicationIcons.ForResource(id), () => Open(id, id, size, create)));

    private void Properties(string owner, string title, Size size, Func<FrameworkElement> create, bool modal = true) =>
        Open(owner + title, title, size, close => CreateProperty(create, close), modal);

    private FrameworkElement CreateProperty(Func<FrameworkElement> create, Action close)
    {
        var document = create();
        try
        {
            var frame = new WindowsXPPropertyWindow(document, close, Scheme);
            if (document is IDisposable disposable) propertyDocuments.Add(frame, disposable);
            return frame;
        }
        catch { (document as IDisposable)?.Dispose(); throw; }
    }

    private void CloseProperties(string owner, string title)
    {
        if (applications.TryGetValue(owner + title, out var entry)) entry.Window.Close();
    }

    private void ShowMachineError(Exception error) => ControlErrorPresenter.ShowEmulation(this, error,
        ControlErrorContexts.EmulationConfigurationOpening, LocExtension.Get(EmulationId));
}
