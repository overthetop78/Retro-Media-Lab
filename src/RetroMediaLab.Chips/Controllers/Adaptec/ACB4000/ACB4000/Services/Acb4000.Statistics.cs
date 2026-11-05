using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private readonly long[] _stepCount = new long[C.DriveCount];
    private readonly long[] _receivedBlocks = new long[C.DriveCount];
    private readonly long[] _emittedBlocks = new long[C.DriveCount];

    public long SeekCount(int unit) { ValidateDriveUnit(unit); return _stepCount[unit]; }
    public long ReadBlocks(int unit) { ValidateDriveUnit(unit); return _receivedBlocks[unit]; }
    public long WrittenBlocks(int unit) { ValidateDriveUnit(unit); return _emittedBlocks[unit]; }

    private void ResetStatistics()
    {
        Array.Clear(_stepCount);
        Array.Clear(_receivedBlocks);
        Array.Clear(_emittedBlocks);
    }
}
