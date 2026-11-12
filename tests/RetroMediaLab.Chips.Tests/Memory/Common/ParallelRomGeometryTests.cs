using System.Text.Json;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Common;

public sealed class ParallelRomGeometryTests
{
    [Fact]
    public void ParallelLevelsPreserveAllProfileModesFeedbackAndPendingState()
    {
        foreach (bool latched in new[] { false, true })
        foreach (long access in new[] { 0L, 25L })
        foreach (var mode in Enum.GetValues<Tmm23256OutputEnableMode>())
        {
            var timing = new Tmm23256Timing("transport-comparison", access, access / 2,
                access / 4, 0, 0, 0, 0, 0, 0, mode, latched, access / 5, 8192, false);
            byte[] data = Enumerable.Range(0, 8192).Select(i => (byte)(i ^ (i >> 7))).ToArray();
            var scalar = new Tmm23256(data, timing);
            var parallel = new Tmm23256(data, timing);
            var expected = new List<(long, Tmm23256Output, long?)>();
            var actual = new List<(long, Tmm23256Output, long?)>();
            bool activeOe = mode == Tmm23256OutputEnableMode.ActiveHigh;
            void Send(Tmm23256 chip, bool direct, long time, Tmm23256Pins pins)
            {
                if (direct) chip.ReceiveParallelPins(time, pins.Address, pins.AddressKnown, pins.CeHigh, pins.OeHigh);
                else chip.ReceivePins(time, pins);
            }
            void Wire(Tmm23256 chip, bool direct, List<(long, Tmm23256Output, long?)> changes)
            {
                chip.OutputChanged = pins =>
                {
                    changes.Add((chip.TimeNanoseconds, pins, chip.NextEventNanoseconds));
                    if (pins.Driven && pins.Known && pins.Data == 0)
                        Send(chip, direct, chip.TimeNanoseconds, new(0, true, true, !activeOe));
                };
            }
            try
            {
                Wire(scalar, false, expected); Wire(parallel, true, actual);
                for (int i = 0; i < 64; i++)
                {
                    long time = i * 200L;
                    var pins = new Tmm23256Pins((ushort)(i * 17), i % 5 != 3, false, activeOe);
                    Send(scalar, false, time, pins); Send(parallel, true, time, pins);
                    scalar.AdvanceTo(time + 7); parallel.AdvanceTo(time + 7);
                    if (i == 7) parallel.RestoreState(parallel.CaptureState());
                    scalar.AdvanceTo(time + 50); parallel.AdvanceTo(time + 50);
                    pins = pins with { Address = (ushort)(i * 17 + 1), OeHigh = !activeOe };
                    Send(scalar, false, time + 60, pins); Send(parallel, true, time + 60, pins);
                    pins = pins with { CeHigh = true };
                    Send(scalar, false, time + 90, pins); Send(parallel, true, time + 90, pins);
                    scalar.AdvanceTo(time + 120); parallel.AdvanceTo(time + 120);
                    Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(parallel.CaptureState()));
                }
                Assert.Equal(expected, actual);
            }
            finally { scalar.OutputChanged = parallel.OutputChanged = null; }
        }
    }

    private static Tmm23256Timing Geometry(int capacity, bool latched = false) => new(
        $"test-logical-ROM-{capacity}", 0, 0, 0, 0, 0, 0, 0, 0, 0,
        Tmm23256OutputEnableMode.ActiveLow, latched, capacity: capacity, electricallyCharacterized: false);

    [Fact]
    public void SharedPeripheralAddressesEveryByteWithoutPaddingAndKeepsChipSelectAndOutputEnable()
    {
        foreach (int capacity in new[] { 8192, 16384, 32768 })
        foreach (bool latched in new[] { false, true })
        {
            var contents = Enumerable.Range(0, capacity).Select(a => (byte)(a ^ (a >> 8))).ToArray();
            var chip = new Tmm23256(contents, Geometry(capacity, latched));
            contents[0] = 0xff; // aucun emprunt du buffer de l'appelant
            Assert.False(chip.IsTimingQualified);
            long time = 0;
            for (int address = 0; address < capacity; address++)
            {
                chip.ReceivePins(time++, new((ushort)address, true, true, true));
                chip.ReceivePins(time++, new((ushort)address, true, false, false));
                Assert.True(chip.Output.Driven); Assert.True(chip.Output.Known);
                Assert.Equal((byte)(address ^ (address >> 8)), chip.Output.Data);
                chip.ReceivePins(time++, new((ushort)address, true, false, true));
                Assert.False(chip.Output.Driven);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => chip.ReceivePins(time,
                new((ushort)capacity, true, false, false)));
            Assert.Throws<ArgumentException>(() => new Tmm23256(new byte[capacity - 1], Geometry(capacity)));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry(12288));
        Assert.Equal(32768, Tmm23256Timing.SpecifiedMaximum.Capacity);
        Assert.True(Tmm23256Timing.SpecifiedMaximum.ElectricallyCharacterized);
    }

    [Fact]
    public void GeometryAndQualificationArePartOfSerializedStateIdentityAndLatchedAddress()
    {
        foreach (int capacity in new[] { 8192, 16384, 32768 })
        {
            var contents = new byte[capacity]; contents[capacity - 1] = 0xa5;
            var profile = Geometry(capacity, true);
            var chip = new Tmm23256(contents, profile);
            var restored = new Tmm23256(contents, profile);
            chip.ReceivePins(0, new((ushort)(capacity - 1), true, false, false));
            chip.ReceivePins(1, new(0, true, false, false)); // pas de nouveau frontCE
            var state = JsonSerializer.Deserialize<Tmm23256State>(JsonSerializer.Serialize(chip.CaptureState()))!;
            restored.RestoreState(state);
            Assert.Equal(0xa5, restored.Output.Data);
            Assert.Equal(capacity, restored.CaptureState().Timing.Capacity);
            Assert.False(restored.IsTimingQualified);
            foreach (var target in new[] { chip, restored })
            {
                target.ReceivePins(2, new(0, true, true, true));
                target.ReceivePins(3, new(0, true, false, false));
                Assert.Equal(0, target.Output.Data);
            }
            string before = JsonSerializer.Serialize(restored.CaptureState());
            int otherCapacity = capacity == 8192 ? 16384 : 8192;
            Tmm23256State[] invalid =
            [
                state with { Version = 3 },
                state with { Timing = Geometry(otherCapacity, true) },
                state with { Pins = state.Pins with { Address = (ushort)capacity } },
                state with { LatchedAddress = (ushort)capacity },
                state with { Timing = new(profile.Id,0,0,0,0,0,0,0,0,0,
                    Tmm23256OutputEnableMode.ActiveLow,true,capacity:capacity,electricallyCharacterized:true) },
            ];
            foreach (var bad in invalid)
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(bad));
                Assert.Equal(before, JsonSerializer.Serialize(restored.CaptureState()));
            }
        }
    }
}
