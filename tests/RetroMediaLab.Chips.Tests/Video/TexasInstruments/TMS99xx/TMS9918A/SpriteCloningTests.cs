using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Circuit = RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A.VramIntegrationTests.Circuit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class SpriteCloningTests
{
    [Fact]
    public void HardwareCoordinateExamplesIncludeColourMasksBoundaryCutsAndFirstEightExemption()
    {
        // Vecteurs des blocs décrits par hap (2008/2012), pas une comparaison
        // avec un autre émulateur. Le déplacement est avant magnification.
        (byte R3, byte R4, byte Y, int Line, int Height, int Offset)[] cases =
        [
            (0xff, 0, 2, 3, 8, 0), (0xff, 0, 2, 67, 8, 0), (0xff, 0, 2, 131, 8, 0),
            (0xff, 1, 2, 67, 8, -1), (0xff, 1, 2, 131, 8, 0),
            (0xff, 2, 2, 67, 8, 0), (0xff, 2, 2, 131, 8, -1),
            (0xff, 0, 66, 67, 16, 0), (0xff, 0, 130, 131, 16, 0),
            (0x9f, 0, 130, 131, 16, -1),
            (0xff, 0, 62, 65, 16, 2), (0xdf, 0, 62, 65, 16, -1),
            (0xff, 0, 126, 129, 16, -1), // bas du bloc1 toujours tronqué en bloc2
            (0xff, 0, 63, 0, 16, 0), (0x9f, 0, 63, 0, 16, 0),
            (0xff, 0, 127, 0, 16, 0), (0xff, 0, 191, 0, 16, 0),
            (0xff, 0, 63, 128, 16, 0),
            (0xff, 0, 62, 70, 8, 7), (0xff, 0, 62, 71, 16, 8),
            (0xdf, 0, 62, 71, 16, -1),
        ];
        var rows = new Tms9918SpriteRows();
        foreach (var c in cases)
        {
            rows.Configure(2, c.R3, c.R4);
            foreach (int sprite in new[] { 8, 15, 31 })
            {
                int actual = rows.Offset(sprite, c.Line, c.Y, c.Height);
                Assert.Equal(c.Offset, actual >= 0 && actual < c.Height ? actual : -1);
            }
            for (int sprite = 0; sprite < 8; sprite++)
                Assert.Equal(c.Line - c.Y - 1, rows.Offset(sprite, c.Line, c.Y, c.Height));
        }

        // Tous les Y/tailles restent ordinaires sans M3 ou avec R4=3.
        foreach (byte r0 in new byte[] { 0, 2 })
        {
            rows.Configure(r0, 0xff, r0 == 0 ? (byte)0 : (byte)3);
            foreach (int height in new[] { 8, 16, 32 })
            for (int line = 0; line < 192; line++)
            for (int y = 0; y < 256; y++)
                Assert.Equal(line - ((y >= 0xe1 ? y - 256 : y) + 1),
                    rows.Offset(31, line, (byte)y, height));
        }
        rows.Configure(2, 0xff, 0);
        Assert.Equal(0, rows.Offset(8, 131, 2, 8));
        rows.Configure(0, 0xff, 0); // invalidation réelle, pas de masque périmé
        Assert.Equal(128, rows.Offset(8, 131, 2, 8));
    }

    [Fact]
    public void PhysicalHit9918ProgramKeepsSpriteZeroAndClonesOrErasesSpriteEight()
    {
        foreach (byte y in new byte[] { 2, 66, 130 })
        {
            using var circuit = Create(0x9f, 0, 0xc2);
            Put(circuit, 0, y, 0, 8);
            Put(circuit, 8, y, 128, 15);
            var pixels = Capture(circuit, [3, 67, 131]);
            try
            {
                circuit.RunTo(8_500_000);
                foreach (int line in new[] { 3, 67, 131 })
                {
                    Assert.Equal(line == y + 1 ? 8 : 3, pixels[(line, 0)].Colour);
                    bool clone = y == 2 || y == 66 && line == 67;
                    Assert.Equal(clone ? 15 : 3, pixels[(line, 128)].Colour);
                    Assert.True(pixels[(line, 128)].ColourKnown);
                }
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }

    [Fact]
    public void PatternRowsAndMagnificationComeFromSelectedCloneAndColourMasksCutItsTail()
    {
        foreach (bool magnified in new[] { false, true })
        foreach (byte r3 in new byte[] { 0xff, 0xdf })
        {
            using var circuit = Create(r3, 0, (byte)(0xc2 | (magnified ? 1 : 0)));
            Put(circuit, 8, 62, 16, 12);
            Array.Clear(circuit.Memory, 0x3800, 32);
            // Ligne65 : déplacement2, donc ligne2 ou1. Motif asymétrique
            // et quadrant droit vérifient aussi la génération d'adresse.
            int row = magnified ? 1 : 2;
            circuit.Memory[0x3800 + row] = 0x80;
            circuit.Memory[0x3810 + row] = 1;
            var pixels = Capture(circuit, [65]);
            try
            {
                circuit.RunTo(4_300_000);
                int scale = magnified ? 2 : 1;
                Assert.Equal(r3 == 0xff ? 12 : 3, pixels[(65, 16)].Colour);
                Assert.Equal(r3 == 0xff ? 12 : 3, pixels[(65, 16 + 15 * scale)].Colour);
                Assert.Equal(3, pixels[(65, 16 + scale)].Colour);
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }

    [Fact]
    public void ClonesUseExistingFourChannelsPriorityCoincidenceFifthAndTerminator()
    {
        using var circuit = Create(0xff, 0);
        for (int sprite = 8; sprite <= 12; sprite++)
            Put(circuit, sprite, 2, 16, (byte)(sprite == 8 ? 0 : sprite - 4));
        var pixels = Capture(circuit, [67]);
        try
        {
            circuit.RunTo(4_400_000);
            Assert.Equal(5, pixels[(67, 16)].Colour); // premier canal transparent
            Assert.Equal(0x6c, circuit.Read(true) & 0x7f); // C,5S, numéro12
            var state = circuit.Chip.CaptureSpriteState();
            Assert.Equal(new[] { 8, 9, 10, 11, 12 }, state.SelectedNumbers);
            circuit.Memory[0x3f00 + 8 * 4] = 0xd0;
            circuit.RunTo(circuit.Time + 250_000);
            circuit.Read(true);
            circuit.RunTo(circuit.Time + 250_000);
            Assert.Equal(0, circuit.Read(true) & 0x60);
        }
        finally { circuit.Chip.VideoSignalProduced = null; }
    }

    [Fact]
    public void PreRenderedTopLineHasClonesWithoutAddingChannelsOrLosingNormalFirstSprites()
    {
        using var circuit = Create(0xff, 0, 0xc2);
        Put(circuit, 0, 0xff, 0, 8);
        Put(circuit, 8, 63, 128, 15);
        var pixels = new Dictionary<(int Line, int X), Tms9918VideoSignal>();
        try
        {
            circuit.Chip.VideoSignalProduced = signal =>
            {
                if (signal.Frame == 1 && signal.Line <= 1 && signal.Active)
                    pixels[(signal.Line, signal.Dot)] = signal;
            };
            circuit.RunTo(16_900_000);
            Assert.Equal(512, pixels.Count);
            foreach (int line in new[] { 0, 1 })
            {
                Assert.Equal(8, pixels[(line, 0)].Colour);
                Assert.Equal(line == 0 ? 15 : 3, pixels[(line, 128)].Colour);
                Assert.True(pixels[(line, 128)].ColourKnown);
            }
        }
        finally { circuit.Chip.VideoSignalProduced = null; }
    }

    private static Circuit Create(byte r3, byte r4, byte r1 = 0xc0)
    {
        var circuit = new Circuit(new(2, r1, 6, r3, r4, 0x7e, 7, 3));
        // D1 est hors image sans terminer la liste avant le sprite8.
        for (int sprite = 0; sprite < 32; sprite++) circuit.Memory[0x3f00 + sprite * 4] = 0xd1;
        Array.Fill(circuit.Memory, (byte)0xff, 0x3800, 32);
        return circuit;
    }

    private static void Put(Circuit circuit, int index, byte y, byte x, byte colour)
    {
        int address = 0x3f00 + index * 4;
        circuit.Memory[address] = y; circuit.Memory[address + 1] = x;
        circuit.Memory[address + 2] = 0; circuit.Memory[address + 3] = colour;
    }

    private static Dictionary<(int Line, int X), Tms9918VideoSignal> Capture(Circuit circuit, int[] lines)
    {
        var pixels = new Dictionary<(int, int), Tms9918VideoSignal>();
        circuit.Chip.VideoSignalProduced = signal =>
        {
            if (signal.Active && lines.Contains(signal.Line)) pixels[(signal.Line, signal.Dot)] = signal;
        };
        return pixels;
    }
}
