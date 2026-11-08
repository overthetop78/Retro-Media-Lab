using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000BufferController
{
    private byte hostInterfaceControl;
    private bool hostSelectInput;
    private bool hostBusyInput;
    private bool hostAcknowledgeInput;
    private bool hostAcknowledgementSeen;
    private uint hostArbitrationElapsed;
    private byte publishedHostOutputs;
    internal event Action? HostOutputsChanged;

    private void PublishHostOutputs()
    {
        var outputs = HostOutputState;
        if (publishedHostOutputs == outputs) return;
        publishedHostOutputs = outputs;
        HostOutputsChanged?.Invoke();
    }

    public bool HostArbitrationPending { get; private set; }
    public bool HostSelectOutput => !ResetAsserted && (hostInterfaceControl & C.HostSelectOutput) != 0;
    public bool HostBusyOutput => !ResetAsserted && (hostInterfaceControl & C.HostBusyOutput) != 0;
    public bool HostInputEnabled => !ResetAsserted && ((hostInterfaceControl & C.HostInputEnable) != 0
        || !PeripheralReading && HostTransferEnabled);
    public bool HostOutputEnabled => (HostOutputState & C.HostOutputEnable) != 0;
    internal byte HostOutputState => ResetAsserted ? (byte)0 : (byte)((hostInterfaceControl & C.HostDrivenOutputs)
        | (PeripheralReading && hostPending && !hostAcknowledgeInput && !hostAcknowledgementSeen ? C.HostOutputEnable : 0));

    /// <summary>Niveaux empruntés des broches ; RESET des registres ne les détruit pas.</summary>
    public void SetHostBusInputs(bool select, bool busy, bool acknowledge = false)
    {
        hostSelectInput = select;
        hostBusyInput = busy;
        hostAcknowledgeInput = acknowledge;
        if (acknowledge && hostPending) hostAcknowledgementSeen = true;
        if (select || busy) hostArbitrationElapsed = 0;
        PublishHostOutputs();
    }

    /// <summary>Délai d'arbitrage après libération de SEL-IN et BSY-IN, exprimé en temps matériel.</summary>
    public void AdvanceHostInterfaceNanoseconds(uint nanoseconds)
    {
        if (!HostArbitrationPending || ResetAsserted || hostSelectInput || hostBusyInput) return;
        if (nanoseconds < C.HostArbitrationNanoseconds - hostArbitrationElapsed)
        {
            hostArbitrationElapsed += nanoseconds;
            return;
        }
        hostInterfaceControl |= C.HostBusyOutput | C.HostOutputEnable;
        HostArbitrationPending = false;
        hostArbitrationElapsed = 0;
        PublishHostOutputs();
    }

    private byte ReadHostInterface() => (byte)(hostInterfaceControl
        | (hostSelectInput ? C.HostSelectInput : 0) | (hostBusyInput ? C.HostBusyInput : 0)
        | (HostBusyOutput ? C.HostArbitrate : 0));

    private void WriteHostInterface(byte value)
    {
        hostInterfaceControl = (byte)(value & C.HostOutputLatches);
        HostArbitrationPending = (value & C.HostArbitrate) != 0;
        hostArbitrationElapsed = 0;
    }

    private void ResetHostInterface()
    {
        hostInterfaceControl = 0;
        hostAcknowledgementSeen = false;
        HostArbitrationPending = false;
        hostArbitrationElapsed = 0;
    }
}
