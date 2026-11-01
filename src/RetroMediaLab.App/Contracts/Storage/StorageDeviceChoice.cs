using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Storage;

internal sealed record StorageDeviceChoice(EmulationMediaType Type, string Text)
{
    public override string ToString() => Text;
}
