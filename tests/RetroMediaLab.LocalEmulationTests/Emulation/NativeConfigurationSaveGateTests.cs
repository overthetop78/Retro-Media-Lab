using RetroMediaLab.Emulation;
using System.IO;
using RetroMediaLab.Emulation.Functions;

namespace RetroMediaLab.Tests.Emulation;

public sealed class NativeConfigurationSaveGateTests
{
    public static IEnumerable<object[]> Stores()
    {
        yield return [typeof(RetroMediaLab.Emulation.Acorn.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Apple.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Atari.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Bandai.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Coleco.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Epoch.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Fairchild.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Mattel.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Panasonic.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Philips.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Sharp.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Sinclair.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Snk.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Thomson.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Watara.Common.Services.ConfigurationStore)];
        yield return [typeof(RetroMediaLab.Emulation.Wenquxing.Common.Services.ConfigurationStore)];
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task PublicNullGuardKeepsPriorityOverCancellation(Type type)
    {
        var store = Activator.CreateInstance(type, [Path.GetFullPath("."), null])!;
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var task = Assert.IsAssignableFrom<Task>(type.GetMethod("SaveAsync")!.Invoke(store, [null, stop.Token]));
        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => task);
        Assert.Equal("configuration", error.ParamName);
    }

    [Fact]
    public async Task InitialCancellationDoesNotRunWriterOrReleaseUnacquiredGate()
    {
        using var gate = new SemaphoreSlim(1, 1);
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            EmulationSdk.ConfigurationFiles.WithSaveGateAsync(gate,
                () => throw new InvalidOperationException("Writer must not run"), stop.Token));
        Assert.Equal(1, gate.CurrentCount);
    }

    [Fact]
    public async Task CancellationWhileWaitingDoesNotReleaseSomeoneElsesGate()
    {
        using var gate = new SemaphoreSlim(0, 1);
        using var stop = new CancellationTokenSource();
        Task? operation = null;
        try
        {
            operation = EmulationSdk.ConfigurationFiles.WithSaveGateAsync(gate,
                () => throw new InvalidOperationException("Writer must not run"), stop.Token);
            Assert.False(operation.IsCompleted);
            stop.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(0, gate.CurrentCount);
        }
        finally
        {
            stop.Cancel();
            if (operation is not null)
                try { await operation; }
                catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("cancel")]
    public async Task GateStaysAcquiredUntilWriterCompletesAndIsBorrowed(string outcome)
    {
        using var gate = new SemaphoreSlim(1, 1);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new IOException("Writer cleanup failed");
        Task? operation = null;
        try
        {
            operation = EmulationSdk.ConfigurationFiles.WithSaveGateAsync(gate, () =>
            {
                Assert.Equal(0, gate.CurrentCount);
                return completion.Task;
            }, CancellationToken.None);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, gate.CurrentCount);
            if (outcome == "failure")
            {
                completion.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => operation));
            }
            else if (outcome == "cancel")
            {
                completion.SetCanceled();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            }
            else
            {
                completion.SetResult();
                await operation;
            }
            Assert.Equal(1, gate.CurrentCount);
            Assert.True(gate.Wait(0));
            gate.Release();
        }
        finally
        {
            completion.TrySetResult();
            if (operation is not null)
                try { await operation; }
                catch (Exception error) when (ReferenceEquals(error, failure) || error is OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SynchronousWriterFailureAlsoReleasesTheGate()
    {
        using var gate = new SemaphoreSlim(1, 1);
        var failure = new InvalidOperationException("Synchronous writer failure");
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EmulationSdk.ConfigurationFiles.WithSaveGateAsync(gate, () => throw failure, CancellationToken.None)));
        Assert.Equal(1, gate.CurrentCount);
    }

    [Fact]
    public async Task CancellationAfterAcquisitionDoesNotDetachAnOngoingWriter()
    {
        using var gate = new SemaphoreSlim(1, 1);
        using var stop = new CancellationTokenSource();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? operation = null;
        try
        {
            operation = EmulationSdk.ConfigurationFiles.WithSaveGateAsync(gate, () => completion.Task, stop.Token);
            stop.Cancel();
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, gate.CurrentCount);
            completion.SetResult();
            await operation;
            Assert.Equal(1, gate.CurrentCount);
        }
        finally
        {
            completion.TrySetResult();
            if (operation is not null) await operation;
        }
    }
}
