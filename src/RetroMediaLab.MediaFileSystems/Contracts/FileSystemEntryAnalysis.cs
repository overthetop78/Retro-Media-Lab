#if RetroMediaLab_MEDIA_ENGINE_EXPLORER
namespace RetroMediaLab.MediaEngine.Contracts.Explorer;

#else
namespace RetroMediaLab.MediaFileSystems.Contracts;

#endif
/// <summary>Identifiants d'affichage d'une entrée extraits de la réponse de MediaAnalysis.</summary>
public sealed record FileSystemEntryAnalysis(string CategoryId, string IconId, string TypeResourceKey, string Extension, string ExecutionKindId, string ContentFormatId, string TextEncodingId, string PreviewKindId);
