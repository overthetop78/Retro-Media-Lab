namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

#if AICA_DSP_DYNAREC_NONE || ARM7_DYNAREC_NONE
internal static unsafe partial class AicaDsp
{
    private static void RunStep()
    {
        var state = (DspState*)StateAddress;
        if (state->Stopped)
            return;

        int accumulator = 0;
        int shifted = 0;
        int x = 0;
        int y = 0;
        int b = 0;
        int inputs = 0;
        Span<int> memoryValues = stackalloc int[4];
        memoryValues.Clear();
        int fractionRegister = 0;
        int yRegister = 0;
        uint addressRegister = 0;

        for (int step = 0; step < 128; ++step)
        {
            uint* instruction = AicaMemory.DspData->MPRO + step * 4;

            if (instruction[0] == 0 && instruction[1] == 0 && instruction[2] == 0 && instruction[3] == 0)
            {
                x = state->TEMP[state->MDEC_CT & 0x7f];
                y = fractionRegister;
                accumulator = unchecked((int)(((long)x * y >> 12) + x));
                continue;
            }

            uint temporaryReadAddress = (instruction[0] >> 9) & 0x7f;
            bool writeTemporary = (instruction[0] & 0x100) != 0;

            bool selectX = (instruction[1] & 0x8000) != 0;
            uint selectY = (instruction[1] >> 13) & 3;
            uint inputReadAddress = (instruction[1] >> 7) & 0x3f;
            bool writeInput = (instruction[1] & 0x40) != 0;

            bool writeEffect = (instruction[2] & 0x1000) != 0;
            bool loadAddress = (instruction[2] & 0x80) != 0;
            bool loadFraction = (instruction[2] & 0x40) != 0;
            uint shift = (instruction[2] >> 4) & 3;
            bool loadY = (instruction[2] & 8) != 0;
            bool negateB = (instruction[2] & 4) != 0;
            bool zero = (instruction[2] & 2) != 0;
            bool selectB = (instruction[2] & 1) != 0;

            uint coefficient = (uint)step;

            if (inputReadAddress <= 0x1f)
                inputs = state->MEMS[inputReadAddress];
            else if (inputReadAddress <= 0x2f)
                inputs = state->MIXS[inputReadAddress - 0x20] << 4;
            else if (inputReadAddress <= 0x31)
                inputs = unchecked((int)(AicaMemory.DspData->EXTS[inputReadAddress - 0x30] << 8));
            else
                inputs = 0;

            if (writeInput)
            {
                uint inputWriteAddress = (instruction[1] >> 1) & 0x1f;
                state->MEMS[inputWriteAddress] = memoryValues[step & 3];
            }

            if (!zero)
            {
                if (selectB)
                    b = accumulator;
                else
                    b = state->TEMP[(temporaryReadAddress + state->MDEC_CT) & 0x7f];
                if (negateB)
                    b = unchecked(-b);
            }
            else
            {
                b = 0;
            }

            if (selectX)
                x = inputs;
            else
                x = state->TEMP[(temporaryReadAddress + state->MDEC_CT) & 0x7f];

            if (selectY == 0)
                y = fractionRegister;
            else if (selectY == 1)
                y = unchecked((short)AicaMemory.DspData->COEF[coefficient]) >> 3;
            else if (selectY == 2)
                y = yRegister >> 11;
            else if (selectY == 3)
                y = (yRegister >> 4) & 0x0fff;

            if (loadY)
                yRegister = inputs;

            if (shift == 0 || shift == 3)
                shifted = accumulator;
            else
                shifted = accumulator << 1;

            if (shift < 2)
                shifted = Math.Min(Math.Max(shifted, -0x00800000), 0x007fffff);

            accumulator = unchecked((int)(((long)x * y >> 12) + b));

            if (writeTemporary)
            {
                uint temporaryWriteAddress = (instruction[0] >> 1) & 0x7f;
                state->TEMP[(temporaryWriteAddress + state->MDEC_CT) & 0x7f] = shifted;
            }

            if (loadFraction)
            {
                if (shift == 3)
                    fractionRegister = shifted & 0x0fff;
                else
                    fractionRegister = shifted >> 11;
            }

            if ((step & 1) != 0)
            {
                bool writeMemory = (instruction[2] & 0x4000) != 0;
                bool readMemory = (instruction[2] & 0x2000) != 0;

                if (readMemory || writeMemory)
                {
                    bool table = (instruction[2] & 0x8000) != 0;
                    uint memoryAddressSelection = (instruction[3] >> 9) & 0x3f;
                    bool addAddressRegister = (instruction[3] & 0x100) != 0;
                    bool nextAddress = (instruction[3] & 0x80) != 0;

                    uint address = AicaMemory.DspData->MADRS[memoryAddressSelection];
                    if (addAddressRegister)
                        address += addressRegister & 0x0fff;
                    if (nextAddress)
                        address++;
                    if (!table)
                    {
                        address += state->MDEC_CT;
                        address &= state->RBL;
                    }
                    else
                        address &= 0xffff;

                    address <<= 1;
                    address += state->RBP;
                    if (readMemory)
                        memoryValues[(step + 2) & 3] = Unpack(*(ushort*)(AicaInterface.Ram + (address & AicaInterface.RamMask)));
                    if (writeMemory)
                        *(ushort*)(AicaInterface.Ram + (address & AicaInterface.RamMask)) = Pack(shifted);
                }
            }

            if (loadAddress)
            {
                if (shift == 3)
                    addressRegister = unchecked((uint)(shifted >> 12));
                else
                    addressRegister = unchecked((uint)(inputs >> 16));
            }

            if (writeEffect)
            {
                uint effectWriteAddress = (instruction[2] >> 8) & 0x0f;
                AicaMemory.DspData->EFREG[effectWriteAddress] = unchecked((uint)(shifted >> 8));
            }
        }
        --state->MDEC_CT;
        if (state->MDEC_CT == 0)
            state->MDEC_CT = state->RBL + 1;
    }
}
#endif
