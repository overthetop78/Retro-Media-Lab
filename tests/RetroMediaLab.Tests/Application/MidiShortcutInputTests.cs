using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Services.Input.Midi;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Infrastructure.Settings.Emulation;

namespace RetroMediaLab.Tests.Application;

public sealed class MidiShortcutInputTests
{
    [Fact]
    public void MidiNoteCanCompleteAnOsdShortcutWithTheKeyboard()
    {
        var notes = new MidiShortcutInput.HeldNotes("midi:Stage%20Keys:1");
        var configured = EmulationShortcutMap.OsdShortcuts(new Dictionary<string, string>
        {
            [EmulationShortcutDefaults.OsdConfirm + EmulationShortcutDefaults.AlternativeSuffix] =
                "Keyboard:K+Controller:midi:Stage%20Keys:1:Ch2-N60"
        });
        string binding = Assert.Single(configured).SourceBinding!;
        Assert.True(MidiShortcutInput.IsMidiBinding(binding));
        Assert.Equal("Stage Keys", MidiShortcutInput.DeviceName(notes.Snapshot().DeviceId));

        float Pressure(params EmulationKey[] keys) => EmulationSdk.InputProcessing.InputSourcePressure(
            binding, EmulationControllerState.Empty, EmulationInputSnapshot.Empty with
            {
                Keys = keys.ToHashSet(),
                Controllers = [notes.Snapshot()]
            });

        notes.Receive(Message(0x90, 60, 100));
        Assert.Equal(0f, Pressure(EmulationKey.K));
        notes.Receive(Message(0x91, 60, 100));
        Assert.Equal(0f, Pressure());
        Assert.Equal(1f, Pressure(EmulationKey.K));
        notes.Receive(Message(0x91, 60, 0));
        Assert.Equal(0f, Pressure(EmulationKey.K));
        notes.Receive(Message(0x91, 60, 100));
        notes.Receive(Message(0x81, 60, 40));
        Assert.Equal(0f, Pressure(EmulationKey.K));
        Assert.True(notes.Snapshot().Controls.ContainsKey("Ch1-N60"));
    }

    private static int Message(int status, int data, int value) => status | (data << 8) | (value << 16);
}
