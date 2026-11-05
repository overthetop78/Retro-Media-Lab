using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Pointeurs et cycles numériques du tampon de carte ; la RAM est empruntée, jamais copiée.</summary>
/// <remarks>Un appel de transfert représente un cycle RAM achevé. L'arbitrage et les fronts REQ/ACK
/// restent à fournir par le propriétaire ; cette classe ne leur substitue pas des délais muraux.</remarks>
public sealed partial class Acb4000BufferController : IAcb4000SequencerBus
{
    private readonly byte[] buffer;
    private readonly int physicalMask;
    private byte dmaControl;
    private byte size;
    private bool done;
    private bool hostPending;
    private bool hostLast;
    private bool clockedPeripheral;
    private bool peripheralReadPending;
    private bool peripheralWritePending;
    private bool peripheralReadReady;
    private bool peripheralWriteReady;
    private byte peripheralReadLatch;
    private byte peripheralWriteLatch;

    public ushort ReadPointer { get; private set; }
    public ushort WritePointer { get; private set; }
    public ushort StopPointer { get; private set; }
    public bool ResetAsserted { get; private set; } = true;
    public bool PeripheralReading => (dmaControl & C.ReadPeripheral) != 0;
    public bool HostTransferPending => hostPending;
    public bool HostTransferEnabled => !ResetAsserted && !done && !hostPending && (dmaControl &
        (PeripheralReading ? C.ReadLatch : C.WriteLatch)) != 0;
    private int PointerMask => (size << C.HighByteShift) | C.PointerLowMask;
    private ushort CpuPointer => PeripheralReading ? WritePointer : ReadPointer;

