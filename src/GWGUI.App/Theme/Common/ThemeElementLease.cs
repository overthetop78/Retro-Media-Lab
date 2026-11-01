using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using GWGUI.App.Contracts.Theming;

namespace GWGUI.App.Theme.Common;

/// <summary>Preserves the existing data and actions while an independent form presents their elements.</summary>
internal sealed class ThemeElementLease : IDisposable
{
    private sealed record PropertyState(DependencyProperty Property, object LocalValue, object Value,
        BindingBase? Binding, bool IsExpression);

    private sealed class Placement(ThemeElementSlot slot, FrameworkElement parent, int index)
    {
        internal ThemeElementSlot Slot { get; } = slot;
        internal FrameworkElement Parent { get; } = parent;
        internal int Index { get; } = index;
        internal ThemeElementHost? Host { get; set; }
        internal bool PinnedDataContext { get; set; }
        internal Dictionary<DependencyProperty, PropertyState> Properties { get; } = [];
    }

    private const string RendererNamespacePrefix = "SkiaSharp.Views.";

    private static readonly IReadOnlySet<DependencyProperty> PresentationProperties = new HashSet<DependencyProperty>
    {
        FrameworkElement.StyleProperty,
        FrameworkElement.MarginProperty,
        FrameworkElement.WidthProperty,
        FrameworkElement.HeightProperty,
        FrameworkElement.MinWidthProperty,
        FrameworkElement.MinHeightProperty,
        FrameworkElement.MaxWidthProperty,
        FrameworkElement.MaxHeightProperty,
        FrameworkElement.HorizontalAlignmentProperty,
        FrameworkElement.VerticalAlignmentProperty,
        FrameworkElement.LayoutTransformProperty,
        Control.BackgroundProperty,
        Control.ForegroundProperty,
        Control.BorderBrushProperty,
        Control.BorderThicknessProperty,
        Control.PaddingProperty,
        Control.FontFamilyProperty,
        Control.FontSizeProperty,
        Control.FontWeightProperty,
        Control.FontStyleProperty,
        Control.HorizontalContentAlignmentProperty,
        Control.VerticalContentAlignmentProperty
    };

    private readonly List<Placement> placements = [];
    private readonly Dictionary<string, ThemeElementHost> hosts = new(StringComparer.Ordinal);
    private bool disposed;

    internal ThemeElementHost this[string id] => hosts[id];

    internal ThemeElementLease(IEnumerable<ThemeElementSlot> slots)
    {
        try
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var elements = new HashSet<FrameworkElement>();
            foreach (var slot in slots)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(slot.Id);
                ArgumentNullException.ThrowIfNull(slot.Element);
                if (!ids.Add(slot.Id) || !elements.Add(slot.Element) || !IsFunctionalElement(slot))
                    throw new ArgumentException(nameof(slots));
                var parent = slot.Element.Parent as FrameworkElement
                    ?? throw new InvalidOperationException(nameof(FrameworkElement.Parent));
                if (parent is ThemeElementHost) throw new InvalidOperationException(nameof(ThemeElementHost));
                var index = Index(parent, slot.Element);
                placements.Add(new Placement(slot, parent, index));
            }

            // Keep inherited bindings alive during both parent transitions, including selection updates.
            foreach (var placement in placements)
            {
                if (placement.Slot.Element.ReadLocalValue(FrameworkElement.DataContextProperty) != DependencyProperty.UnsetValue) continue;
                placement.PinnedDataContext = true;
                BindingOperations.SetBinding(placement.Slot.Element, FrameworkElement.DataContextProperty,
                    new Binding(nameof(FrameworkElement.DataContext)) { Source = placement.Parent });
            }

