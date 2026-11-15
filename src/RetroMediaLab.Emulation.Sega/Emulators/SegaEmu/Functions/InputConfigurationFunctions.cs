using RetroMediaLab.Chips.Controllers.Sega.Input;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Functions;

internal static class InputConfigurationFunctions
{
    internal static InputDeviceConfiguration Create(byte hardware, byte cartridgeType, ushort peripherals, uint special) => new(
        AutomaticPadType: (cartridgeType & 0x80) != 0 ? (byte)((peripherals & 2) != 0 ? 1 : 0) : (byte)2,
        PrimaryDevice: hardware == 0x82 ? (byte)8 : (special & 2) != 0 ? (byte)9 : byte.MaxValue,
        AdditionalPads: (special & 4) != 0);
}
