using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private bool _lightPenLevel;
    private bool _lightPenPending;

    /// <summary>Sortie MA, sans le retard de la sortie d'affichage.</summary>
    public int MemoryAddress => _memoryAddress;

    /// <summary>LPSTB est actif haut. La capture est synchronisée au prochain front CLK,
    /// après la progression de MA (Motorola figure 5). Aucun retard de moniteur n'est ajouté.</summary>
    public void SetLightPen(bool level)
    {
        if (level && !_lightPenLevel) _lightPenPending = true;
        _lightPenLevel = level;
    }

    private void CaptureLightPen()
    {
        if (!_lightPenPending) return;
        _lightPenPending = false;
        var address = MemoryAddress;
        _registers[Crtc6845Constants.LightPenAddressHigh] = (byte)(address >> Crtc6845Constants.AddressByteShift);
        _registers[Crtc6845Constants.LightPenAddressLow] = (byte)address;
    }

    private void ResetLightPen()
    {
        _lightPenLevel = false;
        _lightPenPending = false;
    }
}
