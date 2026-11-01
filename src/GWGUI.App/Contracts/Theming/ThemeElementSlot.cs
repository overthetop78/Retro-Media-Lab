using System.Windows;

namespace GWGUI.App.Contracts.Theming;

/// <summary>References a functional element, never the original form containing it.</summary>
internal sealed record ThemeElementSlot(string Id, FrameworkElement Element, ThemeElementRole Role,
    FrameworkElement? VisibilitySource = null);
