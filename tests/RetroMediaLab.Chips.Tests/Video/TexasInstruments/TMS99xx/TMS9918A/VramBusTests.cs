using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class VramBusTests
{
    [Fact]
    public void CasRisingSamplesBeforeDramReleaseEvenWhenRasHasAlreadyRisen()
    {
        var pins = new Tms9918VramPins(0x12, true, true, true, true);
        var bus = new Tms9918VramBus(new(0, pins, 0, false, 0, null));
        var port = new Tms9918CpuMemoryPort(new(0x3fff, 0x79, Tms9918CpuVramOperation.Read));
        var captured = new List<Tms9918VramRead>();
        try
        {
            bus.PinsChanged = value =>
            {
                if (value.CasHigh) bus.SetInputData(bus.Capture().TimeNanoseconds, 0xff, false);
            };
            bus.ReadCaptured = value =>
            {
                captured.Add(value);
                Assert.True(value.DataKnown);
                port.CompleteRead(value.Data);
                Assert.Throws<InvalidOperationException>(() => bus.ApplyOutputs(value.TimeNanoseconds, pins));
            };
            pins = pins with { RasHigh = false }; bus.ApplyOutputs(45, pins);
            pins = pins with { CasHigh = false }; bus.ApplyOutputs(85, pins);
            pins = pins with { AddressData = 0x34 }; bus.ApplyOutputs(87, pins);
            bus.SetInputData(220, 0xa6, true);
            pins = pins with { RasHigh = true }; bus.ApplyOutputs(275, pins);
            Assert.Empty(captured); Assert.Equal((ushort)0x3fff, port.Capture().Address);
            pins = pins with { CasHigh = true }; bus.ApplyOutputs(315, pins);
            Assert.Equal(new Tms9918VramRead(315, 0xa6, true), Assert.Single(captured));
            Assert.False(bus.Capture().InputDataKnown);
            Assert.Equal((byte)0xa6, port.Data); Assert.Equal((ushort)0, port.Capture().Address);
            bus.ApplyOutputs(315, pins); bus.ApplyOutputs(400, pins);
            Assert.Single(captured);
        }
        finally { bus.PinsChanged = null; bus.ReadCaptured = null; }
    }

    [Fact]
    public void RefreshAndWriteDoNotGenerateReadsAndSetupFailureIsExplicit()
    {
        var pins = new Tms9918VramPins(0, false, true, true, true);
        var bus = new Tms9918VramBus(new(0, pins, 0x51, true, 0, null));
        var captured = new List<Tms9918VramRead>();
        try
        {
            bus.ReadCaptured = captured.Add;
            // Rafraîchissement RAS seul, sans front CAS.
            pins = pins with { RasHigh = false }; bus.ApplyOutputs(100, pins);
            pins = pins with { RasHigh = true }; bus.ApplyOutputs(300, pins);
            // Écriture retardée : R/W bas à la remontée de CAS.
            pins = pins with { RasHigh = false, CasHigh = false }; bus.ApplyOutputs(500, pins);
            pins = pins with { ReadWriteHigh = false }; bus.ApplyOutputs(600, pins);
            pins = pins with { CasHigh = true }; bus.ApplyOutputs(750, pins);
            Assert.Empty(captured);
            pins = pins with { RasHigh = true, ReadWriteHigh = true }; bus.ApplyOutputs(800, pins);
            pins = pins with { RasHigh = false, CasHigh = false }; bus.ApplyOutputs(1000, pins);
            bus.SetInputData(1200, 0xc4, true);
            pins = pins with { CasHigh = true }; bus.ApplyOutputs(1259, pins);
            Assert.Equal(new Tms9918VramRead(1259, 0xc4, false), Assert.Single(captured));
        }
        finally { bus.ReadCaptured = null; }
    }

    [Fact]
    public void ActiveBusRestoreKeepsTheExactInputAndSamplingHistory()
    {
        var pins = new Tms9918VramPins(0x35, true, false, false, true);
        var original = new Tms9918VramBus(new(100, pins, 0x67, true, 70, null));
        var restored = new Tms9918VramBus(original.Capture());
        pins = pins with { RasHigh = true, CasHigh = true };
        original.ApplyOutputs(200, pins); restored.ApplyOutputs(200, pins);
        Assert.Equal(original.Capture(), restored.Capture());
        Assert.Equal(new Tms9918VramRead(200, 0x67, true), restored.Capture().LastRead);
        var before = restored.Capture();
        Assert.Throws<ArgumentOutOfRangeException>(() => restored.SetInputData(199, 0, false));
        Assert.Equal(before, restored.Capture());
    }
}
