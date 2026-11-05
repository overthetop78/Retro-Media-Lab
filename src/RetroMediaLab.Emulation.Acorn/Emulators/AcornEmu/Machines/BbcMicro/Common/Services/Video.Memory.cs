using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    private byte ReadVideoMemory(int ma, int raster)
    {
        // IC39 additionne MA8..11 et le terme issu de C0/C1 et MA12.
        // ScreenWrap est le terme déjà choisi par le latch système de la carte.
        var high = (ma >> C.MemoryHighShift) & C.MemoryHighMask;
        if ((ma & C.MemoryWrapSelect) != 0)
            high = (high + (ScreenWrap >> C.MemoryHighAddressShift)) & C.MemoryHighMask;
        int address;
        if ((ma & C.MemoryTeletextSelect) != 0)
            address = C.MemoryTeletextBase | (ma & C.TeletextRamMask)
                | (_teletextHighBankOnly ? C.MemoryTopBankBit << C.MemoryHighAddressShift
                    : (high & C.MemoryTopBankBit) << C.MemoryHighAddressShift);
        else
            address = ((ma & C.MemoryLowMask) << C.MemoryRasterBits) | (high << C.MemoryHighAddressShift) | (raster & C.MemoryRasterMask);
        var offset = address & (_ram.Length - 1);
        return ReadMemory?.Invoke(offset) ?? _ram[offset];
    }
}
