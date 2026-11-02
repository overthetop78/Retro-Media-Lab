namespace RetroMediaLab.Chips.Tests.Shared;

/// <summary>Broches de lecteur sans sortie audio ni ressource externe.</summary>
internal sealed class TestDriveSignals : IDriveSignals
{
    public void SetMotor(bool running) { }
    public void SetActivity(bool reading, bool writing) { }
    public void Seek(int cylinder, int head, int sector = -1, long logicalBlock = -1) { }
}
