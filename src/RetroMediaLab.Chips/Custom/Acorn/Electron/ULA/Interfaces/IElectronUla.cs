namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

/// <summary>Contrat matériel du composant ElectronUla ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IElectronUla
{
    int RomPage { get; }

    bool Motor { get; }

    bool CapsLock { get; }

    bool Irq { get; }

    bool Nmi { get; set; }
    bool CpuRamPriority { get; }
    /// <summary>PHI OUT level; an outstanding slow access stretches the high portion.</summary>
    bool CpuClockHigh { get; }
    /// <summary>PHI OUT transition and its 16 MHz point position within the video line.</summary>
    Action<bool, int>? CpuClockChanged { get; set; }
    bool CpuWaiting { get; }
    byte CpuRamData { get; }
    /// <summary>Présente les sorties CPU avant la première période 2 MHz du transfert.</summary>
    void BeginCpuCycle(ushort address, bool write, byte data);
    /// <summary>Libère une présentation terminée ou inhibée par RESET ; ne restaure pas la RAM déjà écrite.</summary>
    void EndCpuCycle();

    short SoundSample { get; }

    void Reset(bool cold);
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
}
