using System.Globalization;

namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationModuleLocalization
{
    bool TryGetString(string key, CultureInfo culture, out string value);
}
