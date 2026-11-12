using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using Xunit;
using Circuit = RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A.VramIntegrationTests.Circuit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class SpriteIntegrationTests
{
    [Fact]
    public void LiveCounterStopsAtTerminatorAndReappearsAfterOverflowIsAcknowledgedInVblank()
    {
        foreach (int terminator in new[] { 16, 32 })
        {
            using var circuit = Create();
            for (int sprite = 0; sprite < 32; sprite++) Sprite(circuit, sprite, 0xd1, 0, 0, 0);
            if (terminator < 32) circuit.Memory[0x3f00 + terminator * 4] = 0xd0;
            circuit.RunTo(13_000_000); // vblank, le compteur n'avance plus
            int expected = Math.Min(terminator, 31);
            Assert.Equal(0x80 | expected, circuit.Read(true));
            Assert.Equal(expected, circuit.Read(true));
        }

        using var overflow = Create();
        for (int sprite = 0; sprite < 5; sprite++) Sprite(overflow, sprite, 0xff, (byte)(sprite * 16), 2, 5);
        overflow.Memory[0x3f14] = 0xd0;
        for (int row = 0; row < 8; row++) overflow.Memory[0x3810 + row] = 0x80;
        overflow.RunTo(255_000);
        Assert.Equal(0x44, overflow.Chip.CaptureState().Status);
        // Un scan ultérieur s'arrête à16, sans effacer le numéro5S verrouillé4.
        for (int sprite = 0; sprite < 16; sprite++) Sprite(overflow, sprite, 0xd1, 0, 0, 0);
        overflow.Memory[0x3f40] = 0xd0;
        overflow.RunTo(13_000_000);
        var snapshot = overflow.Chip.CaptureState();
        Assert.Equal(16, snapshot.SpriteCounter);
        Assert.Equal(0xc4, snapshot.Status);
        var restored = new RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services.Tms9918A(
            new(default, 0, default, default, new(0, true, true, true, true)),
            RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918CpuTiming.SpecifiedMaximum);
        try
        {
            restored.RestoreState(System.Text.Json.JsonSerializer.Deserialize<Tms9918AState>(
                System.Text.Json.JsonSerializer.Serialize(snapshot))!);
            Assert.Equal(snapshot.SpriteCounter, restored.CaptureState().SpriteCounter);
            Assert.Equal(snapshot.Status, restored.CaptureState().Status);
            Assert.Equal(0xc4, overflow.Read(true));
            Assert.Equal(16, overflow.Read(true));
            // Reprise de la même fin de strobe sur l'état restauré, sans horloge
            // nécessaire en vblank : le compteur actif ne vaut plus le verrou4.
            var pins = snapshot.CpuInterface.Pins with { Mode = true, CsrHigh = false };
            restored.ReceiveCpuPins(snapshot.TimeNanoseconds + 2000, pins);
            restored.AdvanceTo(snapshot.TimeNanoseconds + 2200);
            Assert.Equal(0xc4, restored.CpuOutputs.Data);
            restored.ReceiveCpuPins(snapshot.TimeNanoseconds + 2300, pins with { CsrHigh = true });
            Assert.Equal(16, restored.CaptureState().Status);
        }
        finally { restored.CpuOutputsChanged = null; restored.NextEventNanosecondsChanged = null; }
    }

    [Fact]
    public void TerminatorIgnoresAnUnknownLaterYAlreadyInFlight()
    {
        using var circuit = Create();
        circuit.UnknownAddresses.Add(0x3f04);
        var pixels = Pixels(circuit);
        try
        {
            circuit.RunTo(255_000);
            Assert.Equal(256, pixels.Count);
            Assert.All(pixels, pixel => Assert.True(pixel.ColourKnown));
            var state = circuit.Chip.CaptureSpriteState();
            Assert.True(state.ScanKnown);
            Assert.True(state.ActiveKnown);
            Assert.All(state.Active, sprite => Assert.False(sprite.Selected));
        }
        finally { circuit.Chip.VideoSignalProduced = null; }
    }

    private static Circuit Create(byte r1 = 0xc0)
    {
        var circuit = new Circuit(new(0, r1, 6, 0x80, 0, 0x7e, 7, 3));
        circuit.Memory[0x3f00] = 0xd0;
        return circuit;
    }

    private static void Sprite(Circuit circuit, int index, byte y, byte x, byte name, byte colour)
    {
        int address = 0x3f00 + index * 4;
        circuit.Memory[address] = y; circuit.Memory[address + 1] = x;
        circuit.Memory[address + 2] = name; circuit.Memory[address + 3] = colour;
    }

    private static List<Tms9918VideoSignal> Pixels(Circuit circuit, int line = 3)
    {
        var result = new List<Tms9918VideoSignal>();
        circuit.Chip.VideoSignalProduced = signal =>
        {
            if (signal.Line == line && signal.Active) result.Add(signal);
        };
        return result;
    }

    [Fact]
    public void FourChannelsKeepPriorityTransparentCoincidenceFifthAndTerminator()
    {
        using var circuit = Create();
        for (int index = 0; index < 5; index++)
        {
            Sprite(circuit, index, 0xff, 16, 2, (byte)(index == 0 ? 0 : index + 4));
        }
        circuit.Memory[0x3f00 + 5 * 4] = 0xd0;
        for (int row = 0; row < 8; row++) circuit.Memory[0x3800 + 2 * 8 + row] = 0x80;
        var pixels = Pixels(circuit);
        try
        {
            circuit.RunTo(255_000);
            Assert.Equal(256, pixels.Count);
            Assert.Equal(5, pixels[16].Colour); // canal 0 transparent, canal 1 visible
            Assert.Equal(3, pixels[17].Colour);
            Assert.True(pixels[16].ColourKnown);
            byte status = circuit.Read(true);
            Assert.Equal(0x64, status & 0x7f); // C, 5S, cinquième numéro 4
            Assert.Equal(0, circuit.Chip.CaptureSpriteState().Active[0].Right); // pas de deuxième octet en 8×8
            var state = circuit.Chip.CaptureSpriteState();
            Assert.Equal(5, state.SelectedNumbers.Length);
            state.SelectedNumbers[0] = 31;
            Assert.NotEqual(31, circuit.Chip.CaptureSpriteState().SelectedNumbers[0]);

            // Le terminateur interdit les sprites ultérieurs, même s'ils se superposent.
            circuit.Memory[0x3f00] = 0xd0;
            circuit.RunTo(circuit.Time + 250_000);
            circuit.Read(true); // acquitter les flags déjà latents des dernières lignes
            circuit.RunTo(circuit.Time + 250_000);
            Assert.Equal(0, circuit.Read(true) & 0x60);
        }
        finally { circuit.Chip.VideoSignalProduced = null; }
    }

    [Fact]
    public void SizeMagnificationQuadrantsAndPartiallySignedYComeFromFetchedPatternRows()
    {
        foreach (bool magnified in new[] { false, true })
        {
            using var circuit = Create((byte)(0xc2 | (magnified ? 1 : 0)));
            // F9=-7 ; le sprite commence à -6. Ligne 3 : range 9,
            // donc ligne 9 en normal (bas), ligne 4 en magnifié (haut).
            Sprite(circuit, 0, 0xf9, 32, 7, 12); // nom arrondi à 4 pour SIZE=1
            circuit.Memory[0x3f04] = 0xd0;
            int row = magnified ? 4 : 9;
            circuit.Memory[0x3800 + 4 * 8 + row] = 0x80;
            circuit.Memory[0x3800 + 4 * 8 + 16 + row] = 0x01;
            var pixels = Pixels(circuit);
            try
            {
                circuit.RunTo(255_000);
                int scale = magnified ? 2 : 1;
                Assert.Equal(12, pixels[32].Colour);
                Assert.Equal(12, pixels[32 + 15 * scale].Colour);
                Assert.Equal(3, pixels[32 + scale].Colour);
                if (magnified)
                {
                    Assert.Equal(12, pixels[33].Colour);
                    Assert.Equal(12, pixels[63].Colour);
                }
                Assert.All(pixels, pixel => Assert.True(pixel.ColourKnown));
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }

    [Fact]
    public void EarlyClockAndRightOverflowCollideOutsidePictureAndTextSuppressesSprites()
    {
        foreach (byte x in new byte[] { 16, 255 })
        {
            using var circuit = Create(0xc1); // 8×8, magnifié
            byte tag = (byte)(x == 16 ? 0x80 : 0); // couleur transparente, collision quand même
            Sprite(circuit, 0, 0xff, x, 2, tag);
            Sprite(circuit, 1, 0xff, x, 2, tag);
            circuit.Memory[0x3f08] = 0xd0;
            for (int row = 0; row < 8; row++) circuit.Memory[0x3800 + 16 + row] = 0x01;
            var pixels = Pixels(circuit);
            try
            {
                circuit.RunTo(255_000);
                Assert.All(pixels, pixel => Assert.Equal(3, pixel.Colour));
                Assert.Equal(0x20, circuit.Read(true) & 0x20);
                // Text ne doit ni afficher ces sprites ni regénérer C/5S.
                circuit.Write(0xd1, true); circuit.Write(0x81, true);
                circuit.Read(true);
                circuit.RunTo(circuit.Time + 250_000);
                Assert.Equal(0, circuit.Read(true) & 0x60);
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }
}
