using C = RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Constants.Pcf8583Constants;
using State = RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Enums.Pcf8583BusState;

namespace RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Interfaces;
/// <summary>Contrat matériel du composant Pcf8583 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IPcf8583
{
    void Advance(int ticks);
    void ExternalClockPulse();
    bool InterruptLow { get; }

    bool Sda { get; }

    void ResetBus();
    byte[] ReadNonvolatileMemory();
    void WriteNonvolatileMemory(ReadOnlySpan<byte> data);
    void WriteLines(bool scl, bool sda);
}
