// Copyright (c) 2018 Renaud Toumazet
// Translated from Saturnin interrupt_sources.h.
// SPDX-License-Identifier: Apache-2.0

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnInterruptSources
{
    public const byte VectorNmi = 0xB;
    public const byte VectorUserBreak = 0xC;
    public const byte VectorVBlankIn = 0x40;
    public const byte VectorVBlankInSlave = 0x43;
    public const byte VectorVBlankOut = 0x41;
    public const byte VectorHBlankIn = 0x42;
    public const byte VectorHBlankInSlave = 0x41;
    public const byte VectorTimer0 = 0x43;
    public const byte VectorTimer1 = 0x44;
    public const byte VectorDspEnd = 0x45;
    public const byte VectorSoundRequest = 0x46;
    public const byte VectorSystemManager = 0x47;
    public const byte VectorPadInterrupt = 0x48;
    public const byte VectorLevel2DmaEnd = 0x49;
    public const byte VectorLevel1DmaEnd = 0x4A;
    public const byte VectorLevel0DmaEnd = 0x4B;
    public const byte VectorDmaIllegal = 0x4C;
    public const byte VectorSpriteDrawEnd = 0x4D;
    public const byte VectorExternal00 = 0x50;
    public const byte VectorExternal01 = 0x51;
    public const byte VectorExternal02 = 0x52;
    public const byte VectorExternal03 = 0x53;
    public const byte VectorExternal04 = 0x54;
    public const byte VectorExternal05 = 0x55;
    public const byte VectorExternal06 = 0x56;
    public const byte VectorExternal07 = 0x57;
    public const byte VectorExternal08 = 0x58;
    public const byte VectorExternal09 = 0x59;
    public const byte VectorExternal10 = 0x5A;
    public const byte VectorExternal11 = 0x5B;
    public const byte VectorExternal12 = 0x5C;
    public const byte VectorExternal13 = 0x5D;
    public const byte VectorExternal14 = 0x5E;
    public const byte VectorExternal15 = 0x5F;
    public const byte VectorFrtInputCapture = 0x64;
    public const byte VectorFrtInputCapture2 = 0x65;

    public static readonly SaturnInterrupt Undefined = new(0, 0, 0, 0, "Undefined");
    public static readonly SaturnInterrupt Nmi = new(VectorNmi, 0xF, 0, 0, "NMI");
    public static readonly SaturnInterrupt UserBreak = new(VectorUserBreak, 0xF, 0, 0, "User break");
    public static readonly SaturnInterrupt VBlankIn = new(VectorVBlankIn, 0xF, 0, 0, "V-Blank-IN");
    public static readonly SaturnInterrupt VBlankInSlave = new(VectorVBlankInSlave, 0x6, 0, 0, "V-Blank-IN (slave)");
    public static readonly SaturnInterrupt VBlankOut = new(VectorVBlankOut, 0xE, 1, 1, "V-Blank-OUT");
    public static readonly SaturnInterrupt HBlankIn = new(VectorHBlankIn, 0xD, 2, 2, "H-Blank-IN");
    public static readonly SaturnInterrupt HBlankInSlave = new(VectorHBlankInSlave, 0x2, 0, 0, "H-Blank-IN (slave)");
    public static readonly SaturnInterrupt Timer0 = new(VectorTimer0, 0xC, 3, 3, "Timer 0");
    public static readonly SaturnInterrupt Timer1 = new(VectorTimer1, 0xB, 4, 4, "Timer 1");
    public static readonly SaturnInterrupt DspEnd = new(VectorDspEnd, 0xA, 5, 5, "DSP End");
    public static readonly SaturnInterrupt SoundRequest = new(VectorSoundRequest, 0x9, 6, 6, "Sound Request");
    public static readonly SaturnInterrupt SystemManager = new(VectorSystemManager, 0x8, 7, 7, "System Manager");
    public static readonly SaturnInterrupt PadInterrupt = new(VectorPadInterrupt, 0x8, 8, 8, "PAD Interrupt");
    public static readonly SaturnInterrupt Level2DmaEnd = new(VectorLevel2DmaEnd, 0x6, 9, 9, "Level-2 DMA End");
    public static readonly SaturnInterrupt Level1DmaEnd = new(VectorLevel1DmaEnd, 0x6, 10, 10, "Level-1 DMA End");
    public static readonly SaturnInterrupt Level0DmaEnd = new(VectorLevel0DmaEnd, 0x5, 11, 11, "Level-0 DMA End");
    public static readonly SaturnInterrupt DmaIllegal = new(VectorDmaIllegal, 0x3, 12, 12, "DMA Illegal");
    public static readonly SaturnInterrupt SpriteDrawEnd = new(VectorSpriteDrawEnd, 0x2, 13, 13, "Sprite Draw End");
    public static readonly SaturnInterrupt External00 = new(VectorExternal00, 0x7, 16, 16, "External Interrupt 00");
    public static readonly SaturnInterrupt External01 = new(VectorExternal01, 0x7, 17, 17, "External Interrupt 01");
    public static readonly SaturnInterrupt External02 = new(VectorExternal02, 0x7, 18, 18, "External Interrupt 02");
    public static readonly SaturnInterrupt External03 = new(VectorExternal03, 0x7, 19, 19, "External Interrupt 03");
    public static readonly SaturnInterrupt External04 = new(VectorExternal04, 0x4, 20, 20, "External Interrupt 04");
    public static readonly SaturnInterrupt External05 = new(VectorExternal05, 0x4, 21, 21, "External Interrupt 05");
    public static readonly SaturnInterrupt External06 = new(VectorExternal06, 0x4, 22, 22, "External Interrupt 06");
    public static readonly SaturnInterrupt External07 = new(VectorExternal07, 0x4, 23, 23, "External Interrupt 07");
    public static readonly SaturnInterrupt External08 = new(VectorExternal08, 0x1, 24, 24, "External Interrupt 08");
    public static readonly SaturnInterrupt External09 = new(VectorExternal09, 0x1, 25, 25, "External Interrupt 09");
    public static readonly SaturnInterrupt External10 = new(VectorExternal10, 0x1, 26, 26, "External Interrupt 10");
    public static readonly SaturnInterrupt External11 = new(VectorExternal11, 0x1, 27, 27, "External Interrupt 11");
    public static readonly SaturnInterrupt External12 = new(VectorExternal12, 0x1, 28, 28, "External Interrupt 12");
    public static readonly SaturnInterrupt External13 = new(VectorExternal13, 0x1, 29, 29, "External Interrupt 13");
    public static readonly SaturnInterrupt External14 = new(VectorExternal14, 0x1, 30, 30, "External Interrupt 14");
    public static readonly SaturnInterrupt External15 = new(VectorExternal15, 0x1, 31, 31, "External Interrupt 15");

    // These interrupt vectors and levels are set dynamically by the SH-2.
    public static SaturnInterrupt Sh2DivisionOverflow = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2Dma0TransferEnd = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2Dma1TransferEnd = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2WdtOverflow = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2BscValuesMatched = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2SciTransmitEnd = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2SciReceiveError = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2SciReceiveDataFull = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2SciTransmitDataEmpty = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2FrtInputCapture = new(VectorFrtInputCapture, 0x1, 0, 0);
    public static SaturnInterrupt Sh2FrtOutputCompareFlagASet = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2FrtOutputCompareFlagBSet = new(0, 0, 0, 0);
    public static SaturnInterrupt Sh2FrtOverflowFlagSet = new(0, 0, 0, 0);
}
