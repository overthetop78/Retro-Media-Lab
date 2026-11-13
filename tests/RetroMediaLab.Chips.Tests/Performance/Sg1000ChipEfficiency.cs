using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Numerics;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Models;
using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Services;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using RetroMediaLab.Chips.Memory.Fujitsu.MB84xx.MB8464.Models;
using RetroMediaLab.Chips.Memory.NEC.UPD40xx.UPD4016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.NEC.UPD23xx.UPD23256A.Models;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Tests.Performance;

/// <summary>Explicit manual benchmark, not part of automatic correctness runs.
/// Uses public physical inputs only. No machine, native resource or user image is owned.</summary>
public static class Sg1000ChipEfficiency
{
    private const int Samples = 5;
    private const int Repetitions = 8;
    private sealed record Counts(long Units, long Time, long Signals);
    private sealed record Sample(double Milliseconds, long Units, long EmulatedNanoseconds, long Signals, long AllocatedBytes);
    private sealed record Result(string Chip, string Route, string Unit, string Conditions,
        List<Sample> Samples, double MillionUnitsPerSecond, double NanosecondsPerUnit,
        string StateHash, string? Error);
    private sealed record Fixture(string Chip, string Route, string Unit, string Conditions,
        int Batch, Action<int> Run, Func<Counts> Count, Func<object> State, Action Check, Action Cleanup);
    private delegate LogicOutputRange LogicRun(in LogicInputRange range);
    private delegate MemoryOutputRange<TOut> MemoryRun<TIn,TOut>(in MemoryInputRange<TIn> range, bool retainResponseHistory) where TIn:struct where TOut:struct;

    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var results = new List<Result>();
        var factories = new List<Func<Fixture>>();
        foreach (bool grouped in new[] { false, true })
        {
            bool g = grouped;
            factories.Add(() => Cpu(false,g)); factories.Add(() => Cpu(true,g));
            factories.Add(() => Logic(139,g)); factories.Add(() => Logic(32,g)); factories.Add(() => Logic(257,g));
            factories.Add(() => Ram2009(g));
            factories.Add(() => Ram2016(Mb8464Profile.Grade15L,g));
            factories.Add(() => Ram2016(Upd4016Profile.Grade2,g));
            foreach (int capacity in new[] {8192,16384,32768})
            {
                int c = capacity;
                factories.Add(() => Rom(new($"logical-{c}-unqualified",0,0,0,0,0,0,0,0,0,
                    Tmm23256OutputEnableMode.ActiveLow,false,capacity:c,electricallyCharacterized:false),g));
            }
            factories.Add(() => Rom(Upd23256AProfile.ActiveLow,g));
            factories.Add(() => Rom(new("MB83256-MPR10152-unqualified-grade",250,100,80,0,0,0,0,0,0,
                Tmm23256OutputEnableMode.ActiveLow,false,electricallyCharacterized:false),g));
            factories.Add(() => Dram(1,g)); factories.Add(() => Dram(8,g));
            factories.Add(() => Video(g)); factories.Add(() => Audio(g));
        }
        foreach (var factory in factories)
        {
            Fixture? f = null;
            var samples = new List<Sample>();
            try
            {
                f = factory();
                // Warm the actual workload for tiered compilation before measuring.
                for (int i=0;i<8;i++) f.Run(f.Batch);
                f.Check();
                for (int i=0;i<Samples;i++)
                {
                    var before = f.Count();
                    long allocation = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    for(int repeat=0;repeat<Repetitions;repeat++) f.Run(f.Batch);
                    long stop = Stopwatch.GetTimestamp();
                    long bytes = GC.GetAllocatedBytesForCurrentThread()-allocation;
                    var after = f.Count();
                    samples.Add(new(Stopwatch.GetElapsedTime(start,stop).TotalMilliseconds,
                        after.Units-before.Units,after.Time-before.Time,after.Signals-before.Signals,bytes));
                    f.Check();
                }
                var median = samples.OrderBy(s=>s.Milliseconds/s.Units).ElementAt(Samples/2);
                results.Add(new(f.Chip,f.Route,f.Unit,f.Conditions,samples,
                    median.Units/median.Milliseconds/1000,median.Milliseconds*1e6/median.Units,
                    Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(f.State()))),null));
            }
            catch (Exception ex)
            {
                results.Add(new(f?.Chip ?? "initialization",f?.Route ?? "",f?.Unit ?? "",f?.Conditions ?? "",
                    samples,0,0,"",ex.ToString()));
            }
            finally { f?.Cleanup(); }
        }
        var mismatches = results.Where(r=>r.Error is null).GroupBy(r=>r.Chip)
            .Where(g=>g.Count()!=2 || g.Select(r=>r.StateHash).Distinct().Count()!=1)
            .Select(g=>g.Key).ToArray();
        var assembly = typeof(CpuZ80).Assembly;
        var report = new { Utc=DateTime.UtcNow,Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Machine=Environment.MachineName,DebuggerAttached=Debugger.IsAttached,
            JitOptimizerDisabled=assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled,
            ChipsSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
            TieredCompilation=Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
            WarmupBatches=8,MeasuredBatches=Samples,BatchesPerSample=Repetitions,StateMismatches=mismatches,Results=results };
        File.WriteAllText(Path.Combine(directory,"results.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        var md = new StringBuilder("# Efficacité individuelle SG-1000\n\nMesures séquentielles, 8 lots d'échauffement puis 5 échantillons de 8 lots chacun. Compilation JIT par paliers désactivée par le lanceur pour éviter une recompilation au milieu des mesures. Médiane du coût par unité; construction, contrôles et empreintes hors chronométrage. Aucun modèle de puce modifié.\n\n");
        md.AppendLine($"Runtime : {report.Runtime}; optimiseur JIT désactivé : {report.JitOptimizerDisabled}; SHA256 Chips : `{report.ChipsSha256}`.\n");
        md.AppendLine("| Composant / profil | Parcours | Unité | Millions unités/s | ns/unité | Octets alloués/lot (médiane) |\n|---|---|---|---:|---:|---:|");
        foreach (var r in results)
            md.AppendLine($"| {r.Chip} | {r.Route} | {r.Unit} | {r.MillionUnitsPerSecond:F4} | {r.NanosecondsPerUnit:F2} | {(r.Samples.Count==0?0:r.Samples.Select(s=>s.AllocatedBytes).Order().ElementAt(r.Samples.Count/2))} |");
        md.AppendLine("\n## Conditions et limites\n");
        foreach (var g in results.GroupBy(r=>r.Chip)) md.AppendLine($"- **{g.Key}** : {g.First().Conditions}");
        md.AppendLine("\nLes deux parcours reçoivent les mêmes entrées datées. Comparaison de leur état capturé complet après les mêmes lots; ce contrôle ne constitue pas une validation de conformité au silicium. Les mesures isolées n'annoncent aucun gain machine. Les plages incluent leur préparation et la réception des sorties; les observateurs sont indiqués ci-dessus.");
        md.AppendLine($"\nÉcarts d'état : {(mismatches.Length==0?"aucun":string.Join(", ",mismatches))}. Erreurs : {results.Count(r=>r.Error!=null)}.");
        foreach (var r in results.Where(r=>r.Error!=null)) md.AppendLine($"\n### {r.Chip} / {r.Route}\n```\n{r.Error}\n```");
        File.WriteAllText(Path.Combine(directory,"results.md"),md.ToString());
        Assert.True(results.All(r=>r.Error is null) && mismatches.Length==0,
            $"Benchmark errors={results.Count(r=>r.Error!=null)}; state mismatches={string.Join(',',mismatches)}. See {directory}");
    }

    private static string Route(bool grouped) => grouped ? "plages/source" : "broches/fronts";
    private static void Require(bool value,string reason) { if (!value) throw new InvalidOperationException(reason); }
    private static long CpuDate(long edge) => (long)((Int128)edge*3_000_000_000/21_477_270);

    private static Fixture Cpu(bool nec,bool grouped)
    {
        var cpu = new CpuZ80(nec?Upd780C1Profile.Hardware:Z80HardwareProfile.ZilogNmos,
            nec?Upd780C1Profile.Timing:Z80BusTiming.ZilogZ80AMaximum,new(){SP=0xe000},default,255);
        byte[] memory = new byte[65536];
        byte[] program = [0x21,0,0xc0,0x7e,0xc6,0x17,0x77,0x23,0x7d,0xe6,0x3f,0x20,0xf6,0x18,0xf1];
        program.CopyTo(memory,0);
        long signals=0,edge=1;
        Z80BusPins previous=cpu.Pins;
        void Receive(Z80BusPins pins)
        {
            signals++;
            if(previous.WriteHigh==false && pins.WriteHigh!=false && previous.MemoryRequestHigh==false
                && previous.Address is {} wa && previous.Data is {} wd) memory[wa]=wd;
            if(pins.ReadHigh==false && pins.Address is {} a) cpu.SetData(pins.MemoryRequestHigh==false?memory[a]:(byte)255);
            previous=pins;
        }
        Z80BusRangeHandler receiver = (in Z80BusRange range) =>
        {
            foreach (ref readonly var change in range.Changes) Receive(change.Pins);
            cpu.ReceiveBusInputWindow(new(long.MaxValue,long.MaxValue,0,0));
        };
        if(!grouped) cpu.PinsChanged=Receive;
        cpu.SetClock(true); cpu.ReceiveClockSource(new(0,3_000_000_000,21_477_270,1,false));
        return new(nec?"CPU NEC µPD780C-1":"CPU Zilog Z80 NMOS",Route(grouped),"clocks",
            "Boucle LD/ADD/écriture/sauts; bus mémoire idéal externe 64K via broches, WAIT/INT stables. Notifications de bus comptées; MHz = clocks CPU, pas instructions.",16384,
            n=>{long end=CpuDate(edge+2L*n); if(grouped)cpu.AdvanceBusRangeUntil(end,receiver);else cpu.AdvanceClockSourceUntil(end);
                cpu.AdvanceTo(end-1);edge+=2L*n;},
            ()=>new(cpu.InputClockCount,cpu.TimeNanoseconds,signals),
            ()=>new{Cpu=cpu.CaptureState(),Memory=Convert.ToHexString(SHA256.HashData(memory))},
            ()=>Require(!cpu.IsFaulted && cpu.InstructionsCompleted>0,"CPU fault/no instructions"),
            ()=>{cpu.PinsChanged=null;cpu.NextEventNanosecondsChanged=null;});
    }

    private static Fixture Logic(int type,bool grouped)
    {
        Action<ushort> set;Action<long> advance;Func<object> state;Action cleanup;LogicRun run;
        long signals=0,words=0,time=0;
        int mask;
        if(type==139)
        {
            var c=new Sn74Ls139(Sn74Ls139Timing.CharacterizedMaximum);mask=63;
            set=v=>c.SetInputs((byte)v,63);advance=c.AdvanceTo;state=()=>c.CaptureState();run=c.ExecuteInputRange;
            if(!grouped)c.OutputChanged=(_,_)=>signals++;
            cleanup=()=>{c.OutputChanged=null;c.NextEventNanosecondsChanged=null;};
        }
        else if(type==257)
        {
            var c=new Sn74Ls257(Sn74Ls257Timing.CharacterizedMaximum);mask=1023;
            set=v=>c.SetInputs(v,1023);advance=c.AdvanceTo;state=()=>c.CaptureState();run=c.ExecuteInputRange;
            if(!grouped)c.OutputChanged=(_,_)=>signals++;
            cleanup=()=>{c.OutputChanged=null;c.NextEventNanosecondsChanged=null;};
        }
        else
        {
            var c=new Sn74Ls32(Sn74Ls32Timing.CharacterizedMaximum);mask=255;
            set=v=>c.SetInputs((byte)((v&1)|((v>>1)&2)|((v>>2)&4)|((v>>3)&8)),15,
                (byte)(((v>>1)&1)|((v>>2)&2)|((v>>3)&4)|((v>>4)&8)),15);
            advance=c.AdvanceTo;state=()=>c.CaptureState();run=c.ExecuteInputRange;
            if(!grouped)c.OutputChanged=(_,_)=>signals++;
            cleanup=()=>{c.OutputChanged=null;c.NextEventNanosecondsChanged=null;};
        }
        var input=new TimedLogicInput[128];
        return new($"SN74LS{type} maximum",Route(grouped),"mots entrée","Mots complets variables, pas de 1 µs, délais physiques conservés; transitions de sortie comptées.",8192,
            n=>{for(int done=0;done<n;){int count=Math.Min(128,n-done);
                for(int j=0;j<count;j++){ushort v=(ushort)((words++*37)&mask);input[j]=new(time,v,(ulong)mask);time+=1000;
                    if(!grouped){advance(input[j].TimeNanoseconds);set(v);}}
                if(grouped){var output=run(new(input.AsSpan(0,count),time));foreach(ref readonly var change in output.Changes)signals+=BitOperations.PopCount(change.ChangedPins);}
                else advance(time);done+=count;}},()=>new(words,time,signals),state,()=>{},cleanup);
    }

    private static Fixture Memory<TIn,TOut>(string chip,string conditions,bool grouped,int phases,int batch,
        Func<long,long,int,TIn> pins,Action<long,TIn> receive,Action<long> advance,
        MemoryRun<TIn,TOut> execute,Func<object> state,Action check,Action cleanup,Action<Action> attach) where TIn:struct where TOut:struct
    {
        long units=0,time=1_000_000,signals=0;
        advance(time);
        if(!grouped)attach(()=>signals++);
        var input=new TimedMemoryInput<TIn>[128];
        return new(chip,Route(grouped),phases==3?"lectures":"paires écrire/lire",conditions,batch,
            n=>{for(int done=0;done<n;){int count=Math.Min(128/phases,n-done),k=0;
                for(int i=0;i<count;i++){for(int p=0;p<phases;p++){long date=time+p*1000;var value=pins(units,time,p);
                    input[k++]=new(date,value);if(!grouped)receive(date,value);}time+=(phases+1)*1000;units++;}
                if(grouped)signals+=execute(new(input.AsSpan(0,k),time),false).ResponseCount;else advance(time);done+=count;}},
            ()=>new(units,time,signals),state,check,cleanup);
    }

    private static Fixture Ram2009(bool grouped)
    {
        var c=new Tmm2009(false);byte last=0;
        return Memory<Tmm2009Pins,Tmm2009Output>("TMM2009 non caractérisée",
            "1K; quatre changements par paire écriture/lecture, 1 µs entre phases. 1 paire = 2 accès. Délais électriques inconnus, aucune fréquence matérielle déduite.",grouped,4,4096,
            (i,t,p)=>{last=(byte)i;return new((ushort)(i&1023),true,false,false,p!=3,p!=1,last,255);},
            c.ReceivePins,c.AdvanceTo,c.ExecuteInputRange,()=>c.CaptureState(),
            ()=>Require(c.Output.Driven && c.Output.Known==255 && c.Output.Data==last,"2009 write/read mismatch"),
            ()=>c.OutputChanged=null,notify=>c.OutputChanged=_=>notify());
    }

    private static Fixture Ram2016(Tmm2016Timing timing,bool grouped)
    {
        var c=new Tmm2016(timing);byte last=0;
        return Memory<Tmm2016Pins,Tmm2016Output>($"SRAM {timing.Id}",
            "Profil du montage; cinq phases, 1 µs entre phases, désélection après écriture puis relecture. 1 paire = 2 accès. Sorties comptées.",grouped,5,4096,
            (i,t,p)=>{last=(byte)i;return new((ushort)(i&(timing.Capacity-1)),true,p is 0 or 3,p!=4,p!=2,last,255,true);},
            c.ReceivePins,c.AdvanceTo,c.ExecuteInputRange,()=>c.CaptureState(),
            ()=>Require(c.Output.Driven && c.Output.Known==255 && c.Output.Data==last,"SRAM write/read mismatch"),
            ()=>{c.OutputChanged=null;c.NextEventNanosecondsChanged=null;},notify=>c.OutputChanged=_=>notify());
    }

    private static Fixture Rom(Tmm23256Timing timing,bool grouped)
    {
        var bytes=Enumerable.Range(0,timing.Capacity).Select(i=>(byte)(i*13)).ToArray();
        var c=new Tmm23256(bytes,timing);byte last=0;
        return Memory<Tmm23256Pins,Tmm23256Output>($"ROM {timing.Id}",
            "Profil utilisé par le montage; trois phases CE/OE, pas de 1 µs, adresses variables. ROM initialisée hors mesure; sorties comptées. Profils unqualified sans preuve de grade physique.",grouped,3,4096,
            (i,t,p)=>{ushort a=(ushort)(i&(timing.Capacity-1));last=bytes[a];return new(a,true,p==0,p!=2);},
            c.ReceivePins,c.AdvanceTo,c.ExecuteInputRange,()=>c.CaptureState(),
            ()=>Require(c.Output.Driven && c.Output.Known && c.Output.Data==last,"ROM output mismatch"),
            ()=>{c.OutputChanged=null;c.NextEventNanosecondsChanged=null;},notify=>c.OutputChanged=_=>notify());
    }

    private static Fixture Dram(int owners,bool grouped)
    {
        var chips=Enumerable.Range(0,owners).Select(_=>new Mb8118(Mb8118Timing.Speed12)).ToArray();
        long time=200_010,units=0,signals=0;
        foreach(var c in chips)
        {
            c.AdvanceTo(200_000);var s=c.CaptureState();
            for(int a=0;a<s.Cells.Length;a++)s.Cells[a]=(byte)(1+(a&1));
            Array.Fill(s.Refreshed,200_000L);c.RestoreState(s with {StartupCycles=8});
            c.OutputChanged=_=>signals++;
        }
        return new($"MB8118 Speed12 ×{owners}",Route(grouped),owners==1?"bits lus":"octets lus",
            "États distincts, cellules et démarrage préparés hors mesure. Lecture RAS+45/CAS+85/RAS haut+270/CAS haut+335 ns, période 1 µs. Speed12 = profil câblé, pas identification du grade de la carte; sorties observées.",2048,
            n=>{for(int i=0;i<n;i++){byte row=(byte)(units&127),col=(byte)((units*13)&127);
                if(grouped){var cycle=new Mb8118ReadCycle(time,time+45,time+85,time+270,time+335,row,col,true,true,null,null);
                    foreach(var c in chips)c.ReceiveReadCycle(in cycle,null,null);}
                else foreach(var c in chips){c.ReceiveMultiplexedPins(time,row,true,true,true,true,null);
                    c.ReceiveMultiplexedPins(time+45,row,true,false,true,true,null);
                    c.ReceiveMultiplexedPins(time+85,col,true,false,false,true,null);
                    c.ReceiveMultiplexedPins(time+270,col,true,true,false,true,null);
                    c.ReceiveMultiplexedPins(time+335,col,true,true,true,true,null);}
                time+=1000;foreach(var c in chips)c.AdvanceTo(time);units++;}},
            ()=>new(units,time,signals),()=>chips.Select(c=>c.CaptureState()).ToArray(),
            ()=>Require(chips.All(c=>c.IsTimingQualified),"DRAM timing violated"),
            ()=>{foreach(var c in chips){c.OutputChanged=null;c.NextEventNanosecondsChanged=null;}});
    }

    private static Fixture Video(bool grouped)
    {
        var c=new Tms9918A(new(new(0,0xc0,6,0x80,0,0x7e,7,0xe3),0,default,
            new(null,0,Tms9918CpuVramOperation.None,false),new(0,true,true,true,true)),Tms9918CpuTiming.SpecifiedMaximum);
        long edge=1,signals=0;ulong pixels=0;
        c.SetVramReadData(0,0,true);c.VideoSignalProduced=v=>{signals++;pixels=unchecked(pixels*31+(ulong)v.GetHashCode());};
        long Date(long e)=>(long)((Int128)e*1_000_000_000/21_477_270);
        c.ReceiveMasterClockSource(new(0,1_000_000_000,21_477_270,1,true));
        return new("TMS9918A NTSC",Route(grouped),"clocks maître",
            "Affichage actif, entrée VRAM externe constante connue; aucun CPU/DRAM connecté. Chaque signal vidéo observé; coût propre VDP, pas coût machine.",21888,
            n=>{long end=Date(edge+n);if(grouped)c.AdvanceMasterClockSourceUntil(end);
                else for(long e=edge;e<edge+n;e++)c.ReceiveMasterClock(Date(e),(e&1)!=0);
                c.AdvanceTo(end-1);edge+=n;},()=>new((edge-1)/2,c.TimeNanoseconds,signals),
            ()=>new{State=c.CaptureState(),Pixels=pixels},()=>Require(signals>0,"No video signals"),
            ()=>c.VideoSignalProduced=null);
    }

    private static Fixture Audio(bool grouped)
    {
        var c=new Sn76489(150);long time=0,signals=0;
        c.ReceiveClockSource(new(0,3_000_000_000,21_477_270,1,true));
        foreach(byte command in new byte[]{0x84,0x02,0x90,0xa6,0x01,0xb0,0xc8,0x03,0xd0,0xe4,0xf0})
        {c.SetBus(time,command,true,true);c.AdvanceTo(time+20_000);c.SetBus(time+20_000,command,false,false);c.AdvanceTo(time+22_000);time+=22_000;}
        long edge=(long)(((Int128)(time+1)*21_477_270+2_999_999_999)/3_000_000_000);
        c.OutputChanged=_=>signals++;
        return new("SN76489AN READY150ns",Route(grouped),"clocks entrée",
            "Trois tonalités et bruit actifs, programmation CE/WE datée hors mesure; toutes les transitions audio observées. READY nominal 150 ns.",16384,
            n=>{long end=CpuDate(edge+2L*n);if(!grouped)for(long e=edge;e<edge+2L*n;e++)c.ReceiveClock(CpuDate(e),(e&1)!=0);
                c.AdvanceTo(end-1);edge+=2L*n;},()=>new(c.InputClockCount,c.TimeNanoseconds,signals),
            ()=>c.CaptureState(),()=>Require(signals>0,"No audio transitions"),
            ()=>{c.OutputChanged=null;c.ReadyChanged=null;c.NextEventNanosecondsChanged=null;});
    }
}

