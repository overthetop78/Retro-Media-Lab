using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RetroMediaLab.App.Services.Updates;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Options;
using RetroMediaLab.Updates.Contracts;
using RetroMediaLab.App.Contracts.Updates;
using RetroMediaLab.App.Constants.Controls.Visual;

namespace RetroMediaLab.App.Options.Models.Updates;

internal enum ModuleOperationPage
{
    Directory,
    Module,
    Advanced
}
