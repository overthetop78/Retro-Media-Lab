using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class CpuInterfaceTests
{
    [Fact]
    public void PendingVramDoesNotDiscardRegisterCommandsAndConflictResumesWithoutAQueue()
    {
        var pins = new Tms9918CpuPins(0, true, true, true, true);
        var chip = new Tms9918A(new(default, 0, default,
            new(0x1234, 0x11, Tms9918CpuVramOperation.Read), pins), Tms9918CpuTiming.SpecifiedMaximum);
        var restored = new Tms9918A(new(default, 0, default, default, pins), Tms9918CpuTiming.SpecifiedMaximum);
        long time = 0;
        void Write(byte value, bool mode)
        {
            time += 1000;
            pins = pins with { Data = value, Mode = mode };
            chip.ReceiveCpuPins(time, pins);
            chip.ReceiveCpuPins(time + 40, pins with { CswHigh = false });
            chip.ReceiveCpuPins(time + 300, pins);
            time += 300;
        }
        try
        {
            Write(0xe0, true); Write(0x81, true);
            Assert.Equal(0xe0, chip.RegisterState.R1);
            Assert.True(chip.HasCpuVramRequest);
            Assert.Null(chip.CpuMemoryState.Address);
            Write(0x22, false); Write(0x33, false);
            Assert.Equal(0x33, chip.CpuMemoryState.Data);
            Assert.False(chip.CpuMemoryState.RequestKnown);
            Assert.False(chip.IsCpuInterfaceQualified);
            var state = System.Text.Json.JsonSerializer.Deserialize<Tms9918AState>(
                System.Text.Json.JsonSerializer.Serialize(chip.CaptureState()))!;
            restored.RestoreState(state);
            Assert.Equal(chip.CpuMemoryState, restored.CpuMemoryState);
            chip.CompleteCpuVramRead(0x44); restored.CompleteCpuVramRead(0x44);
            Assert.False(chip.HasCpuVramRequest); Assert.False(restored.HasCpuVramRequest);
            Assert.False(chip.CpuMemoryState.DataKnown);
            Assert.Equal(chip.CpuMemoryState, restored.CpuMemoryState);
        }
        finally
        {
            chip.CpuOutputsChanged = restored.CpuOutputsChanged = null;
            chip.NextEventNanosecondsChanged = restored.NextEventNanosecondsChanged = null;
        }
    }

    [Fact]
    public void FirstControlByteChangesLiveAddressEvenWhenStatusAbandonsThePairAndStateResumesIt()
    {
        var pins = new Tms9918CpuPins(0, true, true, true, true);
        var chip = new Tms9918A(new(default, 0, default,
            new(0x1234, 0x5a, Tms9918CpuVramOperation.None), pins), Tms9918CpuTiming.SpecifiedMaximum);
        var restored = new Tms9918A(new(default, 0, default, default, pins), Tms9918CpuTiming.SpecifiedMaximum);
        long time = 0;
        void Write(Tms9918A target, byte value, bool mode)
        {
            time += 8000;
            pins = pins with { Data = value, Mode = mode };
            target.ReceiveCpuPins(time, pins);
            target.ReceiveCpuPins(time + 40, pins with { CswHigh = false });
            target.ReceiveCpuPins(time + 300, pins);
            time += 300;
        }
        try
        {
            Write(chip, 0x78, true);
            Assert.Equal((ushort?)0x1278, chip.CpuMemoryState.Address);
            Assert.True(chip.CaptureState().Control.AwaitingSecondByte);
            restored.RestoreState(System.Text.Json.JsonSerializer.Deserialize<Tms9918AState>(
                System.Text.Json.JsonSerializer.Serialize(chip.CaptureState()))!);
            time += 8000;
            pins = pins with { Mode = true, CsrHigh = false };
            chip.ReceiveCpuPins(time, pins); restored.ReceiveCpuPins(time, pins);
            pins = pins with { CsrHigh = true };
            chip.ReceiveCpuPins(time + 300, pins); restored.ReceiveCpuPins(time + 300, pins);
            time += 300;
            Assert.False(chip.CaptureState().Control.AwaitingSecondByte);
            Assert.Equal(chip.CpuMemoryState, restored.CpuMemoryState);
            Assert.Equal((ushort?)0x1278, restored.CpuMemoryState.Address);
            Write(restored, 0xa5, false);
            Assert.Equal((ushort?)0x1278, restored.CpuVramRequest.Address);
            restored.CompleteCpuVramWrite();
            Assert.Equal((ushort?)0x1279, restored.CpuMemoryState.Address);

            // Le second octet ne restaure pas le bas d'une copie temporaire :
            // un transfert intermédiaire a incrémenté le compteur partagé.
            Write(restored, 0xff, true);
            Assert.Equal((ushort?)0x12ff, restored.CpuMemoryState.Address);
            Write(restored, 0x66, false);
            restored.CompleteCpuVramWrite();
            Assert.Equal((ushort?)0x1300, restored.CpuMemoryState.Address);
            Write(restored, 0x4a, true);
            Assert.Equal((ushort?)0x0a00, restored.CpuMemoryState.Address);
        }
        finally
        {
            chip.CpuOutputsChanged = restored.CpuOutputsChanged = null;
            chip.NextEventNanosecondsChanged = restored.NextEventNanosecondsChanged = null;
        }
    }

    [Fact]
    public void IntegratedCpuPinsKeepDataDelaysPrefetchInterruptAndResetEffects()
    {
        var pins = new Tms9918CpuPins(0, true, true, true, true);
        var vdp = new Tms9918A(new(new(0, 0, 2, 3, 4, 5, 6, 7), 0,
            new(0, false), new(0, 0x5a, Tms9918CpuVramOperation.None), pins),
            Tms9918CpuTiming.SpecifiedMaximum);
        var outputs = new List<Tms9918CpuOutputs>();
        long time = 0;
        void Write(byte value, bool mode)
        {
            time += 8000;
            pins = pins with { Mode = mode, Data = value };
            vdp.ReceiveCpuPins(time, pins);
            vdp.ReceiveCpuPins(time + 40, pins with { CswHigh = false });
            vdp.ReceiveCpuPins(time + 300, pins);
            time += 300;
        }
        try
        {
            vdp.CpuOutputsChanged = outputs.Add;
            // IE change : F demeure mémorisé, INT change dès la commande R1.
            vdp.EndOfActiveDisplay();
            Assert.True(vdp.CpuOutputs.IntHigh);
            Write(0x20, true); Write(0x81, true);
            Assert.False(vdp.CpuOutputs.IntHigh);
            pins = pins with { CsrHigh = false };
            time += 8000;
            vdp.ReceiveCpuPins(time, pins);
            Assert.Equal(time + 150, vdp.NextEventNanoseconds);
            vdp.AdvanceTo(time + 149);
            Assert.False(vdp.CpuOutputs.DataDriven);
            vdp.AdvanceTo(time + 150);
            Assert.True(vdp.CpuOutputs.DataDriven);
            Assert.Equal(0x80, vdp.CpuOutputs.Data);
            pins = pins with { CsrHigh = true };
            vdp.ReceiveCpuPins(time + 300, pins);
            Assert.True(vdp.CpuOutputs.IntHigh);
            Assert.True(vdp.CpuOutputs.DataDriven);
            vdp.AdvanceTo(time + 399);
            Assert.True(vdp.CpuOutputs.DataDriven);
            vdp.AdvanceTo(time + 400);
            Assert.False(vdp.CpuOutputs.DataDriven);
            int count = outputs.Count;
            vdp.AdvanceTo(time + 500);
            Assert.Equal(count, outputs.Count);
            time += 500;

            Write(0xff, true); Write(0x3f, true);
            Assert.True(vdp.HasCpuVramRequest);
            Assert.Equal((ushort?)0x3fff, vdp.CpuVramRequest.Address);
            Assert.Equal(Tms9918CpuVramOperation.Read, vdp.CpuVramRequest.Operation);
            vdp.CompleteCpuVramRead(0xa5);
            Assert.Equal((ushort?)0, vdp.CpuMemoryState.Address);
            time += 8000;
            pins = pins with { Mode = false, CsrHigh = false };
            vdp.ReceiveCpuPins(time, pins);
            vdp.AdvanceTo(time + 150);
            Assert.Equal(0xa5, vdp.CpuOutputs.Data);
            pins = pins with { CsrHigh = true };
            vdp.ReceiveCpuPins(time + 300, pins);
            Assert.Equal((ushort?)0, vdp.CpuVramRequest.Address);
            vdp.CompleteCpuVramRead(0xb6);
            time += 500;
            Write(0x7c, false);
            Assert.Equal((ushort?)1, vdp.CpuVramRequest.Address);
            Assert.Equal(0x7c, vdp.CpuVramRequest.Data);
            vdp.CompleteCpuVramWrite();
            Assert.Equal((ushort?)2, vdp.CpuMemoryState.Address);
            Assert.True(vdp.IsCpuInterfaceQualified);

            // Un strobe traversant RESET n'est pas rejoué au relâchement.
            time += 8000;
            vdp.SetReset(time, true);
            vdp.ReceiveCpuPins(time + 500, pins with { CswHigh = false });
            vdp.ReceiveCpuPins(time + 1000, pins);
            vdp.SetReset(time + 3000, false);
            Assert.False(vdp.HasCpuVramRequest);
            Assert.Equal(new Tms9918RegisterState(0, 0, 2, 3, 4, 5, 6, 7), vdp.RegisterState);
            Assert.True(vdp.CpuOutputs.IntHigh);
            Assert.Null(vdp.NextEventNanoseconds);
        }
        finally
        {
            vdp.CpuOutputsChanged = null;
            vdp.NextEventNanosecondsChanged = null;
        }
    }
}
