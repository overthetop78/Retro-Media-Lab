using System.Globalization;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Nec.Common.Constants;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Nec.Common.Functions.CoreHostFunctions;

namespace RetroMediaLab.Emulation.Nec.Common.Exceptions;

/// <summary>Résout les diagnostics du module avec le service public de localisation fourni par Common.</summary>
/// <remarks>Le catalogue appartient à cet assembly. La langue est relue à chaque demande ; le formatage
/// conserve la culture courante pour les valeurs et renvoie la clé si aucune traduction n'existe.</remarks>
internal static class ExceptionText
{
    private static readonly IEmulationModuleLocalization Localization = CommonCoreHostFunctions.CreateLocalization(typeof(ExceptionText).Assembly, "RetroMediaLab.Emulation.Nec.Resources.Emulation");

    /// <summary>Résout une clé puis remplace ses paramètres éventuels dans la culture courante.</summary>
    /// <param name="resourceKey">Clé exacte du texte dans les ressources du module.</param>
    /// <param name="arguments">Valeurs du diagnostic ; une liste vide laisse le texte intact, même s'il contient des accolades.</param>
    /// <returns>Texte traduit et éventuellement formaté, ou clé d'origine selon le même traitement.</returns>
    internal static string Get(string resourceKey, params object[] arguments)
    {
        var value = Localization.TryGetString(resourceKey, CultureInfo.CurrentUICulture, out var text) ? text : resourceKey;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
}