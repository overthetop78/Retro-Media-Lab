// Translated from Yabause peripheral.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using PortData = RetroMediaLab.Chips.Controllers.Sega.SaturnSmpcState.PortData;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnPeripheral
{
    public PortData Port1 { get; } = new();
    public PortData Port2 { get; } = new();

    public SaturnPeripheral() => ResetPorts();

    public void ResetPorts()
    {
        Port1.Data[0] = 0xF0;
        Port1.Size = 1;
        Port2.Data[0] = 0xF0;
        Port2.Size = 1;
    }

    public static Memory<byte>? AddPad(PortData port) => AddPeripheral(port, SaturnPeripheralId.Pad);

    public static Memory<byte>? AddPeripheral(PortData port, SaturnPeripheralId id)
    {
        int peripheralCount = port.Data[0] & 0xF;
        int offset = 1;
        int current = 1;
        if (peripheralCount == 0xF)
            return null;
        else if (id == SaturnPeripheralId.Gun && peripheralCount == 1)
            return null;

        if (peripheralCount == 0)
        {
            peripheralCount = 1;
            if (id != SaturnPeripheralId.Gun)
                port.Data[0] = 0xF1;
            else
                port.Data[0] = 0xA0;
        }
        else
        {
            if (peripheralCount == 1)
            {
                int slot = offset + (port.Data[offset] & 0xF) + 1;
                for (int i = 0; i < 5; i++)
                    port.Data[slot + i] = 0xFF;
            }
            peripheralCount = 6;
            port.Data[0] = 0x16;

            current = 0;
            int size = port.Data[offset] & 0xF;
            while (current < peripheralCount && size != 0xF)
            {
                offset += size + 1;
                current++;
                size = port.Data[offset] & 0xF;
            }
            if (current == peripheralCount)
                return null;
            current++;
        }

        port.Data[offset] = (byte)id;
        offset++;
        int dataSize = (int)id & 0xF;
        switch (id)
        {
            case SaturnPeripheralId.Pad:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xFF;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.Wheel:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xFF;
                port.Data[offset + 2] = 0x7F;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.MissionStick:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xFF;
                port.Data[offset + 2] = 0x7F;
                port.Data[offset + 3] = 0x7F;
                port.Data[offset + 4] = 0x7F;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.TwinSticks:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xFF;
                port.Data[offset + 2] = 0x7F;
                port.Data[offset + 3] = 0x7F;
                port.Data[offset + 4] = 0x7F;
                port.Data[offset + 5] = 0x7F;
                port.Data[offset + 6] = 0x7F;
                port.Data[offset + 7] = 0x7F;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.ThreeDimensionalPad:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xFF;
                port.Data[offset + 2] = 0x7F;
                port.Data[offset + 3] = 0x7F;
                port.Data[offset + 4] = 0x7F;
                port.Data[offset + 5] = 0x7F;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.Gun:
                port.Data[offset] = 0x7C;
                port.Data[offset + 1] = 0xFF;
                port.Data[offset + 2] = 0xFF;
                port.Data[offset + 3] = 0xFF;
                port.Data[offset + 4] = 0xFF;
                port.Size = 1;
                break;
            case SaturnPeripheralId.Keyboard:
                port.Data[offset] = 0xFF;
                port.Data[offset + 1] = 0xF8;
                port.Data[offset + 2] = 0x06;
                port.Data[offset + 3] = 0x00;
                port.Size = offset + dataSize;
                break;
            case SaturnPeripheralId.Mouse:
                port.Data[offset] = 0;
                port.Data[offset + 1] = 0;
                port.Data[offset + 2] = 0;
                port.Size = offset + dataSize;
                break;
        }

        if (id != SaturnPeripheralId.Gun)
        {
            int slot = offset + dataSize;
            for (int i = 0; i < peripheralCount - current; i++)
            {
                port.Data[slot + i] = 0xFF;
                port.Size++;
            }
        }
        return port.Data.AsMemory(offset - 1, dataSize + 1);
    }

    public static SaturnPeripheralId GetId(ReadOnlySpan<byte> peripheral) => (SaturnPeripheralId)peripheral[0];

    public static void Flush(PortData port)
    {
        if (port.Data[1] == (byte)SaturnPeripheralId.Mouse)
        {
            port.Data[2] &= 0x0F;
            port.Data[3] = 0;
            port.Data[4] = 0;
        }
    }

    public static void PadUpPressed(Span<byte> pad) => pad[1] &= 0xEF;
    public static void PadUpReleased(Span<byte> pad) => pad[1] |= 0x10;

    public static void PadDownPressed(Span<byte> pad) => pad[1] &= 0xDF;
    public static void PadDownReleased(Span<byte> pad) => pad[1] |= 0x20;

    public static void PadRightPressed(Span<byte> pad) => pad[1] &= 0x7F;
    public static void PadRightReleased(Span<byte> pad) => pad[1] |= 0x80;

    public static void PadLeftPressed(Span<byte> pad) => pad[1] &= 0xBF;
    public static void PadLeftReleased(Span<byte> pad) => pad[1] |= 0x40;

    public static void PadStartPressed(Span<byte> pad) => pad[1] &= 0xF7;
    public static void PadStartReleased(Span<byte> pad) => pad[1] |= 0x08;

    public static void PadAPressed(Span<byte> pad) => pad[1] &= 0xFB;
    public static void PadAReleased(Span<byte> pad) => pad[1] |= 0x04;

    public static void PadBPressed(Span<byte> pad) => pad[1] &= 0xFE;
    public static void PadBReleased(Span<byte> pad) => pad[1] |= 0x01;

    public static void PadCPressed(Span<byte> pad) => pad[1] &= 0xFD;
    public static void PadCReleased(Span<byte> pad) => pad[1] |= 0x02;

    public static void PadXPressed(Span<byte> pad) => pad[2] &= 0xBF;
    public static void PadXReleased(Span<byte> pad) => pad[2] |= 0x40;

    public static void PadYPressed(Span<byte> pad) => pad[2] &= 0xDF;
    public static void PadYReleased(Span<byte> pad) => pad[2] |= 0x20;

    public static void PadZPressed(Span<byte> pad) => pad[2] &= 0xEF;
    public static void PadZReleased(Span<byte> pad) => pad[2] |= 0x10;

    public static void PadRTriggerPressed(Span<byte> pad) => pad[2] &= 0x7F;
    public static void PadRTriggerReleased(Span<byte> pad) => pad[2] |= 0x80;

    public static void PadLTriggerPressed(Span<byte> pad) => pad[2] &= 0xF7;
    public static void PadLTriggerReleased(Span<byte> pad) => pad[2] |= 0x08;

    public static void MouseLeftPressed(Span<byte> mouse) => mouse[1] |= 1;
    public static void MouseLeftReleased(Span<byte> mouse) => mouse[1] &= 0xFE;
    public static void MouseMiddlePressed(Span<byte> mouse) => mouse[1] |= 4;
    public static void MouseMiddleReleased(Span<byte> mouse) => mouse[1] &= 0xFB;
    public static void MouseRightPressed(Span<byte> mouse) => mouse[1] |= 2;
    public static void MouseRightReleased(Span<byte> mouse) => mouse[1] &= 0xFD;
    public static void MouseStartPressed(Span<byte> mouse) => mouse[1] |= 8;
    public static void MouseStartReleased(Span<byte> mouse) => mouse[1] &= 0xF7;

    public static void MouseMove(Span<byte> mouse, int displacementX, int displacementY)
    {
        int negativeX = (mouse[1] >> 4) & 1;
        int negativeY = (mouse[1] >> 5) & 1;
        int overflowX = (mouse[1] >> 6) & 1;
        int overflowY = (mouse[1] >> 7) & 1;
        byte differenceX = negativeX != 0 ? unchecked((byte)~mouse[2]) : mouse[2];
        byte differenceY = negativeY != 0 ? unchecked((byte)~mouse[3]) : mouse[3];

        unchecked
        {
            if (displacementX >= 0)
            {
                if (negativeX != 0)
                {
                    if (displacementX - differenceX > 0)
                    {
                        differenceX = (byte)(displacementX - differenceX);
                        negativeX = 0;
                    }
                    else differenceX -= (byte)-displacementX;
                }
                else differenceX += (byte)displacementX;
            }
            else if (negativeX != 0)
                differenceX += (byte)-displacementX;
            else if (differenceX + displacementX > 0)
                differenceX += (byte)displacementX;
            else
            {
                differenceX = (byte)(-displacementX - differenceX);
                negativeX = 1;
            }

            if (displacementY >= 0)
            {
                if (negativeY != 0)
                {
                    if (displacementY - differenceY > 0)
                    {
                        differenceY = (byte)(displacementY - differenceY);
                        negativeY = 0;
                    }
                    else differenceY -= (byte)-displacementY;
                }
                else differenceY += (byte)displacementY;
            }
            else if (negativeY != 0)
                differenceY += (byte)-displacementY;
            else if (differenceY + displacementY > 0)
                differenceY += (byte)displacementY;
            else
            {
                differenceY = (byte)(-displacementY - differenceY);
                negativeY = 1;
            }

            mouse[1] = (byte)((overflowY << 7) | (overflowX << 6) | (negativeY << 5) | (negativeX << 4) | (mouse[1] & 0x0F));
            mouse[2] = negativeX != 0 ? (byte)~differenceX : differenceX;
            mouse[3] = negativeY != 0 ? (byte)~differenceY : differenceY;
        }
    }

    public static void Axis1Value(Span<byte> analog, uint value)
    {
        analog[3] = unchecked((byte)value);
        SaturnPeripheralId id = GetId(analog);
        if (id == SaturnPeripheralId.Wheel)
        {
            bool leftPressed = (analog[1] & (1 << 6)) == 0;
            bool rightPressed = (analog[1] & (1 << 7)) == 0;
            if (value <= 0x67) analog[1] &= 0xBF;
            else if (leftPressed && value >= 0x6F) analog[1] |= 0x40;
            if (value >= 0x97) analog[1] &= 0x7F;
            else if (rightPressed && value <= 0x8F) analog[1] |= 0x80;
        }
        else if (id == SaturnPeripheralId.MissionStick || id == SaturnPeripheralId.TwinSticks)
        {
            bool leftPressed = (analog[1] & (1 << 6)) == 0;
            bool rightPressed = (analog[1] & (1 << 7)) == 0;
            if (value <= 0x56) analog[1] &= 0xBF;
            else if (leftPressed && value >= 0x6A) analog[1] |= 0x40;
            if (value >= 0xAB) analog[1] &= 0x7F;
            else if (rightPressed && value <= 0x95) analog[1] |= 0x80;
        }
    }

    public static void Axis2Value(Span<byte> analog, uint value)
    {
        analog[4] = unchecked((byte)value);
        SaturnPeripheralId id = GetId(analog);
        if (id == SaturnPeripheralId.MissionStick || id == SaturnPeripheralId.TwinSticks)
        {
            bool upPressed = (analog[1] & (1 << 4)) == 0;
            bool downPressed = (analog[1] & (1 << 5)) == 0;
            if (value <= 0x65) analog[1] &= 0xEF;
            else if (upPressed && value >= 0x6A) analog[1] |= 0x10;
            if (value >= 0xA9) analog[1] &= 0xDF;
            else if (downPressed && value <= 0x94) analog[1] |= 0x20;
        }
    }

    public static void Axis3Value(Span<byte> analog, uint value)
    {
        if (GetId(analog) != SaturnPeripheralId.MissionStick)
            analog[5] = unchecked((byte)value);
        else
            analog[5] = unchecked((byte)-(sbyte)value);
    }

    public static void Axis4Value(Span<byte> analog, uint value) => analog[6] = unchecked((byte)value);
    public static void Axis5Value(Span<byte> analog, uint value) => analog[7] = unchecked((byte)value);
    public static void Axis6Value(Span<byte> analog, uint value) => analog[8] = unchecked((byte)value);

    public static void Axis7Value(Span<byte> analog, uint value)
    {
        if (GetId(analog) != SaturnPeripheralId.TwinSticks)
            analog[9] = unchecked((byte)value);
        else
            analog[9] = unchecked((byte)-(sbyte)value);
    }

    public static void GunTriggerPressed(Span<byte> gun) => gun[1] &= 0xEF;
    public static void GunTriggerReleased(Span<byte> gun) => gun[1] |= 0x10;
    public static void GunStartPressed(Span<byte> gun) => gun[1] &= 0xDF;
    public static void GunStartReleased(Span<byte> gun) => gun[1] |= 0x20;

    public static void GunMove(Span<byte> gun, int displacementX, int displacementY)
    {
        int x = (gun[2] << 8) + gun[3] + displacementX / 4;
        int y = (gun[4] << 8) + gun[5] - displacementY / 4;
        if (x < 0) x = 0;
        else if (x >= 320) x = 319;
        if (y < 0) y = 0;
        else if (y >= 224) y = 223;

        gun[2] = (byte)(x >> 8);
        gun[3] = unchecked((byte)x);
        gun[4] = (byte)(y >> 8);
        gun[5] = unchecked((byte)y);
    }
}
