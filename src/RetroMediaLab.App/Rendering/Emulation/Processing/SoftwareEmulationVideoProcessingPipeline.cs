using RetroMediaLab.App.Constants.Rendering.Emulation;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed partial class SoftwareEmulationVideoProcessingPipeline : IEmulationVideoProcessingPipeline
{
    private byte[]? _deditheringWorkspace;
    private float[]? _fixedPixelHistory;
    private int _historyWidth;
    private int _historyHeight;
    private TimeSpan _historyTimestamp;
    private float[]? _vectorHistory;
    private int _vectorHistoryWidth;
    private int _vectorHistoryHeight;
    private long _vectorHistorySequence;
    private float[]? _dotMatrixHistory;
    private int _dotMatrixHistoryWidth;
    private int _dotMatrixHistoryHeight;
    private TimeSpan _dotMatrixHistoryTimestamp;
    private float[]? _segmentDisplayHistory;
    private int _segmentDisplayHistoryWidth;
    private int _segmentDisplayHistoryHeight;
    private TimeSpan _segmentDisplayHistoryTimestamp;
    private float[]? _ePaperHistory;
    private int _ePaperHistoryWidth;
    private int _ePaperHistoryHeight;
    private TimeSpan _ePaperHistoryTimestamp;
    private readonly FilterPlasmaPersistence _plasmaPersistence = new();
    private readonly FilterVfdPersistence _vfdPersistence = new();
    private TimeSpan _signalTimestamp;
    private long _signalSequence;

    public EmulationVideoRenderer Renderer => EmulationVideoRenderer.Wpf;

    public VideoFrame Process(EmulationVideoProcessingConfiguration configuration, VideoFrame frame, EmulationVideoProcessingSize sourceSize, EmulationVideoProcessingSize outputSize)
    {
        var normalized = EmulationVideoProcessingConfigurationFunctions.ForRendering(configuration);
        Validate(frame, sourceSize, outputSize);
        var sameSize = sourceSize == outputSize;
        if (sameSize && normalized.Adjustments == new EmulationImageAdjustments()
            && (normalized.Restoration with { Deinterlacing = EmulationDeinterlacingMode.Off }) == new EmulationImageRestorationConfiguration()
            && normalized.SignalSimulation == new EmulationSignalSimulationConfiguration()
            && normalized.Stylistic == new EmulationStylisticVideoConfiguration()
            && normalized.DisplayTechnology == EmulationVideoDisplayTechnology.Normal)
        {
            ResetHistory();
            _plasmaPersistence.Reset();
            ResetVectorHistory();
            _vfdPersistence.Reset();
            ResetDotMatrixHistory();
            ResetSegmentDisplayHistory();
            ResetEPaperHistory();
            return frame;
        }

        var pixels = EmulationVideoPixelFunctions.ToBgra32(frame);
        var prepared = EmulationImageRestorationFunctions.ApplyDedithering(pixels, frame.Width, frame.Height,
            normalized.Restoration.Dedithering, ref _deditheringWorkspace);
        var linear = ToLinear(prepared ?? pixels, frame.Width, frame.Height);
        var reference = normalized.Restoration.DetailRecovery > 0 && !sameSize
            && normalized.Sampling != EmulationVideoSampling.Nearest ? linear.ToArray() : null;
        EmulationImageRestorationFunctions.ApplyDebanding(linear, frame.Width, frame.Height, normalized.Restoration.Debanding);
        var signalStandard = ResolveSignalStandard(frame, normalized.SignalSimulation.Standard);
        ApplySignalConnection(linear, frame.Width, frame.Height, frame.Sequence, normalized.SignalSimulation, signalStandard);
        ApplySignalStandard(linear, frame.Width, frame.Height, normalized.SignalSimulation, signalStandard);
        linear = Resample(linear, frame.Width, frame.Height, outputSize.Width, outputSize.Height, normalized.Sampling);
        if (reference is not null)
            EmulationImageRestorationFunctions.ApplyDetailRecovery(reference, frame.Width, frame.Height,
                linear, outputSize.Width, outputSize.Height, normalized.Restoration.DetailRecovery);
        VideoContrastParameterFunctions.Apply(linear, normalized.Adjustments.Contrast);
        VideoBrightnessParameterFunctions.Apply(linear, normalized.Adjustments.Brightness);
        VideoGammaParameterFunctions.Apply(linear, normalized.Adjustments.Gamma);
        VideoSaturationParameterFunctions.Apply(linear, normalized.Adjustments.Saturation);
        VideoSharpnessParameterFunctions.Apply(linear, outputSize.Width, outputSize.Height, normalized.Adjustments.Sharpness);
        var crt = normalized.DisplayTechnology == EmulationVideoDisplayTechnology.Crt
            ? CrtSignal.From(normalized.Crt, frame) : default;
        ApplyDisplayTechnology(linear, sourceSize.Width, sourceSize.Height, outputSize.Width, outputSize.Height, frame, normalized, crt);
        FilterVhs.Apply(linear, outputSize.Width, outputSize.Height, frame.Sequence, normalized.Stylistic.Vhs);
        FilterChromaticAberration.Apply(linear, outputSize.Width, outputSize.Height, normalized.Stylistic.ChromaticAberration);
        FilterBloom.Apply(linear, outputSize.Width, outputSize.Height, normalized.Stylistic.Bloom);
        FilterSepia.Apply(linear, normalized.Stylistic.Sepia);
        FilterGrain.Apply(linear, outputSize.Width, outputSize.Height, frame.Sequence, normalized.Stylistic.Grain);
        ApplyFixedPixelTemporal(normalized, linear, outputSize.Width, outputSize.Height, frame.Timestamp);
        if (normalized.DisplayTechnology == EmulationVideoDisplayTechnology.Plasma)
            _plasmaPersistence.Apply(linear, outputSize.Width, outputSize.Height, frame.Sequence, normalized.Plasma.PersistenceIntensity);
        else
            _plasmaPersistence.Reset();
        ApplyVectorTemporal(normalized, linear, outputSize.Width, outputSize.Height, frame.Sequence);
        if (normalized.DisplayTechnology == EmulationVideoDisplayTechnology.Vfd)
            _vfdPersistence.Apply(linear, outputSize.Width, outputSize.Height, frame.Timestamp, normalized.Vfd.PersistenceMilliseconds);
        else
            _vfdPersistence.Reset();
        ApplyDotMatrixTemporal(normalized, linear, outputSize.Width, outputSize.Height, frame.Timestamp);
        ApplySegmentDisplayTemporal(normalized, linear, outputSize.Width, outputSize.Height, frame.Timestamp);
        ApplyEPaperTemporal(normalized, linear, outputSize.Width, outputSize.Height, frame.Timestamp);
        if (normalized.DisplayTechnology == EmulationVideoDisplayTechnology.Crt)
            CrtPhosphorMask.Apply(linear, outputSize.Width, outputSize.Height, crt);
        var output = ToSrgb(linear, outputSize.Width, outputSize.Height);
        return frame with
        {
            Pixels = output,
            Width = outputSize.Width,
            Height = outputSize.Height,
            Pitch = checked(outputSize.Width * EmulationVideoPixelConstants.BytesPerBgraPixel),
            PixelFormat = EmulationPixelFormat.Xrgb8888
        };
    }

    private static void ApplySignalConnection(float[] colors, int width, int height, long sequence, EmulationSignalSimulationConfiguration signal, EmulationSignalStandard standard)
    {
        switch (signal.Connection)
        {
            case EmulationSignalConnection.None:
                break;
            case EmulationSignalConnection.RgbScart:
                SignalConnectionRgbScart.Apply(colors, width, height, signal.ConnectionIntensity);
                break;
            case EmulationSignalConnection.Component:
                SignalConnectionComponent.Apply(colors, width, height, signal.ConnectionIntensity);
                break;
            case EmulationSignalConnection.SVideo:
                SignalConnectionSVideo.Apply(colors, width, height, signal.ConnectionIntensity);
                break;
            case EmulationSignalConnection.Composite:
                SignalConnectionComposite.Apply(colors, width, height, sequence, signal.ConnectionIntensity);
                break;
            case EmulationSignalConnection.Rf:
                SignalConnectionRf.Apply(colors, width, height, sequence, signal.ConnectionIntensity, standard);
                break;
        }
    }

    private void ApplySignalStandard(float[] colors, int width, int height, EmulationSignalSimulationConfiguration signal, EmulationSignalStandard standard)
    {
        switch (standard)
        {
            case EmulationSignalStandard.Pal:
                SignalStandardPal.Apply(colors, width, height, signal.StandardIntensity);
                break;
            case EmulationSignalStandard.Ntsc:
                SignalStandardNtsc.Apply(colors, width, height, signal.StandardIntensity);
                break;
            case EmulationSignalStandard.Secam:
                SignalStandardSecam.Apply(colors, width, height, signal.StandardIntensity);
                break;
        }
    }

    private EmulationSignalStandard ResolveSignalStandard(VideoFrame frame, EmulationSignalStandard standard)
    {
        if (standard == EmulationSignalStandard.Automatic)
        {
            var elapsed = frame.Sequence > _signalSequence ? (frame.Timestamp - _signalTimestamp).TotalMilliseconds : 0;
            standard = elapsed > SoftwareVideoProcessingConstants.PalFrameDurationThresholdMilliseconds ? EmulationSignalStandard.Pal : EmulationSignalStandard.Ntsc;
        }
        _signalTimestamp = frame.Timestamp;
        _signalSequence = frame.Sequence;
        return standard;
    }

    internal static float SrgbToLinear(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value <= SoftwareVideoProcessingConstants.SrgbToLinearThreshold ? value / SoftwareVideoProcessingConstants.SrgbLinearScale : MathF.Pow((value + SoftwareVideoProcessingConstants.SrgbOffset) / SoftwareVideoProcessingConstants.SrgbScale, SoftwareVideoProcessingConstants.SrgbGamma);
    }

    internal static float LinearToSrgb(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value <= SoftwareVideoProcessingConstants.LinearToSrgbThreshold ? value * SoftwareVideoProcessingConstants.SrgbLinearScale : SoftwareVideoProcessingConstants.SrgbScale * MathF.Pow(value, 1f / SoftwareVideoProcessingConstants.SrgbGamma) - SoftwareVideoProcessingConstants.SrgbOffset;
    }

    public void Dispose() => ResetTemporalHistory();

}
