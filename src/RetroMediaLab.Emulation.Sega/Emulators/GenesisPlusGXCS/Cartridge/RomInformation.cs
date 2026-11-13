namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed class RomInformation
{
    internal string consoletype = "", copyright = "", domestic = "", international = "", ROMType = "", product = "", country = "";
    internal ushort checksum, realchecksum, peripherals;
    internal uint romstart, romend;
}
