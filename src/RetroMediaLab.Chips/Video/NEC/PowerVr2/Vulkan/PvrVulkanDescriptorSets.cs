using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed class PvrVulkanDescriptorSets
{
    internal PvrVulkanDescriptorSets()
    {
        _descSetsInFlight.Add([]);
    }

    internal void SetLayout(DescriptorSetLayout layout)
    {
        _layout = layout;
    }

    internal void SetAllocChunk(int size)
    {
        _allocChunk = size;
    }

    internal void NextFrame()
    {
        int swapChainSize = (int)PvrVulkanContext.Instance.GetSwapChainSize();
        if (swapChainSize > _descSetsInFlight.Count)
        {
            while (_descSetsInFlight.Count < swapChainSize)
                _descSetsInFlight.Add([]);
        }
        else
        {
            while (swapChainSize < _descSetsInFlight.Count)
            {
                _descSets.AddRange(_descSetsInFlight[^1]);
                _descSetsInFlight.RemoveAt(_descSetsInFlight.Count - 1);
            }
        }

        _index = (_index + 1) % _descSetsInFlight.Count;
        _descSets.AddRange(_descSetsInFlight[_index]);
        _descSetsInFlight[_index].Clear();
    }

    internal DescriptorSet Alloc()
    {
        if (_descSets.Count == 0)
        {
            DescriptorSetLayout[] layouts = new DescriptorSetLayout[_allocChunk];
            Array.Fill(layouts, _layout);
            var context = PvrVulkanContext.Instance;
            _descSets.AddRange(context.GetDevice().AllocateDescriptorSetsUnique(context.GetDescriptorPool(), layouts));
        }

        _descSetsInFlight[_index].Add(_descSets[^1]);
        _descSets.RemoveAt(_descSets.Count - 1);
        return _descSetsInFlight[_index][^1].Handle;
    }

    internal void Term()
    {
        try
        {
            foreach (var set in _descSets)
                set.Dispose();
        }
        finally
        {
            _descSets.Clear();
            try
            {
                foreach (var frame in _descSetsInFlight)
                    foreach (var set in frame)
                        set.Dispose();
            }
            finally
            {
                _descSetsInFlight.Clear();
            }
        }
    }

    private DescriptorSetLayout _layout;
    private readonly List<PvrVulkanOwned<DescriptorSet>> _descSets = [];
    private readonly List<List<PvrVulkanOwned<DescriptorSet>>> _descSetsInFlight = [];
    private int _index;
    private int _allocChunk = 10;
}
