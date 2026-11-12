using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Constants;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Interfaces;

namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

/// <summary>Générateur TI SN76489AN : trois tons carrés et un bruit périodique ou pseudo-aléatoire.</summary>
/// <remarks>Ce composant ignore le modèle BBC, le VIA et la sortie physique. Tick reçoit une horloge d'entrée ;
/// Output représente la somme unipolaire des quatre voies avant le couplage analogique.
/// Les registres sont sélectionnés par les octets portant le bit 7 ; les octets suivants complètent le registre sélectionné.</remarks>
public sealed partial class Sn76489 : ISn76489
{
    private readonly int[] _periods = new int[Sn76489Constants.ToneChannels];
    private readonly int[] _counts = new int[Sn76489Constants.ChannelCount];
    private readonly int[] _amplitudes = new int[Sn76489Constants.ChannelCount];
    private readonly bool[] _levels = new bool[Sn76489Constants.ToneChannels];
    private int _divider;
    private int _selected;
    private int _noise;
    private int _noisePeriod;
    private int _shift;
    private int _transferClocks;
    private byte _busData;
    private bool _chipSelected;
    private bool _writeEnabled;
    private int _output;

    public Sn76489() : this(0) { }

    /// <summary>Choisit le délai CE→READY dans les bornes TI. Zéro conserve
    /// le contrat historique par clocks ; le raccordement daté choisit 90/150 ns.</summary>
    public Sn76489(int readyAssertDelayNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(readyAssertDelayNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(readyAssertDelayNanoseconds,
            Sn76489Constants.MaximumReadyAssertNanoseconds);
        _readyAssertDelay = readyAssertDelayNanoseconds;
        Reset();
    }

    /// <summary>Somme instantanée, sans duplication stéréo ni application du volume de l'hôte.</summary>
    public int Output => _output;
    public Action<int>? OutputChanged { get; set; }

    /// <summary>État relâché de READY (collecteur ouvert) ; false représente le niveau bas.</summary>
    public bool Ready => _readyOutput;

    /// <summary>Périodes d'entrée avant le prochain front interne ou la fin du transfert en cours.</summary>
    public int ClocksUntilNextEvent
    {
        get
        {
            var divided = Sn76489Constants.ClockDivider - _divider;
            return _transferClocks > 0 && _transferClocks < divided ? _transferClocks : divided;
        }
    }

    private void UpdateOutput()
    {
        // Les gains ne changent qu'à l'écriture : aucun indexeur de collection ni calcul de dB dans Tick.
        var output = (_levels[0] ? _amplitudes[0] : 0)
            + (_levels[1] ? _amplitudes[1] : 0)
            + (_levels[2] ? _amplitudes[2] : 0)
            + ((_shift & Sn76489Constants.NoiseOutputBit) != 0 ? _amplitudes[Sn76489Constants.NoiseChannel] : 0);
        if (_output == output) return;
        _output = output;
        bool wasPublishing = _publishing;
        _publishing = true;
        try { OutputChanged?.Invoke(output); }
        finally { _publishing = wasPublishing; }
    }

    /// <summary>Initialisation du modèle ; le circuit ne possède pas de broche RESET externe.</summary>
    public void Reset()
    {
        _hasClockSource = false;
        Array.Clear(_periods);
        Array.Clear(_counts);
        Array.Clear(_levels);
        // Les atténuateurs TI démarrent à zéro dB ; le logiciel les programme ensuite.
        Array.Fill(_amplitudes, Sn76489Constants.Amplitudes[0]);
        _divider = 0;
        _selected = 0;
        _noise = 0;
        _noisePeriod = FixedNoisePeriod();
        _shift = Sn76489Constants.NoiseSeed;
        _transferClocks = 0;
        _busData = 0;
        _chipSelected = false;
        _writeEnabled = false;
        _directTransfer = false;
        _readyOutput = true;
        _readyDue = null;
        _inputClockCount = 0;
        _clockHigh = false;
        UpdateOutput();
        PublishReady();
        PublishReadyDeadline();
    }

    /// <summary>Accepte un octet du bus lorsque le câblage propriétaire valide une écriture.</summary>
    public void Write(byte value)
    {
        ValidateReadyAssertion();
        _directTransfer = true;
        _transferClocks = Sn76489Constants.TransferClocks;
        ApplyData(value);
        ResolveReady();
        UpdateOutput();
        PublishReady();
        PublishReadyDeadline();
    }

    private void ApplyData(byte value)
    {
        var latch = (value & Sn76489Constants.LatchFlag) != 0;
        if (latch)
            _selected = (value >> Sn76489Constants.RegisterShift) & Sn76489Constants.RegisterMask;
        var channel = _selected >> Sn76489Constants.ChannelShift;
        if ((_selected & Sn76489Constants.AttenuationRegisterFlag) != 0)
            _amplitudes[channel] = Sn76489Constants.Amplitudes[value & Sn76489Constants.LowDataMask];
        else if (channel == Sn76489Constants.NoiseChannel)
        {
            _noise = value & Sn76489Constants.NoiseControlMask;
            _shift = Sn76489Constants.NoiseSeed;
            UpdateNoisePeriod();
        }
        else
        {
            _periods[channel] = latch
                ? (_periods[channel] & Sn76489Constants.HighPeriodMask) | (value & Sn76489Constants.LowDataMask)
                : (_periods[channel] & Sn76489Constants.LowDataMask)
                    | ((value & Sn76489Constants.HighDataMask) << Sn76489Constants.PeriodDataShift);
            if (channel == Sn76489Constants.ToneChannels - 1
                && (_noise & Sn76489Constants.NoiseRateMask) == Sn76489Constants.NoiseToneRate)
                UpdateNoisePeriod();
        }
    }

    /// <summary>Niveaux du bus : true signifie CE/WE actifs (broches basses). Le bus reste présenté jusqu'au prochain appel.</summary>
    /// <remarks>Les registres capturent le bus à l'horloge interne, même sans nouveau front de WE.
    /// La carte conserve la responsabilité de la stabilité des données pendant le transfert.</remarks>
    public void SetBus(byte value, bool chipSelected, bool writeEnabled)
    {
        if (chipSelected && (!_chipSelected || writeEnabled && !_writeEnabled))
        {
            ValidateReadyAssertion();
            _transferClocks = Sn76489Constants.TransferClocks;
        }
        _directTransfer = false;
        _busData = value;
        _chipSelected = chipSelected;
        _writeEnabled = writeEnabled;
        ResolveReady();
        PublishReady();
        PublishReadyDeadline();
    }

    /// <summary>Avance d'une période d'entrée ; les compteurs internes travaillent après division par seize.</summary>
    public void Tick()
    {
        if (_publishing) throw new InvalidOperationException("Cannot clock the PSG from an output callback.");
        if (!_advancingClockSource) DetachClockSource();
        _inputClockCount = checked(_inputClockCount + 1);
        bool completed = _transferClocks == 1;
        if (_transferClocks != 0)
            --_transferClocks;
        bool divided = ++_divider == Sn76489Constants.ClockDivider;
        if (divided) { _divider = 0; ClockDivided(); }
        if (completed) ResolveReady();
        if (divided) UpdateOutput();
        if (completed) { PublishReady(); PublishReadyDeadline(); }
    }

    /// <summary>Avance des périodes complètes sans entrée intermédiaire ; chaque front interne garde son ordre.</summary>
    public void AdvanceClocks(int clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_publishing && clocks != 0) throw new InvalidOperationException("Cannot clock the PSG from an output callback.");
        if (!_advancingClockSource && clocks != 0) DetachClockSource();
        _ = checked(_inputClockCount + clocks);
        while (clocks > 0)
        {
            int distance = Math.Min(clocks, Sn76489Constants.ClockDivider - _divider);
            if (_transferClocks > 0) distance = Math.Min(distance, _transferClocks);
            bool completed = _transferClocks > 0 && distance == _transferClocks;
            _inputClockCount += distance;
            if (_transferClocks > 0) _transferClocks -= distance;
            clocks -= distance;
            _divider += distance;
            bool divided = _divider == Sn76489Constants.ClockDivider;
            if (divided) { _divider = 0; ClockDivided(); }
            if (completed) ResolveReady();
            if (divided) UpdateOutput();
            if (completed) { PublishReady(); PublishReadyDeadline(); }
        }
    }

