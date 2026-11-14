using System.IO;
using System.Security.Cryptography;

namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Dictionaries;

public sealed partial class FirmwareCatalog
{
    internal sealed record FirmwareIdentity(string Name, string Version, string[] Models, string[] FileNames, bool IsOfficial = true);
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        FirmwareCatalogConstants.RomExtension, FirmwareCatalogConstants.BinaryExtension,
        FirmwareCatalogConstants.MasterSystemExtension, FirmwareCatalogConstants.GameGearExtension,
        FirmwareCatalogConstants.ZipExtension
    };

    private static readonly IReadOnlyDictionary<string, (string Name, string Version,
        string[] Models, string[] FileNames)> Known = new Dictionary<string,
        (string, string, string[], string[])>(
        StringComparer.OrdinalIgnoreCase)
    {
        [FirmwareConstants.MegaDriveBiosMd5] = (FirmwareConstants.MegaDriveBiosName,
            FirmwareConstants.MegaDriveBiosFileName, [ModelConstants.MegaDrive],
            [FirmwareConstants.MegaDriveBiosFileName]),
        [FirmwareConstants.MegaDriveBiosAlternateMd5] = (FirmwareConstants.MegaDriveBiosName,
            FirmwareConstants.MegaDriveBiosFileName, [ModelConstants.MegaDrive],
            [FirmwareConstants.MegaDriveBiosFileName]),
        [FirmwareConstants.MegaCdEuropeBiosMd5] = (FirmwareConstants.MegaCdBiosName,
            FirmwareConstants.RegionEurope, [ModelConstants.MegaDrive],
            [FirmwareConstants.MegaCdEuropeBiosFileName]),
        [FirmwareConstants.MegaCdUnitedStatesBiosMd5] = (FirmwareConstants.MegaCdBiosName,
            FirmwareConstants.RegionUnitedStates, [ModelConstants.MegaDrive],
            [FirmwareConstants.MegaCdUnitedStatesBiosFileName]),
        [FirmwareConstants.MegaCdJapanBiosMd5] = (FirmwareConstants.MegaCdBiosName,
            FirmwareConstants.RegionJapan, [ModelConstants.MegaDrive],
            [FirmwareConstants.MegaCdJapanBiosFileName]),
        [FirmwareConstants.MasterSystemEuropeBiosMd5] = (FirmwareConstants.MasterSystemBiosName,
            FirmwareConstants.RegionEurope, [ModelConstants.MasterSystem, ModelConstants.MarkIII],
            [FirmwareConstants.MasterSystemEuropeBiosFileName,
             FirmwareConstants.MasterSystemUnitedStatesBiosFileName]),
        [FirmwareConstants.MasterSystemEuropeBiosAlternateMd5] = (FirmwareConstants.MasterSystemBiosName,
            FirmwareConstants.RegionEurope, [ModelConstants.MasterSystem, ModelConstants.MarkIII],
            [FirmwareConstants.MasterSystemEuropeBiosFileName]),
        [FirmwareConstants.MasterSystemUnitedStatesBiosAlternateMd5] = (FirmwareConstants.MasterSystemBiosName,
            FirmwareConstants.RegionUnitedStates, [ModelConstants.MasterSystem, ModelConstants.MarkIII],
            [FirmwareConstants.MasterSystemUnitedStatesBiosFileName]),
        [FirmwareConstants.MasterSystemJapanBiosMd5] = (FirmwareConstants.MasterSystemBiosName,
            FirmwareConstants.RegionJapan, [ModelConstants.MasterSystem, ModelConstants.MarkIII],
            [FirmwareConstants.MasterSystemJapanBiosFileName]),
        [FirmwareConstants.GameGearBiosMd5] = (FirmwareConstants.GameGearBiosName,
            FirmwareConstants.GameGearBiosFileName, [ModelConstants.GameGear],
            [FirmwareConstants.GameGearBiosFileName]),
        [FirmwareConstants.SaturnBiosMd5] = (FirmwareConstants.SaturnBiosName,
            FirmwareConstants.SaturnBiosFileName, [ModelConstants.Saturn],
            [FirmwareConstants.SaturnBiosFileName]),
        [FirmwareConstants.SaturnBiosEuropeUnitedStatesMd5] = (FirmwareConstants.SaturnBiosName,
            FirmwareConstants.SaturnBiosEuropeUnitedStatesVersion, [ModelConstants.Saturn],
            [FirmwareConstants.SaturnBiosFileName]),
        [FirmwareConstants.SaturnBiosJapanV101Md5] = (FirmwareConstants.SaturnBiosName,
            FirmwareConstants.SaturnBiosJapanV101Version, [ModelConstants.Saturn],
            [FirmwareConstants.SaturnBiosFileName]),
        [FirmwareConstants.DreamcastBiosMd5] = (FirmwareConstants.DreamcastBiosName,
            FirmwareConstants.DreamcastBiosFileName, [ModelConstants.Dreamcast],
            [FirmwareConstants.DreamcastBiosRelativeFileName]),
        [FirmwareConstants.NaomiBiosMd5] = (FirmwareConstants.NaomiBiosName,
            FirmwareConstants.NaomiBiosFileName, [ModelConstants.Naomi],
            [FirmwareConstants.NaomiBiosRelativeFileName]),
        [FirmwareConstants.Naomi2BiosMd5] = (FirmwareConstants.Naomi2BiosName,
            FirmwareConstants.Naomi2BiosFileName, [ModelConstants.Naomi2],
            [FirmwareConstants.Naomi2BiosRelativeFileName]),
        [FirmwareConstants.AtomiswaveBiosMd5] = (FirmwareConstants.AtomiswaveBiosName,
            FirmwareConstants.AtomiswaveBiosFileName, [ModelConstants.Atomiswave],
            [FirmwareConstants.AtomiswaveBiosRelativeFileName])
    };

    private readonly string _directory;

    public FirmwareCatalog(string directory) => _directory = Path.GetFullPath(directory);

    public IReadOnlyList<Firmware> Scan()
    {
        Directory.CreateDirectory(_directory);
        return Directory.EnumerateFiles(_directory, FirmwareCatalogConstants.SearchPattern, SearchOption.AllDirectories).Where(path => Extensions.Contains(Path.GetExtension(path))).Select(Inspect)
            .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static Firmware Inspect(string path) => Inspect(path, CancellationToken.None);

    internal static Firmware Inspect(string path, CancellationToken cancellationToken)
    {
        var file = new FileInfo(Path.GetFullPath(path));
        using var stream = file.OpenRead();
        using var md5Hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var sha1Hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using var sha256Hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[65536];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = stream.Read(buffer);
            if (count == 0) break;
            md5Hash.AppendData(buffer, 0, count);
            sha1Hash.AppendData(buffer, 0, count);
            sha256Hash.AppendData(buffer, 0, count);
        }
        var md5 = Convert.ToHexString(md5Hash.GetHashAndReset());
        var sha1 = Convert.ToHexString(sha1Hash.GetHashAndReset());
        var sha256 = Convert.ToHexString(sha256Hash.GetHashAndReset());
        var known = TryIdentify(md5, sha1, out var identity);
        return new Firmware(file.FullName, file.Length, md5, sha256, file.LastWriteTimeUtc, FirmwareType.SystemRom, known, known && identity!.IsOfficial, identity?.Name, identity?.Version, identity?.Models ?? [], identity?.FileNames ?? []);
    }

    internal static bool TryIdentify(string md5, string sha1, out FirmwareIdentity? identity)
    {
        if (AdditionalKnown.TryGetValue(md5, out identity) || AdditionalKnown.TryGetValue(sha1, out identity)) return true;
        if (Known.TryGetValue(md5, out var original))
        {
            identity = new(original.Name, original.Version, original.Models, original.FileNames);
            return true;
        }
        identity = null;
        return false;
    }

    internal static bool TryIdentifyKnown(string md5, out (string Name, string Version, string[] Models, string[] FileNames) identity)
    {
        if (TryIdentify(md5, string.Empty, out var known))
        {
            identity = (known!.Name, known.Version, known.Models, known.FileNames);
            return true;
        }
        identity = default;
        return false;
    }
}
