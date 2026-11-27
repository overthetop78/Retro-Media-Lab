using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed partial class PvrVulkanCommandPool
{
    internal void Init(int chainSize = 2)
    {
        _chainSize = chainSize;
        _device = PvrVulkanContext.Instance.GetDevice();
        if (_commandPools.Count > chainSize)
        {
            RemoveBack(_commandPools, chainSize);
            RemoveBack(_fences, chainSize);
        }
        else
        {
            while (_commandPools.Count < chainSize)
            {
                _commandPools.Add(_device.CreateCommandPoolUnique(CommandPoolCreateFlags.TransientBit,
                    PvrVulkanContext.Instance.GetGraphicsQueueFamilyIndex()));
                _fences.Add(_device.CreateFenceUnique(FenceCreateFlags.SignaledBit));
            }
        }
        ResizeLists(_freeBuffers, chainSize);
        ResizeLists(_inFlightBuffers, chainSize);
        ResizeLists(_inFlightObjects, chainSize);
    }

    internal void Term()
    {
        try
        {
            if (_fences.Count != 0)
            {
                Fence[] allFences = _fences.ConvertAll(fence => fence.Handle).ToArray();
                Result result = _device!.WaitForFences(allFences, true, ulong.MaxValue);
                if (result != Result.Success)
                    PvrRuntime.Log.Warn("CommandPool::Term: waitForFences failed {0}", (int)result);
            }
        }
        finally
        {
            ResizeLists(_inFlightObjects, 0);
            ResizeLists(_freeBuffers, 0);
            ResizeLists(_inFlightBuffers, 0);
            RemoveBack(_fences, 0);
            RemoveBack(_commandPools, 0);
        }
    }

    internal void BeginFrame()
    {
        if (_frameStarted)
            return;
        _frameStarted = true;
        _index = (_index + 1) % _chainSize;
        Result result = _device!.WaitForFences([_fences[_index].Handle], true, ulong.MaxValue);
        if (result != Result.Success)
            PvrRuntime.Log.Warn("CommandPool::BeginFrame: waitForFences failed {0}", (int)result);
        List<PvrVulkanOwned<CommandBuffer>> inFlight = _inFlightBuffers[_index];
        _freeBuffers[_index].AddRange(inFlight);
        inFlight.Clear();
        _device.ResetCommandPool(_commandPools[_index].Handle, CommandPoolResetFlags.ReleaseResourcesBit);
        RemoveBack(_inFlightObjects[_index], 0);
        _lastBuffers.Clear();
    }

    internal void EndFrame()
    {
        if (!_frameStarted)
            return;
        _frameStarted = false;
        CommandBuffer[] commandBuffers = _inFlightBuffers[_index].ConvertAll(buffer => buffer.Handle).ToArray();
        if (commandBuffers.Length != 0)
        {
            int length = commandBuffers.Length - 1;
            while (length != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (_lastBuffers[i] && !_lastBuffers[i + 1])
                    {
                        (_lastBuffers[i], _lastBuffers[i + 1]) = (_lastBuffers[i + 1], _lastBuffers[i]);
                        (commandBuffers[i], commandBuffers[i + 1]) = (commandBuffers[i + 1], commandBuffers[i]);
                    }
                }
                length--;
            }
        }
        _device!.ResetFences([_fences[_index].Handle]);
        PvrVulkanContext.Instance.SubmitCommandBuffers(commandBuffers, _fences[_index].Handle);
    }

    internal CommandBuffer Allocate(bool submitLast = false)
    {
        List<PvrVulkanOwned<CommandBuffer>> free = _freeBuffers[_index];
        List<PvrVulkanOwned<CommandBuffer>> inFlight = _inFlightBuffers[_index];
        if (free.Count == 0)
        {
            inFlight.Add(_device!.AllocateCommandBufferUnique(_commandPools[_index].Handle, CommandBufferLevel.Primary));
        }
        else
        {
            inFlight.Add(free[^1]);
            free.RemoveAt(free.Count - 1);
        }
        _lastBuffers.Add(submitLast);
        return inFlight[^1].Handle;
    }

    internal void EndFrameAndWait()
    {
        EndFrame();
        Result result = _device!.WaitForFences([_fences[_index].Handle], true, ulong.MaxValue);
        if (result != Result.Success)
            PvrRuntime.Log.Warn("CommandPool::waitForCommandCompletion: waitForFences failed {0}", (int)result);
        RemoveBack(_inFlightObjects[_index], 0);
    }

    private static void RemoveBack<T>(List<T> items, int count) where T : IDisposable
    {
        for (int i = items.Count - 1; i >= count; i--)
        {
            try
            {
                items[i].Dispose();
            }
            finally
            {
                items.RemoveAt(i);
            }
        }
    }

    private static void ResizeLists<T>(List<List<T>> items, int count) where T : IDisposable
    {
        for (int i = items.Count - 1; i >= count; i--)
        {
            try
            {
                RemoveBack(items[i], 0);
            }
            finally
            {
                items.RemoveAt(i);
            }
        }
        while (items.Count < count)
            items.Add([]);
    }
}
