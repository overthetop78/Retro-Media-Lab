using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;

internal sealed class Sf7000PrintFile(string? configuredDirectory) : ISf7000ParallelPrinter, IDisposable
{
    internal const string DirectoryOption = "sf7000.printDirectory";
    internal static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Retro Media Lab", "Print");

    private FileStream? _output;
    private bool _disposed;

    internal string DirectoryPath { get; } = Path.GetFullPath(
        string.IsNullOrWhiteSpace(configuredDirectory) ? DefaultDirectory : configuredDirectory);

    public bool Busy => _disposed;

    public void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_output is null)
        {
            Directory.CreateDirectory(DirectoryPath);
            string fileName = FormattableString.Invariant($"SF-7000-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{Guid.NewGuid():N}.prn");
            _output = new FileStream(Path.Combine(DirectoryPath, fileName), FileMode.CreateNew,
                FileAccess.Write, FileShare.Read);
        }
        _output.WriteByte(value);
    }

    public void Dispose()
    {
        _disposed = true;
        FileStream? output = _output;
        _output = null;
        output?.Dispose();
    }
}
