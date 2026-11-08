using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.ScsiHostAdapterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;

/// <summary>Équations des buffers et bascules de l'Acorn Host Adapter, à l'issue d'un strobe de bus valide.</summary>
/// <remarks>Les sorties booléennes représentent une assertion, pas une tension. Le propriétaire résout le bus
/// partagé et assure les délais de la cible. Aucun fichier, média, cible ou mécanisme n'est possédé ici.</remarks>
public sealed class ScsiHostAdapter : IAcornScsiHostAdapter
{
    private const ScsiBusSignals StatusSignals = ScsiBusSignals.Message | ScsiBusSignals.Busy
        | ScsiBusSignals.Request | ScsiBusSignals.InputOutput | ScsiBusSignals.ControlData;
    private byte _targetData;
    private (byte Data, bool Driven, bool Select, bool Acknowledge, bool Reset) _publishedOutputs
        = (C.InitialDataLatch, true, false, false, false);
    /// <summary>Changement des niveaux ou du pilotage du bus ; aucun rappel à état identique.</summary>
    public event Action? OutputsChanged;
    public byte OutputData { get; private set; } = C.InitialDataLatch;
    public bool DrivesData => (TargetSignals & ScsiBusSignals.InputOutput) == 0;
    public bool Select { get; private set; }
    public bool Acknowledge { get; private set; }
    public bool IrqEnabled { get; private set; }
    public bool Irq { get; private set; }
    public Action<bool>? IrqChanged { get; set; }
    public bool ResetAsserted { get; private set; }
    public ScsiBusSignals TargetSignals { get; private set; }

    public void SetTarget(ScsiBusSignals signals, byte data)
    {
        TargetSignals = signals & StatusSignals;
        _targetData = data;
        // Asynchronous /CLR of the two halves of IC11.
        if ((signals & ScsiBusSignals.Busy) != 0) Select = false;
        if ((signals & ScsiBusSignals.Request) == 0) Acknowledge = false;
        UpdateInterruptLatch();
        PublishOutputs();
    }

    public void SetReset(bool asserted)
    {
        ResetAsserted = asserted;
        if (asserted)
        {
            IrqEnabled = Select = false;
            SetIrq(false);
        }
        // Neither the data latch nor the ACK flip-flop has an NRST connection.
        // The target's response to RST releases REQ and therefore clears ACK via SetTarget.
        PublishOutputs();
    }

    public byte Read(int register)
    {
        switch (register & C.RegisterMask)
        {
            case C.DataRegister:
                StrobeAcknowledge();
                // Without target data drivers the host output latch is read back through the buffers.
                return DrivesData ? OutputData : _targetData;
            case C.StatusRegister:
                return (byte)((byte)TargetSignals | (Irq ? C.InterruptStatusMask : 0));
            default:
                return C.OpenBus;
        }
    }

    public void Write(int register, byte value)
    {
        switch (register & C.RegisterMask)
        {
            case C.DataRegister:
                OutputData = value;
                StrobeAcknowledge();
                break;
            case C.SelectRegister:
                Select = !ResetAsserted && (TargetSignals & ScsiBusSignals.Busy) == 0;
                PublishOutputs();
                break;
            case C.InterruptEnableRegister:
                IrqEnabled = !ResetAsserted && (value & C.InterruptEnableMask) != 0;
                UpdateInterruptLatch();
                break;
        }
    }

    private void StrobeAcknowledge()
    {
        Acknowledge = (TargetSignals & ScsiBusSignals.Request) != 0;
        PublishOutputs();
    }

    private void PublishOutputs()
    {
        var outputs = (OutputData, DrivesData, Select, Acknowledge, ResetAsserted);
        if (_publishedOutputs == outputs) return;
        _publishedOutputs = outputs;
        OutputsChanged?.Invoke();
    }

    // IC12's NAND SR latch: REQ active sets it while enabled; only disabling / reset clears it.
    private void UpdateInterruptLatch() => SetIrq(IrqEnabled && (Irq || (TargetSignals & ScsiBusSignals.Request) != 0));

    private void SetIrq(bool asserted)
    {
        if (Irq == asserted) return;
        Irq = asserted;
        IrqChanged?.Invoke(asserted);
    }
}
