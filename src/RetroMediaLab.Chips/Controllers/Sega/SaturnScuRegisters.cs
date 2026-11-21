// Copyright (c) 2018 Renaud Toumazet
// Translated from Saturnin scu_registers.h.
// SPDX-License-Identifier: Apache-2.0

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed class SaturnScuRegisters
{
    public const uint Level0DmaReadAddress = 0x25FE0000;
    public const uint Level0DmaWriteAddress = 0x25FE0004;
    public const uint Level0DmaTransferByteNumber = 0x25FE0008;
    public const uint Level0DmaAddValueRegister = 0x25FE000C;
    public const uint Level0DmaEnableRegister = 0x25FE0010;
    public const uint Level0DmaModeRegister = 0x25FE0014;
    public const uint Level1DmaReadAddress = 0x25FE0020;
    public const uint Level1DmaWriteAddress = 0x25FE0024;
    public const uint Level1DmaTransferByteNumber = 0x25FE0028;
    public const uint Level1DmaAddValueRegister = 0x25FE002C;
    public const uint Level1DmaEnableRegister = 0x25FE0030;
    public const uint Level1DmaModeRegister = 0x25FE0034;
    public const uint Level2DmaReadAddress = 0x25FE0040;
    public const uint Level2DmaWriteAddress = 0x25FE0044;
    public const uint Level2DmaTransferByteNumber = 0x25FE0048;
    public const uint Level2DmaAddValueRegister = 0x25FE004C;
    public const uint Level2DmaEnableRegister = 0x25FE0050;
    public const uint Level2DmaModeRegister = 0x25FE0054;
    public const uint DmaForcedStop = 0x25FE0060;
    public const uint DmaStatusRegister = 0x25FE007C;
    public const uint DspProgramControlPort = 0x25FE0080;
    public const uint DspProgramRamDataport = 0x25FE0084;
    public const uint DspDataRamAddressPort = 0x25FE0088;
    public const uint DspDataRamDataPort = 0x25FE008C;
    public const uint Timer0CompareRegister = 0x25FE0090;
    public const uint Timer1SetDataRegister = 0x25FE0094;
    public const uint Timer1ModeRegister = 0x25FE0098;
    public const uint InterruptMaskRegister = 0x25FE00A0;
    public const uint InterruptStatusRegister = 0x25FE00A4;
    public const uint ABusInterruptAcknowledge = 0x25FE00A8;
    public const uint ABusSetRegister = 0x25FE00B0;
    public const uint ABusRefreshRegister = 0x25FE00B8;
    public const uint ScuSdramSelectRegister = 0x25FE00C4;
    public const uint ScuVersionRegister = 0x25FE00C8;

    public static class Dxr
    {
        public const int RaPosition = 0;
        public const uint RaMask = 0x7FFFFFF;
        public static uint GetRa(uint register) => (register >> RaPosition) & RaMask;
        public static uint SetRa(uint register, uint value) => (register & ~(RaMask << RaPosition)) | (value << RaPosition);
    }

    public uint d0r = 0;
    public uint d1r = 0;
    public uint d2r = 0;

    public static class Dxw
    {
        public const int WaPosition = 0;
        public const uint WaMask = 0x7FFFFFF;
        public static uint GetWa(uint register) => (register >> WaPosition) & WaMask;
        public static uint SetWa(uint register, uint value) => (register & ~(WaMask << WaPosition)) | (value << WaPosition);
    }

    public uint d0w = 0;
    public uint d1w = 0;
    public uint d2w = 0;

    public static class D0c
    {
        public const int TbnPosition = 0;
        public const uint TbnMask = 0xFFFFF;
        public static uint GetTbn(uint register) => (register >> TbnPosition) & TbnMask;
        public static uint SetTbn(uint register, uint value) => (register & ~(TbnMask << TbnPosition)) | (value << TbnPosition);
    }

    public uint d0c = 0;

    public static class D1c
    {
        public const int TbnPosition = 0;
        public const uint TbnMask = 0xFFF;
        public static uint GetTbn(uint register) => (register >> TbnPosition) & TbnMask;
        public static uint SetTbn(uint register, uint value) => (register & ~(TbnMask << TbnPosition)) | (value << TbnPosition);
    }

    public uint d1c = 0;

    public static class D2c
    {
        public const int TbnPosition = 0;
        public const uint TbnMask = 0xFFF;
        public static uint GetTbn(uint register) => (register >> TbnPosition) & TbnMask;
        public static uint SetTbn(uint register, uint value) => (register & ~(TbnMask << TbnPosition)) | (value << TbnPosition);
    }

    public uint d2c = 0;

    public static class Dxad
    {
        public enum ReadAddressAddValue : byte
        {
            Add0 = 0,
            Add4 = 1,
        }

        public enum WriteAddressAddValue : byte
        {
            Add0 = 0b000,
            Add2 = 0b001,
            Add4 = 0b010,
            Add8 = 0b011,
            Add16 = 0b100,
            Add32 = 0b101,
            Add64 = 0b110,
            Add128 = 0b111,
        }

        public const int DxwaPosition = 0;
        public const uint DxwaMask = 0b111;
        public static WriteAddressAddValue GetDxwa(uint register) => (WriteAddressAddValue)((register >> DxwaPosition) & DxwaMask);
        public const int DxraPosition = 8;
        public const uint DxraMask = 0b1;
        public static ReadAddressAddValue GetDxra(uint register) => (ReadAddressAddValue)((register >> DxraPosition) & DxraMask);
    }

    public uint d0ad = 0;
    public uint d1ad = 0;
    public uint d2ad = 0;

    public static class Dxen
    {
        public enum DmaEnable : byte
        {
            Disabled = 0,
            Enabled = 1,
        }

        public enum DmaStarting : byte
        {
            NotStarted = 0,
            Started = 1,
        }

        public const int DxenPosition = 0;
        public const uint DxenMask = 0b1;
        public static DmaEnable GetDxen(uint register) => (DmaEnable)((register >> DxenPosition) & DxenMask);
        public const int DxgoPosition = 8;
        public const uint DxgoMask = 0b1;
        public static DmaStarting GetDxgo(uint register) => (DmaStarting)((register >> DxgoPosition) & DxgoMask);
    }

    public uint d0en = 0;
    public uint d1en = 0;
    public uint d2en = 0;

    public static class Dxmd
    {
        public enum DmaMode : byte
        {
            Direct = 0,
            Indirect = 1,
        }

        public enum ReadAddressUpdate : byte
        {
            Save = 0,
            Update = 1,
        }

        public enum WriteAddressUpdate : byte
        {
            Save = 0,
            Update = 1,
        }

        public enum StartingFactorSelect : byte
        {
            VBlankIn = 0b000,
            VBlankOut = 0b001,
            HBlankIn = 0b010,
            Timer0 = 0b011,
            Timer1 = 0b100,
            SoundReq = 0b101,
            SpriteDrawEnd = 0b110,
            DmaStartFactor = 0b111,
            None = 0b11111111,
        }

        public const int DxftPosition = 0;
        public const uint DxftMask = 0b111;
        public static StartingFactorSelect GetDxft(uint register) => (StartingFactorSelect)((register >> DxftPosition) & DxftMask);
        public const int DxwupPosition = 8;
        public const uint DxwupMask = 0b1;
        public static WriteAddressUpdate GetDxwup(uint register) => (WriteAddressUpdate)((register >> DxwupPosition) & DxwupMask);
        public const int DxrupPosition = 16;
        public const uint DxrupMask = 0b1;
        public static ReadAddressUpdate GetDxrup(uint register) => (ReadAddressUpdate)((register >> DxrupPosition) & DxrupMask);
        public const int DxmodPosition = 24;
        public const uint DxmodMask = 0b1;
        public static DmaMode GetDxmod(uint register) => (DmaMode)((register >> DxmodPosition) & DxmodMask);
    }

    public uint d0md = 0;
    public uint d1md = 0;
    public uint d2md = 0;

    public static class Dsta
    {
        public enum DspBusAccess : byte
        {
            NotAccessing = 0,
            Accessing = 1,
        }

        public enum BBusAccess : byte
        {
            NotAccessing = 0,
            Accessing = 1,
        }

        public enum ABusAccess : byte
        {
            NotAccessing = 0,
            Accessing = 1,
        }

        public enum Level1DmaInterrupt : byte
        {
            NotInterrupted = 0,
            Interrupted = 1,
        }

        public enum Level0DmaInterrupt : byte
        {
            NotInterrupted = 0,
            Interrupted = 1,
        }

        public enum Level2DmaStandBy : byte
        {
            NotOnStandby = 0,
            OnStandby = 1,
        }

        public enum Level2DmaOperation : byte
        {
            NotInOperation = 0,
            InOperation = 1,
        }

        public enum Level1DmaStandBy : byte
        {
            NotOnStandby = 0,
            OnStandby = 1,
        }

        public enum Level1DmaOperation : byte
        {
            NotInOperation = 0,
            InOperation = 1,
        }

        public enum Level0DmaStandBy : byte
        {
            NotOnStandby = 0,
            OnStandby = 1,
        }

        public enum Level0DmaOperation : byte
        {
            NotInOperation = 0,
            InOperation = 1,
        }

        public enum DspDmaStandBy : byte
        {
            NotOnStandby = 0,
            OnStandby = 1,
        }

        public enum DspDmaOperation : byte
        {
            NotInOperation = 0,
            InOperation = 1,
        }

        public const int DdmvPosition = 0;
        public const uint DdmvMask = 0b1;
        public static DspDmaOperation GetDdmv(uint register) => (DspDmaOperation)((register >> DdmvPosition) & DdmvMask);
        public const int DdwtPosition = 1;
        public const uint DdwtMask = 0b1;
        public static DspDmaStandBy GetDdwt(uint register) => (DspDmaStandBy)((register >> DdwtPosition) & DdwtMask);
        public const int D0mvPosition = 4;
        public const uint D0mvMask = 0b1;
        public static Level0DmaOperation GetD0mv(uint register) => (Level0DmaOperation)((register >> D0mvPosition) & D0mvMask);
        public const int D0wtPosition = 5;
        public const uint D0wtMask = 0b1;
        public static Level0DmaStandBy GetD0wt(uint register) => (Level0DmaStandBy)((register >> D0wtPosition) & D0wtMask);
        public const int D1mvPosition = 8;
        public const uint D1mvMask = 0b1;
        public static Level1DmaOperation GetD1mv(uint register) => (Level1DmaOperation)((register >> D1mvPosition) & D1mvMask);
        public const int D1wtPosition = 9;
        public const uint D1wtMask = 0b1;
        public static Level1DmaStandBy GetD1wt(uint register) => (Level1DmaStandBy)((register >> D1wtPosition) & D1wtMask);
        public const int D2mvPosition = 12;
        public const uint D2mvMask = 0b1;
        public static Level2DmaOperation GetD2mv(uint register) => (Level2DmaOperation)((register >> D2mvPosition) & D2mvMask);
        public const int D2wtPosition = 13;
        public const uint D2wtMask = 0b1;
        public static Level2DmaStandBy GetD2wt(uint register) => (Level2DmaStandBy)((register >> D2wtPosition) & D2wtMask);
        public const int D0bkPosition = 16;
        public const uint D0bkMask = 0b1;
        public static Level0DmaInterrupt GetD0bk(uint register) => (Level0DmaInterrupt)((register >> D0bkPosition) & D0bkMask);
        public const int D1bkPosition = 17;
        public const uint D1bkMask = 0b1;
        public static Level1DmaInterrupt GetD1bk(uint register) => (Level1DmaInterrupt)((register >> D1bkPosition) & D1bkMask);
        public const int DacsaPosition = 20;
        public const uint DacsaMask = 0b1;
        public static ABusAccess GetDacsa(uint register) => (ABusAccess)((register >> DacsaPosition) & DacsaMask);
        public const int DacsbPosition = 21;
        public const uint DacsbMask = 0b1;
        public static BBusAccess GetDacsb(uint register) => (BBusAccess)((register >> DacsbPosition) & DacsbMask);
        public const int DacsdPosition = 22;
        public const uint DacsdMask = 0b1;
        public static DspBusAccess GetDacsd(uint register) => (DspBusAccess)((register >> DacsdPosition) & DacsdMask);
    }

    public uint dsta = 0;

    public static class T0c
    {
        public const int T0cPosition = 0;
        public const uint T0cMask = 0x3FF;
        public static uint GetT0c(uint register) => (register >> T0cPosition) & T0cMask;
        public static uint SetT0c(uint register, uint value) => (register & ~(T0cMask << T0cPosition)) | (value << T0cPosition);
    }

    public uint t0c = 0;

    public static class T1s
    {
        public const int T1sPosition = 0;
        public const uint T1sMask = 0x1FF;
        public static uint GetT1s(uint register) => (register >> T1sPosition) & T1sMask;
        public static uint SetT1s(uint register, uint value) => (register & ~(T1sMask << T1sPosition)) | (value << T1sPosition);
    }

    public uint t1s = 0;

    public static class T1md
    {
        public enum Timer1Mode : byte
        {
            InterruptOccursEachLine = 0,
            InterruptOccursOnlyAtTimer = 1,
        }

        public enum TimerEnable : byte
        {
            TimerOperationOff = 0,
            TimerOperationOn = 1,
        }

        public const int TenbPosition = 0;
        public const uint TenbMask = 0b1;
        public static TimerEnable GetTenb(uint register) => (TimerEnable)((register >> TenbPosition) & TenbMask);
        public const int T1mdPosition = 8;
        public const uint T1mdMask = 0b1;
        public static Timer1Mode GetT1md(uint register) => (Timer1Mode)((register >> T1mdPosition) & T1mdMask);
    }

    public uint t1md = 0;

    public static class Ppaf
    {
        public enum ExecutePauseReset : byte
        {
            NoChange = 0,
            ProgramPauseIsReset = 1,
        }

        public enum ExecutePause : byte
        {
            NoChange = 0,
            ProgramPauses = 1,
        }

        public enum D0BusDmaExecution : byte
        {
            NoDma = 0,
            DmaIsExecuting = 1,
        }

        public enum Sign : byte
        {
            OperationResultIsNotNegative = 0,
            OperationResultIsNegative = 1,
        }

        public enum Zero : byte
        {
            OperationResultIsNotZero = 0,
            OperationResultIsZero = 1,
        }

        public enum Carry : byte
        {
            OperationResultHasNoCarry = 0,
            OperationResultHasCarry = 1,
        }

        public enum Overflow : byte
        {
            OperationDoesNotCauseOverflow = 0,
            OperationCausesOverflow = 1,
        }

        public enum ProgramEndInterrupt : byte
        {
            ProgramNotEndedByEndi = 0,
            ProgramEndedByEndi = 1,
        }

        public enum StepExecuteControl : byte
        {
            ProgramDoesNotExecuteOneStep = 0,
            ProgramExecutesOneStep = 1,
        }

        public enum ProgramExecuteControl : byte
        {
            ProgramExecutionStops = 0,
            ProgramExecutionBegins = 1,
        }

        public enum ProgramCounterTransferEnable : byte
        {
            ProgramRamAddressNotLoadedToProgramCounter = 0,
            ProgramRamAddressLoadedToProgramCounter = 1,
        }

        public const int LePosition = 15;
        public const uint LeMask = 0b1;
        public static ProgramCounterTransferEnable GetLe(uint register) => (ProgramCounterTransferEnable)((register >> LePosition) & LeMask);
        public const int ExPosition = 16;
        public const uint ExMask = 0b1;
        public static ProgramExecuteControl GetEx(uint register) => (ProgramExecuteControl)((register >> ExPosition) & ExMask);
        public const int EsPosition = 17;
        public const uint EsMask = 0b1;
        public static StepExecuteControl GetEs(uint register) => (StepExecuteControl)((register >> EsPosition) & EsMask);
        public const int EPosition = 18;
        public const uint EMask = 0b1;
        public static ProgramEndInterrupt GetE(uint register) => (ProgramEndInterrupt)((register >> EPosition) & EMask);
        public const int VPosition = 19;
        public const uint VMask = 0b1;
        public static Overflow GetV(uint register) => (Overflow)((register >> VPosition) & VMask);
        public const int CPosition = 20;
        public const uint CMask = 0b1;
        public static Carry GetC(uint register) => (Carry)((register >> CPosition) & CMask);
        public const int ZPosition = 21;
        public const uint ZMask = 0b1;
        public static Zero GetZ(uint register) => (Zero)((register >> ZPosition) & ZMask);
        public const int SPosition = 22;
        public const uint SMask = 0b1;
        public static Sign GetS(uint register) => (Sign)((register >> SPosition) & SMask);
        public const int T0Position = 23;
        public const uint T0Mask = 0b1;
        public static D0BusDmaExecution GetT0(uint register) => (D0BusDmaExecution)((register >> T0Position) & T0Mask);
        public const int EpPosition = 25;
        public const uint EpMask = 0b1;
        public static ExecutePause GetEp(uint register) => (ExecutePause)((register >> EpPosition) & EpMask);
        public const int PpPosition = 26;
        public const uint PpMask = 0b1;
        public static ExecutePauseReset GetPp(uint register) => (ExecutePauseReset)((register >> PpPosition) & PpMask);
        public const int PPosition = 0;
        public const uint PMask = 0xFF;
        public static uint GetP(uint register) => (register >> PPosition) & PMask;
        public static uint SetP(uint register, uint value) => (register & ~(PMask << PPosition)) | (value << PPosition);
    }

    public uint ppaf = 0;

    public static class Ppd
    {
    }

    public uint ppd = 0;

    public static class Pda
    {
        public enum DataRamSelect : byte
        {
            Ram0 = 0b00,
            Ram1 = 0b01,
            Ram2 = 0b10,
            Ram3 = 0b11,
        }

        public const int DrsPosition = 6;
        public const uint DrsMask = 0b11;
        public static DataRamSelect GetDrs(uint register) => (DataRamSelect)((register >> DrsPosition) & DrsMask);
        public const int DraPosition = 0;
        public const uint DraMask = 0x3F;
        public static uint GetDra(uint register) => (register >> DraPosition) & DraMask;
        public static uint SetDra(uint register, uint value) => (register & ~(DraMask << DraPosition)) | (value << DraPosition);
    }

    public uint pda = 0;

    public static class Pdd
    {
    }

    public uint pdd = 0;

    public static class Ims
    {
        public enum InterruptMask : byte
        {
            NotMasked = 0,
            Masked = 1,
        }

        public const int External15Position = 31;
        public const uint External15Mask = 0b1;
        public static InterruptMask GetExternal15(uint register) => (InterruptMask)((register >> External15Position) & External15Mask);
        public const int External14Position = 30;
        public const uint External14Mask = 0b1;
        public static InterruptMask GetExternal14(uint register) => (InterruptMask)((register >> External14Position) & External14Mask);
        public const int External13Position = 29;
        public const uint External13Mask = 0b1;
        public static InterruptMask GetExternal13(uint register) => (InterruptMask)((register >> External13Position) & External13Mask);
        public const int External12Position = 28;
        public const uint External12Mask = 0b1;
        public static InterruptMask GetExternal12(uint register) => (InterruptMask)((register >> External12Position) & External12Mask);
        public const int External11Position = 27;
        public const uint External11Mask = 0b1;
        public static InterruptMask GetExternal11(uint register) => (InterruptMask)((register >> External11Position) & External11Mask);
        public const int External10Position = 26;
        public const uint External10Mask = 0b1;
        public static InterruptMask GetExternal10(uint register) => (InterruptMask)((register >> External10Position) & External10Mask);
        public const int External09Position = 25;
        public const uint External09Mask = 0b1;
        public static InterruptMask GetExternal09(uint register) => (InterruptMask)((register >> External09Position) & External09Mask);
        public const int External08Position = 24;
        public const uint External08Mask = 0b1;
        public static InterruptMask GetExternal08(uint register) => (InterruptMask)((register >> External08Position) & External08Mask);
        public const int External07Position = 23;
        public const uint External07Mask = 0b1;
        public static InterruptMask GetExternal07(uint register) => (InterruptMask)((register >> External07Position) & External07Mask);
        public const int External06Position = 22;
        public const uint External06Mask = 0b1;
        public static InterruptMask GetExternal06(uint register) => (InterruptMask)((register >> External06Position) & External06Mask);
        public const int External05Position = 21;
        public const uint External05Mask = 0b1;
        public static InterruptMask GetExternal05(uint register) => (InterruptMask)((register >> External05Position) & External05Mask);
        public const int External04Position = 20;
        public const uint External04Mask = 0b1;
        public static InterruptMask GetExternal04(uint register) => (InterruptMask)((register >> External04Position) & External04Mask);
        public const int External03Position = 19;
        public const uint External03Mask = 0b1;
        public static InterruptMask GetExternal03(uint register) => (InterruptMask)((register >> External03Position) & External03Mask);
        public const int External02Position = 18;
        public const uint External02Mask = 0b1;
        public static InterruptMask GetExternal02(uint register) => (InterruptMask)((register >> External02Position) & External02Mask);
        public const int External01Position = 17;
        public const uint External01Mask = 0b1;
        public static InterruptMask GetExternal01(uint register) => (InterruptMask)((register >> External01Position) & External01Mask);
        public const int External00Position = 16;
        public const uint External00Mask = 0b1;
        public static InterruptMask GetExternal00(uint register) => (InterruptMask)((register >> External00Position) & External00Mask);
        public const int ABusPosition = 15;
        public const uint ABusMask = 0b1;
        public static InterruptMask GetABus(uint register) => (InterruptMask)((register >> ABusPosition) & ABusMask);
        public const int SpriteDrawEndPosition = 13;
        public const uint SpriteDrawEndMask = 0b1;
        public static InterruptMask GetSpriteDrawEnd(uint register) => (InterruptMask)((register >> SpriteDrawEndPosition) & SpriteDrawEndMask);
        public const int DmaIllegalPosition = 12;
        public const uint DmaIllegalMask = 0b1;
        public static InterruptMask GetDmaIllegal(uint register) => (InterruptMask)((register >> DmaIllegalPosition) & DmaIllegalMask);
        public const int Level0DmaPosition = 11;
        public const uint Level0DmaMask = 0b1;
        public static InterruptMask GetLevel0Dma(uint register) => (InterruptMask)((register >> Level0DmaPosition) & Level0DmaMask);
        public const int Level1DmaPosition = 10;
        public const uint Level1DmaMask = 0b1;
        public static InterruptMask GetLevel1Dma(uint register) => (InterruptMask)((register >> Level1DmaPosition) & Level1DmaMask);
        public const int Level2DmaPosition = 9;
        public const uint Level2DmaMask = 0b1;
        public static InterruptMask GetLevel2Dma(uint register) => (InterruptMask)((register >> Level2DmaPosition) & Level2DmaMask);
        public const int PadPosition = 8;
        public const uint PadMask = 0b1;
        public static InterruptMask GetPad(uint register) => (InterruptMask)((register >> PadPosition) & PadMask);
        public const int SystemManagerPosition = 7;
        public const uint SystemManagerMask = 0b1;
        public static InterruptMask GetSystemManager(uint register) => (InterruptMask)((register >> SystemManagerPosition) & SystemManagerMask);
        public const int SoundRequestPosition = 6;
        public const uint SoundRequestMask = 0b1;
        public static InterruptMask GetSoundRequest(uint register) => (InterruptMask)((register >> SoundRequestPosition) & SoundRequestMask);
        public const int DspEndPosition = 5;
        public const uint DspEndMask = 0b1;
        public static InterruptMask GetDspEnd(uint register) => (InterruptMask)((register >> DspEndPosition) & DspEndMask);
        public const int Timer1Position = 4;
        public const uint Timer1Mask = 0b1;
        public static InterruptMask GetTimer1(uint register) => (InterruptMask)((register >> Timer1Position) & Timer1Mask);
        public const int Timer0Position = 3;
        public const uint Timer0Mask = 0b1;
        public static InterruptMask GetTimer0(uint register) => (InterruptMask)((register >> Timer0Position) & Timer0Mask);
        public const int HBlankInPosition = 2;
        public const uint HBlankInMask = 0b1;
        public static InterruptMask GetHBlankIn(uint register) => (InterruptMask)((register >> HBlankInPosition) & HBlankInMask);
        public const int VBlankOutPosition = 1;
        public const uint VBlankOutMask = 0b1;
        public static InterruptMask GetVBlankOut(uint register) => (InterruptMask)((register >> VBlankOutPosition) & VBlankOutMask);
        public const int VBlankInPosition = 0;
        public const uint VBlankInMask = 0b1;
        public static InterruptMask GetVBlankIn(uint register) => (InterruptMask)((register >> VBlankInPosition) & VBlankInMask);
        public const int UndefinedPosition = 0;
        public const uint UndefinedMask = 0b1;
        public static InterruptMask GetUndefined(uint register) => (InterruptMask)((register >> UndefinedPosition) & UndefinedMask);
    }

    public uint ims = 0;

    public static class Ist
    {
        public enum InterruptEnable : byte
        {
            Disabled = 0,
            Enabled = 1,
        }

        public const int External15Position = 31;
        public const uint External15Mask = 0b1;
        public static InterruptEnable GetExternal15(uint register) => (InterruptEnable)((register >> External15Position) & External15Mask);
        public const int External14Position = 30;
        public const uint External14Mask = 0b1;
        public static InterruptEnable GetExternal14(uint register) => (InterruptEnable)((register >> External14Position) & External14Mask);
        public const int External13Position = 29;
        public const uint External13Mask = 0b1;
        public static InterruptEnable GetExternal13(uint register) => (InterruptEnable)((register >> External13Position) & External13Mask);
        public const int External12Position = 28;
        public const uint External12Mask = 0b1;
        public static InterruptEnable GetExternal12(uint register) => (InterruptEnable)((register >> External12Position) & External12Mask);
        public const int External11Position = 27;
        public const uint External11Mask = 0b1;
        public static InterruptEnable GetExternal11(uint register) => (InterruptEnable)((register >> External11Position) & External11Mask);
        public const int External10Position = 26;
        public const uint External10Mask = 0b1;
        public static InterruptEnable GetExternal10(uint register) => (InterruptEnable)((register >> External10Position) & External10Mask);
        public const int External09Position = 25;
        public const uint External09Mask = 0b1;
        public static InterruptEnable GetExternal09(uint register) => (InterruptEnable)((register >> External09Position) & External09Mask);
        public const int External08Position = 24;
        public const uint External08Mask = 0b1;
        public static InterruptEnable GetExternal08(uint register) => (InterruptEnable)((register >> External08Position) & External08Mask);
        public const int External07Position = 23;
        public const uint External07Mask = 0b1;
        public static InterruptEnable GetExternal07(uint register) => (InterruptEnable)((register >> External07Position) & External07Mask);
        public const int External06Position = 22;
        public const uint External06Mask = 0b1;
        public static InterruptEnable GetExternal06(uint register) => (InterruptEnable)((register >> External06Position) & External06Mask);
        public const int External05Position = 21;
        public const uint External05Mask = 0b1;
        public static InterruptEnable GetExternal05(uint register) => (InterruptEnable)((register >> External05Position) & External05Mask);
        public const int External04Position = 20;
        public const uint External04Mask = 0b1;
        public static InterruptEnable GetExternal04(uint register) => (InterruptEnable)((register >> External04Position) & External04Mask);
        public const int External03Position = 19;
        public const uint External03Mask = 0b1;
        public static InterruptEnable GetExternal03(uint register) => (InterruptEnable)((register >> External03Position) & External03Mask);
        public const int External02Position = 18;
        public const uint External02Mask = 0b1;
        public static InterruptEnable GetExternal02(uint register) => (InterruptEnable)((register >> External02Position) & External02Mask);
        public const int External01Position = 17;
        public const uint External01Mask = 0b1;
        public static InterruptEnable GetExternal01(uint register) => (InterruptEnable)((register >> External01Position) & External01Mask);
        public const int External00Position = 16;
        public const uint External00Mask = 0b1;
        public static InterruptEnable GetExternal00(uint register) => (InterruptEnable)((register >> External00Position) & External00Mask);
        public const int ABusPosition = 15;
        public const uint ABusMask = 0b1;
        public static InterruptEnable GetABus(uint register) => (InterruptEnable)((register >> ABusPosition) & ABusMask);
        public const int SpriteDrawEndPosition = 13;
        public const uint SpriteDrawEndMask = 0b1;
        public static InterruptEnable GetSpriteDrawEnd(uint register) => (InterruptEnable)((register >> SpriteDrawEndPosition) & SpriteDrawEndMask);
        public const int DmaIllegalPosition = 12;
        public const uint DmaIllegalMask = 0b1;
        public static InterruptEnable GetDmaIllegal(uint register) => (InterruptEnable)((register >> DmaIllegalPosition) & DmaIllegalMask);
        public const int Level0DmaPosition = 11;
        public const uint Level0DmaMask = 0b1;
        public static InterruptEnable GetLevel0Dma(uint register) => (InterruptEnable)((register >> Level0DmaPosition) & Level0DmaMask);
        public const int Level1DmaPosition = 10;
        public const uint Level1DmaMask = 0b1;
        public static InterruptEnable GetLevel1Dma(uint register) => (InterruptEnable)((register >> Level1DmaPosition) & Level1DmaMask);
        public const int Level2DmaPosition = 9;
        public const uint Level2DmaMask = 0b1;
        public static InterruptEnable GetLevel2Dma(uint register) => (InterruptEnable)((register >> Level2DmaPosition) & Level2DmaMask);
        public const int PadPosition = 8;
        public const uint PadMask = 0b1;
        public static InterruptEnable GetPad(uint register) => (InterruptEnable)((register >> PadPosition) & PadMask);
        public const int SystemManagerPosition = 7;
        public const uint SystemManagerMask = 0b1;
        public static InterruptEnable GetSystemManager(uint register) => (InterruptEnable)((register >> SystemManagerPosition) & SystemManagerMask);
        public const int SoundRequestPosition = 6;
        public const uint SoundRequestMask = 0b1;
        public static InterruptEnable GetSoundRequest(uint register) => (InterruptEnable)((register >> SoundRequestPosition) & SoundRequestMask);
        public const int DspEndPosition = 5;
        public const uint DspEndMask = 0b1;
        public static InterruptEnable GetDspEnd(uint register) => (InterruptEnable)((register >> DspEndPosition) & DspEndMask);
        public const int Timer1Position = 4;
        public const uint Timer1Mask = 0b1;
        public static InterruptEnable GetTimer1(uint register) => (InterruptEnable)((register >> Timer1Position) & Timer1Mask);
        public const int Timer0Position = 3;
        public const uint Timer0Mask = 0b1;
        public static InterruptEnable GetTimer0(uint register) => (InterruptEnable)((register >> Timer0Position) & Timer0Mask);
        public const int HBlankInPosition = 2;
        public const uint HBlankInMask = 0b1;
        public static InterruptEnable GetHBlankIn(uint register) => (InterruptEnable)((register >> HBlankInPosition) & HBlankInMask);
        public const int VBlankOutPosition = 1;
        public const uint VBlankOutMask = 0b1;
        public static InterruptEnable GetVBlankOut(uint register) => (InterruptEnable)((register >> VBlankOutPosition) & VBlankOutMask);
        public const int VBlankInPosition = 0;
        public const uint VBlankInMask = 0b1;
        public static InterruptEnable GetVBlankIn(uint register) => (InterruptEnable)((register >> VBlankInPosition) & VBlankInMask);
        public const int UndefinedPosition = 0;
        public const uint UndefinedMask = 0b1;
        public static InterruptEnable GetUndefined(uint register) => (InterruptEnable)((register >> UndefinedPosition) & UndefinedMask);
    }

    public uint ist = 0;
}
