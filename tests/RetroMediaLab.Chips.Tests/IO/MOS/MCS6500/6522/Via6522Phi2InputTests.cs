using System.Collections;
using System.Reflection;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522Phi2InputTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void ReceivedPhasesPreserveTheExplicitEdgesAcrossModesAndInputs(int mode)
    {
        foreach (var latch in new[] { 0, 1, 7, 257, 65535 })
        foreach (var pulse in new[] { false, true })
        {
            using var pair = new Pair();
            pair.Both(via =>
            {
                via.SetPb6(true);
                via.Write(C.Auxiliary, (byte)(mode << 2 | (pulse ? 0x20 : 0) | 0xc0));
                via.Write(C.Enable, 0xff);
                via.Write(C.Timer1Low, (byte)latch); via.Write(C.Timer1High, (byte)(latch >> 8));
                via.Write(C.Timer2Low, (byte)latch); via.Write(C.Timer2High, (byte)(latch >> 8));
                via.Write(C.Shift, 0xa6);
            });
            for (var cycle = 0; cycle < 2000; cycle++)
            {
                pair.High(cycle);
                if (cycle % 53 == 0) pair.Both(via => via.SetPb6((cycle & 1) == 0));
                if (cycle % 71 == 0) pair.Both(via => { via.SetCb2((cycle & 2) == 0); via.SetCb1((cycle & 1) == 0); });
                if (cycle % 97 == 0) pair.Read(C.Timer1Low);
                pair.Low(cycle);
                if (cycle % 109 == 0) pair.Both(via => { via.SetCa1((cycle & 1) == 0); via.SetCa2((cycle & 2) == 0); });
                if (cycle % 127 == 0) pair.Read(C.Shift);
                if (cycle % 131 == 0) pair.Check();
            }
            pair.Check();
        }
    }

    [Fact]
    public void LoadingUnderflowIrqAndAcknowledgementKeepTheirIndependentPhaseExpectations()
    {
        var via = new Via6522();
        try
        {
            via.SetPhi2Input(false);
            via.Write(C.Enable, 0xc0);
            via.Write(C.Timer1Low, 3); via.Write(C.Timer1High, 0);
            foreach (var expected in new[] { 3, 2, 1, 0, 65535 })
            {
                via.SetPhi2Input(true); via.SetPhi2Input(false);
                Assert.False(via.Irq);
                Assert.Equal(expected, via.Read(C.Timer1Low) | via.Read(C.Timer1High) << 8);
            }
            via.SetPhi2Input(true);
            Assert.True(via.Irq);
            Assert.Equal(255, via.Read(C.Timer1Low));
            Assert.True(via.Irq); // Accès sur le montant de timeout : pas d'acquittement.
            via.SetPhi2Input(false);
            via.Read(C.Timer1Low);
            Assert.False(via.Irq);
        }
        finally { via.Reset(); }
    }

    [Fact]
    public void PendingClocksResolveBeforeWritesResetAndHandoverToCanonicalEdges()
    {
        using var pair = new Pair();
        pair.Both(via => { via.Write(C.Timer1Low, 0xff); via.Write(C.Timer1High, 3); });
        for (var cycle = 0; cycle < 500; cycle++)
        {
            pair.High(cycle); pair.Low(cycle);
            if (cycle == 99) pair.Both(via => via.Write(C.Latch1Low, 0x55));
            if (cycle == 199) pair.Read(C.Timer1Low);
            if (cycle == 299) pair.Both(via => via.Reset());
            if (cycle == 399)
            {
                pair.Second.SynchronizePhi2Input();
                pair.Date++;
                pair.First.Phi2LeadingEdge(); pair.Second.Phi2LeadingEdge();
                pair.First.Phi2TrailingEdge(); pair.Second.Phi2TrailingEdge();
            }
        }
        pair.Check();
    }

    [Fact]
    public void DuplicatePinLevelsDoNotInventClocks()
    {
        var via = new Via6522();
        try
        {
            via.SetPhi2Input(false);
            via.Write(C.Timer1Low, 100); via.Write(C.Timer1High, 0);
            via.SetPhi2Input(true); via.SetPhi2Input(false); // Chargement.
            for (var cycle = 0; cycle < 10; cycle++)
            {
                via.SetPhi2Input(true); via.SetPhi2Input(true);
                via.SetPhi2Input(false); via.SetPhi2Input(false);
            }
            Assert.Equal(90, via.Read(C.Timer1Low));
        }
        finally { via.Reset(); }
    }

    [Fact]
    public void ReentrantCallbacksKeepTheNewProgrammedTimerAndPulseDates()
    {
        using var pair = new Pair();
        foreach (var via in new[] { pair.First, pair.Second })
        {
            var receiver = via.IrqChanged;
            var reloads = 0;
            via.IrqChanged = level =>
            {
                receiver!(level);
                if (!level || ++reloads > 3) return;
                via.Write(C.Timer1Low, 17); via.Write(C.Timer1High, 0);
                via.Write(C.PortA, 0x5a);
            };
        }
        pair.Both(via =>
        {
            via.Write(C.Enable, 0xc0);
            via.Write(C.Peripheral, 0x0a);
            via.Write(C.Timer1Low, 31); via.Write(C.Timer1High, 0);
        });
        for (var cycle = 0; cycle < 200; cycle++) { pair.High(cycle); pair.Low(cycle); }
        pair.Check();
    }

    private sealed class Pair : IDisposable
    {
        internal readonly Via6522 First = new();
        internal readonly Via6522 Second = new();
        internal long Date;
        private readonly List<string> _first = [];
        private readonly List<string> _second = [];

        internal Pair()
        {
            Attach(First, _first); Attach(Second, _second);
            First.Phi2TrailingEdge(); Second.SetPhi2Input(false);
        }
        private void Attach(Via6522 via, List<string> events)
        {
            via.IrqChanged = value => events.Add($"{Date}:irq:{value}");
            via.Ca2PinsChanged = value => events.Add($"{Date}:ca2:{value}");
            via.Cb2PinsChanged = value => events.Add($"{Date}:cb2:{value}");
            via.Cb1PinsChanged = value => events.Add($"{Date}:cb1:{value}");
            via.PortAPinsChanged = value => events.Add($"{Date}:pa:{value}");
            via.PortBPinsChanged = value => events.Add($"{Date}:pb:{value}");
            via.ControlOutputsChanged = value => events.Add($"{Date}:drivers:{value}");
        }
        internal void Both(Action<Via6522> action) { action(First); action(Second); }
        internal void High(int cycle) { Date = cycle * 2L; First.Phi2LeadingEdge(); Second.SetPhi2Input(true); }
        internal void Low(int cycle) { Date = cycle * 2L + 1; First.Phi2TrailingEdge(); Second.SetPhi2Input(false); }
        internal void Read(int register) => Assert.Equal(First.Read(register), Second.Read(register));
        internal void Check()
        {
            Second.SynchronizePhi2Input();
            foreach (var field in typeof(Via6522).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.Name is "_stablePhi2Clocks" or "_receivedPhi2Clocks" or "_retryPhi2Prefix" or "_receivingPhi2Input") continue;
                var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                if (!(type.IsPrimitive || type.IsEnum || type.IsArray)) continue;
                Assert.True(StructuralComparisons.StructuralEqualityComparer.Equals(field.GetValue(First), field.GetValue(Second)), field.Name);
            }
            Assert.Equal(_first, _second);
        }
        public void Dispose()
        {
            foreach (var via in new[] { First, Second })
            {
                via.IrqChanged = null; via.Ca2PinsChanged = via.Cb2PinsChanged = via.Cb1PinsChanged = null;
                via.PortAPinsChanged = via.PortBPinsChanged = null; via.ControlOutputsChanged = null;
                via.Reset();
            }
        }
    }
}
