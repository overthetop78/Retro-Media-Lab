using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Decoding;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Decoding.Sectors;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Inspection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Recognition;
using RetroMediaLab.MediaEngine.Interfaces.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Reading.Documents;
using RetroMediaLab.MediaEngine.Images.Reading.Metadata;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition.Msx;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition.Normalizers;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition.Policies;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using FileSystemRegistry = RetroMediaLab.MediaFileSystems.Exploration.SectorFileSystemRegistry;

namespace RetroMediaLab.MediaEngine.Images.Reading;

/// <summary>Assemble la lecture de l'image et les services auxquels l'explorateur délègue.</summary>
internal static class DiskImageExplorerFactory
{
    public static DiskImageExplorer CreateDefaultExplorer()
    {
        var recognition = MediaRecognitionComposition.CreateDefault();
        var fileSystems = CreateFileSystems();
        var (interpretations, documents) = CreateInterpretations(fileSystems);
        var decoding = new ScpSectorDecodingComposition(recognition.ScpReader, fileSystems);
        var scpExploration = CreateScpExploration(recognition.ScpReader, decoding.Decoders, decoding.Candidates, fileSystems, interpretations, documents);
        return new(recognition.ReadingService, fileSystems, scpExploration, interpretations, documents);
    }

    /// <summary>CrÃ©e l'unique registre des lecteurs de systÃ¨mes de fichiers.</summary>
    private static FileSystemRegistry CreateFileSystems() => new();

    /// <summary>CrÃ©e le service d'interprÃ©tation partagÃ© par les explorateurs gÃ©nÃ©ral et SCP.</summary>
    private static (DiskImageInterpretationService Interpretations, DiskImageDocumentFactory Documents) CreateInterpretations(FileSystemRegistry fileSystems)
    {
        var msxInterpreter = new MsxSectorImageInterpreter();
        IRecognizedImageNormalizer[] normalizerPolicies = [new MacRecognizedImageNormalizer(), new MsxRecognizedImageNormalizer(msxInterpreter), new AtariRecognizedImageNormalizer()];
        IAdditionalImageInterpretationPolicy[] additionalPolicies = [new IbmAdditionalImageInterpretationPolicy(fileSystems.SupportedFormatIds), new MsxAdditionalImageInterpretationPolicy(msxInterpreter), new CompatibleFormatInterpretationPolicy()];
        var normalizers = new RecognizedImageNormalizerRegistry(normalizerPolicies);
        var additionalInterpretations = new AdditionalImageInterpretationRegistry(additionalPolicies);
        var metadata = new DiskImageMetadataFactory(new DiskSystemResolver(), new DiskProtectionResolver());
        return (new(normalizers, additionalInterpretations), new(metadata));
    }

    /// <summary>CrÃ©e la dÃ©tection de famille et les deux parcours d'exploration SCP avec leurs instances partagÃ©es.</summary>
    private static ScpImageExplorationService CreateScpExploration(ScpReader scpReader, FluxDecoderRegistry decoders, ScpCandidateRegistry candidates, FileSystemRegistry fileSystems, DiskImageInterpretationService interpretations, DiskImageDocumentFactory documents)
    {
        var automatic = new ScpAutomaticImageExplorer(scpReader, candidates, new ScpFamilyProbe(scpReader, decoders), new ScpCandidateInspector(fileSystems, interpretations), documents);
        return new(automatic, new ScpSectorImageReader(candidates, fileSystems));
    }

}
