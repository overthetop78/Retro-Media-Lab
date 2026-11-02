namespace RetroMediaLab.Emulation.Acorn.Common.Constants;

internal static class EmulationModuleConstants
{
    /// <summary>Identifiant stable du module d’émulation.</summary>
    internal const string ModuleId = "acorn";
    /// <summary>Clé de traduction du nom de la famille de machines du module.</summary>
    internal const string ResourceFamily = "Emulation.Family.Acorn";
    /// <summary>Chemin du dossier des Assets externes du module, relatif au dossier racine de l’application.</summary>
    internal const string AssetPathPrefix = "Assets/Emulation/acorn";
    /// <summary>Chemin relatif de l’image BBC Micro partagée entre les modèles A et B.</summary>
    internal const string BbcMicroImagePath = AssetPathPrefix + "/Machines/BbcMicro.png";
    internal const string Master128ImagePath = AssetPathPrefix + "/Machines/BbcMaster128.png";
    internal const string System1ImagePath = AssetPathPrefix + "/Machines/AcornSystem1.png";
    internal const string System2ImagePath = AssetPathPrefix + "/Machines/AcornSystem2.png";
    internal const string System3ImagePath = AssetPathPrefix + "/Machines/AcornSystem3.png";
    internal const string System4ImagePath = AssetPathPrefix + "/Machines/AcornSystem4.png";
    internal const string System5ImagePath = AssetPathPrefix + "/Machines/AcornSystem5.png";
    internal const string AtomImagePath = AssetPathPrefix + "/Machines/AcornAtom.png";
    internal const string ElectronImagePath = AssetPathPrefix + "/Machines/AcornElectron.png";
    internal const string ArchimedesImagePath = AssetPathPrefix + "/Machines/AcornArchimedes.png";
    internal const string A3000ImagePath = AssetPathPrefix + "/Machines/AcornA3000.png";
    /// <summary>Préfixe des ressources de traduction intégrées au module.</summary>
    internal const string LocalizationResourceName = "RetroMediaLab.Emulation.Acorn.Resources.Emulation";
    /// <summary>Nom du sous-dossier des configurations de machines du module.</summary>
    internal const string ConfigurationsDirectoryName = "Configurations";
    /// <summary>Nom du sous-dossier des cœurs d’émulation.</summary>
    internal const string CoreDirectoryName = "Core";
}

