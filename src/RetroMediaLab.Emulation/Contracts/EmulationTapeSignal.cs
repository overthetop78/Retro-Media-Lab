namespace RetroMediaLab.Emulation.Contracts;

/// <summary>Signal mono d'une image tape, préparé par MediaEngine derrière les interfaces du SDK.</summary>
/// <param name="Samples">Niveaux PCM signés sur seize bits, conservés en lecture seule par le consommateur.</param>
/// <param name="SampleRate">Cadence du signal enregistré, indépendante du débit de sortie sonore du cœur.</param>
/// <param name="BlockPositions">Repères des blocs en échantillons depuis le début de la bande.</param>
/// <param name="Blocks">Blocs décodés et validés pour les modes directs explicitement gérés par la machine.</param>
public sealed record EmulationTapeSignal(ReadOnlyMemory<short> Samples, int SampleRate, IReadOnlyList<int>? BlockPositions = null, IReadOnlyList<EmulationTapeBlock>? Blocks = null);

/// <summary>Données de bloc et repère de bande, sans exposer les objets de MediaEngine aux modules.</summary>
public sealed record EmulationTapeBlock(int SamplePosition, ReadOnlyMemory<byte> Data, bool? IntegrityValid, IReadOnlyDictionary<string, string> Metadata);
