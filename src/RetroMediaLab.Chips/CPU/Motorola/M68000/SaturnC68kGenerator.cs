// Copyright 2003-2004 Stephane Dallongeville
// Copyright 2004 Theo Berkau
// Translated from Yabause c68k/gen68k.c.
// SPDX-License-Identifier: GPL-2.0-or-later

#if C68K_GEN
namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68kGenerator
{
    private void GenerateOpcodes(OpcodeInfo[] opcodes)
    {
        GenerateJumpTable(0x0000, 0x0000, 0xFFFF, 1, 0, 0, 0, 0, 0, 0, OpIllegal);

        foreach (OpcodeInfo opcode in opcodes)
        {
            _currentOp = opcode;
            _currentSize = 0;
            int lastSize = SizeLong;
            if (_currentOp.SizeType == 0)
                lastSize = 0;
            else if (_currentOp.SizeType == 1)
                _currentSize = 1;

            for (int size = _currentSize; size <= lastSize; size++)
            {
                if (_currentOp.EffectiveAddressModeShift != -1)
                {
                    for (_currentEa = 0; _currentEa <= EaADec7; _currentEa++)
                    {
                        if (!HasEffectiveAddress(_currentEa))
                            continue;

                        _currentEam = EffectiveAddressToModeAndRegister(_currentEa) >> 3;
                        _currentReg = EffectiveAddressToModeAndRegister(_currentEa) & 7;

                        if (opcode.SecondEffectiveAddressModeShift != -1)
                        {
                            for (_currentEa2 = 0; _currentEa2 <= EaADec7; _currentEa2++)
                            {
                                if (!HasSecondEffectiveAddress(_currentEa2))
                                    continue;

                                _currentEam2 = EffectiveAddressToModeAndRegister(_currentEa2) >> 3;
                                _currentReg2 = EffectiveAddressToModeAndRegister(_currentEa2) & 7;
                                SetCurrentSize(size);
                                _currentOp.Generate();
                            }
                        }
                        else
                        {
                            _currentReg2 = 0;
                            SetCurrentSize(size);
                            _currentOp.Generate();
                        }
                    }
                }
                else
                {
                    _currentReg = 0;
                    SetCurrentSize(size);
                    _currentOp.Generate();
                }
            }
        }
    }
}
#endif
