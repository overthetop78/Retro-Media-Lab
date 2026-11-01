using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;
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

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95;

internal sealed partial class Windows95Workspace
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
        Add(ReadId, Windows95ProgramGroup.Media, ReadSize, close => new AcquisitionView(read, status,
            () => Properties(ReadId, AdvancedTitle, PropertiesSize, () => new AcquisitionOptionsView(read.AdvancedBlock)), close));
        Add(WriteId, Windows95ProgramGroup.Media, WriteSize, close => new WriteView(write, status,
            () => Properties(WriteId, AdvancedTitle, PropertiesSize, () => new WriteOptionsView(write.AdvancedBlock)), close));
        Add(ConversionId, Windows95ProgramGroup.Media, ConversionSize, close => new ConversionView(conversion, status,
            () => Properties(ConversionId, AdvancedTitle, PropertiesSize, () => new ConversionOptionsView(conversion.AdvancedBlock)), close));
        Add(VisualizationId, Windows95ProgramGroup.Media, VisualizationSize, close => new VisualizationView(visualization,
            () => Properties(VisualizationId, InspectorTitle, InspectorSize,
                () => new InspectorView(visualization.FirstInspector, visualization.SecondInspector, () => CloseProperties(VisualizationId, InspectorTitle)), false),
            () => Properties(VisualizationId, ClassificationTitle, ClassificationSize,
                () => new ClassificationView(visualization.Header.ClassificationSelector, () => CloseProperties(VisualizationId, ClassificationTitle))), close));
        Add(ExplorerId, Windows95ProgramGroup.Media, ExplorerSize, close => new ExplorerView(explorer,
            () => Properties(ExplorerId, InformationTitle, InspectorSize, () => new ExplorerDetailsView(explorer.DetailsPanel), false),
            () => Properties(ExplorerId, ClassificationTitle, ClassificationSize,
                () => new ClassificationView(explorer.ClassificationSelector, () => CloseProperties(ExplorerId, ClassificationTitle))),
            () =>
            {
                var issues = new ExplorerIssuesWindow(explorer.CurrentIssues);
                try { TryShowWindow(issues); }
                finally { issues.Close(); }
            }, close));
        Add(ToolsId, Windows95ProgramGroup.System, ToolsSize, close => new ToolsView(tools, status, close));
        Add(EmulationId, Windows95ProgramGroup.Emulation, EmulationSize, close => new EmulationView(machineSource, close, ShowMachineError));
        Add(TerminalId, Windows95ProgramGroup.System, TerminalSize, close => new TerminalView(terminal, close));
    }

    private void Add(string id, Windows95ProgramGroup group, Size size, Func<Action, FrameworkElement> create) =>
        launchers.Add(new(id, group, LocExtension.Get(id), Windows95ApplicationIcons.ForResource(id), () => Open(id, id, size, create)));

    private void Properties(string owner, string title, Size size, Func<FrameworkElement> create, bool modal = true) =>
        Open(owner + title, title, size, close => new Windows95PropertyWindow(create(), close), modal);

    private void CloseProperties(string owner, string title)
    {
        if (applications.TryGetValue(owner + title, out var entry)) entry.Window.Close();
    }

    private void ShowMachineError(Exception error) => ControlErrorPresenter.ShowEmulation(this, error,
        ControlErrorContexts.EmulationConfigurationOpening, LocExtension.Get(EmulationId));
}
