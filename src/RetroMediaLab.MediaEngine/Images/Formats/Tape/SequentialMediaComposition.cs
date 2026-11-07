using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Amstrad;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Atari;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Commodore;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Msx;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Spectrum;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Acorn;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Amstrad;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Atari;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Commodore;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Msx;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Spectrum;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape;

/// <summary>Assembles the sequential media codecs shared by conversion and exploration.</summary>
public sealed class SequentialMediaComposition
{
    private SequentialMediaComposition(SequentialDecoderRegistry decoders, SequentialEncoderRegistry encoders)
    {
        Decoders = decoders;
        Encoders = encoders;
    }

    public SequentialDecoderRegistry Decoders { get; }

    public SequentialEncoderRegistry Encoders { get; }

    public static SequentialMediaComposition CreateDefault() => new(
        new SequentialDecoderRegistry(
        [
            new AtariTapeDecoder(),
            new AmstradCpcTapeDecoder(),
            new SpectrumTapeDecoder(),
            new CommodoreTapeDecoder(),
            new MsxTapeDecoder(),
            new AcornTapeDecoder()
        ]),
        new SequentialEncoderRegistry(
        [
            new AtariTapeEncoder(),
            new AmstradCpcTapeEncoder(),
            new SpectrumTapeEncoder(),
            new CommodoreTapeEncoder(),
            new MsxTapeEncoder(),
            new AcornTapeEncoder()
        ]));
}
