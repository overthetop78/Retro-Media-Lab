using System.Collections;
using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using Chip = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class Intel8271DriveAdvanceTests
{
    [Theory]
    [InlineData(15_625)]
    [InlineData(15_627)]
    [InlineData(1_000_000)]
    [InlineData(3_000_000)]
    [InlineData(int.MaxValue)]
    public void QuietPrefixesPreserveExactTimeAndFraction(int frequency)
    {
        using var pair = new Pair(frequency);
        foreach (var count in new[] { 1, 7, 97, 10_001, 65_537 }) pair.Advance(count);
        Assert.Throws<ArgumentOutOfRangeException>(() => pair.Grouped.AdvanceTo(0));
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(3_000_000)]
    public void IndexBoundariesMatchTheIndependentRotationPeriod(int frequency)
    {
        using var pair = new Pair(frequency);
        pair.ChangeMedia(false);
        pair.Control(H.Drive0Select | H.OutputLoadHead);
        pair.Advance(1);
        Assert.True(pair.Index);
        pair.Advance(frequency / 500 - 2);
        Assert.True(pair.Index);
        pair.Advance(1);
        Assert.False(pair.Index);
        pair.Advance(frequency / 5 - frequency / 500 - 1);
        Assert.False(pair.Index);
        pair.Advance(1);
        Assert.True(pair.Index);
    }

    [Theory]
    [InlineData(1_000_000, 2_000_000)]
    [InlineData(2_000_000, 4_000_000)]
    [InlineData(3_000_000, 2_000_000)]
    public void RotationFluxIndexAndInputChangesKeepTheirDates(int frequency, int chipClock)
    {
        using var pair = new Pair(frequency, chipClock);
        pair.Control(H.Drive0Select | H.OutputLoadHead);
        // Deux tours complets, incluant la dernière impulsion, le retour index et sa fin.
        for (var index = 0; index < 43; index++) pair.Advance(frequency / 100 + index % 3);
        pair.Control(H.Drive0Select | H.OutputLoadHead | H.OutputStep | H.OutputDirection);
        pair.Advance(127);
        pair.Control(H.Drive1Select | H.OutputLoadHead);
        pair.Advance(1_001);
        pair.ChangeMedia(false);
        pair.Advance(1_003);
        pair.ChangeMedia(true);
        pair.Advance(1_007);
        pair.Reset();
        pair.Advance(1_009);
        pair.Control(H.Drive0Select | H.OutputLoadHead);
        pair.Advance(10_001);
        Assert.NotEmpty(pair.ScalarEdges);
    }

    [Theory]
    [InlineData(1_000_000, 2_000_000, false)]
    [InlineData(1_000_000, 2_000_000, true)]
    [InlineData(2_000_000, 4_000_000, false)]
    [InlineData(2_000_000, 4_000_000, true)]
    public void SectorTransfersKeepRequestsDataAndWriteWindows(int frequency, int chipClock, bool write)
    {
        using var pair = new Pair(frequency, chipClock);
        pair.Command(0x35, 0x0d, 1, 0, 0x10);
        pair.Command(write ? (byte)0x4a : (byte)0x52, 0, 1);
        var bytes = new List<byte>();
        while (pair.Time < frequency)
        {
            // La carte écoute l'échéance ; elle ne traverse pas une demande d'octet.
            var end = Math.Min(pair.Time + 997, pair.Next ?? long.MaxValue);
            pair.Advance(checked((int)(end - pair.Time)));
            if (pair.First.Drq)
            {
                pair.SynchronizeClock();
                if (write) pair.WriteData((byte)(bytes.Count ^ 0xa5));
                else
                {
                    var data = pair.First.Read(4);
                    Assert.Equal((byte)bytes.Count, data);
                    Assert.Equal(data, pair.Second.Read(4));
                }
                bytes.Add((byte)(bytes.Count ^ 0xa5));
            }
            if ((pair.First.Read(0) & 0x90) != 0x10) continue;
            Assert.Equal(0, pair.First.Read(1));
            Assert.Equal(0, pair.Second.Read(1));
            pair.Compare();
            break;
        }
        Assert.Equal(128, bytes.Count);
        Assert.True(pair.Time < frequency, "Transfer did not finish within one second.");
        if (write)
        {
            Assert.True(pair.Disk1.WindowWrites > 0);
            Assert.Equal(pair.Disk1.LastWriteStart, pair.Disk2.LastWriteStart);
            Assert.Equal(pair.Disk1.LastWriteDuration, pair.Disk2.LastWriteDuration);
            Assert.Equal(pair.Disk1.LastWritePulses, pair.Disk2.LastWritePulses);
            Assert.Equal(pair.Disk1.Sectors[0].Data, pair.Disk2.Sectors[0].Data);
        }
        pair.Advance(113);
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(3_000_000)]
    public void ManualWriteGatePublishesWholeRevolutionsAndPartialEnding(int frequency)
    {
        using var pair = new Pair(frequency);
        pair.Control(H.Drive0Select | H.OutputLoadHead | H.OutputWriteEnable);
        pair.Advance(frequency / 2 + 17);
        Assert.True(pair.Disk1.WindowWrites >= 2);
        pair.Control(H.Drive0Select | H.OutputLoadHead);
        pair.Compare();
        Assert.Equal(pair.Disk1.WindowWrites, pair.Disk2.WindowWrites);
        Assert.Equal(pair.Disk1.LastWriteStart, pair.Disk2.LastWriteStart);
        Assert.Equal(pair.Disk1.LastWriteDuration, pair.Disk2.LastWriteDuration);
        Assert.Equal(pair.Disk1.AfterWrite!.FluxIntervalsNanoseconds, pair.Disk2.AfterWrite!.FluxIntervalsNanoseconds);
    }

    private sealed class Pair : IDisposable
    {
        internal readonly MemoryTrackDisk Disk1 = new();
        internal readonly MemoryTrackDisk Disk2 = new();
        internal readonly Chip First;
        internal readonly Chip Second;
        internal readonly Intel8271Drive Scalar;
        internal readonly Intel8271Drive Grouped;
        internal readonly List<string> ScalarEdges = [];
        private readonly List<string> _groupedEdges = [];
        internal long Time;
        internal long? Next;
        private bool _present = true;
        private object? _checkedPulses;
        private object? _checkedGroupedPulses;
        internal bool Index => ((bool?[])IndexField.GetValue(Scalar)!)[0] == true;
        private readonly Action<long?> _deadline;

        internal Pair(int frequency, int chipClock = 2_000_000)
        {
            First = new(frequency, chipClock);
            Second = new(frequency, chipClock);
            Scalar = new(First, index => _present && index == 0 ? Disk1 : null, frequency, chipClock);
            Grouped = new(Second, index => _present && index == 0 ? Disk2 : null, frequency, chipClock);
            _deadline = value => Next = value;
            Next = Grouped.NextClockEvent;
            Grouped.NextClockEventChanged += _deadline;
            First.NmiChanged = high => Edge(ScalarEdges, Scalar, First, "nmi", high);
            Second.NmiChanged = high => Edge(_groupedEdges, Grouped, Second, "nmi", high);
            First.WriteDataChanged += high => Edge(ScalarEdges, Scalar, First, "data", high);
            Second.WriteDataChanged += high => Edge(_groupedEdges, Grouped, Second, "data", high);
            First.DriveSignalsChanged += () => Edge(ScalarEdges, Scalar, First, "drive", First.DriveControl);
            Second.DriveSignalsChanged += () => Edge(_groupedEdges, Grouped, Second, "drive", Second.DriveControl);
            ChangeMedia(true);
        }

        internal void Advance(int clocks)
        {
            for (var index = 0; index < clocks; index++) Scalar.Tick();
            Time += clocks;
            Grouped.AdvanceTo(Time);
            Compare();
            Assert.Equal(Grouped.NextClockEvent, Next);
        }

        internal void SynchronizeClock() { Scalar.SynchronizeClock(); Grouped.SynchronizeClock(); }
        internal void Command(byte command, params byte[] parameters)
        {
            SynchronizeClock();
            First.Write(0, command); Second.Write(0, command);
            foreach (var value in parameters) { First.Write(1, value); Second.Write(1, value); }
            Scalar.Synchronize(); Grouped.Synchronize();
        }
        internal void WriteData(byte data) { First.Write(4, data); Second.Write(4, data); }
        internal void Control(int control) => Command(H.WriteSpecial, H.DriveOutput, (byte)control);
        internal void ChangeMedia(bool present)
        {
            SynchronizeClock();
            _present = present;
            Scalar.MediaChanged(0); Grouped.MediaChanged(0);
        }
        internal void Reset()
        {
            SynchronizeClock();
            First.Reset(); Second.Reset();
            Scalar.Reset(); Grouped.Reset();
        }
        internal void Compare()
        {
            CompareFields(Scalar, Grouped);
            CompareFields(First, Second);
            // La table de flux est immuable entre deux chargements de piste.
            var pulses = PulsesField.GetValue(Scalar);
            var groupedPulses = PulsesField.GetValue(Grouped);
            if (!ReferenceEquals(pulses, _checkedPulses) || !ReferenceEquals(groupedPulses, _checkedGroupedPulses))
            {
                Assert.True(StructuralComparisons.StructuralEqualityComparer.Equals(pulses, groupedPulses));
                _checkedPulses = pulses;
                _checkedGroupedPulses = groupedPulses;
            }
            Assert.Equal(ScalarEdges, _groupedEdges);
        }
        public void Dispose()
        {
            try { Reset(); }
            finally
            {
                Grouped.NextClockEventChanged -= _deadline;
                First.NmiChanged = Second.NmiChanged = null;
                try { Disk1.Dispose(); }
                finally { Disk2.Dispose(); }
            }
        }
    }

    private static void Edge(List<string> edges, Intel8271Drive drive, Chip chip, string signal, object value) =>
        edges.Add($"{chip.InputClockCount}:{TimeField.GetValue(drive)}:{signal}:{value}");

    private static readonly FieldInfo TimeField = typeof(Intel8271Drive).GetField("_time", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo PulsesField = typeof(Intel8271Drive).GetField("_pulses", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo IndexField = typeof(Intel8271Drive).GetField("_indexLevels", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void CompareFields(object first, object second)
    {
        foreach (var field in first.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (field.Name is "_publishedClockEvent" or "_clockMutationDepth" or "_pulses") continue;
            var type = field.FieldType;
            if (!(type.IsPrimitive || type.IsEnum || type.IsArray || type == typeof(List<uint>))) continue;
            var expected = field.GetValue(first);
            var actual = field.GetValue(second);
            Assert.True(StructuralComparisons.StructuralEqualityComparer.Equals(
                expected is List<uint> left ? left.ToArray() : expected,
                actual is List<uint> right ? right.ToArray() : actual), $"{first.GetType().Name}.{field.Name}: {expected} != {actual}");
        }
    }
}
