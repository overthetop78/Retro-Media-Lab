using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Enums;
using RetroMediaLab.MediaEngine.Constants;
using SkiaSharp;
using Xunit.Abstractions;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmu80186BootDiagnostic(ITestOutputHelper output)
{
    [Fact]
    public async Task BootActualDosPlusWithoutChangingTheUsersConfiguration()
    {
        var firmware = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Retro Media Lab", "Emulation", "Machines", "acorn", "Firmware");
        var bundle = File.ReadAllBytes(Path.Combine(firmware, "Acorn Master MOS 3.20 (128K).rom"));
        var client = File.ReadAllBytes(Path.Combine(firmware, "80186 Tube Client 1.01.rom"));
        var banks = new Dictionary<int, byte[]>();
        for (var bank = 9; bank <= 15; bank++)
            if (bank != 12) banks[bank] = bundle.AsSpan((bank - 8) * 16384, 16384).ToArray();
        var machine = new BbcMicroMachine(bundle.AsSpan(0, 16384), bundle.AsSpan(4 * 16384, 16384),
            32768, File.ReadAllBytes(Path.Combine(firmware, "saa5050")), diskInterface: true,
            optionRoms: banks, wd1770: true, expandedIo: true, master: true,
            coprocessor: MasterCoprocessor.Master512, coprocessorRom: client);
        IEmulationDiskImage? image = null;
        var clock = Stopwatch.StartNew();
        byte[]? frame = null;
        try
        {
            var cmos = machine.ReadNonvolatileMemory()!;
            cmos[19] = (byte)((cmos[19] & 0xf0) | 8); // Image de démarrage ADL : ADFS au RESET de ce diagnostic.
            machine.WriteNonvolatileMemory(cmos);
            machine.Reset(true);
            image = await new EmulationMediaProcessingService().OpenDiskAsync(
                Path.Combine(Path.GetTempPath(), "RetroMediaLab-80186-boot.img"),
                formatId: DiskImageFormatIds.AcornAdfs640);
            machine.AttachFloppy(0, image);
            output.WriteLine($"Disk: {image.Cylinders}/{image.Heads}/{image.SectorsPerTrack}/{image.SectorSize}");
            var card = Field<Master512Board>(machine, "_master512");
            var cpu = Field<Cpu80186>(card, "_cpu");
            for (var index = 0; index < 80; index++) frame = machine.RunFrame();
            machine.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.LeftShift } });
            machine.Reset(true);
            for (var index = 0; index < 500 && clock.Elapsed < TimeSpan.FromSeconds(90); index++)
            {
                frame = machine.RunFrame();
                if (index == 120) machine.SetInput(EmulationInputSnapshot.Empty);
                if (index % 100 == 0)
                    output.WriteLine($"Frame {index}: x86 {cpu.Segment(1):X4}:{cpu.IP:X4} host {machine.Cpu.PC:X4}, disk bytes {machine.FloppyReadBytes(0)}");
            }
            var destination = Path.Combine("F:\\GW GUI", "artifacts", "acorn-master", "master512-80186-audit.png");
            using (var bitmap = new SKBitmap(640, 256, SKColorType.Bgra8888, SKAlphaType.Opaque))
            {
                Marshal.Copy(frame!, 0, bitmap.GetPixels(), frame!.Length);
                using var picture = SKImage.FromBitmap(bitmap);
                using var encoded = picture.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(destination);
                encoded.SaveTo(stream);
            }
            Console.WriteLine($"Saved {destination}; duration {clock.Elapsed}; x86 {cpu.Segment(1):X4}:{cpu.IP:X4}");
            Console.WriteLine("Command code at 20000: " + string.Join(" ", Enumerable.Range(0, 64).Select(index => card.ReadMemory(0x20000 + index).ToString("X2"))));
            Assert.True(Field<int>(cpu, "_diagnosticInvalidCount") < 2, "Le système ne doit pas boucler dans son gestionnaire opcode invalide.");
            Assert.True(machine.FloppyReadBytes(0) > 65536, "Le diagnostic doit réellement charger le système depuis l'image.");
        }
        finally
        {
            machine.SetInput(EmulationInputSnapshot.Empty);
            machine.AttachFloppy(0, null);
            image?.Dispose();
            machine.Reset(true);
        }
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
}
