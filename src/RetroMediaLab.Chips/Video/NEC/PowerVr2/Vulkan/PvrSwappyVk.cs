using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal static partial class PvrSwappyVk
{
#if SWAPPY
    internal static partial void SetQueueFamilyIndex(bool googleTimingSupported, Device device, Queue queue, uint queueFamilyIndex);
    internal static partial void Init(PhysicalDevice physicalDevice, Device device, SwapchainKHR swapchain);
    internal static partial void SetSwapInterval(int interval);
    internal static partial Result Present(Queue queue, in PresentInfoKHR presentInfo);
    internal static partial void DetachSwapchain();
    internal static partial void DetachDevice();
#else
    internal static void SetQueueFamilyIndex(bool googleTimingSupported, Device device, Queue queue, uint queueFamilyIndex) { }
    internal static void Init(PhysicalDevice physicalDevice, Device device, SwapchainKHR swapchain) { }
    internal static void SetSwapInterval(int interval) { }

    internal static Result Present(Queue queue, in PresentInfoKHR presentInfo)
    {
        return PvrVulkanContext.Instance.SwapchainApi.QueuePresent(queue, presentInfo);
    }

    internal static void DetachSwapchain() { }
    internal static void DetachDevice() { }
#endif
}
