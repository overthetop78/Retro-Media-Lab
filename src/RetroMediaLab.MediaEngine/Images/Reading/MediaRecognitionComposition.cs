using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Apple;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Csw;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.AcornAtom;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Apridisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Atr;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Atx;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CommodoreDos;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Cp2;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.D64;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.D71;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.D81;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.I86f;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.KryoFlux;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Msa;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Rx02;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.St;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Xfd;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.AmstradCpr;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.AmstradRom;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.Console;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.FamicomFds;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Chd;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Qcow2;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vdi;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vhd;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vhdx;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vmdk;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.CloneCd;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Chd;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Iso;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Alcohol;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Xdvdfs;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.WiiU;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtariCas;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Audio;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.CommodoreTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.MsxCas;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Simh;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.SpectrumTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Tzx;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Uef;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Voc;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Wav;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using System.Collections.Frozen;

namespace RetroMediaLab.MediaEngine.Images.Reading;

/// <summary>Provides the registered media image readers and their recognition service.</summary>
public sealed class MediaRecognitionComposition
{
    private MediaRecognitionComposition(ScpReader scpReader, IReadOnlyList<IMediaImageReader> readers)
    {
        ScpReader = scpReader;
        Readers = readers;
        SupportedExtensions = readers.SelectMany(reader => reader.Extensions).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        Registry = new MediaRecognitionRegistry(readers);
        ReadingService = new MediaImageReadingService(Registry);
    }

    public ScpReader ScpReader { get; }

    public IReadOnlyList<IMediaImageReader> Readers { get; }

    public IReadOnlySet<string> SupportedExtensions { get; }

    public MediaRecognitionRegistry Registry { get; }

    public MediaImageReadingService ReadingService { get; }

    public static MediaRecognitionComposition CreateDefault()
    {
        var scpReader = new ScpReader();
        return new MediaRecognitionComposition(
            scpReader,
            [
                new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf.AdfReader(),
                new AmstradCprReader(),
                new AmstradRomReader(),
                new ConsoleCartridgeReader(),
                new FamicomFdsReader(),
                new AcornAtomDskReader(),
                new ApridiskReader(),
                new DefinedRawImageReader(),
                new BbcDfsReader(),
                new CoherentRawImageReader(),
                new DecRx02Reader(),
                new AtariStReader(),
                new MsaReader(),
                new AtrReader(),
                new XfdReader(),
                new AtxReader(),
                new D64Reader(),
                new D71Reader(),
                new D81Reader(),
                new AppleDiskImageReader(),
                new MsxRawImageReader(),
                new CpcDskReader(),
                new RawImgReader(),
                new IbmRawImageReader(),
                new Td0Reader(),
                new I86fReader(),
                new Cp2Reader(),
                new ImdReader(),
                new EpsonQx10RawImageReader(),
                new UcsdRawImageReader(),
                new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe.HfeReader(),
                new HxcStreamReader(),
                new KryoFluxReader(),
                scpReader,
                new BinCueReader(),
                new CloneCdReader(),
                new AlcoholMdsReader(),
                new ChdOpticalReader(),
                new GdiReader(),
                new XdvdfsReader(),
                new WiiUReader(),
                new IsoReader(),
                new WavTapeReader(),
                new CswReader(),
                new RawTapeReader(),
                new AudioTapeReader(),
                new VocReader(),
                new UefReader(),
                new AtariCasReader(),
                new TzxReader(),
                new CommodoreTapReader(),
                new MsxCasReader(),
                new SpectrumTapReader(),
                new AtomTapReader(),
                new SimhTapeReader(),
                new Qcow2Reader(),
                new VhdxReader(),
                new VhdReader(),
                new VdiReader(),
                new VmdkReader(),
                new ChdHardDiskReader(),
                new RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterReader(),
                new RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuReader(),
                new RawHardDiskReader()
            ]);
    }
}
