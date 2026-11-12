using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

/// <summary>Scénarios numériques TI MP010A §2.1–2.3. Aucun oracle de balayage
/// ou de délais de broches n'est déduit de ce groupe.</summary>
public sealed class CpuDataPathTests
{
    [Fact]
    public void NominalOverrunsReuseOneRegisterAndRequestWithoutCertifyingConflictingResults()
    {
        foreach (var original in new[] { Tms9918CpuVramOperation.Read, Tms9918CpuVramOperation.Write })
        foreach (bool finishRead in new[] { false, true })
        {
            var port = new Tms9918CpuMemoryPort(new(0x1234, 0x11, original));
            for (int value = 0; value < 256; value++) port.DataWriteAccepted((byte)value);
            Assert.Equal(255, port.Data);
            Assert.False(port.CurrentRequest.Qualified);
            Assert.Equal(Tms9918CpuVramOperation.Write, port.CurrentRequest.Operation);
            port.DataReadAccepted(); // le registre commun conserve le dernier octet capturé
            Assert.Equal(255, port.Data);
            var resumed = new Tms9918CpuMemoryPort(port.Capture());
            foreach (var target in new[] { port, resumed })
            {
                if (finishRead) target.CompleteRead(0x66); else target.CompleteWrite();
                Assert.False(target.HasPendingRequest);
                Assert.False(target.DataKnown);
                Assert.Null(target.Capture().Address);
                Assert.Throws<InvalidOperationException>(() => target.CompleteWrite());
                target.SetAddress(0x3fff, false);
                target.DataWriteAccepted(0xa5);
                Assert.True(target.CurrentRequest.Qualified);
                target.CompleteWrite();
                Assert.True(target.DataKnown);
                Assert.Equal((ushort?)0, target.Capture().Address);
            }
            Assert.Equal(port.Capture(), resumed.Capture());
        }
    }

    [Fact]
    public void PhysicalWriteStrobesDecodeCommandsAndResumeWithTheModeCapturedAtCswLow()
    {
        var pins = new Tms9918CpuPins(0, true, false, true, true);
        var bus = new Tms9918CpuWriteBus(pins, 0);
        var protocol = new Tms9918CpuProtocol(new(0, false));
        var registers = new Tms9918Registers(new(0, 0, 0, 0, 0, 0, 0, 0));
        var writes = new List<Tms9918CpuWrite>();
        Tms9918CpuWriteBus? restoredBus = null;
        try
        {
            bus.WriteCaptured = capture =>
            {
                writes.Add(capture); Apply(capture, protocol, registers);
                // Retour combinatoire inchangé accepté à la même date ; une
                // avance récursive ne doit pas déplacer la capture matérielle.
                bus.ReceivePins(capture.TimeNanoseconds, bus.Capture().Pins);
                Assert.Throws<InvalidOperationException>(() => bus.ReceivePins(capture.TimeNanoseconds + 1, bus.Capture().Pins));
            };
            pins = pins with { Data = 0x60, Mode = true };
            bus.ReceivePins(1000, pins);
            pins = pins with { CswHigh = false }; bus.ReceivePins(1040, pins);
            pins = pins with { CswHigh = true }; bus.ReceivePins(1240, pins);
            Assert.Equal((byte)0, registers.Capture().R1);
            pins = pins with { Data = 0x81 }; bus.ReceivePins(4000, pins);
            pins = pins with { CswHigh = false }; bus.ReceivePins(4040, pins);
            pins = pins with { Mode = false }; bus.ReceivePins(4100, pins); // maintien MODE >30 ns

            restoredBus = new Tms9918CpuWriteBus(bus.Capture());
            var restoredProtocol = new Tms9918CpuProtocol(protocol.Capture());
            var restoredRegisters = new Tms9918Registers(registers.Capture());
            Tms9918CpuWrite? resumedWrite = null;
            restoredBus.WriteCaptured = capture =>
            { resumedWrite = capture; Apply(capture, restoredProtocol, restoredRegisters); };

            pins = pins with { CswHigh = true };
            bus.ReceivePins(4240, pins); restoredBus.ReceivePins(4240, pins);
            Assert.Equal(registers.Capture(), restoredRegisters.Capture());
            Assert.Equal(bus.Capture(), restoredBus.Capture());
            Assert.Equal((byte)0x60, registers.Capture().R1);
            Assert.True(writes[1].Mode); Assert.Equal(writes[1], resumedWrite);
            bus.ReceivePins(4240, pins); bus.ReceivePins(4300, pins);
            Assert.Equal(2, writes.Count); Assert.All(writes, capture => Assert.True(capture.Qualified));
            Assert.False(bus.Capture().QualificationLost);

            // Autre instance : une donnée changée 10 ns après capture viole
            // le maintien 30 ns. C'est une perte de qualification, pas une donnée
            // matérielle corrigée rétroactivement par le banc.
            var violation = new Tms9918CpuWriteBus(restoredBus.Capture());
            violation.ReceivePins(4250, pins with { Data = 0x82 });
            Assert.True(violation.Capture().QualificationLost);
        }
        finally
        {
            bus.WriteCaptured = null;
            if (restoredBus is not null) restoredBus.WriteCaptured = null;
        }

        static void Apply(Tms9918CpuWrite capture, Tms9918CpuProtocol protocol, Tms9918Registers registers)
        {
            Assert.True(capture.Qualified); Assert.True(capture.Mode);
            var command = protocol.AcceptControlByte(capture.Data);
            if (command.Kind == Tms9918ControlCommandKind.WriteRegister)
                registers.Write(command.Register, command.FirstByte);
        }
    }

