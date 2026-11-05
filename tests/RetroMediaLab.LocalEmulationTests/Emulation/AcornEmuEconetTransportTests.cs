using System.Net;
using System.Net.Sockets;
using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using Board = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.NetworkConstants;
using D = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuEconetTransportTests
{
    private const byte Station = 2;
    private const byte PeerStation = 3;
    private const int PollCycles = 128;
    private const int IdlePasses = 50_000;
    private const int TimeoutMilliseconds = 2_000;
    private const ushort AdlcControl = 0xfea0;
    private const ushort AdlcStatusTwo = 0xfea1;
    private const ushort AdlcData = 0xfea2;
    private const ushort AdlcLastData = 0xfea3;
    private static readonly FieldInfo SocketField = typeof(EconetTransport).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo ReceiveField = typeof(EconetTransport).GetField("_receive", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public void IdleAdvancesKeepOneReceiveAndDisposeReleasesItsPort()
    {
        WithTransport(C.RawUdp, (transport, board, sender, endpoint) =>
        {
            transport.Advance(PollCycles);
            var first = (ValueTask<SocketReceiveFromResult>)ReceiveField.GetValue(transport)!;
            Assert.False(first.IsCompleted);
            for (var pass = 0; pass < IdlePasses; pass++) transport.Advance(PollCycles);
            Assert.Equal(first, (ValueTask<SocketReceiveFromResult>)ReceiveField.GetValue(transport)!);
            transport.Dispose();
            Assert.Throws<ObjectDisposedException>(() => transport.Advance(PollCycles));
            Socket? replacement = null;
            try
            {
                replacement = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                replacement.ExclusiveAddressUse = true;
                replacement.Bind(endpoint);
                Assert.Equal(endpoint, replacement.LocalEndPoint);
            }
            finally { replacement?.Dispose(); }
        });
    }

    [Theory]
    [InlineData(C.RawUdp)]
    [InlineData(C.Aun)]
    public void PendingDatagramIsDeliveredThroughTheRealAdlc(string mode)
    {
        WithTransport(mode, (transport, board, sender, endpoint) =>
        {
            transport.Advance(PollCycles);
            byte[] packet = mode == C.RawUdp
                ? [Station, 0, PeerStation, 0, 0x55, 0xaa]
                : [1, 0x99, 5, 0, 4, 0, 0, 0, 0x55, 0xaa];
            byte[] expected = mode == C.RawUdp
                ? packet
                : [255, 255, PeerStation, 0, 0x85, 0x99, 0x55, 0xaa];
            sender.SendTo(packet, endpoint);
            Assert.Equal(expected, ReadFrame(transport, board, expected.Length));
        });
    }

    [Fact]
    public void UnknownPeerAndInvalidDatagramsDoNotHideTheNextValidFrame()
    {
        WithTransport(C.RawUdp, (transport, board, sender, endpoint) =>
        {
            Socket? stranger = null;
            try
            {
                stranger = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                stranger.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                stranger.SendTo(new byte[] { Station, 0, PeerStation, 0, 0xee }, endpoint);
                sender.SendTo(new byte[] { Station }, endpoint);
                sender.SendTo(new byte[] { PeerStation, 0, PeerStation, 0, 0xdd }, endpoint);
                byte[] expected = [Station, 0, PeerStation, 0, 0x55];
                sender.SendTo(expected, endpoint);
                Assert.Equal(expected, ReadFrame(transport, board, expected.Length));
            }
            finally { stranger?.Dispose(); }
        });
    }

    [Fact]
    public void ResetKeepsThePendingHostReceiveUsable()
    {
        WithTransport(C.RawUdp, (transport, board, sender, endpoint) =>
        {
            transport.Advance(PollCycles);
            transport.Reset();
            byte[] expected = [Station, 0, PeerStation, 0, 0x66];
            sender.SendTo(expected, endpoint);
            Assert.Equal(expected, ReadFrame(transport, board, expected.Length));
        });
    }

    [Theory]
    [InlineData(C.RawUdp)]
    [InlineData(C.Aun)]
    public void TransmissionStillUsesTheConfiguredPeer(string mode)
    {
        WithTransport(mode, (transport, board, sender, endpoint) =>
        {
            byte[] frame = mode == C.RawUdp ? [PeerStation, 0, Station, 0, 0x55] : [255, 255, Station, 0, 0x85, 0x99, 0x55];
            transport.Transmit(frame);
            var bytes = new byte[C.MaximumDatagramBytes];
            var length = sender.Receive(bytes);
            if (mode == C.RawUdp) Assert.Equal(frame, bytes.AsSpan(0, length).ToArray());
            else Assert.Equal(new byte[] { 1, 0x99, 5, 0, 4, 0, 0, 0, 0x55 }, bytes.AsSpan(0, length).ToArray());
        });
    }

    private static byte[] ReadFrame(EconetTransport transport, Board board, int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < bytes.Length; index++)
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                transport.Advance(PollCycles);
                board.Tick();
                return (board.Read(AdlcStatusTwo) & D.ReceiveAvailable) != 0;
            }, TimeoutMilliseconds), $"L'octet {index} du datagramme attendu n'atteint pas le FIFO ADLC.");
            bytes[index] = board.Read(index == bytes.Length - 1 ? AdlcLastData : AdlcData);
        }
        return bytes;
    }

    private static void WithTransport(string mode, Action<EconetTransport, Board, Socket, IPEndPoint> action)
    {
        Socket? sender = null;
        EconetTransport? transport = null;
        var board = new Board(new byte[16384], new byte[16384], econetInterface: true, econetCyclesPerByte: 4);
        try
        {
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            sender.ReceiveTimeout = TimeoutMilliseconds;
            var peer = new EconetPeer(0, PeerStation, (IPEndPoint)sender.LocalEndPoint!);
            var settings = new EconetSettings(true, Station, C.DefaultClockRate, mode, new IPEndPoint(IPAddress.Loopback, 0), [peer]);
            transport = new EconetTransport(settings, board);
            var socket = (Socket)SocketField.GetValue(transport)!;
            board.SetNetworkLine(true);
            board.Write(AdlcControl, D.AddressControl);
            board.Write(AdlcLastData, 0x1e);
            board.Write(AdlcControl, 0);
            action(transport, board, sender, (IPEndPoint)socket.LocalEndPoint!);
        }
        finally
        {
            try { transport?.Dispose(); }
            finally
            {
                try { sender?.Dispose(); }
                finally { board.Reset(true); }
            }
        }
    }
}
