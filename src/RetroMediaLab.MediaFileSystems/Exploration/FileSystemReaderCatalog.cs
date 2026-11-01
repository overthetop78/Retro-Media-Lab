namespace RetroMediaLab.MediaFileSystems.Exploration;

/// <summary>Construit le catalogue ordonné des lecteurs de systèmes de fichiers.</summary>
public static class FileSystemReaderCatalog
{
    /// <summary>Crée une nouvelle collection contenant les lecteurs par défaut dans leur ordre de détection.</summary>
    public static IReadOnlyList<IFileSystemReader> CreateDefault() => Array.AsReadOnly<IFileSystemReader>(
    [
        new RetroMediaLab.MediaFileSystems.FileSystems.Amiga.AmigaDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Amiga.FlatArchive.AmigaFlatResourceArchiveReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Acorn.BbcDfs.BbcDfsFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Coherent.CoherentFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Dec.Rt11.Rt11FileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Ucsd.UcsdFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Apple.Dos.AppleDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Apple.ProDos.ProDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Apple.Macintosh.Mfs.MacMfsFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Apple.Macintosh.Hfs.MacHfsFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Apple.Lisa.LisaFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Cpm.AmstradCpmFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Cpm.CpmFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Commodore.Dos.CommodoreDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Fat12.Fat12FileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Atari.SpartaDos.SpartaDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Atari.Dos.AtariDosFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Atari.KFile.AtariKFileFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Atari.ClkGraphicsLibrary.AtariClkGraphicsLibraryFileSystemReader(),
        new RetroMediaLab.MediaFileSystems.FileSystems.Atari.BootDisk.AtariBootDiskFileSystemReader()
    ]);
}

