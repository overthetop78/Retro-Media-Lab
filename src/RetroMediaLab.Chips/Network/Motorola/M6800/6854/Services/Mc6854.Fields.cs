using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private int FieldLength(AdlcFrameField field, int shift) => field == AdlcFrameField.Information
        ? C.MinimumInformationBits + ((_control4 >> shift) & C.WordLengthMask)
        : C.BitsPerOctet;

    private AdlcFrameField NextField(AdlcFrameField field, byte value) => field switch
    {
        // Le Null Address (00) ne prolonge pas le champ, même si AEX est actif.
        AdlcFrameField.Address when (_control3 & C.ExtendedAddress) != 0 && value != 0 &&
            (value & C.ExtensionAddressEnd) == 0 => AdlcFrameField.Address,
        AdlcFrameField.Address => AdlcFrameField.Control,
        AdlcFrameField.Control when (_control3 & C.ExtendedControl) != 0 => AdlcFrameField.ExtendedControl,
        AdlcFrameField.Control or AdlcFrameField.ExtendedControl => (_control3 & C.LogicalControl) != 0
            ? AdlcFrameField.LogicalControl : AdlcFrameField.Information,
        AdlcFrameField.LogicalControl when (value & C.ExtensionLogicalContinue) != 0 => AdlcFrameField.LogicalControl,
        _ => AdlcFrameField.Information
    };
}
