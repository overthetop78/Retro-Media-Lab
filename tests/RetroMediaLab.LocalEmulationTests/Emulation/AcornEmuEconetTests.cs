using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using ModelAMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Échanges et erreurs ADLC purement en mémoire, sans socket, serveur ou ROM externe.</summary>
public sealed class AcornEmuEconetTests
{

    [Fact]
    public void BbcRtsReleaseLetsClosingFlagCompleteBeforeCtsRises()
    {
        var board = new ModelAMachine(new byte[16384], new byte[16384], econetInterface: true, econetCyclesPerByte: 4);
        var frames = new List<byte[]>();
        board.NetworkFrameTransmitted = frame => frames.Add(frame.ToArray());
        try
        {
            board.SetNetworkLine(true);
            board.Write(0xfea0, C.AddressControl);
            board.Write(0xfea3, 0x1e);
            board.Write(0xfea0, 0);
            Assert.NotEqual(0, board.Read(0xfea0) & C.NotClearToSend);
            board.Write(0xfea1, C.Rts | C.ClearTransmit);
            Assert.Equal(0, board.Read(0xfea0) & C.NotClearToSend);
            board.Write(0xfea2, 0xa5);
            for (var tick = 0; tick < 4; tick++) board.Tick(); // Le flag seul est parti.
            board.Write(0xfea1, C.LastData | C.FrameCompleteMode);
            Assert.Empty(frames);
            Assert.Equal(0, board.Read(0xfea0) & C.NotClearToSend);
            for (var tick = 0; tick < 128 && frames.Count == 0; tick++) board.Tick();
            Assert.Equal(new byte[] { 0xa5 }, Assert.Single(frames));
            for (var tick = 0; tick < 4 && (board.Read(0xfea0) & C.NotClearToSend) == 0; tick++) board.Tick();
            Assert.NotEqual(0, board.Read(0xfea0) & C.NotClearToSend);
            Assert.NotEqual(0, board.Read(0xfea0) & C.TransmitAvailable);
        }
        finally { board.NetworkFrameTransmitted = null; }
    }

    [Fact]
    public void NetworkFlagFillDoesNotLookLikeInactiveIdle()
    {
        var device = Device(C.ReceiveInterrupt); device.SetLine(true, true, false);
        for (var tick = 0; tick < 32; tick++) device.Tick();
        Assert.Equal(0, device.Read(1) & C.Idle);
        device.SetLine(true, true); for (var tick = 0; tick < 8; tick++) device.Tick();
        Assert.NotEqual(0, device.Read(1) & C.Idle);
    }
    [Fact]
    public void NetworkPollingContinuesInsideCpuBusTicks()
    {
        var board = new ModelAMachine(new byte[16384], new byte[16384]); var currentTick = 0;
        var calls = new List<(int Tick, int Cycles)>(); board.NetworkPoll = elapsed => calls.Add((currentTick, elapsed));
        try
        {
            for (currentTick = 1; currentTick <= 256; currentTick++) board.Tick();
            Assert.Equal(2, calls.Count); Assert.Equal(128, calls[1].Tick - calls[0].Tick); Assert.All(calls, call => Assert.Equal(128, call.Cycles));
        }
        finally { board.NetworkPoll = null; }
    }

