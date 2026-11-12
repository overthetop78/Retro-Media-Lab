using System.Text.Json;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Common;

public sealed class ParallelRomGeometryTests
{
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
