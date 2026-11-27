namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal interface IPvrVulkanDeletable : IDisposable
{
}

internal interface IPvrVulkanFlightManager : IDisposable
{
    void AddToFlight(IPvrVulkanDeletable value);
}

internal sealed class PvrVulkanDeleter<T>(T value) : IPvrVulkanDeletable
{
    private T _value = value;

    public void Dispose()
    {
        try
        {
            if (_value is IDisposable owned)
                owned.Dispose();
        }
        finally
        {
            _value = default!;
        }
    }
}

internal sealed class PvrVulkanOwned<T>(T handle, Action<T> delete) : IDisposable where T : struct
{
    private Action<T>? _delete = delete;
    internal T Handle { get; private set; } = handle;

    public static implicit operator T(PvrVulkanOwned<T> owned) => owned.Handle;

    public void Dispose()
    {
        try
        {
            _delete?.Invoke(Handle);
        }
        finally
        {
            Handle = default;
            _delete = null;
        }
    }
}
