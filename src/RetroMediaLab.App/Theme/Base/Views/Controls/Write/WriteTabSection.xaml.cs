using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.App.Views.Controls.Options;
using System.Windows;
using System.Windows.Controls;


namespace RetroMediaLab.App.Views.Controls.Write;

public partial class WriteTabSection : UserControl
{
    public WriteTabSection()
    {
        InitializeComponent();
        ExecuteButton.Click += (_, e) => ExecuteRequested?.Invoke(this, e);
    }

    public event RoutedEventHandler? ExecuteRequested;

    public WriteAdvancedSection AdvancedBlock => AdvancedSection;
    public PathSection SourceBlock => SourceSection;
    public ProfileSection ProfileBlock => ProfileSection;
    public WriteFormatSection FormatBlock => FormatSection;
    public Button ExecuteActionButton => ExecuteButton;
}