public sealed class Mb8118ReadWaveformTests
{
    [Fact]
    public void DeadlineCacheIsCurrentInsideCallbacksAndAfterCancellationOrRestore()
    {
        var chip=Seed(Mb8118Timing.Speed12);
        const long origin=200_010;
        bool observed=false;
        chip.NextEventNanosecondsChanged=next=>Assert.Equal(next,chip.NextEventNanoseconds);
        chip.OutputChanged=_=>
        {
            observed=true;
            Assert.Equal(origin+385,chip.NextEventNanoseconds);
            chip.CancelReadCycle(chip.TimeNanoseconds);
            Assert.Null(chip.NextEventNanoseconds);
        };
        try
        {
            chip.ReceiveReadCycle(new(origin,origin+45,origin+85,origin+270,origin+335,3,7,true,true,false,true));
            Assert.Equal(origin+165,chip.NextEventNanoseconds);
            chip.AdvanceTo(origin+165);
            Assert.True(observed);Assert.Null(chip.NextEventNanoseconds);
            var saved=chip.CaptureState();
            chip.RestoreState(saved);Assert.Null(chip.NextEventNanoseconds);
        }
        finally{chip.OutputChanged=null;chip.NextEventNanosecondsChanged=null;}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryPhaseAndCancellationMatchesPhysicalInputs(bool faster)
    {
        foreach(int cancellation in new[]{-1,0,44,45,84,85,164,165,269,270,334,335})
            Compare(faster,cancellation,false);
        Compare(faster,-1,true);
    }

    private static Mb8118 Seed(Mb8118Timing timing)
    {
        var c=new Mb8118(timing);c.AdvanceTo(200_000);var s=c.CaptureState();
        for(int i=0;i<s.Cells.Length;i++)s.Cells[i]=(byte)(1+(i&1));
        Array.Fill(s.Refreshed,200_000L);c.RestoreState(s with {StartupCycles=8});return c;
    }

    private static void Compare(bool faster,int cancellation,bool coincident)
    {
        const long origin=200_010;
        var timing=faster?Mb8118Timing.Speed10:Mb8118Timing.Speed12;
        var scalar=Seed(timing);var range=Seed(timing);
        var a=new List<(long,LogicPinState)>();var b=new List<(long,LogicPinState)>();
        scalar.OutputChanged=p=>a.Add((scalar.TimeNanoseconds,p));
        range.OutputChanged=p=>b.Add((range.TimeNanoseconds,p));
        long[] dates=coincident?[origin,origin,origin,origin,origin]:[origin,origin+45,origin+85,origin+270,origin+335];
        Mb8118Pins[] pins=[new(3,true,true,true,true,false),new(3,true,false,true,true,false),
            new(7,true,false,false,true,true),new(7,true,true,false,true,true),new(7,true,true,true,true,true)];
        int next=0;
        try
        {
            range.ReceiveReadCycle(new(origin,dates[1],dates[2],dates[3],dates[4],3,7,true,true,false,true));
            long[] offsets=coincident?[0,1,49,50,99,100,119,120,400]:
                [0,44,45,46,84,85,86,119,120,149,150,164,165,166,269,270,271,334,335,336,384,385,400];
            foreach(long offset in offsets)
            {
                if(cancellation>=0 && offset>cancellation)break;
                long now=origin+offset;
                while(next<dates.Length && dates[next]<=now){scalar.ReceivePins(dates[next],pins[next]);next++;}
                scalar.AdvanceTo(now);range.AdvanceTo(now);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),JsonSerializer.Serialize(range.CaptureState()));
                Assert.Equal(a,b);
            }
            if(cancellation>=0)
            {
                long now=origin+cancellation;
                while(next<dates.Length && dates[next]<=now){scalar.ReceivePins(dates[next],pins[next]);next++;}
                scalar.AdvanceTo(now);range.CancelReadCycle(now);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),JsonSerializer.Serialize(range.CaptureState()));
                // Explicit release after cancellation exercises replacement of a partial prefix.
                var release=new Mb8118Pins(7,true,true,true,true,true);
                scalar.ReceivePins(now+1,release);range.ReceivePins(now+1,release);
                scalar.AdvanceTo(now+1000);range.AdvanceTo(now+1000);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),JsonSerializer.Serialize(range.CaptureState()));
                Assert.Equal(a,b);
            }
        }
        finally{scalar.OutputChanged=null;range.OutputChanged=null;}
    }
}


