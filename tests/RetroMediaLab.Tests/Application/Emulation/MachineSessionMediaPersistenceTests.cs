using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Application.Emulation;

public sealed class MachineSessionMediaPersistenceTests
{
    public static IEnumerable<object[]> MediaCases =>
        Enum.GetValues<EmulationMediaType>().SelectMany(type => new[] { false, true }.Select(powered => new object[] { type, powered }));

    [Theory]
    [MemberData(nameof(MediaCases))]
    public async Task EjectionPersistsAndReopeningDoesNotRestoreTheMedium(EmulationMediaType type, bool powered)
    {
        var slot = type switch
        {
            EmulationMediaType.Floppy => EmulationMediaSlot.Floppy0,
            EmulationMediaType.HardDisk => EmulationMediaSlot.HardDisk0,
            EmulationMediaType.CompactDisc => EmulationMediaSlot.Cd0,
            EmulationMediaType.Cartridge => EmulationMediaSlot.Cartridge0,
            EmulationMediaType.Cassette => EmulationMediaSlot.Cassette0,
            EmulationMediaType.MemoryCard => EmulationMediaSlot.MemoryCard0,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var medium = new EmulationMedia("synthetic-medium", slot, type, false, true);
        IReadOnlyList<EmulationMedia> saved = [medium];
        var saves = 0;
        var ejects = 0;
        var inserts = 0;
        var media = ControlledDependencies.Simulate<IEmulationMedia>((method, _) =>
        {
            switch (method.Name)
            {
                case nameof(IEmulationMedia.EjectAsync): ejects++; return ValueTask.CompletedTask;
                case nameof(IEmulationMedia.InsertAsync): inserts++; return ValueTask.CompletedTask;
                default: throw new InvalidOperationException(method.Name);
            }
        });
        var lifecycle = ControlledDependencies.Simulate<IEmulationLifecycle>((method, _) =>
            method.Name is nameof(IEmulationLifecycle.StartAsync) or nameof(IEmulationLifecycle.StopAsync)
                ? ValueTask.CompletedTask : throw new InvalidOperationException(method.Name));
        IEmulatedMachine CreateMachine() => ControlledDependencies.Simulate<IEmulatedMachine>((method, _) => method.Name switch
        {
            "get_Lifecycle" => lifecycle,
            "get_Media" => media,
            nameof(IAsyncDisposable.DisposeAsync) => ValueTask.CompletedTask,
            _ => throw new InvalidOperationException(method.Name)
        });
        await using (var session = new MachineSession(string.Empty, Guid.Empty, string.Empty, CreateMachine(), _ => CreateMachine(), saved,
            next => { saved = next.ToArray(); saves++; return Task.CompletedTask; }))
        {
            if (powered) await session.PowerOnAsync();
            await session.EjectAsync(slot, false);
            Assert.Empty(session.MountedMedia);
            Assert.Empty(saved);
            Assert.Equal(1, saves);
            Assert.Equal(powered ? 1 : 0, ejects);
            await session.InsertAsync(medium, false);
            Assert.Equal(medium, Assert.Single(saved));
            Assert.Equal(powered ? 1 : 0, inserts);
            await session.EjectAsync(slot, false);
            Assert.Empty(saved);
            Assert.Equal(3, saves);
        }
        await using var reopened = new MachineSession(CreateMachine(), _ => CreateMachine(), saved);
        Assert.Empty(reopened.MountedMedia);
    }
}
