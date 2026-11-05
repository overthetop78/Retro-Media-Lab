using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ReadSyncConstants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Qualification numérique du préambule et latch AMD de l'AIC-250.</summary>
internal sealed class Acb4000ReadSynchronizer
{
    private ushort shift;
    private int alternatingCells;
    private int count;
    private int oscillatorCells;
    private bool previousCell;
    private bool clockCell;
    private bool acquired;
    private bool found;

    internal void Reset()
    {
        shift = 0;
        alternatingCells = count = oscillatorCells = 0;
        previousCell = clockCell = acquired = found = false;
    }

    internal bool ClockCell(bool cell, bool readGate)
    {
        if (!readGate) { Reset(); return false; }
        if (found) return false;
        shift = (ushort)((shift << 1) | (cell ? 1 : 0));
        if (oscillatorCells != 0)
        {
            oscillatorCells--;
            return false;
        }
        if (!acquired)
        {
            alternatingCells = cell != previousCell ? alternatingCells + 1 : 1;
            previousCell = cell;
            if (!cell && alternatingCells >= C.AcquisitionCells)
            {
                acquired = true;
                count = C.AcquisitionBits;
                clockCell = false;
            }
            return false;
        }
        clockCell = !clockCell;
        if (clockCell) return false;
        if (count < C.AddressSearchBits && cell)
        {
            acquired = false;
            alternatingCells = 1;
            previousCell = cell;
            return false;
        }
        count++;
        if (count >= C.AddressSearchBits && shift == M.MissingClockA1)
        {
            found = true;
            return true;
        }
        if (count == C.SearchLimitBits)
        {
            acquired = false;
            alternatingCells = 0;
            oscillatorCells = C.OscillatorBits * M.CellsPerBit;
        }
        return false;
    }
}
