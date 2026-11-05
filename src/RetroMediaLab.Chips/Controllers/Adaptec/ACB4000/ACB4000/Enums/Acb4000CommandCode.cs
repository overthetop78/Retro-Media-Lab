namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

/// <summary>Opcodes documentés ; leur déclaration n'annonce pas à elle seule leur réalisation.</summary>
public enum Acb4000CommandCode : byte
{
    TestUnitReady = 0x00,
    Rezero = 0x01,
    RequestSense = 0x03,
    Format = 0x04,
    ExpandedCheckTrack = 0x05,
    Read = 0x08,
    Write = 0x0a,
    Seek = 0x0b,
    ExpandedInitialize = 0x0c,
    ExpandedReadEccBurst = 0x0d,
    TranslateOrExpandedWriteBuffer = 0x0f,
    ExpandedReadBuffer = 0x10,
    WriteBuffer = 0x13,
    ReadBuffer = 0x14,
    ModeSelect = 0x15,
    ModeSense = 0x1a,
    StartStop = 0x1b,
    ReceiveDiagnostic = 0x1c,
    SendDiagnostic = 0x1d,
    ReadCapacity = 0x25,
    ReadLong = 0x28,
    WriteLong = 0x2a,
    WriteAndVerify = 0x2e,
    Verify = 0x2f,
    SearchDataEqual = 0x31,
    ExpandedRamDiagnostic = 0xe0,
    ExpandedDriveDiagnostic = 0xe3,
    ExpandedControllerDiagnostic = 0xe4
}
