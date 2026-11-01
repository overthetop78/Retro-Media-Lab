using System.Windows;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;
using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Views.Controls.Tools;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>New media presentations share the existing operational endpoints, not Base containers.</summary>
internal static class Windows31MediaApplications
{
    internal static IReadOnlyList<Windows31ApplicationDefinition> Create(Windows31Desktop desktop,
        ReadTabSection read, WriteTabSection write, ConversionTabSection conversion,
        VisualizerTabSection visualization, ApplicationStatusBar status, ExplorerSection? explorer = null,
        ToolsTabSection? tools = null, EmulationSection? emulation = null)
    {
        List<Windows31ApplicationDefinition> applications =
        [
            new(Windows31ApplicationId.Read, Windows31ProgramGroup.Media, ReadTitle,
                () => Own(new AcquisitionView(read, status,
                    () => Options(Windows31ApplicationId.Read, Windows31DialogKind.AcquisitionOptions,
                        ReadOptionsSize, () => Own(new AcquisitionOptionsView(read.AdvancedBlock))),
                    () => desktop.CloseApplication(Windows31ApplicationId.Read))), ReadSize),
            new(Windows31ApplicationId.Write, Windows31ProgramGroup.Media, WriteTitle,
                () => Own(new WriteView(write, status,
                    () => Options(Windows31ApplicationId.Write, Windows31DialogKind.WriteOptions,
                        WriteOptionsSize, () => Own(new WriteOptionsView(write.AdvancedBlock))),
                    () => desktop.CloseApplication(Windows31ApplicationId.Write))), WriteSize),
            new(Windows31ApplicationId.Conversion, Windows31ProgramGroup.Media, ConversionTitle,
                () => Own(new ConversionView(conversion, status,
                    () => Options(Windows31ApplicationId.Conversion, Windows31DialogKind.ConversionOptions,
                        ConversionOptionsSize, () => Own(new ConversionOptionsView(conversion.AdvancedBlock))),
                    () => desktop.CloseApplication(Windows31ApplicationId.Conversion))), ConversionSize),
            new(Windows31ApplicationId.Visualization, Windows31ProgramGroup.Media, VisualizationTitle,
                () => Own(new VisualizationView(visualization,
                    () => desktop.ShowDialog(Windows31ApplicationId.Visualization, Windows31DialogKind.Inspector,
                        Windows31DialogMode.Modeless, InspectorTitle, InspectorSize,
                        close => Own(new InspectorView(visualization.FirstInspector, visualization.SecondInspector, close))),
                    () => desktop.ShowDialog(Windows31ApplicationId.Visualization, Windows31DialogKind.Classification,
                        Windows31DialogMode.Modal, ClassificationTitle, ClassificationSize,
                        close => Own(new ClassificationView(visualization.Header.ClassificationSelector, close))),
                    () => desktop.CloseApplication(Windows31ApplicationId.Visualization))), VisualizationSize)
        ];

        if (explorer is not null)
            applications.Add(new(Windows31ApplicationId.Explorer, Windows31ProgramGroup.Media, ExplorerTitle,
                () => Own(new ExplorerView(explorer,
                    () => desktop.ShowDialog(Windows31ApplicationId.Explorer, Windows31DialogKind.Information,
                        Windows31DialogMode.Modeless, InformationTitle, InformationSize(explorer),
                        close => Own(new Windows31DialogForm(Own(new ExplorerDetailsView(explorer.DetailsPanel)), close))),
                    () => desktop.ShowDialog(Windows31ApplicationId.Explorer, Windows31DialogKind.Classification,
                        Windows31DialogMode.Modal, ClassificationTitle, ClassificationSize,
                        close => Own(new ClassificationView(explorer.ClassificationSelector, close))),
                    () =>
                    {
                        var window = new RetroMediaLab.App.Views.Dialogs.Explorer.ExplorerIssuesWindow(explorer.CurrentIssues);
                        try { desktop.TryShowWindow(window); }
                        finally { RetroMediaLab.App.Services.Windows.WpfWindowNavigationService.CloseOwnedDialog(window); }
                    },
                    () => desktop.CloseApplication(Windows31ApplicationId.Explorer))), ExplorerSize));
        if (tools is not null)
            applications.Add(new(Windows31ApplicationId.Tools, Windows31ProgramGroup.System, ToolsTitle,
                () => Own(new ToolsView(tools, status,
                    () => desktop.CloseApplication(Windows31ApplicationId.Tools))), ToolsSize));
        if (emulation is not null) desktop.ConnectMachines(emulation);
        return applications;

        void Options(Windows31ApplicationId owner, Windows31DialogKind kind, Size size, Func<Windows31Document> create)
        {
            desktop.ShowDialog(owner, kind, Windows31DialogMode.Modal, OptionsTitle, size,
                close => Own(new Windows31DialogForm(create(), close)));
        }
    }

    private static Windows31Document Own<T>(T view) where T : FrameworkElement, IDisposable => new(view, view);

    private static Size InformationSize(ExplorerSection source) => new(InformationWidth,
        Math.Clamp(InformationBaseHeight + source.DetailsPanel.Presentation.Rows.Count * InformationRowHeight,
            InformationMinimumHeight, InformationMaximumHeight));

    private const double InformationWidth = 540;
    private const double InformationBaseHeight = 105;
    private const double InformationRowHeight = 22;
    private const double InformationMinimumHeight = 260;
    private const double InformationMaximumHeight = 500;

    private const string ReadTitle = "Tab.Read";
    private const string WriteTitle = "Tab.Write";
    private const string ConversionTitle = "Tab.Convert";
    private const string VisualizationTitle = "Tab.Visualize";
    private const string ExplorerTitle = "Tab.Explorer";
    private const string ToolsTitle = "Tab.Tools";
    private const string EmulationTitle = "Tab.Emulation";
    private const string InformationTitle = "Menu.Information";
    private const string WarningsTitle = "Explorer.Warnings";
    private const string InspectorTitle = "Visual.Inspector";
    private const string ClassificationTitle = "Classification.Format";
    private const string OptionsTitle = "Common.Advanced";
    private static readonly Size ReadSize = new(850, 600);
    private static readonly Size WriteSize = new(740, 360);
    private static readonly Size ConversionSize = new(860, 580);
    private static readonly Size VisualizationSize = new(1000, 660);
    private static readonly Size ExplorerSize = new(1000, 610);
    private static readonly Size ToolsSize = new(620, 380);
    private static readonly Size EmulationSize = new(700, 400);
    private static readonly Size WarningsSize = new(700, 450);
    private static readonly Size ReadOptionsSize = new(900, 580);
    private static readonly Size WriteOptionsSize = new(620, 530);
    private static readonly Size ConversionOptionsSize = new(600, 380);
    private static readonly Size InspectorSize = new(540, 440);
    private static readonly Size ClassificationSize = new(460, 330);
}
