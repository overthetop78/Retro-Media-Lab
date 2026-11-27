namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal enum Sh4InterruptId
{
    Irl9 = 0,
    Irl11 = 1,
    Irl13 = 2,
    Hudi = 3,
    Gpio = 4,
    DmacDmte0 = 5,
    DmacDmte1 = 6,
    DmacDmte2 = 7,
    DmacDmte3 = 8,
    DmacDmae = 9,
    Tmu0Tuni0 = 10,
    Tmu1Tuni1 = 11,
    Tmu2Tuni2 = 12,
    Tmu2Ticpi2 = 13,
    RtcAti = 14,
    RtcPri = 15,
    RtcCui = 16,
    SciEri = 17,
    SciRxi = 18,
    SciTxi = 19,
    SciTei = 20,
    ScifEri = 21,
    ScifRxi = 22,
    ScifBri = 23,
    ScifTxi = 24,
    WdtIti = 25,
    RefRcmi = 26,
    RefRovi = 27,
    Count
}

internal sealed partial class Sh4Interrupts
{
    internal void SetPending(Sh4InterruptId interrupt, bool pending)
    {
        if (pending)
            SetInterruptPending(interrupt);
        else
            ResetInterruptPending(interrupt);
    }

    internal void SetMask(Sh4InterruptId interrupt, bool enabled)
    {
        if (enabled)
            SetInterruptMask(interrupt);
        else
            ResetInterruptMask(interrupt);
    }
}
