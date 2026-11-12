using System.Diagnostics;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Memory.Common;

/// <summary>Complete asynchronous bus workload shared by vendor-specific tests.</summary>
internal static class AsynchronousSramThroughput
{
    internal static void Measure(Tmm2016Timing profile, ITestOutputHelper output)
    {
        foreach (bool observed in new[] { false,true })
        {
            var durations = new List<double>();
            for (int repetition = 0; repetition < 3; repetition++)
            {
                var chip = new Tmm2016(profile);
                var pins = new Tmm2016Pins(0,true,true,true,true,0x5a,255);
                chip.ReceivePins(1000,pins);
                chip.ReceivePins(1030,pins with { CsHigh = false });
                chip.ReceivePins(1060,pins with { CsHigh = false,WeHigh = false });
                chip.ReceivePins(1400,pins with { CsHigh = false });
                chip.ReceivePins(1450,pins);
                chip.AdvanceTo(1500);
                int changes = 0;
                if (observed) chip.OutputChanged = _ => changes++;
                try
                {
                    Run(chip,1600,4096,profile.Access);
                    changes = 0;
                    var watch = Stopwatch.StartNew();
                    uint sum = Run(chip,chip.TimeNanoseconds + 100,65536,profile.Access);
                    watch.Stop();
                    Assert.Equal(65536U * 0x5a,sum);
                    Assert.Equal(observed ? 65536 * 3 : 0,changes);
                    Assert.True(chip.IsTimingQualified);
                    durations.Add(watch.Elapsed.TotalMilliseconds);
                }
                finally { chip.OutputChanged = null; }
            }
            double median = durations.Order().ElementAt(1);
            output.WriteLine("{4}: 65536SRAM read transactions, observed={0}, ms=[{1}], median={2:F3}ms, {3:F0}accesses/s. No machine speed inferred.",
                observed,string.Join(",",durations.Select(value => value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture))),
                median,65536000 / median,profile.Id);
        }
        static uint Run(Tmm2016 chip,long time,int count,long access)
        {
            uint sum = 0;
            var pins = new Tmm2016Pins(0,true,false,false,true,0,0);
            for (int index = 0; index < count; index++)
            {
                chip.ReceivePins(time,pins); chip.AdvanceTo(time + access);
                var data = chip.Output;
                if (data.Known != 255 || !data.Driven) throw new InvalidOperationException("Invalid SRAM read.");
                sum += data.Data;
                chip.ReceivePins(time + access + 30,pins with { CsHigh = true,OeHigh = true });
                chip.AdvanceTo(time + access + 130); time += access + 180;
            }
            return sum;
        }
    }
}
