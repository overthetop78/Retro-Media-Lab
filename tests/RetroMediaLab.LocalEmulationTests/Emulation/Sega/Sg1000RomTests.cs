using System.Text.Json;
using RetroMediaLab.Chips.Memory.Fujitsu.MB83xx.MB83256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class Sg1000RomTests
{
    private static readonly ushort[] ProbeAddresses = [0x1abc,0x3abc,0x5abc,0x7abc,0x9abc,0xbabc];
    private static byte Pattern(int address) => (byte)((address >> 8) ^ (address * 13));

    [Fact]
    public void StandardRomPackagesDriveCpuReadsMirrorsAndProtectTheirContents()
    {
        foreach (int size in new[] { 8192,16384,32768,49152 })
        {
            Sg1000Board? board = null;
            try
            {
                var image = ProbeImage(size);
                board = new(image);
                board.AdvanceTo(1_000_000);
                Assert.True(board.Cpu.IsHalted); Assert.False(board.Cpu.IsFaulted);
                var state = board.CaptureState();
                Assert.NotNull(state.CartridgeLow);
                Assert.Equal(Math.Min(size,32768),state.CartridgeLow!.Timing.Capacity);
                Assert.Equal(size == 49152,state.CartridgeHigh is not null);
                Assert.False(state.CartridgeLow.Timing.ElectricallyCharacterized);
                for (int index = 0; index < ProbeAddresses.Length; index++)
                {
                    int address = ProbeAddresses[index];
                    byte expected = address < 0x8000 ? image[address & (Math.Min(size,32768) - 1)]
                        : size == 49152 ? image[address] : (byte)0xff; // explicit floating-bus seed
                    Assert.Equal(expected,state.Ram.Cells[index]);
                    Assert.Equal(255,state.Ram.Known[index]);
                }
            }
            finally { board?.Dispose(); }
        }
    }

    [Fact]
    public void ExplicitTwo8KiBLayoutChecksRawDumpMirrorsAndBothAddressWindows()
    {
        var low = ProbeImage(8192);
        var high = Enumerable.Repeat((byte)0x69,8192).ToArray();
        foreach (int size in new[] { 16384,40960,49152 })
        {
            Sg1000Board? board = null;
            try
            {
                var image = new byte[size];
                int highStart = size == 16384 ? 8192 : 32768;
                for (int offset = 0; offset < highStart; offset += 8192) low.CopyTo(image,offset);
                for (int offset = highStart; offset < size; offset += 8192) high.CopyTo(image,offset);
                board = new(image,romConfiguration:new(CartridgeLayout:Sg1000CartridgeLayout.Two8KiB));
                board.AdvanceTo(1_000_000);
                Assert.True(board.Cpu.IsHalted);
                var state = board.CaptureState();
                Assert.Equal(8192,state.CartridgeLow!.Timing.Capacity);
                Assert.Equal(8192,state.CartridgeHigh!.Timing.Capacity);
                for (int index = 0; index < ProbeAddresses.Length; index++)
                    Assert.Equal(ProbeAddresses[index] < 0x8000 ? low[ProbeAddresses[index] & 8191] : (byte)0x69,
                        state.Ram.Cells[index]);
                byte[] before = Snapshot(board);
                var malformed = image.ToArray();
                if (size > 16384)
                {
                    malformed[8192] ^= 1;
                    Assert.Throws<ArgumentException>(() => board.SetCartridge(malformed));
                    Assert.Equal(before,Snapshot(board));
                }
                Assert.Throws<ArgumentException>(() => board.SetCartridge(new byte[8193]));
                Assert.Equal(before,Snapshot(board));
            }
            finally { board?.Dispose(); }
        }
    }

    [Fact]
    public void InternalGameIsOffByDefaultAndCartridgePresenceInhibitsItUntilEjection()
    {
        foreach (bool withoutOe in new[] { false,true })
        {
        var internalGame = MarkerImage(16384,0xa1);
        Sg1000Board? disabled = null, board = null;
        try
        {
            disabled = new([],false,internalGame:internalGame);
            Assert.False(disabled.InternalGameEnabled); Assert.False(disabled.InternalGameSelected);
            Assert.Null(disabled.CaptureState().InternalRom);
            Assert.Throws<ArgumentException>(() => new Sg1000Board([],false,new(InternalGameEnabled:true)));
            var profile = withoutOe ? new Tmm23256Timing("test-internal-without-OE",0,0,0,0,0,0,0,0,0,
                Tmm23256OutputEnableMode.AlwaysEnabled,false,capacity:16384,electricallyCharacterized:false) : null;
            board = new([],romConfiguration:new(InternalGameEnabled:true,InternalGameProfile:profile),internalGame:internalGame);
            board.AdvanceTo(1_000_000);
            Assert.Equal(0xa1,board.CaptureState().Ram.Cells[0]);
            Assert.True(board.InternalGameSelected);
            board.SetCartridge(MarkerImage(8192,0xc2));
            board.AdvanceTo(2_000_000);
            Assert.True(board.CartridgePresent); Assert.True(board.InternalGameEnabled);
            Assert.False(board.InternalGameSelected);
            Assert.False(board.CaptureState().InternalRom!.Output.Driven);
            Assert.Equal(0xc2,board.CaptureState().Ram.Cells[0]);
            board.SetCartridge([]);
            board.AdvanceTo(3_000_000);
            Assert.False(board.CartridgePresent); Assert.True(board.InternalGameSelected);
            Assert.Equal(0xa1,board.CaptureState().Ram.Cells[0]);
        }
        finally { try { disabled?.Dispose(); } finally { board?.Dispose(); } }
        }
    }

    [Fact]
    public void ManufacturerRomProfilesKeepTheirCaptureAndDelaysOnTheConnectedCpuBus()
    {
        foreach (var profile in new[] { Tmm23256Timing.SpecifiedMaximum,Mb83256Profile.Grade25 })
        {
            Sg1000Board? board = null;
            try
            {
                var image = ProbeImage(32768);
                board = new(image,romConfiguration:new(CartridgeLowProfile:profile));
                board.AdvanceTo(1_000_000);
                Assert.True(board.Cpu.IsHalted); Assert.False(board.Cpu.IsFaulted);
                var state = board.CaptureState();
                Assert.Equal(profile,state.CartridgeLow!.Timing);
                Assert.True(state.CartridgeLow.Initialized);
                Assert.Equal(image[0x1abc],state.Ram.Cells[0]);
                Assert.Equal(image[0x7abc],state.Ram.Cells[3]);
                // These are intentionally selected manufacturer profiles in a
                // synthetic fixture, not identification of every SG cartridge.
            }
            finally { board?.Dispose(); }
        }
    }

    [Fact]
    public void InsertedCartridgePowerPhaseAndRomStateResumeAndRejectIncompatibleConfigurationAtomically()
    {
        Sg1000Board? original = null, restored = null;
        try
        {
            var options = new Sg1000RomConfiguration(InternalGameEnabled:true,
                CartridgeLowProfile:Tmm23256Timing.SpecifiedMaximum);
            var internalGame = MarkerImage(16384,0xa1);
            var image = MarkerImage(32768,0xb2);
            original = new([],romConfiguration:options,internalGame:internalGame);
            original.AdvanceTo(1_000_000);
            original.Cpu.SetReset(true); // keep the CPU quiescent while the new ROM powers up
            original.AdvanceTo(1_005_000); // RESET is sampled; finish the current CPU bus cycle first
            Assert.True(original.Cpu.Pins.MemoryRequestHigh);
            original.SetCartridge(image);
            long origin = original.TimeNanoseconds;
            var inserted = original.CaptureState();
            Assert.Equal(origin,inserted.CartridgeOrigin);
            Assert.Equal(0,inserted.CartridgeLow!.Time);
            Assert.False(inserted.CartridgeLow.Initialized);
            original.AdvanceTo(origin + 50_037);
            Assert.False(original.CaptureState().CartridgeLow!.Initialized);
            var state = JsonSerializer.Deserialize<Sg1000BoardState>(Snapshot(original))!;
            restored = new(image,false,options,internalGame);
            restored.RestoreState(state);
            Assert.Equal(Snapshot(original),Snapshot(restored));
            foreach (long time in new long[] { origin + 100_007,origin + 150_111 })
            {
                original.AdvanceTo(time); restored.AdvanceTo(time);
                Assert.Equal(Snapshot(original),Snapshot(restored));
            }
            Assert.True(original.CaptureState().CartridgeLow!.Initialized);
            original.Cpu.SetReset(false); restored.Cpu.SetReset(false);
            original.AdvanceTo(2_000_000); restored.AdvanceTo(2_000_000);
            Assert.Equal(Snapshot(original),Snapshot(restored));
            Assert.Equal(0xb2,original.CaptureState().Ram.Cells[0]);
            byte[] before = Snapshot(original);
            foreach (var invalid in new[]
            {
                state with { Version = 1 },state with { InternalGameHash = [] },
                state with { RomConfiguration = options with { InternalGameEnabled = false } },
                state with { CartridgeOrigin = state.Time + 1 },state with { CartridgeLow = null },
                state with { CartridgeLow = state.CartridgeLow! with { RomHash = "different" } },
            })
            {
                Assert.Throws<ArgumentException>(() => original.RestoreState(invalid));
                Assert.Equal(before,Snapshot(original));
            }
        }
        finally { try { original?.Dispose(); } finally { restored?.Dispose(); } }
    }

    private static byte[] Snapshot(Sg1000Board board) => JsonSerializer.SerializeToUtf8Bytes(board.CaptureState());

    private static byte[] ProbeImage(int size)
    {
        var rom = Enumerable.Range(0,size).Select(Pattern).ToArray();
        new byte[] { 0xc3,0,1 }.CopyTo(rom,0);
        var code = new List<byte> { 0xf3,0x31,0xff,0xc3,0x3e,0x55,0x32,0xbc,0x1a };
        for (int index = 0; index < ProbeAddresses.Length; index++)
        {
            ushort address = ProbeAddresses[index];
            code.AddRange([0x3a,(byte)address,(byte)(address >> 8),0x32,(byte)index,0xc0]);
        }
        code.Add(0x76); code.CopyTo(rom,0x100);
        return rom;
    }

    private static byte[] MarkerImage(int size,byte marker)
    {
        var rom = Enumerable.Repeat((byte)0xff,size).ToArray();
        new byte[] { 0xc3,0,1 }.CopyTo(rom,0);
        new byte[] { 0xf3,0x31,0xff,0xc3,0x3e,marker,0x32,0,0xc0,0xc3,0,1 }.CopyTo(rom,0x100);
        return rom;
    }
}