public sealed class VdpClockBoundaryTests
{
    [Fact]
    public void CoincidentVramTransitionSummaryPreservesExcursionsAndLaterObservations()
    {
        Tms9918A Make() => new(new(default, 0, default, new(null, 0, Tms9918CpuVramOperation.None, false),
            new(0, true, true, true, true)), Tms9918CpuTiming.SpecifiedMaximum);
        ushort[][] sequences = [[0, 256, 0], [255, 0, 255], [7, 7], [259, 511, 0, 259], [0], [511, 0, 256]];
        foreach (ushort initial in new ushort[] { 0, 255, 256, 511 })
        foreach (var words in sequences)
        {
            var explicitInputs = Make();
            var condensedInputs = Make();
            explicitInputs.SetVramReadData(100, (byte)initial, (initial & 256) != 0);
            condensedInputs.SetVramReadData(100, (byte)initial, (initial & 256) != 0);
            ushort previous = words[0];
            explicitInputs.SetVramReadData(200, (byte)previous, (previous & 256) != 0);
            condensedInputs.SetVramReadData(200, (byte)previous, (previous & 256) != 0);
            Assert.Equal(explicitInputs.CaptureState().VramBus, condensedInputs.CaptureState().VramBus);
            bool transitioned = false;
            foreach (ushort word in words.AsSpan(1))
            {
                explicitInputs.SetVramReadData(200, (byte)word, (word & 256) != 0);
                transitioned |= word != previous;
                previous = word;
            }
            condensedInputs.SetVramReadData(200, (byte)previous, (previous & 256) != 0, transitioned);
            Assert.Equal(explicitInputs.CaptureState().VramBus, condensedInputs.CaptureState().VramBus);
            explicitInputs.SetVramReadData(300, (byte)previous, (previous & 256) != 0);
            condensedInputs.SetVramReadData(300, (byte)previous, (previous & 256) != 0);
            Assert.Equal(explicitInputs.CaptureState().VramBus, condensedInputs.CaptureState().VramBus);
        }
    }

