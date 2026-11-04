namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

/// <summary>Contrat matériel du composant ElectronVideo ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IElectronVideo
{
    int Mode { get; }

    int Line { get; }

    int Clock { get; }

    int Dot { get; }

    int VideoAddress { get; }

    byte VideoData { get; }

    /// <summary>Last emitted point: red in bits 0–7, green 8–15, blue 16–23.</summary>
    int PixelColor { get; }

    long Frame { get; }

    byte[] CompletedFrame { get; }

    bool LowBandwidth { get; }

    bool DisplayFetching { get; }

    bool HorizontalSyncActive { get; }

    bool CompositeSyncActive { get; }

    bool ClockInterruptEdge { get; }

    bool DisplayEndInterruptEdge { get; }

    Action<int>? LineStarted { get; set; }

    void Reset();
    void SetMode(byte control);
    void WriteStart(bool high, byte value);
    void WritePalette(int register, byte value);
    bool Tick();
    bool TickPixel();
}
