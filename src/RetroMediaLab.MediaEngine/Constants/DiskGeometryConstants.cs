namespace RetroMediaLab.MediaEngine.Constants;

/// <summary>Regroupe les dimensions géométriques conventionnelles réellement partagées par plusieurs familles de médias.</summary>
internal static class DiskGeometryConstants
{
    /// <summary>Nombre de cylindres d'une géométrie conventionnelle à 40 pistes.</summary>
    public const int FortyTrackCylinderCount = RetroMediaLab.MediaFileSystems.Constants.DiskGeometryConstants.FortyTrackCylinderCount;

    /// <summary>Nombre de cylindres d'une géométrie conventionnelle à 80 pistes.</summary>
    public const int EightyTrackCylinderCount = RetroMediaLab.MediaFileSystems.Constants.DiskGeometryConstants.EightyTrackCylinderCount;

    /// <summary>Nombre de têtes d'un média simple face.</summary>
    public const int SingleSidedHeadCount = RetroMediaLab.MediaFileSystems.Constants.DiskGeometryConstants.SingleSidedHeadCount;

    /// <summary>Nombre de têtes d'un média double face.</summary>
    public const int DoubleSidedHeadCount = RetroMediaLab.MediaFileSystems.Constants.DiskGeometryConstants.DoubleSidedHeadCount;
}
