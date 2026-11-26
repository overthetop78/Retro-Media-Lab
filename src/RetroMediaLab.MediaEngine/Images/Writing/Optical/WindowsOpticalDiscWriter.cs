using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace RetroMediaLab.MediaEngine.Images.Writing.Optical;

/// <summary>Writes a standard 2048-byte-sector data ISO through the Windows optical recording service.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsOpticalDiscWriter
{
    private const int SectorSize = 2048;
    private const uint ReadDenyWrite = 0x20;

    public Task<IReadOnlyList<WindowsOpticalRecorder>> ListRecordersAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => ListRecorders(cancellationToken), cancellationToken);

    /// <summary>Writes to the selected blank disc. Cancellation is observed until physical recording begins.</summary>
    public Task BurnIsoAsync(string recorderId, string isoPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recorderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoPath);
        string path = Path.GetFullPath(isoPath);
        return Task.Run(() => BurnIso(recorderId, path, cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<WindowsOpticalRecorder> ListRecorders(CancellationToken cancellationToken)
    {
        object? masterObject = null;
        object? formatObject = null;
        try
        {
            masterObject = CreateObject("IMAPI2.MsftDiscMaster2");
            dynamic master = masterObject;
            if (!(bool)master.IsSupportedEnvironment)
                return [];

            formatObject = CreateObject("IMAPI2.MsftDiscFormat2Data");
            dynamic format = formatObject;
            var list = new List<WindowsOpticalRecorder>();
            int count = master.Count;
            for (int index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string id = master[index];
                object? recorderObject = null;
                try
                {
                    recorderObject = CreateObject("IMAPI2.MsftDiscRecorder2");
                    dynamic recorder = recorderObject;
                    recorder.InitializeDiscRecorder(id);
                    if (!(bool)format.IsRecorderSupported(recorder))
                        continue;

                    string vendor = recorder.VendorId;
                    string product = recorder.ProductId;
                    var paths = ((Array)recorder.VolumePathNames).OfType<string>().ToArray();
                    list.Add(new WindowsOpticalRecorder(id, vendor.Trim(), product.Trim(), Array.AsReadOnly(paths)));
                }
                finally
                {
                    Release(recorderObject);
                }
            }
            return list.AsReadOnly();
        }
        finally
        {
            try { Release(formatObject); }
            finally { Release(masterObject); }
        }
    }

    private static void BurnIso(string recorderId, string path, CancellationToken cancellationToken)
    {
        object? recorderObject = null;
        object? formatObject = null;
        IStream? image = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            int result = SHCreateStreamOnFileEx(path, ReadDenyWrite, 0, false, IntPtr.Zero, out image);
            Marshal.ThrowExceptionForHR(result);
            image!.Stat(out var stat, 1);
            if (stat.cbSize <= 0 || stat.cbSize % SectorSize != 0)
                throw new InvalidDataException("The ISO file must consist of complete 2048-byte sectors.");

            recorderObject = CreateObject("IMAPI2.MsftDiscRecorder2");
            dynamic recorder = recorderObject;
            recorder.InitializeDiscRecorder(recorderId);
            formatObject = CreateObject("IMAPI2.MsftDiscFormat2Data");
            dynamic format = formatObject;
            if (!(bool)format.IsRecorderSupported(recorder) || !(bool)format.IsCurrentMediaSupported(recorder))
                throw new InvalidOperationException("The selected recorder cannot write data to the inserted disc.");

            format.Recorder = recorder;
            format.ClientName = "Retro Media Lab";
            if (!(bool)format.MediaHeuristicallyBlank)
                throw new InvalidOperationException("The selected optical disc must be blank.");
            int freeSectors = format.FreeSectorsOnMedia;
            if (stat.cbSize / SectorSize > freeSectors)
                throw new InvalidOperationException("The ISO image exceeds the available space on the selected optical disc.");

            format.ForceMediaToBeClosed = true;
            cancellationToken.ThrowIfCancellationRequested();
            format.Write(image);
        }
        finally
        {
            try { Release(formatObject); }
            finally
            {
                try { Release(recorderObject); }
                finally { Release(image); }
            }
        }
    }

    private static object CreateObject(string programId) =>
        Activator.CreateInstance(Type.GetTypeFromProgID(programId, throwOnError: true)!)!;

    private static void Release(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
            Marshal.FinalReleaseComObject(instance);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHCreateStreamOnFileEx(string path, uint mode, uint attributes,
        [MarshalAs(UnmanagedType.Bool)] bool create, IntPtr template,
        [MarshalAs(UnmanagedType.Interface)] out IStream? stream);
}

public sealed record WindowsOpticalRecorder(string Id, string Vendor, string Product, IReadOnlyList<string> VolumePaths);
