using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    [InlineData(0, 260)]
    [InlineData(1, 65535)]
    public void ReadPolicyCanBeSetWithoutADriveAndOnlyRequestsFourBytes(int unit, ushort declaredLength)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var result = Run(target, DiagnosticCommand(unit, declaredLength), [0x65, 0xff, 2, 0xff]);
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DiagnosticRejectsTruncatedDeclaredListsBeforeAskingForData(ushort length)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(2, Run(target, DiagnosticCommand(0, length)).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void DiagnosticRejectsReservedCdbFields(int offset)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var command = DiagnosticCommand(0, 4);
        command[offset] = 1;
        Assert.Equal(2, Run(target, command).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(0, true, 0)]
    [InlineData(1, false, 2)]
    [InlineData(1, true, 2)]
    [InlineData(2, false, 2)]
    [InlineData(2, true, 2)]
    public void DiagnosticPoliciesControlActualEccReads(byte mode, bool extended, byte expectedStatus)
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, mode);
        SendCommand(target, extended ? Long(0x28, address: 2, count: 1) : Short(8, address: 2, count: 1));
        var payload = Enumerable.Range(0, 256).Select(i => (byte)(i * 31 + 5)).ToArray();
        var damaged = PhysicalData(payload);
        damaged[2] ^= 0x80;
        var attempts = mode == 1 ? 1 : 6;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            PhysicalPair(target, PhysicalId(2), damaged);
            if (attempt + 1 < attempts) Assert.True(target.ReadGate);
        }
        var data = DrainRead(target, out var status);
        Assert.Equal(expectedStatus, status);
        if (mode == 1) Assert.Empty(data);
        else Assert.Equal(payload, data);
        var sense = Run(target, Short(3, count: 4)).Data;
        Assert.Equal(expectedStatus == 0 ? new byte[4] : new byte[] { 0x98, 0, 0, 2 }, sense);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CleanReadDoesNotReportAnErrorInAnyPolicy(byte mode)
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, mode);
        SendCommand(target, Short(8, count: 1));
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.Equal(new byte[256], DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    [Fact]
    public void SuccessfulRereadIsNotReportedAsACorrection()
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, 2);
        SendCommand(target, Short(8, count: 1));
        var damaged = PhysicalData(new byte[256]);
        damaged[2] ^= 0x80;
        PhysicalPair(target, PhysicalId(0), damaged);
        Assert.True(target.ReadGate);
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.Equal(new byte[256], DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    [Fact]
    public void ReportingModeStillSendsAnUncorrectableBlockBeforeSense91()
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, 2);
        SendCommand(target, Short(8, count: 2));
        var damaged = PhysicalData(new byte[256]);
        damaged[2] ^= 0x80;
        damaged[7] ^= 0x80;
        for (var attempt = 0; attempt < 6; attempt++) PhysicalPair(target, PhysicalId(0), damaged);
        Assert.Equal(damaged.AsSpan(2, 256).ToArray(), DrainRead(target, out var status));
        Assert.Equal(2, status);
        Assert.Equal(new byte[] { 0x91, 0, 0, 0 }, Run(target, Short(3, count: 4)).Data);
    }

    [Fact]
    public void PoliciesAreIndependentPerUnitAndBusResetPreservesBoth()
    {
        var disk = new Disk();
        var target = CreateManualTarget(disk);
        SetReadPolicy(target, 1, unit: 0);
        SetReadPolicy(target, 2, unit: 1);
        for (var unit = 0; unit < 2; unit++)
        {
            SendCommand(target, Short(8, unit, count: 1));
            var damaged = PhysicalData(new byte[256]);
            damaged[2] ^= 0x80;
            for (var attempt = 0; attempt < (unit == 0 ? 1 : 6); attempt++)
                PhysicalPair(target, PhysicalId(0), damaged);
            var data = DrainRead(target, out var status);
            Assert.Equal(unit == 0 ? 0 : 256, data.Length);
            Assert.Equal(2, status);
        }
        target.Reset();
        for (var unit = 0; unit < 2; unit++)
        {
            SendCommand(target, Short(8, unit, count: 1));
            var damaged = PhysicalData(new byte[256]);
            damaged[2] ^= 0x80;
            for (var attempt = 0; attempt < (unit == 0 ? 1 : 6); attempt++) PhysicalPair(target, PhysicalId(0), damaged);
            var data = DrainRead(target, out var status);
            Assert.Equal(unit == 0 ? 0 : 256, data.Length);
            Assert.Equal(2, status);
        }
    }

    [Theory]
    [InlineData(0x65, 3)]
    [InlineData(0x65, 255)]
    [InlineData(0xff, 0)]
    public void InvalidPolicyListDoesNotReplaceThePreviousPolicy(byte option, byte mode)
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, 1);
        Assert.Equal(2, Run(target, DiagnosticCommand(0, 4), [option, 0, mode, 0]).Status);
        SendCommand(target, Short(8, count: 1));
        var damaged = PhysicalData(new byte[256]);
        damaged[2] ^= 0x80;
        PhysicalPair(target, PhysicalId(0), damaged);
        Assert.Empty(DrainRead(target, out var status));
        Assert.Equal(2, status);
        Assert.Equal(0x98, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ReadPolicyDoesNotChangeVerifyEccError19(byte mode)
    {
        var target = CreateManualTarget();
        SetReadPolicy(target, mode);
        SendCommand(target, Long(0x2f, count: 1));
        target.Tick();
        var damaged = PhysicalData(new byte[256]);
        damaged[2] ^= 0x80;
        PhysicalPair(target, PhysicalId(0), damaged);
        Assert.Empty(DrainRead(target, out var status));
        Assert.Equal(2, status);
        Assert.Equal(0x99, Run(target, Short(3, count: 4)).Data[0]);
    }

    private static byte[] DiagnosticCommand(int unit, ushort length)
    {
        var command = Short(0x1d, unit);
        command[3] = (byte)(length >> 8);
        command[4] = (byte)length;
        return command;
    }

    private static void SetReadPolicy(Acb4000 target, byte mode, int unit = 0) =>
        Assert.Equal(0, Run(target, DiagnosticCommand(unit, 4), [0x65, 0, mode, 0]).Status);
}