    private void ClockDivided()
    {
        for (var channel = 0; channel < Sn76489Constants.ToneChannels; channel++)
        {
            if (--_counts[channel] > 0)
                continue;
            _levels[channel] = !_levels[channel];
            _counts[channel] = Period(channel);
        }
        if (--_counts[Sn76489Constants.NoiseChannel] <= 0)
        {
            var feedback = (_shift & Sn76489Constants.NoiseFeedbackTap) != 0;
            if ((_noise & Sn76489Constants.WhiteNoiseFlag) != 0)
                feedback ^= (_shift & Sn76489Constants.WhiteNoiseFeedbackTap) != 0;
            _shift = (_shift >> 1) | (feedback ? Sn76489Constants.NoiseSeed : 0);
            _counts[Sn76489Constants.NoiseChannel] = _noisePeriod;
        }
        if (_chipSelected && _writeEnabled)
            ApplyData(_busData);
    }

    /// <summary>Résout le diviseur TI ; fréquence du ton = horloge / (32 × période).</summary>
    private int Period(int channel) => _periods[channel] == 0 ? Sn76489Constants.ZeroPeriod : _periods[channel];

    private int FixedNoisePeriod() => 1 << (Sn76489Constants.FixedNoisePeriodShift
        + (_noise & Sn76489Constants.NoiseRateMask));

    private void UpdateNoisePeriod() => _noisePeriod = (_noise & Sn76489Constants.NoiseRateMask) == Sn76489Constants.NoiseToneRate
        ? Sn76489Constants.ToneNoisePeriodMultiplier * Period(Sn76489Constants.ToneChannels - 1)
        : FixedNoisePeriod();
}