            // Capture every sibling position before removing the first element.
            foreach (var placement in placements) Remove(placement.Parent, placement.Slot.Element);
            foreach (var placement in placements)
            {
                placement.Host = new ThemeElementHost(placement.Slot, placement.Parent,
                    (property, value) => SetPresentationValue(placement, property, value));
                hosts.Add(placement.Slot.Id, placement.Host);
            }
        }
        catch { Dispose(); throw; }
    }

    private static bool IsFunctionalElement(ThemeElementSlot slot)
    {
        if (slot.Element is UserControl or Panel or Decorator or ThemeElementHost) return false;
        return slot.Role switch
        {
            ThemeElementRole.Input => slot.Element is TextBoxBase or Selector or ToggleButton or RangeBase or PasswordBox
                && slot.Element is not TabControl,
            ThemeElementRole.Action => slot.Element is Button,
            ThemeElementRole.Information => slot.Element is TextBlock or Image or ProgressBar,
            ThemeElementRole.Document => slot.Element is TextBoxBase or DocumentViewerBase or Image or HwndHost
                || slot.Element.GetType().Namespace?.StartsWith(RendererNamespacePrefix, StringComparison.Ordinal) == true,
            _ => false
        };
    }

    private static int Index(FrameworkElement parent, FrameworkElement element) => parent switch
    {
        Panel panel => panel.Children.IndexOf(element),
        Decorator decorator when ReferenceEquals(decorator.Child, element) => 0,
        ContentControl content when ReferenceEquals(content.Content, element) => 0,
        _ => throw new InvalidOperationException(nameof(FrameworkElement.Parent))
    };

    private static void Remove(FrameworkElement parent, FrameworkElement element)
    {
        if (parent is Panel panel) panel.Children.Remove(element);
        else if (parent is Decorator decorator && ReferenceEquals(decorator.Child, element)) decorator.Child = null;
        else if (parent is ContentControl content && ReferenceEquals(content.Content, element)) content.Content = null;
    }

    private static void Restore(Placement placement)
    {
        var element = placement.Slot.Element;
        if (ReferenceEquals(element.Parent, placement.Parent)) return;
        if (placement.Parent is Panel panel)
            panel.Children.Insert(Math.Min(placement.Index, panel.Children.Count), element);
        else if (placement.Parent is Decorator { Child: null } decorator) decorator.Child = element;
        else if (placement.Parent is ContentControl { Content: null } content) content.Content = element;
        else throw new InvalidOperationException(nameof(FrameworkElement.Parent));
    }

    private static void SetPresentationValue(Placement placement, DependencyProperty property, object? value)
    {
        if (!PresentationProperties.Contains(property)) throw new ArgumentException(nameof(property));
        var element = placement.Slot.Element;
        if (!placement.Properties.ContainsKey(property))
            placement.Properties.Add(property, new PropertyState(property, element.ReadLocalValue(property),
                element.GetValue(property), BindingOperations.GetBindingBase(element, property),
                DependencyPropertyHelper.GetValueSource(element, property).IsExpression));
        element.SetCurrentValue(property, value);
    }

    private static void RestoreProperties(Placement placement)
    {
        var element = placement.Slot.Element;
        foreach (var state in placement.Properties.Values)
        {
            if (state.Binding is { } binding) BindingOperations.SetBinding(element, state.Property, binding);
            else if (state.LocalValue == DependencyProperty.UnsetValue) element.ClearValue(state.Property);
            else if (state.IsExpression) element.InvalidateProperty(state.Property);
            else element.SetCurrentValue(state.Property, state.Value);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            foreach (var placement in placements)
                try { placement.Host?.Detach(); }
                catch (Exception error) { failure ??= error; }
            foreach (var placement in placements.OrderBy(item => item.Index))
            {
                try { Restore(placement); }
                catch (Exception error) { failure ??= error; }
                finally
                {
                    try
                    {
                        RestoreProperties(placement);
                    }
                    catch (Exception error) { failure ??= error; }
                    finally
                    {
                        try
                        {
                            if (placement.PinnedDataContext)
                                placement.Slot.Element.ClearValue(FrameworkElement.DataContextProperty);
                        }
                        catch (Exception error) { failure ??= error; }
                    }
                }
            }
        }
        finally
        {
            hosts.Clear();
            placements.Clear();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
