namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;

/// <summary>Cycle de tampon parallèle du séquenceur ; son propriétaire garde la RAM et son adressage.</summary>
public interface IAcb4000SequencerBus
{
    byte ReadBuffer();
    void WriteBuffer(byte value);
    /// <summary>Le montage peut décomposer un cycle ; les bancs de cycle complet conservent leur contrat.</summary>
    void ConfigureClockedTransfers(bool enabled) { }
    void RequestBufferRead() { }
    bool TryCompleteBufferRead(out byte value) { value = ReadBuffer(); return true; }
    void RequestBufferWrite(byte value) => WriteBuffer(value);
    bool TryCompleteBufferWrite() => true;
    void CancelBufferTransfers() { }
}
