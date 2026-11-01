using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Shell;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class TerminalView : UserControl, IDisposable
{
    private readonly TerminalSection source;
    private readonly Action close;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal TerminalView(TerminalSection source, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.OneWay(source.CommandTextBox, TextBox.TextProperty, Command, TextBox.TextProperty);
            bindings.Action(source.CopyButton, CopyCommand);
            source.OutputTextBox.TextChanged += OutputChanged;
            RefreshOutput();
        }
        catch { Dispose(); throw; }
    }

    private void OutputChanged(object sender, TextChangedEventArgs args) { if (!disposed) RefreshOutput(); }

    private void RefreshOutput()
    {
        Output.Document.Blocks.Clear();
        foreach (var block in source.OutputTextBox.Document.Blocks)
        {
            var target = new Paragraph { Margin = new Thickness(0) };
            if (block is Paragraph paragraph)
                foreach (var inline in paragraph.Inlines) target.Inlines.Add(CopyInline(inline));
            else target.Inlines.Add(new Run(new TextRange(block.ContentStart, block.ContentEnd).Text));
            Output.Document.Blocks.Add(target);
        }
        Output.ScrollToEnd();
    }

    private static Inline CopyInline(Inline source)
    {
        Inline target;
        if (source is Run run) target = new Run(run.Text);
        else if (source is Span span)
        {
            var copy = new Span();
            foreach (var child in span.Inlines) copy.Inlines.Add(CopyInline(child));
            target = copy;
        }
        else if (source is LineBreak) target = new LineBreak();
        else target = new Run(new TextRange(source.ContentStart, source.ContentEnd).Text);
        // Explicit message colours carry severity; inherited Base colours belong to its presentation.
        if (source.ReadLocalValue(TextElement.ForegroundProperty) is Brush brush) target.Foreground = brush;
        if (source.ReadLocalValue(TextElement.FontWeightProperty) is FontWeight weight) target.FontWeight = weight;
        if (source.ReadLocalValue(TextElement.FontStyleProperty) is FontStyle style) target.FontStyle = style;
        return target;
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            source.OutputTextBox.TextChanged -= OutputChanged;
            bindings.Dispose();
        }
        finally
        {
            if (Output is not null) Output.Document.Blocks.Clear();
            try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Content = null; }
            Resources.Clear();
        }
    }
}
