namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    private readonly record struct DriveOutputs(bool Motor, bool Step, bool Direction,
        bool WriteGate, bool Active, bool Reading, bool Writing, bool DoubleDensity);

    private DriveOutputs _publishedDriveOutputs;

    // Called at output-mutating operations, never as surveillance in Tick.
    private void PublishDriveSignals()
    {
        var outputs = new DriveOutputs(_motor, _step, _direction, _writer.Gate,
            Active, Reading, Writing, DoubleDensity);
        if (_publishedDriveOutputs == outputs) return;
        _publishedDriveOutputs = outputs;
        DriveSignalsChanged?.Invoke();
    }
}
