using RetroMediaLab.App.Dictionaries.Tools;
using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.Infrastructure.Commands;
using RetroMediaLab.Infrastructure.Commands.Building;
using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.App.Services.Maintenance;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using RetroMediaLab.Infrastructure.Processes;

namespace RetroMediaLab.App.Views.Windows.Tools;
public partial class GwToolWindow : Window
{
    private readonly string _verb;
    private readonly GwToolSession _session;
    private readonly Dictionary<string, TextBox> _fields = [];
    private readonly Dictionary<string, CheckBox> _checks = [];
    private Task _operation = Task.CompletedTask;
    private Task _closing = Task.CompletedTask;
    private bool _closeRequested;
    private bool _allowClose;
    private readonly Func<string, string, bool> _confirm;
    internal IReadOnlyDictionary<string, TextBox> PresentationFields => _fields;
    internal IReadOnlyDictionary<string, CheckBox> PresentationChecks => _checks;
    internal TextBlock PresentationSummary => Summary;
    internal TextBox PresentationOutput => RawOutput;
    internal TextBox PresentationCommand => CommandText;
    internal Button PresentationExecute => ExecuteButton;
    internal Task PresentationOperation => Task.WhenAll(_operation, _closing);
    public GwToolWindow(
        string executable,
        string verb,
        string? device = null,
        string? drive = null,
        IGreaseweazleRunner? runner = null,
        IGwCommandBuilder? commandBuilder = null,
        ConsoleLogSession? consoleLog = null,
        Func<string, string, bool>? confirm = null,
        Action<Exception, string>? logError = null
    )
    {
        InitializeComponent();
        _verb = verb;
        _session = new GwToolSession(executable, verb, device, drive, runner ?? new GreaseweazleRunner(),
            commandBuilder ?? new GwCommandBuilder(), consoleLog,
            logError ?? ((exception, context) => ErrorLog.Write(exception, context)));
        _confirm = confirm ?? ((message, title) => WpfMessageDialogService.ShowMessage(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes);
        Heading.Text = Title = TitleFor(verb);
        CreateParameters();
        UpdateCommand();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            RequestPresentationClose();
        }

        base.OnClosing(e);
    }

    internal void RequestPresentationClose()
    {
        if (_closeRequested) return;
        _closeRequested = true;
        _session.Cancel();
        _closing = CloseAfterOperationAsync();
    }

    private async Task CloseAfterOperationAsync()
    {
        try { await _operation; }
        finally
        {
            _allowClose = true;
            await Dispatcher.InvokeAsync(Close);
        }
    }

    private void CreateParameters()
    {
        foreach (var parameter in GwToolParameterCatalog.For(_verb))
        {
            var label = L(parameter.LabelResourceKey);
            switch (parameter.Kind)
            {
                case GwToolParameterCatalog.ParameterKind.Required:
                    AddField(parameter.Key, label, parameter.DefaultValue);
                    break;
                case GwToolParameterCatalog.ParameterKind.Optional:
                    AddOptionalField(parameter.Key, label, parameter.DefaultValue);
                    break;
                case GwToolParameterCatalog.ParameterKind.Check:
                    AddCheck(parameter.Key, label);
                    break;
            }
        }
    }

    private void AddField(string key, string label, string value)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 14, 8)
        };
        panel.Children.Add(new TextBlock { Text = label });
        var text = new TextBox
        {
            Text = value,
            Width = 150
        };
        AutomationProperties.SetName(text, label);
        text.TextChanged += (_, _) => UpdateCommand();
        panel.Children.Add(text);
        ParametersPanel.Children.Add(panel);
        _fields[key] = text;
    }

    private void AddOptionalField(string key, string label, string value)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 14, 8)
        };
        var check = new CheckBox
        {
            Content = label
        };
        check.Checked += (_, _) => UpdateCommand();
        check.Unchecked += (_, _) => UpdateCommand();
        panel.Children.Add(check);
        var text = new TextBox
        {
            Text = value,
            Width = 145
        };
        AutomationProperties.SetName(text, label);
        text.TextChanged += (_, _) => UpdateCommand();
        panel.Children.Add(text);
        ParametersPanel.Children.Add(panel);
        _checks[key] = check;
        _fields[key] = text;
    }

    private void AddCheck(string key, string label)
    {
        var check = new CheckBox
        {
            Content = label,
            Margin = new Thickness(0, 8, 16, 8)
        };
        check.Checked += (_, _) => UpdateCommand();
        check.Unchecked += (_, _) => UpdateCommand();
        ParametersPanel.Children.Add(check);
        _checks[key] = check;
    }

    private GwCommand BuildCommand() => _session.BuildCommand(_fields.ToDictionary(x => x.Key, x => x.Value.Text),
        _checks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToHashSet());

    private bool Checked(string key) => _checks.GetValueOrDefault(key)?.IsChecked == true;
    private void UpdateCommand()
    {
        if (CommandText is null)
            return;
        try
        {
            CommandText.Text = BuildCommand().ToDisplayString();
            ExecuteButton.IsEnabled = true;
            Summary.Text = L("Tool.Ready");
        }
        catch (ArgumentException)
        {
            CommandText.Text = L("Tool.InvalidParameters");
            ExecuteButton.IsEnabled = false;
            Summary.Text = L("Tool.InvalidParametersHelp");
        }
    }

    private async void Execute_Click(object sender, RoutedEventArgs e) => await ExecuteAsync();
    internal Task ExecuteAsync()
    {
        if (_closeRequested) return Task.CompletedTask;
        if (_session.IsRunning)
        {
            if (_confirm(LocExtension.Get("Operation.StopConfirm"), LocExtension.Get("Operation.StopTitle")))
                _session.Cancel();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _operation = completion.Task;
        return ExecuteTrackedAsync(completion);
    }

    private async Task ExecuteTrackedAsync(TaskCompletionSource completion)
    {
        try { await RunToolAsync(); }
        finally { completion.SetResult(); }
    }

    private async Task RunToolAsync()
    {
        ExecuteButton.Content = LocExtension.Get("Common.Stop");
        RawOutput.Clear();
        Summary.Text = L("Tool.Running");
        try
        {
            await _session.ExecuteAsync(BuildCommand,
                () => !Checked("bootloader") || _confirm(L("Tool.BootloaderWarning"), L("Tool.BootloaderTitle")),
                publish => Dispatcher.CheckAccess() ? publish() : Dispatcher.InvokeAsync(publish).Task.Unwrap(),
                line => { RawOutput.AppendText(line + Environment.NewLine); RawOutput.ScrollToEnd(); },
                summary => Summary.Text = summary);
        }
        finally { ExecuteButton.Content = LocExtension.Get("Common.Execute"); }
    }

    private static string L(string key) => LocExtension.Get(key);
    private static string TitleFor(string verb) => verb switch
    {
        "info" => L("Tool.Title.Info"),
        "bandwidth" => L("Tool.Title.Bandwidth"),
        "rpm" => L("Tool.Title.Rpm"),
        "seek" => L("Tool.Title.Seek"),
        "pin" => L("Tool.Title.Pin"),
        "reset" => L("Tool.Title.Reset"),
        "delays" => L("Tool.Title.Delays"),
        "update" => L("Tool.Title.Update"),
        "align" => L("Tool.Title.Align"),
        _ => verb
    };
}
