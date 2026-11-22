using System.IO;

namespace RetroMediaLab.App.Services.Storage;

internal static class BlankMediaFile
{
    internal static void Create(string path, long size, Action<Stream>? initialize = null)
    {
        var created = false;
        var finished = false;
        try
        {
            using (var image = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                image.SetLength(size);
                initialize?.Invoke(image);
            }
            finished = true;
        }
        finally
        {
            if (created && !finished && File.Exists(path)) File.Delete(path);
        }
    }
}
