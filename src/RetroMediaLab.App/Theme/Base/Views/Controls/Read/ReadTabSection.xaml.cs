using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.App.Views.Controls.Options;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Infrastructure.Settings;


namespace RetroMediaLab.App.Views.Controls.Read;

public partial class ReadTabSection : UserControl
{
    public ReadTabSection()
    {
        InitializeComponent();
        ExecuteButton.Click += (_, e) => ExecuteRequested?.Invoke(this, e);
    }

    public event RoutedEventHandler? ExecuteRequested;

    public ReadImageSection ImageBlock => ImageSection;
    public ProfileSection ProfileBlock => ProfileSection;
    public PathSection FolderBlock => FolderSection;
    public ReadFileNameSection FileNameBlock => FileNameSection;
    public ReadAdvancedSection AdvancedBlock => AdvancedSection;
    public ReadCompletionBanner CompletionBlock => CompletionBanner;
    public Button ExecuteActionButton => ExecuteButton;
}
