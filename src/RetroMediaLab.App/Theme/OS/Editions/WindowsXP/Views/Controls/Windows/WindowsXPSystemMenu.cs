using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class WindowsXPSystemMenu : IDisposable
{
    private const uint SystemCommandMessage = 0x0112;
    private const nuint MoveCommand = 0xF010;
    private const nuint SizeCommand = 0xF000;
    private readonly WindowsXPWindow owner;
    private readonly ContextMenu menu;
    private WindowsXPResources? resources;
    private bool disposed;

    internal WindowsXPSystemMenu(WindowsXPWindow owner)
    {
        this.owner = owner;
        menu = new ContextMenu { PlacementTarget = owner, Placement = PlacementMode.Relative };
        try
        {
            resources = new WindowsXPResources(menu, owner.Scheme);
            Add("Options.Themes.OS.Restore", SystemCommands.RestoreWindowCommand);
            AddNative("Options.Themes.OS.Move", MoveCommand, owner.WindowState == WindowState.Normal);
            AddNative("Options.Themes.OS.Size", SizeCommand, owner.WindowState == WindowState.Normal
                && owner.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip);
            Add("Options.Themes.Minimize", SystemCommands.MinimizeWindowCommand);
            Add("Options.Themes.OS.Maximize", SystemCommands.MaximizeWindowCommand);
            menu.Items.Add(new Separator());
            Add("Common.Close", SystemCommands.CloseWindowCommand);
        }
        catch { Dispose(); throw; }
    }

    internal void Show(Point position)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        menu.HorizontalOffset = position.X;
        menu.VerticalOffset = position.Y;
        menu.IsOpen = true;
    }

    private void Add(string resource, RoutedCommand command)
    {
        var item = new MenuItem { Command = command, CommandTarget = owner };
        item.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(resource));
        menu.Items.Add(item);
    }

    private void AddNative(string resource, nuint command, bool enabled)
    {
        var item = new MenuItem { Tag = command, IsEnabled = enabled };
        item.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(resource));
        item.Click += NativeCommand;
        menu.Items.Add(item);
    }

    private void NativeCommand(object sender, RoutedEventArgs args)
    {
        if (disposed || sender is not MenuItem { Tag: nuint command }) return;
        menu.IsOpen = false;
        var handle = new WindowInteropHelper(owner).Handle;
        if (handle != IntPtr.Zero) SendMessage(handle, SystemCommandMessage, command, IntPtr.Zero);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { menu.IsOpen = false; }
        finally
        {
            foreach (var item in menu.Items.OfType<MenuItem>())
            {
                item.Click -= NativeCommand;
                item.Command = null;
                item.CommandTarget = null;
            }
            menu.Items.Clear();
            menu.PlacementTarget = null;
            try { resources?.Dispose(); }
            finally { resources = null; }
        }
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, nuint command, IntPtr parameter);
}
