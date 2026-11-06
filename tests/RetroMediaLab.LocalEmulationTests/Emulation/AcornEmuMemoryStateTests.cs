using System.Text.Json;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.MemoryExtensionConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Services;
using Plus = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuMemoryStateTests
{
    [Fact]
    public void BPlusPrivateAndShadowSelectionsReturnWithTheirRam()
    {
        var memory = new ModelBPlusMemory();
        var main = new byte[Plus.MainBytes];
        memory.Select(Plus.ControlStart, Plus.EnableBit);
        memory.Select(Plus.ControlStart + Plus.ShadowRegister, Plus.EnableBit);
        memory.WritePrivate(0x8123, 0x5a);
        memory.Write(0x3456, 0xc000, 0xa5, main);
        var saved = RoundTrip(memory.CaptureState());
        memory.Reset(true);
        memory.RestoreState(saved);
        Assert.True(memory.PrivateSelected(0x8123));
        Assert.True(memory.ShadowEnabled);
        Assert.Equal(0x5a, memory.ReadPrivate(0x8123));
        Assert.Equal(0xa5, memory.ReadVideo(0x3456, main));
        Assert.Equal(0, main[0x3456]);
    }

    [Fact]
    public void KeyboardContactsAndBreakReturnToTheSavedState()
    {
        var keyboard = new Keyboard();
        keyboard.SetInput(new HashSet<EmulationKey> { EmulationKey.A, EmulationKey.Break });
        var saved = RoundTrip(keyboard.CaptureState());
        var contact = (byte)BbcMicroKeyboardDictionary.Matrix[EmulationKey.A];
        var before = keyboard.Read(contact);
        keyboard.SetInput(new HashSet<EmulationKey>());
        keyboard.RestoreState(saved);
        Assert.Equal(before, keyboard.Read(contact));
        Assert.True(keyboard.BreakPressed);
        saved.Matrix[contact] = false;
        Assert.Equal(before, keyboard.Read(contact));
    }

    [Fact]
    public void SidewaysRamAndSelectionReturnWithoutReplacingTheRom()
    {
        var basic = Enumerable.Repeat((byte)0xa5, 16384).ToArray();
        var banks = new SidewaysRoms(basic, master: true);
        banks.Select(4);
        banks.Write(0x8123, 0x5a);
        var saved = RoundTrip(banks.CaptureState());
        banks.Write(0x8123, 0x33);
        banks.Select(12);
        banks.RestoreState(saved);
        Assert.Equal(0x5a, banks.Read(0x8123));
        saved.RamBanks[4]![0x123] = 0;
        Assert.Equal(0x5a, banks.Read(0x8123));
        banks.Select(12);
        Assert.Equal(0xa5, banks.Read(0x8123));
    }

    [Theory]
    [InlineData(C.AriesB20)]
    [InlineData(C.Watford32)]
    public void ExpansionRamAndBankSelectionReturnToTheSavedState(string card)
    {
        var extension = new MemoryExtension(new MemoryExtensionSettings(card));
        var main = new byte[32768];
        extension.Control(C.ShadowRamSelect);
        extension.Write(0x3456, 0x5a, main);
        var saved = RoundTrip(extension.CaptureState());
        extension.Write(0x3456, 0x22, main);
        extension.Control(C.MainRamSelect);
        extension.RestoreState(saved);
        Assert.Equal(0x5a, extension.Read(0x3456, main));
        Assert.Equal(0, main[0x3456]);
        saved.Ram[card == C.AriesB20 ? 0x3456 - C.ShadowStart : 0x3456] ^= 1;
        Assert.Equal(0x5a, extension.Read(0x3456, main));
    }

    private static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