    [Fact]
    public void RegisterCommandsStatusInterruptAndResetKeepTheirDistinctEffects()
    {
        var registers = new Tms9918Registers(new(0, 0, 0, 0, 0, 0, 0, 0));
        var protocol = new Tms9918CpuProtocol(new(0x79, false));
        var status = new Tms9918Status(0x1f);
        byte[] values = [2, 0xe3, 15, 0xff, 7, 0x7f, 5, 0xf1];
        for (int register = 0; register < values.Length; register++)
            Write(register, values[register]);
        Assert.Equal(new Tms9918RegisterState(2, 0xe3, 15, 0xff, 7, 0x7f, 5, 0xf1), registers.Capture());
        Assert.Equal(Tms9918DisplayMode.GraphicsII, registers.Mode);
        Assert.True(registers.DisplayEnabled); Assert.True(registers.InterruptEnabled);
        Assert.True(registers.LargeSprites); Assert.True(registers.MagnifiedSprites);
        Assert.Equal(0x1fff, registers.GraphicsIIPatternMask);
        Assert.Equal(0x1fff, registers.GraphicsIIColorMask);
        Assert.Equal(0x3c00, registers.NameBase); Assert.Equal(0x3fc0, registers.ColorBase);
        Assert.Equal(0x3800, registers.PatternBase); Assert.Equal(0x3f80, registers.SpriteAttributeBase);
        Assert.Equal(0x2800, registers.SpritePatternBase);
        Assert.Equal(0x2000, registers.GraphicsIIPatternBase); Assert.Equal(0x2000, registers.GraphicsIIColorBase);
        Assert.Equal(15, registers.TextColor); Assert.Equal(1, registers.BackdropColor);

        status.FifthSpriteDetected(9); status.SpriteCoincidence(); status.EndOfActiveDisplay();
        Assert.Equal(0xe9, status.Value); Assert.True(status.InterruptAsserted(registers.InterruptEnabled));
        status.FifthSpriteDetected(17); Assert.Equal(0xe9, status.Value);
        Write(1, 0xc3); Assert.False(status.InterruptAsserted(registers.InterruptEnabled));
        Assert.Equal(0xe9, status.Value); // IE ne remet pas F à zéro.
        Write(1, 0xe3); Assert.True(status.InterruptAsserted(registers.InterruptEnabled));

        protocol.AcceptControlByte(0x55); // transaction interrompue avant son second octet
        Assert.Equal(0xe9, status.ReadAccepted()); protocol.StatusReadAccepted();
        Assert.Equal(9, status.Value); Assert.False(status.HasValidFifthSpriteNumber);
        Assert.False(status.InterruptAsserted(registers.InterruptEnabled));
        Write(7, 0xa4); // doit être un nouveau couple, pas la suite de 0x55
        Assert.Equal(10, registers.TextColor); Assert.Equal(4, registers.BackdropColor);

        var before = registers.Capture(); registers.ApplyReset(); status.ClearFlags();
        Assert.Equal(before with { R0 = 0, R1 = 0 }, registers.Capture());
        Assert.False(registers.DisplayEnabled); Assert.False(registers.InterruptEnabled);
        Assert.Equal(Tms9918DisplayMode.GraphicsI, registers.Mode);
        Assert.Equal(9, status.Value); // numéro invalide conservé, pas une garantie de zéro

        void Write(int register, byte value)
        {
            Assert.Equal(Tms9918ControlCommandKind.FirstByteLatched, protocol.AcceptControlByte(value).Kind);
            var command = protocol.AcceptControlByte((byte)(0x80 | register));
            Assert.True(command.IsDocumentedFormat);
            Assert.Equal(Tms9918ControlCommandKind.WriteRegister, command.Kind);
            registers.Write(command.Register, command.FirstByte);
        }
    }

