using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed partial class PvrVulkanCommandPool : IPvrVulkanFlightManager
{
    internal int GetIndex() => _index;

    public void AddToFlight(IPvrVulkanDeletable value)
    {
        _inFlightObjects[_index].Add(value);
    }

    public void Dispose() => Term();

    private int _index;
    private readonly List<List<PvrVulkanOwned<CommandBuffer>>> _freeBuffers = [];
    private readonly List<List<PvrVulkanOwned<CommandBuffer>>> _inFlightBuffers = [];
    private readonly List<bool> _lastBuffers = [];
    private readonly List<PvrVulkanOwned<CommandPool>> _commandPools = [];
    private readonly List<PvrVulkanOwned<Fence>> _fences = [];
    private int _chainSize;
    private readonly List<List<IPvrVulkanDeletable>> _inFlightObjects = [];
    private bool _frameStarted;
    private PvrVulkanDevice? _device;
}
