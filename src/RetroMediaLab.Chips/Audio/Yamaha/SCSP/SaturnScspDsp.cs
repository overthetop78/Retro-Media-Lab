// Copyright 2015 Theo Berkau
// Translated from the audio interpreter in Yabause scspdsp.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Numerics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Audio.Yamaha.SCSP;

internal sealed class SaturnScspDsp
{
    private sealed class DspState
    {
        public readonly ushort[] Coef = new ushort[64];
        public readonly ushort[] Madrs = new ushort[32];
        public readonly ulong[] Mpro = new ulong[128];
        public readonly int[] Temp = new int[128];
        public readonly int[] Mems = new int[32];
        public readonly int[] Mixs = new int[16];
        public readonly short[] Efreg = new short[16];
        public readonly short[] Exts = new short[2];

        public uint MdecCt;
        public int Inputs;
        public int Shifted;
        public int YReg;
        public ushort FrcReg;
        public ushort AdrsReg;
        public uint MrdValue;
        public int Rbl;
        public int Rbp;
        public int NeedRead;
        public uint IoAddr;
        public int NeedWrite;
        public ushort WriteData;
    }

    private DspState _dsp = new();
    private readonly Memory<byte> _soundRam;

    public SaturnScspDsp(Memory<byte> soundRam)
    {
        _soundRam = soundRam;
        Interface = new SaturnScspDspInterface(
            SetRblRbp, SetExternalInputs, SetMix, GetEffectOut,
            SetMpro, GetMpro, SetCoefficient, SetMemoryAddress,
            GetCoefficient, GetExternalInput, GetMemoryAddress, GetMems, Execute);
    }

    public SaturnScspDspInterface Interface { get; }

    // Mednafen scsp.inc, RunDSP.
    private static void ExecuteInstruction(DspState dsp, int addressIndex, Span<ushort> soundRam)
    {
        unchecked
        {
            var instruction = new SaturnScspDspInstruction(dsp.Mpro[addressIndex]);
            uint tempWriteAddress = (instruction.Twa + dsp.MdecCt) & 0x7F;
            uint tempReadAddress = (instruction.Tra + dsp.MdecCt) & 0x7F;
            uint inputAddress = instruction.Ira;

            if ((inputAddress & 0x20) != 0)
            {
                if ((inputAddress & 0x10) != 0)
                {
                    if ((inputAddress & 0xE) == 0)
                        dsp.Inputs = dsp.Exts[inputAddress & 1] << 8;
                }
                else
                {
                    dsp.Inputs = dsp.Mixs[inputAddress & 0xF] << 4;
                }
            }
            else
            {
                dsp.Inputs = dsp.Mems[inputAddress & 0x1F];
            }

            int inputs = (dsp.Inputs << 8) >> 8;
            int y = instruction.Ysel switch
            {
                0 => dsp.FrcReg,
                1 => dsp.Coef[instruction.Coef],
                2 => (dsp.YReg >> 11) & 0x1FFF,
                _ => (dsp.YReg >> 4) & 0xFFF,
            };
            if (instruction.Yrl != 0)
                dsp.YReg = inputs & 0xFFFFFF;

            int shiftLow = (int)instruction.Shift & 1;
            int shiftHigh = (int)instruction.Shift >> 1;
            int shifterOutput = ((dsp.Shifted << 6) >> 6) << (shiftLow ^ shiftHigh);
            if (shiftHigh == 0)
                shifterOutput = Math.Clamp(shifterOutput, -0x800000, 0x7FFFFF);
            shifterOutput &= 0xFFFFFF;

            if (instruction.Frcl != 0)
                dsp.FrcReg = (ushort)(instruction.Shift == 3 ? shifterOutput & 0xFFF : shifterOutput >> 11);

            int temp = (dsp.Temp[tempReadAddress] << 8) >> 8;
            int x = instruction.Xsel != 0 ? inputs : temp;
            int product = (int)(((long)((y << 19) >> 19) * x) >> 12);
            int addend = instruction.Bsel != 0 ? dsp.Shifted : temp;
            if (instruction.Negb != 0)
                addend = -addend;
            if (instruction.Zero != 0)
                addend = 0;
            dsp.Shifted = (product + addend) & 0x3FFFFFF;

            if (instruction.Ewt != 0)
                dsp.Efreg[instruction.Ewa] = (short)(shifterOutput >> 8);
            if (instruction.Twt != 0)
                dsp.Temp[tempWriteAddress] = shifterOutput;
            if (instruction.Iwt != 0)
                dsp.Mems[instruction.Iwa] = (int)dsp.MrdValue;

            if (dsp.NeedRead != 0)
            {
                ushort data = dsp.IoAddr < soundRam.Length ? soundRam[(int)dsp.IoAddr] : (ushort)0;
                dsp.MrdValue = dsp.NeedRead == 2 ? (uint)data << 8 : (uint)FloatToInt(data);
                dsp.NeedRead = 0;
            }
            else if (dsp.NeedWrite != 0)
            {
                if (dsp.IoAddr < soundRam.Length)
                    soundRam[(int)dsp.IoAddr] = dsp.WriteData;
                dsp.NeedWrite = 0;
            }

            ushort address = (ushort)(dsp.Madrs[instruction.Masa] + instruction.Nxadr);
            if (instruction.Adreb != 0)
                address = (ushort)(address + ((dsp.AdrsReg << 20) >> 20));
            if (instruction.Table == 0)
            {
                address = (ushort)(address + dsp.MdecCt);
                address &= (ushort)((0x2000 << dsp.Rbl) - 1);
            }
            dsp.IoAddr = (uint)(address + (dsp.Rbp << 12)) & 0x7FFFF;

            if (instruction.Mrd != 0)
                dsp.NeedRead = 1 + (int)instruction.Nofl;
            if (instruction.Mwt != 0)
            {
                dsp.NeedWrite = 1;
                dsp.WriteData = instruction.Nofl != 0
                    ? (ushort)(shifterOutput >> 8)
                    : (ushort)IntToFloat((uint)shifterOutput);
            }

            if (instruction.Adrl != 0)
            {
                dsp.AdrsReg = (ushort)(instruction.Shift == 3
                    ? shifterOutput >> 12
                    : (inputs >> 16) & 0xFFF);
            }
        }
    }

