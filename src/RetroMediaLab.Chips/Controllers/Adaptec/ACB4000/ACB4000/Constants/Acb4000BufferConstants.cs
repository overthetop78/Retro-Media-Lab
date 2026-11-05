namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Registres du tampon ACB-4000, fiche AIC-300 pages 9–12 et 16.</summary>
public static class Acb4000BufferConstants
{
    public const byte HostData = 0x50;
    public const byte HostInterfaceControl = 0x52;
    public const byte HostArbitrate = 0x01;
    public const byte HostInputEnable = 0x04;
    public const byte HostOutputEnable = 0x08;
    public const byte HostSelectInput = 0x10;
    public const byte HostBusyInput = 0x20;
    public const byte HostSelectOutput = 0x40;
    public const byte HostBusyOutput = 0x80;
    internal const byte HostOutputLatches = HostInputEnable | HostOutputEnable | HostSelectOutput | HostBusyOutput;
    internal const byte HostDrivenOutputs = HostOutputEnable | HostSelectOutput | HostBusyOutput;
    internal const uint HostArbitrationNanoseconds = 100;
    public const byte DmaControl = 0x53;
    public const byte BufferSize = 0x54;
    public const byte ResetControl = 0x59;
    public const byte ReadPointerLow = 0x5a;
    public const byte ReadPointerHigh = 0x5b;
    public const byte WritePointerLow = 0x5c;
    public const byte WritePointerHigh = 0x5d;
    public const byte StopPointerLow = 0x5e;
    public const byte StopPointerHigh = 0x5f;
    public const byte CpuBuffer = 0x70;
    public const byte WriteLatch = 0x04;
    public const byte ReadLatch = 0x08;
    public const byte ReadPeripheral = 0x10;
    public const byte DmaDone = 0x20;
    public const byte EnableInitiator = 0x40;
    public const byte EnableTarget = 0x80;
    public const byte WritableDmaControl = WriteLatch | ReadLatch | ReadPeripheral | EnableInitiator | EnableTarget;
    public const byte HoldReset = 0x01;
    internal const byte HostTargetControl = EnableInitiator | EnableTarget;
    internal const byte HostReadPreparation = HostTargetControl | ReadPeripheral;
    internal const byte HostReadControl = HostReadPreparation | ReadLatch;
    internal const byte HostWriteControl = HostTargetControl | WriteLatch;
    public const int HighByteShift = 8;
    public const int PointerLowMask = 0xff;
    public const ushort PointerHighMask = 0xff00;
    internal const byte FirmwareBufferSize = 0x03;
    internal const byte FirmwareDmaControl = EnableInitiator;
}