    [Fact]
    public void NetworkOptionsStayOptionalAndDeclareTheExternalNfsRom()
    {
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var factory = new AcornEmuMachineFactory(); var defaults = NetworkFunctions.Read(configuration);
        Assert.False(defaults.Enabled); Assert.Equal(NetworkConstants.Disconnected, defaults.Mode);
        Assert.DoesNotContain(factory.GetFirmwareSlots(configuration), slot => slot.FieldId == NetworkConstants.Firmware);
        var enabled = configuration with { Options = new Dictionary<string, string>(configuration.Options) { [NetworkConstants.Enabled] = "True", [NetworkConstants.Station] = "42", [NetworkConstants.ClockRate] = "125000", [NetworkConstants.Mode] = "udp", [NetworkConstants.Peers] = "0.254=127.0.0.1:32769;1.12=127.0.0.2:32768" } };
        factory.ValidateOptions(enabled); var settings = NetworkFunctions.Read(enabled);
        Assert.True(settings.Enabled); Assert.Equal(42, settings.Station); Assert.Equal(128, settings.CyclesPerByte); Assert.Equal(2, settings.Peers.Count);
        Assert.Equal(254, settings.Peers[0].Station); Assert.Equal(32769, settings.Peers[0].EndPoint.Port);
        Assert.True(Assert.Single(factory.GetFirmwareSlots(enabled), slot => slot.FieldId == NetworkConstants.Firmware).IsRequired);
        Assert.True(factory.RequiresOptionRestart(enabled, NetworkConstants.Mode)); Assert.True(factory.RequiresOptionRestart(enabled, NetworkConstants.Firmware));
        var block = Assert.Single(factory.DescribeSettings(enabled), block => block.Tab == EmulationMachineTab.Ethernet);
        Assert.All(block.Fields, field => Assert.True(field.RequiresRestart));
        Assert.Contains(block.Fields, field => field.Id == NetworkConstants.Station && field.NumericRange!.Maximum == 254);
        Assert.Contains(block.Fields, field => field.Id == NetworkConstants.Port && field.NumericRange!.Maximum == 65535);
        Assert.Contains(block.Fields, field => field.Id == NetworkConstants.BindAddress && field.Editor == EmulationSettingsEditor.IPv4);
        Assert.Contains(block.Fields, field => field.Id == NetworkConstants.Peers && field.IsMultiline);
        Assert.All(block.Fields.Where(field => field.Id != NetworkConstants.Enabled), field =>
        {
            Assert.NotNull(field.ExplanationResourceKey);
            Assert.Equal(field.ExplanationResourceKey + ".Detailed", field.DetailedExplanationResourceKey);
        });
    }

    [Theory]
    [InlineData("station", "0")]
    [InlineData("clock", "333")]
    [InlineData("port", "65536")]
    [InlineData("port", "text")]
    [InlineData("bind", "::1")]
    [InlineData("bind", "127.1")]
    [InlineData("bind", "2130706433")]
    [InlineData("bind", "0x7f.0.0.1")]
    [InlineData("bind", "192.168.001.2")]
    [InlineData("bind", "1.2.3.256")]
    [InlineData("peers", "0.3=127.1:32769")]
    [InlineData("mode", "unknown")]
    [InlineData("peers", "0.2=127.0.0.1:1234;0.2=127.0.0.2:1234")]
    [InlineData("peers", "0.255=127.0.0.1:1234")]
    [InlineData("peers", "missing-endpoint")]
    public void InvalidNetworkSettingsAreRejectedBeforeAnySocketOpens(string key, string value)
    {
        Assert.Throws<ArgumentException>(() => new MachineConfiguration(MachineModel.BbcMicroModelA, options: new Dictionary<string, string> { [NetworkConstants.Prefix + key] = value }, core: Emulator.AcornEmu));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.42")]
    [InlineData("0.0.0.0")]
    public void ListeningAddressesAcceptLoopbackLanAndAllInterfacesWithoutOpeningSockets(string address)
    {
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA,
            options: new Dictionary<string, string> { [NetworkConstants.BindAddress] = address }, core: Emulator.AcornEmu);
        Assert.Equal(address, NetworkFunctions.Read(configuration).LocalEndPoint.Address.ToString());
    }

