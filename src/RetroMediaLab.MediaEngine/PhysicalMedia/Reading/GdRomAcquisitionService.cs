using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaEngine.Images.Reading.Optical;
using RetroMediaLab.MediaEngine.Images.Writing;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Reading;

/// <summary>Combines a low-density dump with the high-density tracks acquired by Dreamdump.</summary>
[SupportedOSPlatform("windows")]
public sealed class GdRomAcquisitionService
{
    public async Task AcquireAsync(string dreamdumpPath, string driveName, string lowDensityCuePath,
        string destinationCuePath, CancellationToken cancellationToken = default)
    {
        string executable = Path.GetFullPath(dreamdumpPath);
        if (!File.Exists(executable))
            throw new FileNotFoundException("The selected Dreamdump executable was not found.", executable);

        string drive = driveName.TrimEnd('\\');
        if (!new WindowsOpticalDiscReader().ListDrives().Contains(drive + "\\", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Select a Windows optical drive by its drive letter.", nameof(driveName));

        string destination = Path.GetFullPath(destinationCuePath);
        if (!Path.GetExtension(destination).Equals(".cue", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A combined GD-ROM dump requires a .cue destination.", nameof(destinationCuePath));
        if (File.Exists(destination))
            throw new IOException("The destination CUE already exists.");

        var cueReader = new CueSheetReader();
        string lowCue = Path.GetFullPath(lowDensityCuePath);
        string lowText = await File.ReadAllTextAsync(lowCue, cancellationToken).ConfigureAwait(false);
        var lowSheet = cueReader.Parse(lowText.Split('\n'));
        var lowTracks = lowSheet.Files.SelectMany(file => file.Tracks).ToArray();
        if (!lowTracks.Select(track => track.Number).SequenceEqual([1, 2]) || lowTracks.Any(track => track.GdRomSession is not null))
            throw new InvalidDataException("The low-density CUE must describe the original tracks 1 and 2 as a CD.");
        string lowDirectory = Path.GetDirectoryName(lowCue)!;
        var lowNames = lowSheet.Files.Select(file => RequireLocalBinName(file.DeclaredPath))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (string name in lowNames)
        {
            string path = Path.Combine(lowDirectory, name);
            if (!File.Exists(path))
                throw new FileNotFoundException("A low-density track file is missing.", path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        DirectoryInfo staging = Directory.CreateTempSubdirectory("rml-gdrom-");
        try
        {
            string imageName = Path.GetFileNameWithoutExtension(destination);
            await DumpHighDensityAsync(executable, drive, staging.FullName, imageName, cancellationToken).ConfigureAwait(false);
            string stagedCue = Path.Combine(staging.FullName, imageName + ".cue");
            string highText = await File.ReadAllTextAsync(stagedCue, cancellationToken).ConfigureAwait(false);
            var highSheet = cueReader.Parse(highText.Split('\n'));
            var highTracks = highSheet.Files.SelectMany(file => file.Tracks).ToArray();
            if (highTracks[0].Number != 3 || highTracks.Any(track => track.GdRomSession != 2))
                throw new InvalidDataException("Dreamdump did not produce a high-density CUE starting at track 3.");

            var sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in highSheet.Files)
                sourceNames.Add(RequireLocalBinName(file.DeclaredPath));
            foreach (string name in lowNames)
            {
                string target = Path.Combine(staging.FullName, name);
                if (!sourceNames.Add(name))
                    throw new InvalidDataException("The GD-ROM track files must have distinct names.");

                await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    64 * 1024, FileOptions.Asynchronous);
                await CopyFileAsync(Path.Combine(lowDirectory, name), output, cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(stagedCue, "REM SINGLE-DENSITY AREA\n" + lowText.TrimEnd() + "\n" + highText,
                cancellationToken).ConfigureAwait(false);
            using (var disc = await GdRomDisc.OpenAsync(stagedCue, cancellationToken).ConfigureAwait(false))
                await disc.ReadDataSectorAsync(GdiConstants.HighDensityStartLba + GdRomDisc.FirstFrameAddress,
                    new byte[GdRomDisc.DataSectorSize], cancellationToken).ConfigureAwait(false);

            var outputs = new Dictionary<string, Func<Stream, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in sourceNames)
            {
                string source = Path.Combine(staging.FullName, name);
                outputs.Add(Path.Combine(Path.GetDirectoryName(destination)!, name),
                    (output, token) => CopyFileAsync(source, output, token));
            }
            outputs.Add(destination, (output, token) => CopyFileAsync(stagedCue, output, token));
            await AtomicMediaFileSetWriter.WriteAsync(outputs, cancellationToken, replaceExisting: false).ConfigureAwait(false);
        }
        finally
        {
            staging.Delete(recursive: true);
        }
    }

    private static string RequireLocalBinName(string path)
    {
        if (!Path.GetExtension(path).Equals(".bin", StringComparison.OrdinalIgnoreCase) || path != Path.GetFileName(path) ||
            path.AsSpan().ContainsAny(Path.GetInvalidFileNameChars()))
            throw new InvalidDataException("Each CUE track file must be a local BIN file beside its cue sheet.");
        return path;
    }

    private static async Task CopyFileAsync(string source, Stream output, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private static async Task DumpHighDensityAsync(string executable, string drive, string directory, string imageName,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("disc");
        start.ArgumentList.Add($"--drive={drive}");
        start.ArgumentList.Add($"--image-path={directory}");
        start.ArgumentList.Add($"--image-name={imageName}");

        cancellationToken.ThrowIfCancellationRequested();
        Process process = Process.Start(start) ?? throw new IOException("Dreamdump could not be started.");
        try
        {
            Task output = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
            Task<string> errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await output.ConfigureAwait(false);
            string error = await errors.ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new IOException($"Dreamdump exited with code {process.ExitCode}. {error.Trim()}");
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) when (process.HasExited) { }
                }
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
