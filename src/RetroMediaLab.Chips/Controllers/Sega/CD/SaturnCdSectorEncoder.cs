// Copyright (C) 1998-2002 Andreas Mueller <andreas@daneb.de>
// Sector encoding translated from lec.cpp in Mednafen (originally cdrdao).
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

internal static class SaturnCdSectorEncoder
{
    private const int Gf8PrimitivePolynomial = 0x11D;
    private const uint EdcPolynomial = 0x8001801B;
    private const int HeaderOffset = 12;
    private const int DataOffset = 16;
    private const int Mode1EdcOffset = 2064;
    private const int Mode1IntermediateOffset = 2068;
    private const int PParityOffset = 2076;
    private const int QParityOffset = 2248;
    private const int Mode2Form2EdcOffset = 2348;

    private static readonly ushort[,] CoefficientProducts = CreateCoefficientProducts();
    private static readonly uint[] CrcTable = CreateCrcTable();

    private static ushort[,] CreateCoefficientProducts()
    {
        Span<byte> logarithms = stackalloc byte[256];
        Span<byte> inverseLogarithms = stackalloc byte[256];
        logarithms.Clear();
        inverseLogarithms.Clear();
        int value = 1;
        for (int log = 0; log < 255; log++)
        {
            logarithms[value] = (byte)log;
            inverseLogarithms[log] = (byte)value;
            value <<= 1;
            if ((value & 0x100) != 0)
                value ^= Gf8PrimitivePolynomial;
        }

        Span<byte> coefficient0 = stackalloc byte[45];
        Span<byte> coefficient1 = stackalloc byte[45];
        for (int j = 0; j < 45; j++)
            coefficient1[j] = (byte)(inverseLogarithms[44 - j] ^ 1);
        for (int j = 0; j < 45; j++)
            coefficient1[j] = Divide(coefficient1[j], coefficient1[43], logarithms, inverseLogarithms);
        for (int j = 0; j < 45; j++)
            coefficient0[j] = (byte)(1 ^ Divide(inverseLogarithms[44 - j], inverseLogarithms[1], logarithms, inverseLogarithms));
        for (int j = 0; j < 45; j++)
            coefficient0[j] = Divide(coefficient0[j], coefficient0[44], logarithms, inverseLogarithms);

        var products = new ushort[43, 256];
        for (int j = 0; j < 43; j++)
        {
            for (int i = 1; i < 256; i++)
            {
                int c = logarithms[i] + logarithms[coefficient0[j]];
                if (c >= 255) c -= 255;
                products[j, i] = inverseLogarithms[c];

                c = logarithms[i] + logarithms[coefficient1[j]];
                if (c >= 255) c -= 255;
                products[j, i] |= (ushort)(inverseLogarithms[c] << 8);
            }
        }

        return products;
    }

    private static byte Divide(byte a, byte b, ReadOnlySpan<byte> logarithms, ReadOnlySpan<byte> inverseLogarithms)
    {
        if (a == 0)
            return 0;

        int difference = logarithms[a] - logarithms[b];
        if (difference < 0)
            difference += 255;
        return inverseLogarithms[difference];
    }

    private static uint MirrorBits(uint data, int bits)
    {
        uint reversed = 0;
        for (int i = 0; i < bits; i++)
        {
            reversed = (reversed << 1) | (data & 1);
            data >>= 1;
        }

        return reversed;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint remainder = MirrorBits(i, 8) << 24;
            for (int j = 0; j < 8; j++)
            {
                if ((remainder & 0x80000000) != 0)
                    remainder = (remainder << 1) ^ EdcPolynomial;
                else
                    remainder <<= 1;
            }

            table[i] = MirrorBits(remainder, 32);
        }

        return table;
    }

    private static uint CalculateEdc(ReadOnlySpan<byte> data)
    {
        uint crc = 0;
        foreach (byte value in data)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static void SetSyncPattern(Span<byte> sector)
    {
        sector[0] = 0;
        sector.Slice(1, 10).Fill(0xFF);
        sector[11] = 0;
    }

    private static byte ToBcd(byte value) => (byte)((((value / 10) << 4) & 0xF0) | (value % 10));

    private static void SetSectorHeader(byte mode, uint fad, Span<byte> sector)
    {
        sector[HeaderOffset] = ToBcd(unchecked((byte)(fad / (60 * 75))));
        sector[HeaderOffset + 1] = ToBcd((byte)((fad / 75) % 60));
        sector[HeaderOffset + 2] = ToBcd((byte)(fad % 75));
        sector[HeaderOffset + 3] = mode;
    }

    private static void CalculatePParity(Span<byte> sector)
    {
        int sourceStart = HeaderOffset;
        int parity1 = PParityOffset;
        int parity0 = PParityOffset + 2 * 43;
        for (int i = 0; i <= 42; i++)
        {
            int source = sourceStart;
            int low = 0;
            int high = 0;
            for (int j = 19; j <= 42; j++)
            {
                low ^= CoefficientProducts[j, sector[source]];
                high ^= CoefficientProducts[j, sector[source + 1]];
                source += 2 * 43;
            }

            sector[parity0] = unchecked((byte)low);
            sector[parity0 + 1] = unchecked((byte)high);
            sector[parity1] = (byte)(low >> 8);
            sector[parity1 + 1] = (byte)(high >> 8);
            parity0 += 2;
            parity1 += 2;
            sourceStart += 2;
        }
    }

    private static void CalculateQParity(Span<byte> sector)
    {
        int sourceStart = HeaderOffset;
        int parity1 = QParityOffset;
        int parity0 = QParityOffset + 2 * 26;
        for (int i = 0; i <= 25; i++)
        {
            int source = sourceStart;
            int low = 0;
            int high = 0;
            for (int j = 0; j <= 42; j++)
            {
                low ^= CoefficientProducts[j, sector[source]];
                high ^= CoefficientProducts[j, sector[source + 1]];
                source += 2 * 44;
                if (source >= QParityOffset)
                    source -= 2 * 1118;
            }

            sector[parity0] = unchecked((byte)low);
            sector[parity0 + 1] = unchecked((byte)high);
            sector[parity1] = (byte)(low >> 8);
            sector[parity1 + 1] = (byte)(high >> 8);
            parity0 += 2;
            parity1 += 2;
            sourceStart += 2 * 43;
        }
    }

    internal static void EncodeMode1(uint fad, Span<byte> sector)
    {
        SetSyncPattern(sector);
        SetSectorHeader(1, fad, sector);
        BinaryPrimitives.WriteUInt32LittleEndian(sector[Mode1EdcOffset..], CalculateEdc(sector[..Mode1EdcOffset]));
        sector.Slice(Mode1IntermediateOffset, 8).Clear();
        CalculatePParity(sector);
        CalculateQParity(sector);
    }

    internal static void EncodeMode2(uint fad, Span<byte> sector)
    {
        SetSyncPattern(sector);
        SetSectorHeader(2, fad, sector);
    }

    internal static void EncodeMode2Form2(uint fad, Span<byte> sector)
    {
        SetSyncPattern(sector);
        BinaryPrimitives.WriteUInt32LittleEndian(sector[Mode2Form2EdcOffset..], CalculateEdc(sector[DataOffset..Mode2Form2EdcOffset]));
        SetSectorHeader(2, fad, sector);
    }
}