    [Fact]
    public void NetworkFirmwareUsesTheSharedMachineRulesAndPadsAnEightKibBank()
    {
        var bytes = Enumerable.Repeat((byte)0x5a, 8192).ToArray();
        var bank = BbcMicroFirmwareFunctions.GetRomBytes(bytes, FirmwareCategory.Network);
        Assert.Equal(16384, bank.Length); Assert.Equal(bytes, bank.AsSpan(0, 8192).ToArray()); Assert.All(bank.AsSpan(8192).ToArray(), value => Assert.Equal(255, value));
        Assert.Equal(FirmwareCategory.Network, FirmwareFunctions.Role(NetworkConstants.Firmware)); Assert.Equal(NetworkConstants.Firmware, FirmwareFunctions.Field(FirmwareCategory.Network));
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "8564d944fedd17caf2023454ba5a7c3947aedc1f"));
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "7462ced7b83d74b822815bc00ed40a89f84e0276"));
    }

    [Fact]
    public void AunRequiresAUniqueEndpointForEachStationIdentity()
    {
        Assert.Throws<ArgumentException>(() => new MachineConfiguration(MachineModel.BbcMicroModelA, options: new Dictionary<string, string>
        {
            [NetworkConstants.Mode] = NetworkConstants.Aun, [NetworkConstants.Peers] = "0.3=127.0.0.1:32769;0.4=127.0.0.1:32769"
        }, core: Emulator.AcornEmu));
    }

    [Fact]
    public void BbcStationMasksOnlyEconetNmiAndVideoWritesDoNotEnableIt()
    {
        var board = new ModelAMachine(new byte[16384], new byte[16384], econetInterface: true, econetStation: 42);
        Assert.Equal(42, board.Read(0xfe18)); Assert.Equal(42, board.Read(0xfe1f));
        board.Write(0xfea0, C.ReceiveInterrupt); Assert.False(board.Nmi);
        board.Write(0xfe20, 2); Assert.False(board.Nmi);
        board.Read(0xfe20); Assert.True(board.Nmi);
        board.Read(0xfe18); Assert.False(board.Nmi);
        board.Read(0xfe27); Assert.True(board.Nmi);
        board.Write(0xfe1f, 0); Assert.False(board.Nmi);
        board.Read(0xfe20); Assert.True(board.Nmi);
        board.Write(0xfebc, C.ReceiveReset); Assert.False(board.Nmi);
        board.Reset(false); Assert.False(board.Nmi);
    }

    [Fact]
    public void UnexpandedBbcDoesNotExposeAnAdlcOrAssertNetworkNmi()
    {
        var board = new ModelAMachine(new byte[16384], new byte[16384]);
        board.Write(0xfea0, C.ReceiveInterrupt); board.Read(0xfe20);
        Assert.Equal(0xfe, board.Read(0xfea0)); Assert.Equal(0xfe, board.Read(0xfe18)); Assert.False(board.Nmi);
        board.SetNetworkLine(true); Assert.False(board.ReceiveNetworkFrame([1, 2, 3]));
    }

    [Fact]
    public void BbcClockAndRepeatedAdlcAddressesReachTheSameFifo()
    {
        var board = new ModelAMachine(new byte[16384], new byte[16384], econetInterface: true, econetCyclesPerByte: 4);
        board.SetNetworkLine(true);
        board.Write(0xfea0, C.AddressControl);
        board.Write(0xfea3, 0x1e);
        board.Write(0xfea0, 0);
        Assert.True(board.ReceiveNetworkFrame([0xa5, 0x80]));
        for (var tick = 0; tick < 4; tick++) board.Tick();
        Assert.Equal(0, board.Read(0xfebd) & C.ReceiveAvailable); // Pas de raccourci par octet.
        for (var tick = 0; tick < 128 && (board.Read(0xfebd) & C.ReceiveAvailable) == 0; tick++) board.Tick();
        Assert.Equal(C.ReceiveAvailable | C.AddressPresent, board.Read(0xfebd));
        Assert.Equal(0xa5, board.Read(0xfebe));
        for (var tick = 0; tick < 128 && (board.Read(0xfebd) & C.ReceiveAvailable) == 0; tick++) board.Tick();
        Assert.Equal(C.ReceiveAvailable | C.FrameValid, board.Read(0xfebd));
        Assert.Equal(0x80, board.Read(0xfebf));
        Assert.Equal(0, board.Read(0xfea2));
    }

    private static Mc6854 Device(byte control = 0)
    {
        var device = new Mc6854(4);
        device.SetLine(true);
        device.Write(0, C.AddressControl);
        device.Write(3, 0x1e);
        device.Write(0, control);
        device.Read(0);
        device.Read(1);
        device.Write(1, C.ClearReceive | C.ClearTransmit);
        return device;
    }
}
