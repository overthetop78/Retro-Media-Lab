using Ym2612 = RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction.Ym2612;
using Ym3438 = RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction.Ym3438;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

internal sealed class SoundConfiguration
{
    public int Ym2612Type = Ym2612.Discrete;
    public bool UseYm3438;
    public uint Ym3438Type = Ym3438.ReadMode;
    public bool UseOpll;
    public int Ym2413Mode = 2;
    public bool HighQualityFm = true;
    public bool HighQualityPsg = true;
    public int FmPreamp = 100;
    public uint PsgPreamp = 100;
    public int Filter;
    public uint LowPassRange = 0x9999;
    public int LowFrequency = 200;
    public int HighFrequency = 8000;
    public int LowGain = 100, MidGain = 100, HighGain = 100;
    public bool Mono;
    public readonly int[] PsgChannelVolumes = [100, 100, 100, 100];
    public readonly int[] MdChannelVolumes = [100, 100, 100, 100, 100, 100];
    public readonly int[] SmsChannelVolumes = [100, 100, 100, 100, 100, 100, 100, 100, 100];
}
