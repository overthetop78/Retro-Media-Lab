namespace RetroMediaLab.Chips.Controllers.Common.Interfaces;

/// <summary>Signaux d'un lecteur emprunté au système qui raccorde le contrôleur.</summary>
public interface IDriveSignals
{
    void SetMotor(bool running);
    void SetActivity(bool reading, bool writing);
    void Seek(int cylinder, int head, int sector = -1, long logicalBlock = -1);
}