    public Acb4000BufferController(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buffer.Length);
        if ((buffer.Length & (buffer.Length - 1)) != 0) throw new ArgumentException(nameof(buffer));
        this.buffer = buffer;
        physicalMask = buffer.Length - 1;
    }

    public static bool CanRead(byte register) => register is C.HostInterfaceControl or C.DmaControl
        || register is >= C.ReadPointerLow and <= C.StopPointerHigh || register == C.CpuBuffer;

    public static bool CanWrite(byte register) => register is C.HostInterfaceControl or C.DmaControl or C.BufferSize or C.ResetControl
        || register is >= C.ReadPointerLow and <= C.StopPointerHigh || register == C.CpuBuffer;

    public bool TryRead(byte register, out byte value)
    {
        value = register switch
        {
            C.HostInterfaceControl => ReadHostInterface(),
            C.DmaControl => (byte)(dmaControl | (done ? C.DmaDone : 0)),
            C.ReadPointerLow => (byte)ReadPointer,
            C.ReadPointerHigh => (byte)(ReadPointer >> C.HighByteShift),
            C.WritePointerLow => (byte)WritePointer,
            C.WritePointerHigh => (byte)(WritePointer >> C.HighByteShift),
            C.StopPointerLow => (byte)StopPointer,
            C.StopPointerHigh => (byte)(StopPointer >> C.HighByteShift),
            C.CpuBuffer when !ResetAsserted => buffer[CpuPointer & physicalMask],
            _ => 0
        };
        return CanRead(register) && (register != C.CpuBuffer || !ResetAsserted);
    }

    public bool TryWrite(byte register, byte value)
    {
        if (!CanWrite(register)) return false;
        if (register == C.ResetControl)
        {
            ReadPointer = WritePointer = StopPointer = 0;
            done = false;
            hostPending = hostLast = false;
            CancelHostCycle();
            CancelBufferTransfers();
            ResetAsserted = (value & C.HoldReset) != 0;
            if (ResetAsserted)
            {
                dmaControl = size = 0;
                ResetHostInterface();
            }
            return true;
        }
        if (ResetAsserted) return true;
        switch (register)
        {
            case C.HostInterfaceControl: WriteHostInterface(value); break;
            case C.DmaControl: dmaControl = (byte)(value & C.WritableDmaControl); break;
            case C.BufferSize: size = value; break;
            case C.ReadPointerLow: ReadPointer = Low(ReadPointer, value); break;
            case C.ReadPointerHigh: ReadPointer = High(ReadPointer, value); break;
            case C.WritePointerLow: WritePointer = Low(WritePointer, value); break;
            case C.WritePointerHigh: WritePointer = High(WritePointer, value); break;
            case C.StopPointerLow: StopPointer = Low(StopPointer, value); done = false; break;
            case C.StopPointerHigh: StopPointer = High(StopPointer, value); done = false; break;
            case C.CpuBuffer: buffer[CpuPointer & physicalMask] = value; break;
        }
        return true;
    }

    private static ushort Low(ushort pointer, byte value) => (ushort)((pointer & C.PointerHighMask) | value);
    private static ushort High(ushort pointer, byte value) => (ushort)((pointer & C.PointerLowMask) | value << C.HighByteShift);
    private ushort Next(ushort pointer) => (ushort)((pointer + 1) & PointerMask);

    public byte ReadBuffer()
    {
        if (ResetAsserted) throw new InvalidOperationException();
        var result = buffer[ReadPointer & physicalMask];
        ReadPointer = Next(ReadPointer);
        return result;
    }

    public void WriteBuffer(byte value)
    {
        if (ResetAsserted) return;
        buffer[WritePointer & physicalMask] = value;
        WritePointer = Next(WritePointer);
    }

    public void ConfigureClockedTransfers(bool enabled)
    {
        CancelBufferTransfers();
        clockedPeripheral = enabled;
    }

    public void RequestBufferRead()
    {
        if (!clockedPeripheral || ResetAsserted) return;
        peripheralReadPending = true;
        peripheralReadReady = false;
    }

    public bool TryCompleteBufferRead(out byte value)
    {
        value = peripheralReadLatch;
        if (!clockedPeripheral) { value = ReadBuffer(); return true; }
        if (!peripheralReadReady) return false;
        peripheralReadReady = false;
        return true;
    }

    public void RequestBufferWrite(byte value)
    {
        if (!clockedPeripheral) { WriteBuffer(value); return; }
        if (ResetAsserted) return;
        peripheralWriteLatch = value;
        peripheralWritePending = true;
        peripheralWriteReady = false;
    }

    public bool TryCompleteBufferWrite()
    {
        if (!clockedPeripheral) return true;
        if (!peripheralWriteReady) return false;
        peripheralWriteReady = false;
        return true;
    }

    internal bool PeripheralCyclePending => peripheralReadPending || peripheralWritePending;
    internal uint PeripheralCycleEpoch { get; private set; }

    /// <summary>Cycle RAM périphérique effectivement accordé par le propriétaire des horloges.</summary>
    internal bool CompletePeripheralCycle()
    {
        if (ResetAsserted) return false;
        if (peripheralReadPending)
        {
            peripheralReadLatch = ReadBuffer();
            peripheralReadPending = false;
            peripheralReadReady = true;
            return true;
        }
        if (!peripheralWritePending) return false;
        WriteBuffer(peripheralWriteLatch);
        peripheralWritePending = false;
        peripheralWriteReady = true;
        return true;
    }

    public void CancelBufferTransfers()
    {
        PeripheralCycleEpoch = unchecked(PeripheralCycleEpoch + 1);
        peripheralReadPending = peripheralWritePending = false;
        peripheralReadReady = peripheralWriteReady = false;
    }

    /// <summary>Cycle périphérique achevé ; son sens vient de ROP/WOP, sans limite STOP.</summary>
    public bool TryTransferPeripheral(byte incoming, out byte outgoing)
    {
        outgoing = 0;
        if (ResetAsserted) return false;
        if (PeripheralReading) WriteBuffer(incoming);
        else outgoing = ReadBuffer();
        return true;
    }

    /// <summary>Cycle RAM de lecture vers l'hôte ; DONE attend l'acquittement complet.</summary>
    public bool TryBeginHostRead(out byte outgoing)
    {
        outgoing = 0;
        if (!PeripheralReading || !HostTransferEnabled) return false;
        hostLast = ReadPointer == StopPointer;
        outgoing = ReadBuffer();
        hostAcknowledgementSeen = false;
        hostPending = true;
        return true;
    }

    /// <summary>Cycle RAM après prélèvement de la donnée hôte à ACK ; DONE reste différé.</summary>
    public bool TryBeginHostWrite(byte incoming)
    {
        if (PeripheralReading || !HostTransferEnabled) return false;
        hostLast = WritePointer == StopPointer;
        WriteBuffer(incoming);
        hostAcknowledgementSeen = false;
        hostPending = true;
        return true;
    }

    public bool CompleteHostAcknowledgement()
    {
        if (!hostPending) return false;
        hostPending = false;
        done = hostLast;
        hostLast = false;
        return true;
    }

    /// <summary>Bus hôte libéré avant la fin du cycle ; aucune complétion DMA ni annulation des écritures RAM.</summary>
    public void CancelHostTransfer()
    {
        CancelHostCycle();
        hostPending = hostLast = done = false;
        dmaControl &= unchecked((byte)~(C.ReadLatch | C.WriteLatch));
    }

    /// <summary>Échange hôte complet, pour un propriétaire qui a déjà achevé REQ/ACK.</summary>
    public bool TryTransferHost(byte incoming, out byte outgoing)
    {
        outgoing = 0;
        if (!(PeripheralReading ? TryBeginHostRead(out outgoing) : TryBeginHostWrite(incoming))) return false;
        return CompleteHostAcknowledgement();
    }
}