    [Fact]
    public void ExternalVramServiceControlsPrefetchAndAddressWrapWithoutAnInstantRead()
    {
        var protocol = new Tms9918CpuProtocol(new(0, false));
        var port = new Tms9918CpuMemoryPort(new(null, 0x93, Tms9918CpuVramOperation.None));
        var memory = new byte[0x4000];
        SetAddress(0x3ffe, false);
        for (int i = 0; i < 3; i++)
        {
            port.DataWriteAccepted((byte)(0xa0 + i));
            var request = port.CurrentRequest;
            Assert.Equal((ushort)((0x3ffe + i) & 0x3fff), request.Address);
            Assert.Equal(Tms9918CpuVramOperation.Write, request.Operation);
            Assert.Equal((byte)0, memory[request.Address!.Value]); // rien n'a été écrit sans service
            memory[request.Address.Value] = request.Data;
            port.CompleteWrite();
        }
        Assert.Equal((ushort)1, port.Capture().Address);
        SetAddress(0x3ffe, true);
        Assert.Equal((byte)0xa2, port.Data); Assert.Equal((ushort)0x3ffe, port.Capture().Address);
        ServiceRead();
        Assert.Equal((ushort)0x3fff, port.Capture().Address);
        Assert.Equal((byte)0xa0, port.DataReadAccepted());
        memory[0x3fff] = 0xb4; // changer la mémoire avant le service doit changer le résultat
        Assert.Equal((byte)0xa0, port.Data); ServiceRead();
        Assert.Equal((ushort)0, port.Capture().Address);
        Assert.Equal((byte)0xb4, port.DataReadAccepted()); ServiceRead();
        Assert.Equal((byte)0xa2, port.DataReadAccepted()); ServiceRead();
        port.RegisterWriteAccepted(); Assert.Null(port.Capture().Address);
        Assert.Equal((byte)0, port.Data); // dernier octet réellement préchargé à l'adresse 1

        void SetAddress(ushort address, bool read)
        {
            protocol.AcceptControlByte((byte)address);
            var command = protocol.AcceptControlByte((byte)((address >> 8) | (read ? 0 : 0x40)));
            Assert.Equal(read ? Tms9918ControlCommandKind.SetReadAddress
                : Tms9918ControlCommandKind.SetWriteAddress, command.Kind);
            port.SetAddress(command.Address, read);
        }
        void ServiceRead()
        {
            var request = port.CurrentRequest;
            Assert.Equal(Tms9918CpuVramOperation.Read, request.Operation);
            port.CompleteRead(memory[request.Address!.Value]);
        }
    }

    [Fact]
    public void LatchAndPendingMemoryStatesResumeWithoutDuplicatingTheDataRegister()
    {
        var protocol = new Tms9918CpuProtocol(new(0x34, true));
        var restoredProtocol = new Tms9918CpuProtocol(protocol.Capture());
        var command = restoredProtocol.AcceptControlByte(0x52);
        Assert.Equal(Tms9918ControlCommandKind.SetWriteAddress, command.Kind);
        Assert.Equal((ushort)0x1234, command.Address);

        var pending = new Tms9918CpuMemoryState(0x3fff, 0x29, Tms9918CpuVramOperation.Read);
        var original = new Tms9918CpuMemoryPort(pending);
        var restored = new Tms9918CpuMemoryPort(original.Capture());
        original.CompleteRead(0xc7); restored.CompleteRead(0xc7);
        Assert.Equal(original.Capture(), restored.Capture());
        Assert.Equal((ushort)0, restored.Capture().Address);
        restored.DataWriteAccepted(0x5e);
        Assert.Equal((byte)0x5e, restored.Data); // même registre, pas un tampon d'écriture ajouté
        Assert.Equal(0x5e, restored.DataReadAccepted()); // même registre malgré le croisement
        Assert.Equal(0x5e, restored.Data);
        Assert.Equal(Tms9918CpuVramOperation.Read, restored.Capture().PendingOperation);
        Assert.False(restored.CurrentRequest.Qualified);
    }
}
