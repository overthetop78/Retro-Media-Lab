using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmWriteConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Encode les bits NRZ et WAM en paires CLOCK/DATA MFM, retardées de trois bits.</summary>
/// <remarks>Sortie numérique nominale ; les délais analogiques de précompensation ne sont pas des cellules supplémentaires.</remarks>
public sealed class Acb4000MfmWriteEncoder
{
    private uint delay;
    private bool previousData;
    private bool suppressNextClock;

    /// <summary>Front de bit NRZ. Le résultat contient CLOCK au bit 1 et DATA au bit 0.</summary>
    public byte ClockBit(bool data, bool writeAddressMark)
    {
        var clock = !previousData && !data && !suppressNextClock;
        var pair = (uint)((clock ? 1 << C.ClockCellShift : 0) | (data ? 1 : 0));
        var output = (byte)(delay >> (C.DelayCells - C.CellsPerBit));
        delay = ((delay << C.CellsPerBit) | pair) & C.DelayMask;
        previousData = data;
        suppressNextClock = writeAddressMark;
        return (byte)(output & C.PairMask);
    }

    public void Reset()
    {
        delay = 0;
        previousData = suppressNextClock = false;
    }
}
