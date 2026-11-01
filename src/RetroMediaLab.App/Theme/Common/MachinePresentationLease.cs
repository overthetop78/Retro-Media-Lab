using System.Windows.Controls;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;

namespace RetroMediaLab.App.Theme.Common;

/// <summary>Borrows a machine presentation for a theme window. The machine and its controller stay owned by their source.</summary>
internal sealed class MachinePresentationLease : IDisposable
{
    private readonly MachineController controller;
    private readonly ContentControl previousHost;
    private readonly Func<EmulationMachinePresentation> previousFactory;
    private readonly Func<bool>? previousActivation;
    private bool disposed;

    internal MachinePresentationLease(MachineController controller, ContentControl host,
        Func<EmulationMachinePresentation> create, Func<bool> isActive)
    {
        this.controller = controller;
        previousHost = controller.PresentationHost;
        previousFactory = controller.PresentationFactory;
        previousActivation = controller.PresentationActivation;
        var attached = false;
        try
        {
            controller.SetPresentation(create);
            controller.MovePresentationTo(host);
            controller.PresentationActivation = isActive;
            attached = true;
        }
        finally { if (!attached) Dispose(); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        controller.PresentationActivation = previousActivation;
        try { controller.MovePresentationTo(previousHost); }
        finally
        {
            if (!controller.IsStopped) controller.SetPresentation(previousFactory);
        }
    }
}