    [Fact]
    public void SameDateDataKnowledgeExcursionCannotBeReplacedByItsFinalWord()
    {
        Tms9918A Make()=>new(new(default,0,default,new(null,0,Tms9918CpuVramOperation.None,false),
            new(0,true,true,true,true)),Tms9918CpuTiming.SpecifiedMaximum);
        var transitions=Make();var finalWord=Make();
        transitions.SetVramReadData(100,0,false);finalWord.SetVramReadData(100,0,false);
        transitions.SetVramReadData(200,0,true);
        transitions.SetVramReadData(200,0,false);
        finalWord.SetVramReadData(200,0,false);
        Assert.Equal(200,transitions.CaptureState().VramBus.InputDataChanged);
        Assert.Equal(0,finalWord.CaptureState().VramBus.InputDataChanged);
    }

    [Fact]
    public void ClockPrefixConsumesExactlyTheRepresentableEdgesAndRemainder()
    {
        var sources = new RationalClockSource[]
        {
            new(0, 1_000_000_000, 21_477_270, 1, true),
            new(19, 1234, 17, 3, false, 12),
            new(0, 1, 1, 0, true),
            new(long.MaxValue - 100, 3, 1, 1, true),
            new(0, long.MaxValue / 2, long.MaxValue / 3, 0, true),
        };
        foreach (var source in sources)
        foreach (int maximum in new[] { 1, 2, 3, 4, 7, 31 })
        foreach (int offset in new[] { 0, 1, 2, 5, 12, 49 })
        {
            var expected = new RetroMediaLab.Chips.Logic.Common.Services.RationalClockInput(source);
            var actual = expected;
            long boundary = checked(expected.NextTime + offset);
            int count = Math.Min(maximum, expected.EdgesBefore(boundary));
            long last = count == 0 ? expected.NextTime : expected.TimeAfterEdges(count - 1);
            expected.Advance(count);
            Assert.Equal(count, actual.ConsumeBefore(maximum, boundary, out long observed));
            Assert.Equal(last, observed);
            Assert.Equal(expected.NextTime, actual.NextTime);
            Assert.Equal(expected.NextHigh, actual.NextHigh);
            // Equal current dates alone do not establish the fractional phase.
            for (int following = 0; following < 16; following++)
                Assert.Equal(expected.TimeAfterEdges(following), actual.TimeAfterEdges(following));
        }
    }

