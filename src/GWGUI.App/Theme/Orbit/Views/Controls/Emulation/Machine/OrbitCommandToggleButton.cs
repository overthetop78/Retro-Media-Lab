using System.Windows.Controls.Primitives;

namespace GWGUI.App.Theme.Orbit.Views;

/// <summary>Only the machine controller can confirm a power, pause or audio state.</summary>
internal sealed class OrbitCommandToggleButton : ToggleButton
{
    public OrbitCommandToggleButton() { }

    protected override void OnToggle() { }
}
