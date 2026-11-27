using System.Diagnostics;
using System.IO;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static partial class PvrRenderer
{
    internal static uint FrameCount = 1;
    internal static PvrRenderBackend? Backend;

    private static readonly ResetEvent RenderEnd = new();
    private static uint _framebufferWriteCurrent = 1;
    private static readonly ResetEvent VramRollback = new();

    private static bool _renderCalled;
    internal static uint FramebufferWatchStart;
    internal static uint FramebufferWatchEnd;
    internal static bool FramebufferDirty;

    private static bool _renderPending;
    private static bool _rendererEnabled = true;

    private static bool _presented;
    private static readonly uint[] FramebufferAddressHistory = [1, 1];

    private sealed class PvrMessageQueue
    {
        internal enum MessageType
        {
            NoMessage = -1,
            Render,
            RenderFramebuffer,
            Present,
            Stop
        }

        private readonly record struct Message(MessageType Type, PvrFramebufferInfo Configuration);

        private readonly object _mutex = new();
        private readonly ResetEvent _enqueueEvent = new();
        private readonly ResetEvent _dequeueEvent = new();
        private readonly LinkedList<Message> _queue = new();

        internal void Enqueue(MessageType type, PvrFramebufferInfo configuration = default)
        {
            var message = new Message(type, configuration);
            if (PvrConfiguration.ThreadedRendering)
            {
                bool duplicate;
                do
                {
                    duplicate = false;
                    lock (_mutex)
                    {
                        foreach (Message queued in _queue)
                        {
                            if (queued.Type == type)
                            {
                                duplicate = true;
                                break;
                            }
                        }
                        if (!duplicate || type == MessageType.Present)
                        {
                            const int MaxQueueDepth = 4;
                            if (_queue.Count >= MaxQueueDepth)
                            {
                                for (LinkedListNode<Message>? node = _queue.First; node is not null; node = node.Next)
                                {
                                    if (node.Value.Type == MessageType.Present)
                                    {
                                        _queue.Remove(node);
                                        break;
                                    }
                                }
                            }
                            _queue.AddLast(message);
                            duplicate = false;
                        }
                    }
                    if (duplicate)
                    {
                        if (type == MessageType.Stop)
                            return;
                        _dequeueEvent.Wait();
                    }
                } while (duplicate);
                _enqueueEvent.Set();
            }
            else
            {
                PvrRuntime.SetDefaultRoundingMode();
                while (_queue.Count != 0)
                    WaitAndExecute();
                Execute(message);
                PvrRuntime.RestoreHostRoundingMode();
            }
        }

        internal bool WaitAndExecute(int timeoutMs = -1) => Execute(Dequeue(timeoutMs));

        internal void Reset()
        {
            lock (_mutex)
                _queue.Clear();
        }

        internal void CancelEnqueue()
        {
            lock (_mutex)
            {
                LinkedListNode<Message>? node = _queue.First;
                while (node is not null)
                {
                    LinkedListNode<Message>? next = node.Next;
                    if (node.Value.Type != MessageType.Render)
                        _queue.Remove(node);
                    node = next;
                }
                _dequeueEvent.Set();
            }
        }

        private Message Dequeue(int timeoutMs = -1)
        {
            using var profile = PvrRuntime.Profiler.Scope();

            var message = new Message(MessageType.NoMessage, default);
            while (true)
            {
                lock (_mutex)
                {
                    if (_queue.First is { } node)
                    {
                        message = node.Value;
                        _queue.RemoveFirst();
                    }
                }
                if (message.Type != MessageType.NoMessage)
                {
                    _dequeueEvent.Set();
                    break;
                }
                if (timeoutMs == -1)
                    _enqueueEvent.Wait();
                else if (!_enqueueEvent.Wait(timeoutMs))
                    break;
            }
            return message;
        }

        private static bool Execute(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Render:
                    Render();
                    return true;
                case MessageType.RenderFramebuffer:
                    RenderFramebuffer(message.Configuration);
                    return true;
                case MessageType.Present:
                    Present();
                    return true;
                case MessageType.Stop:
                case MessageType.NoMessage:
                default:
                    return false;
            }
        }

        private static void Render()
        {
            using var profile = PvrRuntime.Profiler.Scope();

            TaContext? context = PowerVr2Ta.DequeueRender();
            if (context is null)
                return;

            PowerVr2Ta.SetTileClipping(context.Rend);
            PvrTransformMatrix.GetScaledFramebufferSize(context.Rend, out int width, out int height);
            context.Rend.FramebufferWidth = width;
            context.Rend.FramebufferHeight = height;
            bool renderToScreen = !context.Rend.IsRtt && !PvrConfiguration.EmulateFramebuffer;
            {
                using var processProfile = PvrRuntime.Profiler.Scope("Renderer::Process");
                try
                {
                    Backend!.Process(context);
                }
                catch
                {
                    RenderEnd.Set();
                    AllowRollback();
                    PowerVr2Ta.FinishRender(context);
                    throw;
                }
            }

            if (renderToScreen)
                RenderEnd.Set();
            AllowRollback();
            {
                using var renderProfile = PvrRuntime.Profiler.Scope("Renderer::Render");
                try
                {
                    Backend!.Render();
                }
                catch
                {
                    if (!renderToScreen)
                        RenderEnd.Set();
                    PowerVr2Ta.FinishRender(context);
                    throw;
                }
            }

            if (!renderToScreen)
                RenderEnd.Set();
            else if (PvrConfiguration.DelayFrameSwapping && _framebufferWriteCurrent == PvrRegisters.FB_R_SOF1)
                Present();

            PowerVr2Ta.FinishRender(context);
        }

        private static void RenderFramebuffer(in PvrFramebufferInfo configuration)
        {
            using var profile = PvrRuntime.Profiler.Scope();
            Backend!.RenderFramebuffer(configuration);
        }

        private static void Present()
        {
            using var profile = PvrRuntime.Profiler.Scope();

            if (Backend!.Present())
            {
                _presented = true;
                if (!PvrConfiguration.ThreadedRendering && !PvrConfiguration.VblankPacing && !PvrRuntime.Ggpo.Active)
                    PvrRuntime.Sh4Executor.Stop();
            }
        }
    }

    private static readonly PvrMessageQueue MessageQueue = new();

    internal static partial bool SingleFrame(in bool enabled)
    {
        using var profile = PvrRuntime.Profiler.Scope();

        int timeout = PvrRegisters.SPG_CONTROL.IsPAL() ? 23 : 20;
        _presented = false;
        while (enabled && !_presented)
        {
            if (!MessageQueue.WaitAndExecute(timeout))
                return false;
        }
        return true;
    }

    private sealed class SwapIntervalDetector : IDisposable
    {
        private int _lastInterval;
        private int _stable;
        private int _unstable;
        private int _currentInterval;
        private ulong _lastRender;
        private ulong _renderInterval;
        private float _averageRenderInterval;
        private int _renders;
        private int _rendersFullSpeed;

        internal SwapIntervalDetector()
        {
            PvrRuntime.Events.Listen(PvrEvent.LoadState, OnEvent, this);
            Reset();
        }

        public void Dispose() => PvrRuntime.Events.Unlisten(PvrEvent.LoadState, OnEvent, this);

        internal void Render()
        {
            ulong now = PvrRuntime.Scheduler.Now64();
            if (_lastRender != 0)
                _renderInterval = now - _lastRender;
            _lastRender = now;
            _renders++;
        }

        internal void Vblank()
        {
            _averageRenderInterval = 0.1f * _renderInterval + 0.9f * _averageRenderInterval;
            if (_renders != 0)
            {
                _renders = 0;
                _rendersFullSpeed++;
                if (_rendersFullSpeed >= 3)
                {
                    _lastInterval = 1;
                    _currentInterval = 1;
                    return;
                }
            }
            else
                _rendersFullSpeed = 0;

            float vblankPerRender = _averageRenderInterval / (PvrRegisters.SPG_CONTROL.IsPAL() ? 4_000_000 : 3_333_400);
            int interval = (int)MathF.Round(vblankPerRender, MidpointRounding.AwayFromZero);
            float fraction = MathF.Abs(vblankPerRender - interval);
            if ((interval == 2 && fraction <= .05f) || (interval == 1 && fraction <= .2f))
            {
                if (_lastInterval == interval)
                {
                    if (++_stable >= 10)
                        _currentInterval = Math.Min(_lastInterval, 2);
                    _unstable = 0;
                }
                else
                {
                    _stable = 0;
                    _lastInterval = interval;
                    _unstable++;
                }
            }
            else
            {
                _stable = 0;
                _unstable++;
            }
            if (_unstable >= 30 && vblankPerRender < 2f)
                _currentInterval = 1;
        }

        internal int SwapInterval() => _currentInterval;

        internal void Reset()
        {
            _lastInterval = 1;
            _stable = 0;
            _unstable = 0;
            _currentInterval = 1;

            _lastRender = 0;
            _renderInterval = 0;
            _averageRenderInterval = 0f;
            _renders = 0;
            _rendersFullSpeed = 0;
        }

        private static void OnEvent(PvrEvent @event, object argument)
        {
            var self = (SwapIntervalDetector)argument;
            self._lastRender = 0;
            self._lastInterval = 1;
        }
    }

    private static readonly SwapIntervalDetector IntervalDetector = new();


    private static void CreateRenderer()
    {
#if PVR_NO_RENDERER
        Backend = new PvrNoRenderer();
#else
        switch (PvrConfiguration.RendererType)
        {
#if PVR_USE_OPENGL
            default:
            case PvrRenderType.OpenGL:
                Backend = PvrRuntime.CreateOpenGlRenderer();
                break;
#if !PVR_USE_GLES2 && !PVR_APPLE
            case PvrRenderType.OpenGlOit:
                Backend = PvrRuntime.CreateOpenGlOitRenderer();
                break;
#endif
#else
            default:
                break;
#endif
#if PVR_USE_VULKAN
            case PvrRenderType.Vulkan:
                Backend = PvrRuntime.CreateVulkanRenderer();
                break;
            case PvrRenderType.VulkanOit:
                Backend = PvrRuntime.CreateVulkanOitRenderer();
                break;
#endif
#if PVR_USE_DX9
            case PvrRenderType.DirectX9:
                Backend = PvrRuntime.CreateDirectX9Renderer();
                break;
#endif
#if PVR_USE_DX11
            case PvrRenderType.DirectX11:
                Backend = PvrRuntime.CreateDirectX11Renderer();
                break;
            case PvrRenderType.DirectX11Oit:
                Backend = PvrRuntime.CreateDirectX11OitRenderer();
                break;
#endif
        }
#endif
    }

    internal static partial bool Init()
    {
        _rendererEnabled = true;
        if (Backend is null)
            CreateRenderer();
        bool success = Backend is not null && Backend.Init();
        if (!success)
        {
            Backend?.Dispose();
            Backend = new PvrNoRenderer();
            Backend.Init();
        }
        return success;
    }

    internal static partial void Term()
    {
        CancelEmulationWait();
        if (Backend is { } renderer)
        {
            try
            {
                renderer.Term();
            }
            finally
            {
                try
                {
                    renderer.Dispose();
                }
                finally
                {
                    Backend = null;
                }
            }
        }
    }

    internal static partial void Reset()
    {
        PowerVr2Ta.FinishRender(PowerVr2Ta.DequeueRender());
        _renderCalled = false;
        _renderPending = false;
        FrameCount = 1;
        _framebufferWriteCurrent = 1;
        MessageQueue.Reset();
        _rendererEnabled = true;
        FramebufferAddressHistory[0] = 1;
        FramebufferAddressHistory[1] = 1;
        IntervalDetector.Reset();
    }

    internal static unsafe partial void StartRender()
    {
        _renderCalled = true;
        _renderPending = false;

        TaContext? context = null;
        uint* addresses = stackalloc uint[10];
        int count = PowerVr2Ta.GetTaContextAddresses(addresses);
        if (count > 0)
        {
            context = PowerVr2Ta.TaContextPop(addresses[0]);
            if (context is not null)
            {
                TaContext linked = context;
                for (int i = 1; i < count; i++)
                {
                    linked.NextContext = PowerVr2Ta.TaContextPop(addresses[i]);
                    if (linked.NextContext is not null)
                        linked = linked.NextContext;
                    else
                        Trace.TraceInformation($"rend_start_render: Context{i} @ {addresses[i]:x} not found");
                }
            }
            else
                Trace.TraceInformation($"rend_start_render: Context0 @ {addresses[0]:x} not found");
        }
        else
            Trace.TraceInformation("rend_start_render: No context not found");

        PvrScanGenerator.ScheduleRenderDone(context);

        if (context is null)
            return;

        PowerVr2Ta.FillBgp(context);

        context.Rend.IsRtt = (PvrRegisters.FB_W_SOF1 & 0x1000000) != 0;
        context.Rend.FbWSof1 = PvrRegisters.FB_W_SOF1;
        context.Rend.FbWCtrl.full = PvrRegisters.FB_W_CTRL.full;

        context.Rend.GlobClip.X = ((int)PvrRegisters.TA_GLOB_TILE_CLIP.tile_x_num + 1) * 32;
        context.Rend.GlobClip.Y = ((int)PvrRegisters.TA_GLOB_TILE_CLIP.tile_y_num + 1) * 32;
        context.Rend.ScalerCtl = PvrRegisters.SCALER_CTL;
        context.Rend.FbClip.Origin.X = (int)PvrRegisters.FB_X_CLIP.min;
        context.Rend.FbClip.Origin.Y = (int)PvrRegisters.FB_Y_CLIP.min;
        context.Rend.FbClip.Size.X = (int)(PvrRegisters.FB_X_CLIP.max - PvrRegisters.FB_X_CLIP.min + 1);
        context.Rend.FbClip.Size.Y = (int)(PvrRegisters.FB_Y_CLIP.max - PvrRegisters.FB_Y_CLIP.min + 1);
        context.Rend.FbWLineStride = PvrRegisters.FB_W_LINESTRIDE.stride;

        context.Rend.FogClampMin = PvrRegisters.FOG_CLAMP_MIN;
        context.Rend.FogClampMax = PvrRegisters.FOG_CLAMP_MAX;

        if (!context.Rend.IsRtt)
        {
            if (PvrRegisters.FB_W_SOF1 != FramebufferAddressHistory[0]
                && PvrRegisters.FB_W_SOF1 != FramebufferAddressHistory[1])
            {
                context.Rend.ClearFramebuffer = true;
                FramebufferAddressHistory[0] = FramebufferAddressHistory[1];
                FramebufferAddressHistory[1] = PvrRegisters.FB_W_SOF1;
            }
            else
                context.Rend.ClearFramebuffer = false;
            PvrRuntime.Ggpo.EndOfFrame();
            IntervalDetector.Render();
            if (!PvrConfiguration.EmulateFramebuffer)
                context.Rend.SwapInterval = IntervalDetector.SwapInterval();
            else
                context.Rend.SwapInterval = 1;
        }

        if (PowerVr2Ta.QueueRender(context))
        {
            PvrTextureConversion.UpdatePalette();
            _renderPending = true;
            MessageQueue.Enqueue(PvrMessageQueue.MessageType.Render);
            if (!PvrConfiguration.DelayFrameSwapping && !context.Rend.IsRtt && !PvrConfiguration.EmulateFramebuffer)
                MessageQueue.Enqueue(PvrMessageQueue.MessageType.Present);
        }
    }

    internal static partial int EndRender(int tag, int cycles, int jitter, nint argument)
    {
        if (PvrRuntime.Platform.IsNaomi2())
        {
            PvrRuntime.RaiseInterruptBothClx(HollyInterrupt.RenderDone);
            PvrRuntime.RaiseInterruptBothClx(HollyInterrupt.RenderDoneIsp);
            PvrRuntime.RaiseInterruptBothClx(HollyInterrupt.RenderDoneVd);
        }
        else
        {
            PvrRuntime.RaiseInterrupt(HollyInterrupt.RenderDone);
            PvrRuntime.RaiseInterrupt(HollyInterrupt.RenderDoneIsp);
            PvrRuntime.RaiseInterrupt(HollyInterrupt.RenderDoneVd);
        }
        if (_renderPending && PvrConfiguration.ThreadedRendering)
            RenderEnd.Wait();

        return 0;
    }

    internal static partial void Vblank()
    {
        if (PvrConfiguration.EmulateFramebuffer
            || (!_renderCalled && FramebufferDirty && PvrRegisters.FB_R_CTRL.fb_enable != 0))
        {
            if (IsEnabled())
            {
                PvrFramebufferInfo info = default;
                info.Update();
                MessageQueue.Enqueue(PvrMessageQueue.MessageType.RenderFramebuffer, info);
                MessageQueue.Enqueue(PvrMessageQueue.MessageType.Present);
                if (!PvrConfiguration.EmulateFramebuffer)
                    Debug.WriteLine("Direct framebuffer write detected");
            }
            FramebufferDirty = false;
        }
        _renderCalled = false;
        CheckFramebufferWrite();
        PvrRuntime.Vblank();
        IntervalDetector.Vblank();
    }

    internal static partial void CheckFramebufferWrite()
    {
        uint size = (PvrRegisters.FB_R_SIZE.fb_y_size + 1)
            * (PvrRegisters.FB_R_SIZE.fb_x_size + PvrRegisters.FB_R_SIZE.fb_modulus) * 4;
        FramebufferWatchStart = (PvrRegisters.SPG_CONTROL.interlace != 0
            ? PvrRegisters.FB_R_SOF2 : PvrRegisters.FB_R_SOF1) & PvrMemory.VramMask;
        FramebufferWatchEnd = FramebufferWatchStart + size;
    }

    internal static partial void CancelEmulationWait()
    {
        if (PvrConfiguration.ThreadedRendering)
        {
            PowerVr2Ta.FinishRender(null);
            RenderEnd.Set();
            AllowRollback();
            MessageQueue.CancelEnqueue();
            MessageQueue.Enqueue(PvrMessageQueue.MessageType.Stop);
        }
    }

    internal static partial void SetFramebufferWriteAddress(uint framebufferWriteStart)
    {
        if ((framebufferWriteStart & 0x1000000) != 0)
            return;
        _framebufferWriteCurrent = framebufferWriteStart;
    }

    internal static partial void SwapFrame(uint framebufferReadStart)
    {
        if (!PvrConfiguration.EmulateFramebuffer && framebufferReadStart == _framebufferWriteCurrent && IsEnabled())
            MessageQueue.Enqueue(PvrMessageQueue.MessageType.Present);
    }

    internal static partial void DisableRollback() => VramRollback.Reset();

    internal static partial void AllowRollback() => VramRollback.Set();

    internal static partial void StartRollback()
    {
        if (PvrConfiguration.ThreadedRendering)
            VramRollback.Wait();
    }

    internal static partial void Enable(bool enabled) => _rendererEnabled = enabled;

    internal static partial bool IsEnabled() => _rendererEnabled;

    internal static partial void Serialize(BinaryWriter serializer)
    {
        serializer.Write(_framebufferWriteCurrent);
        serializer.Write(_renderCalled);
        serializer.Write(FramebufferDirty);
        serializer.Write(FramebufferWatchStart);
        serializer.Write(FramebufferWatchEnd);
    }

    internal static partial void Deserialize(BinaryReader deserializer, int version)
    {
        _framebufferWriteCurrent = deserializer.ReadUInt32();
        if (version >= 20)
        {
            _renderCalled = deserializer.ReadBoolean();
            FramebufferDirty = deserializer.ReadBoolean();
            FramebufferWatchStart = deserializer.ReadUInt32();
            FramebufferWatchEnd = deserializer.ReadUInt32();
        }
        _renderPending = false;
        FramebufferAddressHistory[0] = 1;
        FramebufferAddressHistory[1] = 1;
    }

    private sealed class ResetEvent
    {
        private readonly object _mutex = new();
        private bool _state;

        internal void Set()
        {
            lock (_mutex)
            {
                _state = true;
                Monitor.Pulse(_mutex);
            }
        }

        internal void Reset()
        {
            lock (_mutex)
                _state = false;
        }

        internal bool Wait(int timeoutMs)
        {
            lock (_mutex)
            {
                if (!_state)
                    Monitor.Wait(_mutex, timeoutMs);
                bool result = _state;
                _state = false;
                return result;
            }
        }

        internal void Wait()
        {
            lock (_mutex)
            {
                if (!_state)
                    Monitor.Wait(_mutex);
                _state = false;
            }
        }
    }
}