    public static int FloatToInt(ushort floatValue)
    {
        unchecked
        {
            uint signXor = (uint)(((floatValue & 0x8000) << 16) >> 1);
            int exponent = (floatValue >> 11) & 0xF;
            uint result = (uint)floatValue & 0x7FF;
            if (exponent < 12)
                result |= 0x800;

            result = (result << 19) ^ signXor;
            return ((int)result >> (8 + Math.Min(11, exponent))) & 0xFFFFFF;
        }
    }

    public static uint IntToFloat(uint intValue)
    {
        unchecked
        {
            uint shiftedInput = intValue << 8;
            uint signXor = (uint)((int)shiftedInput >> 31);
            int exponent = BitOperations.LeadingZeroCount(((shiftedInput ^ signXor) << 1) | (1u << 19));
            int shift = exponent - (exponent == 12 ? 1 : 0);
            uint result = (uint)((int)shiftedInput >> (19 - shift)) & 0x87FF;
            return result | ((uint)exponent << 11);
        }
    }

    private void SetMpro(ulong input, uint address) => _dsp.Mpro[address] = input;

    private void SetCoefficient(uint input, uint address) => _dsp.Coef[address] = unchecked((ushort)input);

    private void SetMemoryAddress(uint input, uint address) => _dsp.Madrs[address] = unchecked((ushort)input);

    private void SetRblRbp(uint rbl, uint rbp)
    {
        _dsp.Rbl = unchecked((int)rbl);
        _dsp.Rbp = unchecked((int)rbp);
    }

    private void SetExternalInputs(uint left, uint right)
    {
        _dsp.Exts[0] = unchecked((short)left);
        _dsp.Exts[1] = unchecked((short)right);
    }

    private void SetMix(uint channel, uint data) =>
        _dsp.Mixs[channel] = unchecked(_dsp.Mixs[channel] + (int)(data << 4)) & 0xFFFFF;

    private uint GetEffectOut(int index)
    {
        if (index < 16)
            return unchecked((uint)_dsp.Efreg[index]);
        else if (index == 16)
            return unchecked((uint)_dsp.Exts[0]);
        else if (index == 17)
            return unchecked((uint)_dsp.Exts[1]);

        return 0;
    }

    private uint GetCoefficient(uint address) => _dsp.Coef[address];
    private uint GetExternalInput(uint address) => unchecked((uint)_dsp.Exts[address]);
    private uint GetMemoryAddress(uint address) => _dsp.Madrs[address];
    private uint GetMems(uint address) => unchecked((uint)_dsp.Mems[address]);
    private ulong GetMpro(uint address) => _dsp.Mpro[address];

    // DSP data register layout from Mednafen scsp.inc.
    internal ushort AccessDataRegister(uint address, ushort? writeValue = null)
    {
        if (address < 0xE00)
            return AccessSplitRegister(ref _dsp.Temp[(address & 0x1FF) >> 2], address, 8, writeValue);
        if (address < 0xE80)
            return AccessSplitRegister(ref _dsp.Mems[(address & 0x7F) >> 2], address, 8, writeValue);
        if (address < 0xEC0)
            return AccessSplitRegister(ref _dsp.Mixs[(address & 0x3F) >> 2], address, 4, writeValue);
        if (address < 0xEE0)
        {
            uint index = (address & 0x1F) >> 1;
            if (writeValue is ushort data)
                _dsp.Efreg[index] = unchecked((short)data);
            return unchecked((ushort)_dsp.Efreg[index]);
        }
        return unchecked((ushort)_dsp.Exts[(address & 3) >> 1]);
    }

    private static ushort AccessSplitRegister(ref int register, uint address, int lowBits, ushort? writeValue)
    {
        bool upperWord = (address & 2) != 0;
        int shift = upperWord ? lowBits : 0;
        int mask = upperWord ? 0xFFFF << shift : (1 << lowBits) - 1;
        if (writeValue is ushort data)
            register = (register & ~mask) | ((data << shift) & mask);
        return (ushort)((register & mask) >> shift);
    }

    private void Execute()
    {
        Span<ushort> soundRam = MemoryMarshal.Cast<byte, ushort>(_soundRam.Span);
        for (int i = 0; i < 128; i++)
            ExecuteInstruction(_dsp, i, soundRam);

        if (_dsp.MdecCt == 0)
            _dsp.MdecCt = 0x2000u << _dsp.Rbl;
        _dsp.MdecCt--;

        for (int i = 0; i < 16; i++)
            _dsp.Mixs[i] = 0;
    }

    public void Reset() => _dsp = new DspState();
}
