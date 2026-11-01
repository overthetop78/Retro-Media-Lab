using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using GWGUI.App.Contracts.Machine;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.Common;

/// <summary>Presentation endpoints used by the existing video, input and media controllers.
/// Each theme owns its layout; the machine controller owns and releases the presentation.</summary>
internal abstract class EmulationMachinePresentation : UserControl, IDisposable
{
    internal abstract DockPanel Toolbar { get; }
    internal abstract Grid VideoHost { get; }
    internal abstract Border Screen { get; }
    internal abstract Grid DisplayHost { get; }
    internal abstract IReadOnlyDictionary<string, Ellipse> DeviceLeds { get; }
    internal abstract void AttachCommands(MachineCommandPresentation commands);

    internal abstract void SetVideoView(FrameworkElement view);
    internal abstract FrameworkElement? DetachVideoView();
    internal abstract void SetShaderLoading(bool isLoading);
    internal abstract void SetDevices(IEnumerable<MachineViewDevice> devices, Action<Exception> showError, Action restoreFocus);
    internal abstract void SetDeviceStatus(string key, string status);
    internal abstract void SetCassetteTransport(string key, EmulationCassetteState state,
        EmulationCassetteCommand? activeOperation, IReadOnlySet<EmulationCassetteCommand> supportedCommands, bool powered);

    public abstract void Dispose();
}
