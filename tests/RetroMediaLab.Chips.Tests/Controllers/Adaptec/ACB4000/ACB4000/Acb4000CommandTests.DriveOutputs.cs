using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void ReadGatePublishesReadyAndEccChangesWithoutWaitingForAnotherClock()
    {
        var target = CreateManualTarget();
        var gates = ObserveGates(target);
        try
        {
            SendCommand(target, Short(8, count: 1));
            Assert.True(target.ReadGate);
            var count = gates.Count;
            for (var i = 0; i < 100; i++) target.Tick();
            Assert.Equal(count, gates.Count);
            target.SetDriveSignals(0, false, true, true);
            Assert.Equal((false, false), gates[^1]);
            target.SetDriveSignals(0, true, true, true);
            Assert.Equal((true, false), gates[^1]);
            target.Aic.Ecc.WriteControl(0x20);
            Assert.Equal((false, false), gates[^1]);
            target.Aic.Ecc.WriteControl(0xc0);
            Assert.Equal((true, false), gates[^1]);
            count = gates.Count;
            target.Aic.Ecc.WriteControl(0xc0);
            target.SetDriveSignals(0, true, true, true);
            Assert.Equal(count, gates.Count);
            PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
            Assert.Equal(new byte[256], DrainRead(target, out var status));
            Assert.Equal(0, status);
            Assert.Equal((false, false), gates[^1]);
        }
        finally { target.Reset(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeWriteGatePublishesFaultAndResetAtTheirActualMutation(bool fault)
    {
        var target = CreateDiskTarget();
        var gates = ObserveGates(target);
        try
        {
            SendBufferedWrite(target);
            for (var i = 0; i < 4000 && !target.WriteGate; i++) Tick(target);
            Assert.True(target.WriteGate);
            Assert.Equal((false, true), gates[^1]);
            var count = gates.Count;
            if (fault) target.SetWriteFault(true); else target.Reset();
            Assert.False(target.WriteGate);
            Assert.Equal((false, false), gates[^1]);
            Assert.True(gates.Count > count);
        }
        finally { target.Reset(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NativeFormatPublishesHeadSelectionAndGateEdgesOnBothPorts(int unit)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var gates = ObserveGates(target);
        var selections = new List<(int Unit, int Head, int Cylinder, int WriteHead)>();
        target.DriveSelectionChanged += (selected, head, cylinder, writeHead) =>
        {
            var value = (selected, head, cylinder, writeHead);
            Assert.Equal((target.Unit, target.SelectedHead, target.FormatCylinder, target.FormatHead), value);
            if (selections.Count > 0) Assert.NotEqual(selections[^1], value);
            selections.Add(value);
        };
        try
        {
            var parameters = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 2 };
            Assert.Equal(0, Run(target, Short(0x15, unit, count: 22), EncodeParameters(parameters)).Status);
            Assert.Equal(0, Run(target, [4, (byte)((unit << 5) | 6), 0xe5, 0, 2, 0]).Status);
            Assert.Contains(selections, selected => selected.Unit == unit && selected.WriteHead == 1);
            Assert.Contains((false, true), gates);
            Assert.Equal((false, false), gates[^1]);
            Assert.Equal(new[] { (0, 0), (0, 1) }, disk.TrackWrites.Select(track => (track.Cylinder, track.Head)));
        }
        finally { target.Reset(); }
    }

    private static List<(bool Read, bool Write)> ObserveGates(Acb4000 target)
    {
        var gates = new List<(bool Read, bool Write)>();
        target.DriveGatesChanged += (read, write) =>
        {
            Assert.Equal((target.ReadGate, target.WriteGate), (read, write));
            if (gates.Count > 0) Assert.NotEqual(gates[^1], (read, write));
            gates.Add((read, write));
        };
        return gates;
    }
}
