using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Views.Dialogs.Read;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class ReadConflictView : BusinessDialogView
{
    internal ReadConflictView(ReadConflictWindow source, bool? dark = null) : base(source, dark)
    {
        try
        {
            Footer(Forward(source.EditNameButton), Forward(source.NextNumberButton), Forward(source.OverwriteButton));
            var form = new StackPanel();
            form.Children.Add(Label("Read.FileExists"));
            var path = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
            Bindings.OneWay(source.OutputPathText, TextBlock.TextProperty, path, TextBox.TextProperty);
            form.Children.Add(path);
            Children.Add(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }
}
