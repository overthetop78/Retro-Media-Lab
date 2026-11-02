using RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Services;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Transactions sur les broches : aucun accès direct aux registres pour lire les résultats.</summary>
public sealed class AcornEmuPcf8583Tests
{
    [Theory]
    [InlineData(0, "F0EAE9A535505D82BA3488DDB7895434DF940D73")]
    [InlineData(1, "96163285797E0D54017D8D4AE87835328A4658BD")]
    [InlineData(2, "9DAB30B4C3305E1142819687889FCA334B532679")]
    public void InitialConfigurationMatchesDocumentedReferenceImages(int profile, string expected)
    {
        var image = new byte[256];
        ArchimedesCmosConstants.Reference((ArchimedesCmosProfile)profile).CopyTo(image.AsSpan(16));
        Assert.Equal(expected, Convert.ToHexString(SHA1.HashData(image)));
    }

    [Fact]
    public void FactoryRamDoesNotReplaceClockRegistersAndIsCopiedAtConstruction()
    {
        var ram = Enumerable.Range(0, 240).Select(index => (byte)(index + 16)).ToArray();
        var rtc = new Pcf8583(1000, new DateTime(2026, 11, 2, 13, 45, 56), initialRam: ram);
        Array.Clear(ram);
        Assert.Equal(new byte[] { 0, 0x56, 0x45, 0x13, 0x82, 0x31 }, Read(rtc, 1, 6));
        Assert.Equal(new byte[] { 0x10, 0x11, 0x12 }, Read(rtc, 16, 3));
        Assert.Equal(0, Read(rtc, 0, 1)[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pcf8583(1000, initialRam: new byte[239]));
    }

    [Fact]
    public void RestoredUserCmosTakesPrecedenceOverTheInitialConfigurationEvenAfterReset()
    {
        var time = new DateTime(2026, 11, 2);
        var defaults = ArchimedesCmosConstants.Create(ArchimedesCmosProfile.RiscOs3, time);
        var rtc = new Pcf8583(1000, time, initialRam: defaults);
        var saved = rtc.ReadNonvolatileMemory();
        saved[0xc7] = 2; // L'utilisateur a configuré deux lecteurs, au lieu d'un.
        saved[0xd3] = 7;
        rtc.WriteNonvolatileMemory(saved);
        rtc.ResetBus();
        Assert.Equal(2, Read(rtc, 0xc7, 1)[0]);
        Assert.Equal(7, Read(rtc, 0xd3, 1)[0]);
        Assert.Equal(1, defaults[0xc7 - 16]);
    }

    [Fact]
    public void FactoryYearIsCoherentWithTheClockAndMaintainsTheCmosChecksum()
    {
        var time = new DateTime(2026, 11, 2);
        var defaults = ArchimedesCmosConstants.Create(ArchimedesCmosProfile.RiscOs3, time);
        var rtc = new Pcf8583(1000, time, initialRam: defaults);
        Assert.Equal(new byte[] { 26, 20 }, Read(rtc, 0xc0, 2));
        Assert.Equal(2, (Read(rtc, 5, 1)[0] >> 6) & 3);
        // Image de 1992 : &6D ; retirer 92+19 et ajouter 26+20.
        Assert.Equal(0x2c, Read(rtc, 0x3f, 1)[0]);
    }

    private static Pcf8583 Create() => new(1000, new DateTime(2024, 1, 1));
    private static void Start(Pcf8583 rtc)
    {
        rtc.WriteLines(false, true);
        rtc.WriteLines(true, true);
        rtc.WriteLines(true, false);
        rtc.WriteLines(false, false);
    }
    private static void Stop(Pcf8583 rtc)
    {
        rtc.WriteLines(false, false);
        rtc.WriteLines(true, false);
        rtc.WriteLines(true, true);
    }
    private static bool Send(Pcf8583 rtc, byte value)
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            var high = (value & (1 << bit)) != 0;
            rtc.WriteLines(false, high);
            rtc.WriteLines(true, high);
            rtc.WriteLines(false, high);
        }
        rtc.WriteLines(false, true);
        rtc.WriteLines(true, true);
        var acknowledged = !rtc.Sda;
        rtc.WriteLines(false, true);
        return acknowledged;
    }
    private static void Write(Pcf8583 rtc, byte pointer, params byte[] bytes)
    {
        Start(rtc);
        Assert.True(Send(rtc, 0xa0));
        Assert.True(Send(rtc, pointer));
        foreach (var value in bytes) Assert.True(Send(rtc, value));
        Stop(rtc);
    }
    private static byte[] Read(Pcf8583 rtc, byte pointer, int count, Action<int>? afterByte = null)
    {
        Start(rtc);
        Assert.True(Send(rtc, 0xa0));
        Assert.True(Send(rtc, pointer));
        Start(rtc);
        Assert.True(Send(rtc, 0xa1));
        var bytes = new byte[count];
        for (var index = 0; index < count; index++)
        {
            for (var bit = 0; bit < 8; bit++)
            {
                rtc.WriteLines(true, true);
                bytes[index] = (byte)((bytes[index] << 1) | (rtc.Sda ? 1 : 0));
                rtc.WriteLines(false, true);
            }
            var nack = index == count - 1;
            afterByte?.Invoke(index);
            rtc.WriteLines(false, nack);
            rtc.WriteLines(true, nack);
            rtc.WriteLines(false, nack);
        }
        Stop(rtc);
        return bytes;
    }
}