    [Fact]
    public void SpeculativeGroupPastLongMaxDoesNotRejectRepresentablePrefix()
    {
        Tms9918A Make()=>new(new(default,0,default,new(null,0,Tms9918CpuVramOperation.None,false),
            new(0,true,true,true,true)),Tms9918CpuTiming.SpecifiedMaximum);
        var scalar=Make();var range=Make();scalar.SetReset(0,true);range.SetReset(0,true);
        const long origin=long.MaxValue-100,boundary=long.MaxValue-2;
        range.ReceiveMasterClockSource(new(origin,3,1,1,true));
        long edge=1;
        while(origin+edge*3<boundary){scalar.ReceiveMasterClock(origin+edge*3,(edge&1)!=0);edge++;}
        scalar.AdvanceTo(boundary-1);range.AdvanceMasterClockSourceUntil(boundary);range.AdvanceTo(boundary-1);
        Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),JsonSerializer.Serialize(range.CaptureState()));
    }

    [Theory]
    [InlineData(1000000000L,21477270L)]
    [InlineData(1234L,17L)]
    [InlineData(1L,1L)]
    public void RationalSourceMatchesEdgesAtIntermediateObservations(long numerator,long denominator)
    {
        Tms9918A Make()=>new(new(new(0,0xc0,6,0x80,0,0x7e,7,0xe3),0,default,
            new(null,0,Tms9918CpuVramOperation.None,false),new(0,true,true,true,true)),Tms9918CpuTiming.SpecifiedMaximum);
        var scalar=Make();var range=Make();
        var a=new List<object>();var b=new List<object>();
        scalar.VramReadCycleProduced=static (in RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts.Tms9918VramReadCycle _) => true;
        range.VramReadCycleProduced=static (in RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts.Tms9918VramReadCycle _) => true;
        scalar.VramPinsChanged=_=>{};
        scalar.VideoSignalProduced=v=>a.Add(v);range.VideoSignalProduced=v=>b.Add(v);
        long Date(long edge)=>(long)((Int128)edge*numerator/denominator);
        long next=1;
        try
        {
            range.ReceiveMasterClockSource(new(0,numerator,denominator,1,true));
            foreach(long edge in new long[]{1,2,3,4,5,9,10,11,13,17,23,24,25,53,97,128,263,1025,4097})
            {
                long boundary=Date(edge);
                while(Date(next)<boundary){scalar.ReceiveMasterClock(Date(next),(next&1)!=0);next++;}
                scalar.AdvanceTo(boundary-1);range.AdvanceMasterClockSourceUntil(boundary);range.AdvanceTo(boundary-1);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),JsonSerializer.Serialize(range.CaptureState()));
                Assert.Equal(JsonSerializer.Serialize(a),JsonSerializer.Serialize(b));
                if(edge==53)range.NextEventNanosecondsChanged=_=>{};
                if(edge==97)range.NextEventNanosecondsChanged=null;
                if(edge==128)range.VramPinsChanged=_=>{};
                if(edge==263)range.VramPinsChanged=null;
            }
        }
        finally
        {
            scalar.VideoSignalProduced=null;range.VideoSignalProduced=null;
            scalar.VramPinsChanged=null;range.VramPinsChanged=null;range.NextEventNanosecondsChanged=null;
            scalar.VramReadCycleProduced=null;range.VramReadCycleProduced=null;
        }
    }

    [Fact]
    public void RepresentedVramReadResetMatchesObservedPinsAtEveryPhase()
    {
        foreach (long offset in new long[] { 0, 44, 45, 84, 85, 164, 165, 269, 270, 334, 335, 385 })
        {
            Tms9918A Make()=>new(new(new(0,0xc0,6,0x80,0,0x7e,7,0xe3),0,default,
                new(null,0,Tms9918CpuVramOperation.None,false),new(0,true,true,true,true)),Tms9918CpuTiming.SpecifiedMaximum);
            var observed=Make();var represented=Make();
            long row=-1;
            observed.VramReadCycleProduced=static (in RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts.Tms9918VramReadCycle _) => true;
            represented.VramReadCycleProduced=(in RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts.Tms9918VramReadCycle cycle) => { if(row<0)row=cycle.RowTime;return true; };
            observed.VramPinsChanged=_=>{};
            try
            {
                var source=new RationalClockSource(0,1_000_000_000,21_477_270,1,true);
                observed.ReceiveMasterClockSource(source);represented.ReceiveMasterClockSource(source);
                for(long edge=1;row<0;edge++)
                {
                    Assert.True(edge<1000,"Expected a physical VRAM read.");
                    long until=source.EdgeNanoseconds(edge)+1;
                    observed.AdvanceMasterClockSourceUntil(until);represented.AdvanceMasterClockSourceUntil(until);
                }
                long time=row+offset;
                observed.AdvanceMasterClockSourceUntil(time+1);represented.AdvanceMasterClockSourceUntil(time+1);
                observed.SetReset(time,true);represented.SetReset(time,true);
                Assert.Equal(JsonSerializer.Serialize(observed.CaptureState()),JsonSerializer.Serialize(represented.CaptureState()));
            }
            finally
            {
                observed.VramPinsChanged=null;
                observed.VramReadCycleProduced=null;represented.VramReadCycleProduced=null;
            }
        }
    }
}
