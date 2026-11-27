using System.IO;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    internal static uint FrameSkipCount;
    private static int _renderCount;

    internal static TaContext? CurrentContext;
    internal static TaDataContext CurrentData;

    internal static void SetCurrentTarc(uint address)
    {
        if (address != NoTaContext)
        {
            if (CurrentContext is not null)
                SetCurrentTarc(NoTaContext);

            Sh4HostRuntime.Verify(CurrentContext is null);
            CurrentContext = FindContext(address, true);
            CurrentData = CurrentContext!.Tad;
        }
        else
        {
            Sh4HostRuntime.Verify(CurrentContext is not null);
            CurrentContext!.Tad = CurrentData;
            CurrentContext = null;
            CurrentData.Reset(null);
        }
    }

    private static TaContext? _renderQueue;
    private static readonly object FrameFinishedMutex = new();
    private static bool _frameFinished;

    internal static bool QueueRender(TaContext context)
    {
        Sh4HostRuntime.Verify(context is not null);
        bool skipFrame = !PvrRenderer.IsEnabled();
        if (!skipFrame)
        {
            _renderCount++;
            if (_renderCount % (PvrConfiguration.SkipFrame + 1) != 0)
                skipFrame = true;
            else if (PvrConfiguration.ThreadedRendering && _renderQueue is not null
                && (PvrConfiguration.AutoSkipFrame == 0 || (PvrConfiguration.AutoSkipFrame == 1 && PvrScanGenerator.Sh4FastEnough)))
            {
                lock (FrameFinishedMutex)
                {
                    if (!_frameFinished)
                        Monitor.Wait(FrameFinishedMutex);
                    _frameFinished = false;
                }
            }
        }

        if (skipFrame || _renderQueue is not null)
        {
            RecycleContext(context);
            if (PvrRenderer.IsEnabled())
                FrameSkipCount++;
            return false;
        }
        PvrRenderer.DisableRollback();
        lock (FrameFinishedMutex)
            _frameFinished = false;
        Sh4HostRuntime.Verify(_renderQueue is null);
        _renderQueue = context;
        return true;
    }

    internal static TaContext? DequeueRender()
    {
        if (_renderQueue is not null)
            PvrRenderer.FrameCount++;
        return _renderQueue;
    }

    internal static void FinishRender(TaContext? context)
    {
        if (context is not null)
        {
            Sh4HostRuntime.Verify(_renderQueue == context);
            _renderQueue = null;
            RecycleContext(context);
        }
        lock (FrameFinishedMutex)
        {
            _frameFinished = true;
            Monitor.Pulse(FrameFinishedMutex);
        }
    }

    private static readonly object PoolMutex = new();
    private static readonly List<TaContext> ContextPool = [];
    private static readonly List<TaContext> ContextList = [];

    internal static TaContext AllocateContext()
    {
        TaContext? context = null;
        lock (PoolMutex)
        {
            if (ContextPool.Count != 0)
            {
                context = ContextPool[^1];
                ContextPool.RemoveAt(ContextPool.Count - 1);
            }
        }

        if (context is null)
        {
            context = new TaContext();
            bool allocated = false;
            try
            {
                context.Allocate();
                allocated = true;
            }
            finally
            {
                if (!allocated)
                    context.Dispose();
            }
        }
        return context;
    }

    private static void RecycleContext(TaContext context)
    {
        if (context.NextContext is not null)
            RecycleContext(context.NextContext);
        lock (PoolMutex)
        {
            if (ContextPool.Count > 3)
                context.Dispose();
            else
            {
                context.Reset();
                ContextPool.Add(context);
            }
        }
    }

    private static TaContext? FindContext(uint address, bool allocateNew = false)
    {
        TaContext? oldContext = null;
        foreach (TaContext context in ContextList)
        {
            if (context.Address == address)
            {
                context.LastFrameUsed = PvrRenderer.FrameCount;
                return context;
            }
            if (unchecked(PvrRenderer.FrameCount - context.LastFrameUsed) > 60)
                oldContext = context;
        }

        if (allocateNew)
        {
            TaContext context;
            if (oldContext is not null)
            {
                context = oldContext;
                context.Reset();
            }
            else
            {
                context = AllocateContext();
                ContextList.Add(context);
            }
            context.Address = address;
            context.LastFrameUsed = PvrRenderer.FrameCount;
            return context;
        }
        return null;
    }

    internal static TaContext? TaContextPop(uint address)
    {
        for (int i = 0; i < ContextList.Count; i++)
        {
            if (ContextList[i].Address == address)
            {
                TaContext context = ContextList[i];

                if (CurrentContext == context)
                    SetCurrentTarc(NoTaContext);

                ContextList.RemoveAt(i);
                return context;
            }
        }
        return null;
    }

    internal static void TerminateContexts()
    {
        if (CurrentContext is not null)
            SetCurrentTarc(NoTaContext);

        foreach (TaContext context in ContextList)
            context.Dispose();
        ContextList.Clear();

        lock (PoolMutex)
        {
            foreach (TaContext context in ContextPool)
                context.Dispose();
            ContextPool.Clear();
        }
    }

    private const uint NullContext = uint.MaxValue;

    private static void SerializeContext(BinaryWriter writer, TaContext? context, bool dryRun)
    {
        if (dryRun)
        {
            writer.BaseStream.Seek(4 + 4 + TaDataSize, SeekOrigin.Current);
            return;
        }
        if (context is null)
        {
            writer.Write(NullContext);
            return;
        }
        writer.Write(context.Address);
        TaDataContext data = context == CurrentContext ? CurrentData : context.Tad;
        uint size = (uint)(data.ThdData - data.ThdRoot);
        writer.Write(size);
        writer.Write(new ReadOnlySpan<byte>(data.ThdRoot, (int)size));
    }

    private static TaContext? DeserializeContext(BinaryReader reader, int version)
    {
        uint address = reader.ReadUInt32();
        if (address == NullContext)
            return null;

        uint size = reader.ReadUInt32();
        if (size > TaDataSize)
            throw new InvalidDataException("Invalid TA context size");

        TaContext context = FindContext(address, true)!;
        ref TaDataContext data = ref context.Tad;
        reader.BaseStream.ReadExactly(new Span<byte>(data.ThdRoot, (int)size));
        data.ThdData = data.ThdRoot + size;
        if (version < 821)
        {
            uint renderPassCount = reader.ReadUInt32();
            ulong remaining = (ulong)sizeof(uint) * renderPassCount;
            Span<byte> discarded = stackalloc byte[256];
            while (remaining > 0)
            {
                int count = (int)Math.Min((ulong)discarded.Length, remaining);
                reader.BaseStream.ReadExactly(discarded[..count]);
                remaining -= (ulong)count;
            }
        }
        return context;
    }

    internal static void SerializeTaContext(BinaryWriter writer, bool dryRun = false)
    {
        writer.Write((uint)ContextList.Count);
        int currentContext = -1;
        for (int index = 0; index < ContextList.Count; index++)
        {
            TaContext context = ContextList[index];
            if (context == CurrentContext)
                currentContext = index;
            SerializeContext(writer, context, dryRun);
        }
        writer.Write(currentContext);
    }

    internal static void DeserializeTaContext(BinaryReader reader, int version)
    {
        if (CurrentContext is not null)
            SetCurrentTarc(NoTaContext);
        if (version >= 820)
        {
            uint listSize = reader.ReadUInt32();
            foreach (TaContext context in ContextList)
                RecycleContext(context);
            ContextList.Clear();
            for (uint i = 0; i < listSize; i++)
                DeserializeContext(reader, version);
            int currentContext = reader.ReadInt32();
            if (currentContext >= 0 && currentContext < ContextList.Count)
                SetCurrentTarc(ContextList[currentContext].Address);
        }
        else
        {
            TaContext? currentContext = DeserializeContext(reader, version);
            if (currentContext is not null)
                SetCurrentTarc(currentContext.Address);
            if (version >= 815)
                DeserializeContext(reader, version);
        }
    }
}
