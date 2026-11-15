using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Pico;

internal sealed partial class PicoMachine
{
    protected override Action<uint>? MachineAudioSynchronizer => _pcm.Update;
    protected override Func<uint,bool>? MachineInterruptAcknowledger => AcknowledgeAudioInterrupt;
    protected override uint AuxiliaryInterruptLevel => _pcm is not null && _pcm.InterruptPending ? 3u : 0u;
    protected override uint MapVideoInterruptLevel(uint level) => level == 4 ? 5u : level;
    protected override void ConfigureMachineAudio(int sampleRate, double frameRate)
    {
        uint clock = frameRate != 0 ? (uint)(3420 * Video.lines_per_frame * frameRate) : MasterClock;
        _pcm.Configure(clock, sampleRate);
    }
    protected override void ResetMachineDevices() => _pcm.Reset();
    protected override void FinishMachineAudioFrame(uint cycles) => _pcm.EndFrame(cycles);
    private bool AcknowledgeAudioInterrupt(uint level)
    {
        if (level != 3) return false;
        _pcm.AcknowledgeInterrupt(); return true;
    }

    private uint ReadWord(uint address)
    {
        if ((address & 0xffffe0) != 0x800000) return 0;
        switch (address & 0x1e)
        {
            case 0: return Region switch
            {SystemHardware.JapanNtsc=>0x40u,SystemHardware.JapanPal=>0u,SystemHardware.Europe=>0x20u,_=>0x60u};
            case 2: return ~(uint)Input.DigitalInputs[0] & 0xffff;
            case 4: return (uint)(Input.AnalogInputs[0] >> 8) & 255;
            case 6: return (uint)Input.AnalogInputs[0] & 255;
            case 8: return (uint)(Input.AnalogInputs[1] >> 8) & 255;
            case 10: return (uint)Input.AnalogInputs[1] & 255;
            case 12: return (uint)((1 << (PicoPage & 7)) - 1);
            case 16: _pcm.Update(MachineCpuCycles); return (uint)_pcm.FreeBytes;
            case 18: _pcm.Update(MachineCpuCycles); return _pcm.Status;
            default: return 0;
        }
    }
    private uint ReadByte(uint address) => (ReadWord(address) >> ((address & 1) == 0 ? 8 : 0)) & 255;
    private void WriteByte(uint address, uint value)
    {
        // The PCM data/control ports are word-wide; byte writes to the SEGA latch are ignored.
    }
    private void WriteWord(uint address, uint value)
    {
        if (address == 0x800010) _pcm.WriteData((ushort)value, MachineCpuCycles);
        else if (address == 0x800012) _pcm.WriteControl((ushort)value, MachineCpuCycles);
    }
}
