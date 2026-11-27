namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal readonly record struct PvrCustomTextureData(byte[] Data, int Width, int Height);

internal abstract unsafe class PvrBaseCustomTextureSource : IDisposable
{
    public virtual void Dispose() { }
    internal virtual bool ShouldReplace() => false;
    internal virtual bool ShouldPreload() => false;
    internal abstract bool LoadMap();
    internal virtual nuint GetTextureCount() => 0;
    internal virtual void Terminate() { }
    internal abstract byte* LoadCustomTexture(uint hash, ref int width, ref int height);
    internal abstract bool IsTextureReplaced(uint hash);
    internal virtual void PreloadTextures(Action<uint, PvrCustomTextureData> callback, Func<bool>? stopRequested) { }
}

internal sealed unsafe partial class PvrCustomTexture : IDisposable
{
    private bool _initialized;
    private readonly List<PvrBaseCustomTextureSource> _sources = [];
    private PvrWorkerThread? _loaderThread;
    private readonly SortedDictionary<uint, PvrCustomTextureData> _preloadedTextures = new();
    private int _preloadTotal;
    private int _preloadLoaded;
    private long _preloadLoadedSize;
    private int _pendingPreloads;
    private bool _stopPreload;

    public void Dispose() => Terminate();

    internal partial bool Init();
    internal partial bool Enabled();
    internal partial bool Preloaded();
    internal partial bool IsPreloading();
    internal partial void AddSource(PvrBaseCustomTextureSource source);
    internal partial void LoadCustomTextureAsync(PvrBaseTextureCacheData texture);
    internal partial void DumpTexture(PvrBaseTextureCacheData texture, int width, int height, void* source);
    internal partial void Terminate();
    internal partial void GetPreloadProgress(out int completed, out int total, out nuint loadedSize);
}
