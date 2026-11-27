namespace RetroMediaLab.Chips.Dreamcast.Holly;

internal enum HollyInterruptType
{
    Normal = 0x0000,
    External = 0x0100,
    Error = 0x0200
}

internal enum HollyInterruptId
{
    RenderDoneVd = (int)HollyInterruptType.Normal | 0,
    RenderDoneIsp = (int)HollyInterruptType.Normal | 1,
    RenderDone = (int)HollyInterruptType.Normal | 2,
    ScanInt1 = (int)HollyInterruptType.Normal | 3,
    ScanInt2 = (int)HollyInterruptType.Normal | 4,
    HBlank = (int)HollyInterruptType.Normal | 5,
    YuvDma = (int)HollyInterruptType.Normal | 6,
    Opaque = (int)HollyInterruptType.Normal | 7,
    OpaqueMod = (int)HollyInterruptType.Normal | 8,
    Trans = (int)HollyInterruptType.Normal | 9,
    TransMod = (int)HollyInterruptType.Normal | 10,
    PvrDma = (int)HollyInterruptType.Normal | 11,
    MapleDma = (int)HollyInterruptType.Normal | 12,
    MapleVboi = (int)HollyInterruptType.Normal | 13,
    GdromDma = (int)HollyInterruptType.Normal | 14,
    SpuDma = (int)HollyInterruptType.Normal | 15,
    ExtDma1 = (int)HollyInterruptType.Normal | 16,
    ExtDma2 = (int)HollyInterruptType.Normal | 17,
    DevDma = (int)HollyInterruptType.Normal | 18,
    Ch2Dma = (int)HollyInterruptType.Normal | 19,
    PvrSortDma = (int)HollyInterruptType.Normal | 20,
    PunchThru = (int)HollyInterruptType.Normal | 21,
    GdromCommand = (int)HollyInterruptType.External | 0x00,
    SpuIrq = (int)HollyInterruptType.External | 0x01,
    Exp8Bit = (int)HollyInterruptType.External | 0x02,
    ExpPci = (int)HollyInterruptType.External | 0x03,
    PrimNoMem = (int)HollyInterruptType.Error | 0x02,
    MatrNoMem = (int)HollyInterruptType.Error | 0x03,
    MapleInvalidAddress = (int)HollyInterruptType.Error | 0x08,
    MapleOverrun = (int)HollyInterruptType.Error | 0x09,
    MapleFifo = (int)HollyInterruptType.Error | 0x0a,
    MapleInvalidCommand = (int)HollyInterruptType.Error | 0x0b,
    G1InvalidAddress = (int)HollyInterruptType.Error | 0x0c,
    G1Overrun = (int)HollyInterruptType.Error | 0x0d,
    G1Access = (int)HollyInterruptType.Error | 0x0e,
    AicaInvalidAddress = (int)HollyInterruptType.Error | 0x0f,
    Ext1InvalidAddress = (int)HollyInterruptType.Error | 0x10,
    Ext2InvalidAddress = (int)HollyInterruptType.Error | 0x11,
    DevInvalidAddress = (int)HollyInterruptType.Error | 0x12,
    AicaOverrun = (int)HollyInterruptType.Error | 0x13,
    Ext1Overrun = (int)HollyInterruptType.Error | 0x14,
    Ext2Overrun = (int)HollyInterruptType.Error | 0x15,
    DevOverrun = (int)HollyInterruptType.Error | 0x16
}
