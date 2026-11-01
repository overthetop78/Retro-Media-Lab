using RetroMediaLab.Emulation.Contracts;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Conversion analogique à quatre canaux ; échantillonnage au départ et fin de conversion sur CB1.</summary>
internal sealed class Adc7002(Action<bool> endOfConversion)
{
    private readonly ushort[] _channels = new ushort[C.ChannelCount];
    private int _remaining;
    private ushort _sample;
    private ushort _result;
    private byte _status;
    internal byte FireInputs { get; private set; } = C.OpenInputs;

    internal void Reset()
    {
        _remaining = 0;
        _sample = _result = 0;
        _status = 0;
        endOfConversion(false);
    }

    internal void SetInput(IReadOnlyList<EmulationControllerState> controllers)
    {
        FireInputs = C.OpenInputs;
        var fireMask = 1u << EmulationSdk.InputProcessing.ControllerButtonIndex(EmulationControllerCommandIds.A);
        for (var index = 0; index < C.ControllerCount; index++)
        {
            var controller = index < controllers.Count ? controllers[index] : EmulationControllerState.Empty;
            _channels[index * C.AxesPerController] = (ushort)(short.MaxValue - controller.LeftX);
            _channels[index * C.AxesPerController + 1] = (ushort)(short.MaxValue - controller.LeftY);
            if ((controller.Buttons & fireMask) != 0)
                FireInputs &= (byte)~(C.FirstFireLine << index);
        }
    }

    internal byte Read(int register) => (register & C.RegisterMask) switch
    {
        C.StatusRegister => _status,
        C.HighRegister => (byte)(_result >> C.HighShift),
        _ => (byte)(_result & C.LowMask)
    };

    internal void Write(int register, byte value)
    {
        if ((register & C.RegisterMask) != C.StatusRegister) return;
        _status = (byte)((value & C.CommandMask) | C.Busy);
        _sample = _channels[value & C.ChannelMask];
        _remaining = (value & C.TwelveBitMode) != 0 ? C.TwelveBitCycles : C.EightBitCycles;
        endOfConversion(true);
    }

    internal void Tick()
    {
        if (_remaining == 0 || --_remaining != 0) return;
        _result = _sample;
        _status = (byte)((_status & C.CommandMask) | C.Complete | ((_result & C.ResultStatusMask) >> C.ResultStatusShift));
        endOfConversion(false);
    }
}
