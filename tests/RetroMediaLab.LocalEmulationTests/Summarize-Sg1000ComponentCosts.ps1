[CmdletBinding()]
param(
    [string[]]$Report,
    [string]$PairedManifest,
    [string[]]$WindowsStackCsv,
    [ValidateRange(0,100000000)][int]$ObservedRunFrameCalls = 0,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
if ($PairedManifest) {
    $manifest=Get-Content -Raw -LiteralPath $PairedManifest | ConvertFrom-Json
    $acquisitions=@($manifest.Slots)
    $results=@(foreach($configuration in @($acquisitions.Configuration | Select-Object -Unique)) {
        $slots=@($acquisitions | Where-Object Configuration -eq $configuration)
        foreach($block in @($slots.Block | Select-Object -Unique)) {
            $ordered=@($slots | Where-Object Block -eq $block | Sort-Object Order)
            if($ordered.Count -ne 4 -or ($ordered.Variant -join '') -ne 'ABBA'){throw 'Acquisition must contain complete ABBA blocks.'}
        }
        $loaded=@(foreach($slot in $slots){foreach($path in $slot.Reports){
            $r=Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
            if($r.Status -ne 'Completed'){throw "Incomplete acquisition: $path"}
            [pscustomobject]@{Block=$slot.Block;Order=$slot.Order;Variant=$slot.Variant;Path=$path;Report=$r}
        }})
        foreach($rom in @($loaded.Report.Rom | Select-Object -Unique)) {
            $game=@($loaded | Where-Object {$_.Report.Rom -eq $rom})
            if($game.Count -ne $slots.Count){throw 'Every slot must contain every selected ROM exactly once.'}
            $reference=$game[0].Report
            $referencePass=@($reference.Passes | Where-Object Kind -eq 'Measure')[0]
            $physicalFrame=$referencePass.Frames
            $ignore=@('WallMs','ProcessCpuMs','AllocatedBytes','GcPauseMs','Gc0','Gc1','Gc2','BoardWallMs','PresentationWallMs','AudioWallMs',
                'DramOutputNotifications','VramDataDeliveries','VramDataGroups',
                'DramReadOutputPrograms','VramReadProgramWords','DramReadOutputFallbacks','ClosedDramReadResolutions','ReadPortProjectedReads','ReadPortCanonicalReceives','ReadPortEpochs')
            $observations=@(foreach($entry in $game) {
                $r=$entry.Report
                foreach($field in @('Sha256','Layout','FrameCount','WarmupFrames','Host','Runtime','ScenarioVersion','TimingPolicy')){
                    if($r.$field -cne $reference.$field){throw "Incompatible $field for $rom"}
                }
                $native=@($r.Passes | Where-Object {$_.Kind -like 'Measure*'})
                if($native.Count -lt 2 -or $r.TimingPolicy -ne 'tiering-disabled/primed'){throw 'Repeated, primed native acquisitions are required.'}
                $fingerprints=@($native.BinaryFingerprint | Select-Object -Unique)
                if($fingerprints.Count -ne 1 -or !$fingerprints[0]){throw 'Incomplete binary identity.'}
                foreach($pass in $native){
                    if(($pass.Output | ConvertTo-Json -Compress) -cne ($referencePass.Output | ConvertTo-Json -Compress)){throw 'Native output format, geometry, clock or audio contract differs.'}
                    if($pass.StateHash -ne $referencePass.StateHash -or $pass.FinalVideoHash -ne $referencePass.FinalVideoHash -or $pass.AudioHash -ne $referencePass.AudioHash){throw "State/video/PCM mismatch: $($entry.Path)"}
                    if($pass.Frames.Count -ne $physicalFrame.Count){throw 'Frame count mismatch.'}
                    for($i=0;$i -lt $physicalFrame.Count;$i++){
                        foreach($property in $physicalFrame[$i].PSObject.Properties){
                            if($property.Name -notin $ignore -and $pass.Frames[$i].($property.Name) -cne $property.Value){throw "Native observation differs: frame $i, $($property.Name)"}
                        }
                    }
                }
                [pscustomobject]@{Block=$entry.Block;Order=$entry.Order;Variant=$entry.Variant;Report=$entry.Path;
                    Fingerprint=$fingerprints[0];NativeRuns=$native.Count;
                    MeanMs=($native.Statistics.MeanMs | Measure-Object -Average).Average}
            })
            foreach($variant in @('A','B')){
                if(@($observations | Where-Object Variant -eq $variant | Select-Object -ExpandProperty Fingerprint -Unique).Count -ne 1){throw "Binary $variant changed during acquisition."}
            }
            $blocks=@(foreach($block in @($slots.Block | Select-Object -Unique)) {
                $current=@($observations | Where-Object Block -eq $block)
                $a=($current | Where-Object Variant -eq A | Measure-Object MeanMs -Average).Average
                $b=($current | Where-Object Variant -eq B | Measure-Object MeanMs -Average).Average
                [pscustomobject]@{Block=$block;AMeanMs=$a;BMeanMs=$b;TimeReductionPercent=100*(1-$b/$a)}
            })
            $aMean=($observations | Where-Object Variant -eq A | Measure-Object MeanMs -Average).Average
            $bMean=($observations | Where-Object Variant -eq B | Measure-Object MeanMs -Average).Average
            [pscustomobject]@{Configuration=$configuration;Game=[IO.Path]::GetFileNameWithoutExtension($rom);
                FramesPerRun=$reference.FrameCount;WarmupFrames=$reference.WarmupFrames;Blocks=$blocks;
                AMeanMs=$aMean;BMeanMs=$bMean;TimeReductionPercent=100*(1-$bMean/$aMean);
                EveryHardwareObservationEqual=$true;SameBinaryControl=(@($observations.Fingerprint | Select-Object -Unique).Count -eq 1);Slots=$observations}
        }
    })
    [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
    $results | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'abba-results.json')
    $md=[Text.StringBuilder]::new("# Acquisition alternée ABBA SG-1000`n`n")
    [void]$md.AppendLine('Moyennes de scénarios natifs complets, préparation et échauffement identiques; aucune reconstruction entre les processus A/B. Chaque bloc suit ABBA. États complets, images, PCM et observations natives vérifiés. Les images et répétitions internes ne sont pas considérées comme des processus indépendants. Deux blocs permettent de vérifier leur cohérence, pas de revendiquer une significativité statistique pour de petits écarts.')
    [void]$md.AppendLine("`n| Configuration | ROM | A ms/image | B ms/image | Temps réduit |`n|---|---|---:|---:|---:|")
    foreach($r in $results){[void]$md.AppendLine(('| {0} | {1} | {2:F3} | {3:F3} | {4:F2} % |' -f $r.Configuration,$r.Game,$r.AMeanMs,$r.BMeanMs,$r.TimeReductionPercent))
        foreach($b in $r.Blocks){[void]$md.AppendLine(('Bloc {0} : A {1:F3}, B {2:F3} ms/image, temps réduit {3:F2} %.' -f $b.Block,$b.AMeanMs,$b.BMeanMs,$b.TimeReductionPercent))}}
    $md.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'abba-results.md')
    $results | Select-Object Configuration,Game,AMeanMs,BMeanMs,TimeReductionPercent,EveryHardwareObservationEqual | Format-Table -AutoSize
    exit 0
}
if(!$Report -and !$WindowsStackCsv){throw 'Report, PairedManifest or WindowsStackCsv is required.'}
function Component([string]$Name) {
    # Parameter types are not owners: a wiring method can take a CPU contract.
    $owner=($Name -split '\(',2)[0]
    if($owner.Contains('Chips.Memory.Fujitsu.MB81xx.MB8118.')) { return 'DRAM MB8118' }
    if($owner.Contains('Chips.CPU.')) { return 'CPU' }
    if($owner.Contains('Chips.Video.')) { return 'VDP' }
    if($owner.Contains('Chips.Memory.')) { return 'Autres mémoires' }
    if($owner.Contains('Chips.Audio.')) { return 'PSG' }
    if($owner.Contains('Chips.Logic.')) { return 'Logique' }
    if($owner.Contains('Sg1000Board')) { return 'Câblage' }
    if($owner.Contains('EmulatorCore')) { return 'Backend' }
    return 'Runtime et autres'
}
if ($WindowsStackCsv) {
    $paths=@($WindowsStackCsv | ForEach-Object { $_ -split '\|' } | ForEach-Object { (Get-Item -LiteralPath $_).FullName })
    $quality=@(foreach($path in $paths) {
        $stats=Join-Path (Split-Path -Parent (Split-Path -Parent $path)) 'trace-stats.txt'
        $lost=$null
        $lostBuffers=$null
        if(Test-Path -LiteralPath $stats) {
            $content=Get-Content -LiteralPath $stats -Raw
            if($content -match 'Total # Lost Events\s*:\s*(\d+)') { $lost=[long]$Matches[1] }
            if($content -match 'Total # Lost Buffers\s*:\s*(\d+)') { $lostBuffers=[long]$Matches[1] }
        }
        if(($null -ne $lost -and $lost -ne 0) -or ($null -ne $lostBuffers -and $lostBuffers -ne 0)) {
            throw "Lost events or buffers exclude this capture from method attribution: $path"
        }
        [pscustomobject]@{Csv=$path;LostEvents=$lost;LostBuffers=$lostBuffers;
            LossMetadataAvailable=($null -ne $lost -and $null -ne $lostBuffers)}
    })
    $python=(Get-Command python.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    $analysis=@'
import collections,csv,json,math,sys
own=collections.Counter();inclusive=collections.Counter();scope=0.0;samples=0;process_total=0.0;stack_total=0.0
for path in json.loads(sys.argv[1]):
    roots=collections.Counter();stacks=collections.Counter();processes=set()
    with open(path,encoding='utf-8-sig',newline='') as stream:
        reader=csv.DictReader(stream)
        if reader.fieldnames!=['Process','Stack','Weight (in view) (ms)','Count']:
            raise ValueError('Expected the WPA Process, Stack, Weight and Count columns')
        for row in reader:
            process=row['Process'];stack=row['Stack']
            if not process.lower().startswith('testhost.exe ('):continue
            weight=float(row['Weight (in view) (ms)'].replace(',','.'))
            if not math.isfinite(weight) or weight<0:raise ValueError('Invalid CPU weight')
            if not stack:roots[process]+=weight;continue
            stacks[process]+=weight
            if 'SegaEmu.Services.EmulatorCore::RunFrame' not in stack:continue
            processes.add(process);scope+=weight;samples+=int(row['Count']);frames=stack.split('/')
            own[frames[-1]]+=weight
            for frame in set(frames):inclusive[frame]+=weight
    for process in processes:
        if process not in roots or abs(roots[process]-stacks[process])>.01:
            raise ValueError('CPU process weights do not reconcile')
        process_total+=roots[process];stack_total+=stacks[process]
if scope<=0:raise ValueError('No SG-1000 RunFrame samples')
methods=[dict(Name=name,OwnMs=own[name],InclusiveMs=inclusive[name],OwnPercent=100*own[name]/scope,
              InclusivePercent=100*inclusive[name]/scope) for name in sorted(set(own)|set(inclusive))]
methods.sort(key=lambda method:-method['OwnMs'])
print(json.dumps(dict(ScopedSampleMs=scope,SampleCount=samples,ProcessTotalMs=process_total,
                     StackTotalMs=stack_total,Methods=methods)))
'@
    $reader=[Diagnostics.Process]::new()
    $reader.StartInfo=[Diagnostics.ProcessStartInfo]::new($python)
    $reader.StartInfo.UseShellExecute=$false
    $reader.StartInfo.CreateNoWindow=$true
    $reader.StartInfo.RedirectStandardOutput=$true
    $reader.StartInfo.RedirectStandardError=$true
    $reader.StartInfo.ArgumentList.Add('-c')
    $reader.StartInfo.ArgumentList.Add($analysis)
    $reader.StartInfo.ArgumentList.Add((ConvertTo-Json -InputObject @($paths) -Compress))
    $started=$false
    try {
        $started=$reader.Start()
        if(!$started){throw 'CPU stack reader did not start.'}
        $stdout=$reader.StandardOutput.ReadToEndAsync()
        $stderr=$reader.StandardError.ReadToEndAsync()
        $reader.WaitForExit()
        $raw=$stdout.GetAwaiter().GetResult()
        $failure=$stderr.GetAwaiter().GetResult()
        if($reader.ExitCode -ne 0){throw "Windows CPU stack analysis failed. $failure"}
    }
    finally {
        if($started -and !$reader.HasExited){$reader.Kill($true);$reader.WaitForExit()}
        $reader.Dispose()
    }
    $cost=$raw | ConvertFrom-Json
    $components=@($cost.Methods | Group-Object { Component $_.Name } | ForEach-Object {
        $own=[double](($_.Group | Measure-Object OwnMs -Sum).Sum)
        [pscustomobject]@{Component=$_.Name;OwnSampleMs=$own;OwnPercent=100*$own/$cost.ScopedSampleMs;
            SampledOwnMsPerFrame=if($ObservedRunFrameCalls){$own/$ObservedRunFrameCalls}else{$null}}
    } | Sort-Object OwnSampleMs -Descending)
    $accessors=[double](($cost.Methods | Where-Object Name -Match '::(get_|set_)' | Measure-Object OwnMs -Sum).Sum)
    $result=[pscustomobject]@{Source='WPR CPU sampled';Captures=$quality;ScopedSampleMs=$cost.ScopedSampleMs;
        SampleCount=$cost.SampleCount;ProcessTotalMs=$cost.ProcessTotalMs;StackTotalMs=$cost.StackTotalMs;
        ObservedRunFrameCalls=$ObservedRunFrameCalls;AccessorsOwnMs=$accessors;
        AccessorsOwnPercent=100*$accessors/$cost.ScopedSampleMs;Components=$components;Methods=$cost.Methods}
    [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'windows-cpu-costs.json')
    $md=[Text.StringBuilder]::new("# CPU Windows SG-1000`n`n")
    [void]$md.AppendLine('Piles RunFrame uniquement. Les coûts propres partitionnent les échantillons. Les coûts inclusifs se recouvrent. Les feuilles natives sans symboles gardent leur module. Les poids CPU échantillonnés ne remplacent pas les durées murales ABBA. Les pertes inconnues restent signalées comme inconnues.')
    [void]$md.AppendLine(('Temps CPU RunFrame {0:F3} ms, {1} échantillons, {2} appels RunFrame fournis par le scénario. Accesseurs {3:F3} % déjà inclus dans les composants.' -f $cost.ScopedSampleMs,$cost.SampleCount,$ObservedRunFrameCalls,$result.AccessorsOwnPercent))
    [void]$md.AppendLine("`n| Composant | Propre % | ms CPU échantillonnées/image |`n|---|---:|---:|")
    foreach($c in $components){[void]$md.AppendLine(('| {0} | {1:F3} | {2:F3} |' -f $c.Component,$c.OwnPercent,$c.SampledOwnMsPerFrame))}
    foreach($order in @('OwnMs','InclusiveMs')) {
        [void]$md.AppendLine("`n## $order`n`n| Méthode | Propre % | Inclusif % |`n|---|---:|---:|")
        foreach($method in $cost.Methods | Sort-Object $order -Descending | Select-Object -First 40){[void]$md.AppendLine(('| `{0}` | {1:F3} | {2:F3} |' -f $method.Name,$method.OwnPercent,$method.InclusivePercent))}
    }
    foreach($q in $quality){[void]$md.AppendLine(('Capture `{0}`, contrôle des pertes {1}, événements perdus {2}, buffers perdus {3}.' -f $q.Csv,$q.LossMetadataAvailable,$q.LostEvents,$q.LostBuffers))}
    $md.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'windows-cpu-costs.md')
    $components | Format-Table Component,OwnPercent,SampledOwnMsPerFrame -AutoSize
    exit 0
}
# A pipe-separated list also supports powershell.exe/pwsh -File argument passing.
$paths=@($Report | ForEach-Object { $_ -split '\|' })
$results=@(foreach($path in $paths) {
    $source=Get-Item -LiteralPath $path
    $r=Get-Content -Raw -LiteralPath $source.FullName | ConvertFrom-Json
    if($r.Status -ne 'Completed') { throw "Incomplete profile: $path" }
    $native=@($r.Passes | Where-Object Kind -eq 'Measure')[0]
    $profile=@($r.Passes | Where-Object Kind -eq 'Profile')[0]
    if(!$profile) { throw "A sampled pass is required: $path" }
    if($native -and $native.CoreSha256 -ne $profile.CoreSha256) { throw "Different core binaries: $path" }
    $costPath=Join-Path $source.DirectoryName 'Profile/cpu-costs.json'
    $cost=Get-Content -Raw -LiteralPath $costPath | ConvertFrom-Json
    $nativeMean = if($null -ne $r.Repeatability) { $r.Repeatability.MeanMs } elseif($native) { $native.Statistics.MeanMs } else { $null }
    $frames=@($profile.Frames)
    if($frames.Count -ne $r.FrameCount -or $cost.ScopedSampleMs -le 0) { throw "Invalid sample/frame counts: $path" }
    $components=@($cost.Methods | Group-Object { Component $_.Name } | ForEach-Object {
        $own=[double](($_.Group | Measure-Object OwnMs -Sum).Sum)
        [pscustomobject]@{
            Component=$_.Name; OwnSampleMs=$own
            OwnPercent=100*$own/$cost.ScopedSampleMs
            SampledOwnMsPerFrame=$own/$frames.Count
            EstimatedNativeOwnMsPerFrame=if($null -ne $nativeMean){$own/$cost.ScopedSampleMs*$nativeMean}else{$null}
            Methods=@($_.Group | Sort-Object OwnMs -Descending | Select-Object -First 8 Name,OwnPercent,InclusivePercent,
                @{n='SampledOwnMsPerFrame';e={$_.OwnMs/$frames.Count}},
                @{n='SampledInclusiveMsPerFrame';e={$_.InclusiveMs/$frames.Count}})
        }
    } | Sort-Object OwnSampleMs -Descending)
    $sum=[double](($components | Measure-Object OwnSampleMs -Sum).Sum)
    if([Math]::Abs(($sum+$cost.BlockedSampleMs)-$cost.ScopedSampleMs) -gt .01) { throw 'Sample partition does not reconcile.' }
    # First measured row includes prior startup/warmup. Differences of subsequent
    # cumulative rows give volumes restricted to complete measured images.
    $volumes=@(foreach($counter in @('BusRangeDeliveries','CpuOutputWords','LogicInputWords','MemoryInputWords',
        'VramReadWaveDeliveries','DramExplicitPinDeliveries','VideoSynchronizationCalls','NativeVideoSamples',
        'CpuBusResolutions','DirectCauseVisits','GateCauseVisits','GateSkippedCpuWords','MemoryResponseRecordsOmitted',
        'DramOutputNotifications','VramDataDeliveries','VramDataGroups',
        'DramReadOutputPrograms','VramReadProgramWords','DramReadOutputFallbacks','ClosedDramReadResolutions','ReadPortProjectedReads','ReadPortCanonicalReceives','ReadPortEpochs')) {
        if($frames.Count -gt 1) {
            [pscustomobject]@{Counter=$counter;AveragePerFrame=($frames[-1].$counter-$frames[0].$counter)/($frames.Count-1)}
        }
    })
    [pscustomobject]@{
        Game=[IO.Path]::GetFileNameWithoutExtension($r.Rom);Source=$source.FullName;RomBytes=$r.Bytes
        CoreSha256=$profile.CoreSha256;Frames=$frames.Count;WarmupFrames=$r.WarmupFrames;NativeAvailable=($null -ne $native)
        NativeMedianMs=$native.Statistics.MedianMs;NativeMeanMs=$nativeMean;Repeatability=$r.Repeatability;TimingPolicy=$r.TimingPolicy
        ProfileMedianMs=$profile.Statistics.MedianMs;ScopedSampleMs=$cost.ScopedSampleMs
        BlockedSampleMs=$cost.BlockedSampleMs;Components=$components;Volumes=$volumes
        DirectBoardMeanMs=($frames.BoardWallMs | Measure-Object -Average).Average
        DirectPresentationMeanMs=($frames.PresentationWallMs | Measure-Object -Average).Average
        DirectAudioDrainMeanMs=($frames.AudioWallMs | Measure-Object -Average).Average
        Accessors=@($cost.Methods | Where-Object Name -Match '\.(get_|set_)' | Sort-Object OwnMs -Descending |
            Select-Object -First 25 Name,OwnPercent,InclusivePercent,@{n='SampledOwnMsPerFrame';e={$_.OwnMs/$frames.Count}})
        Presentation=@($cost.Methods | Where-Object Name -Match 'SegaEmu\.Services\.EmulatorCore\.PresentVideo\(' |
            Select-Object Name,OwnPercent,InclusivePercent,@{n='SampledOwnMsPerFrame';e={$_.OwnMs/$frames.Count}})
    }
})
[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
$results | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'component-costs.json')
$md=[Text.StringBuilder]::new()
[void]$md.AppendLine('# Coûts des composants sur les images complètes SG-1000')
[void]$md.AppendLine('')
[void]$md.AppendLine('Réanalyse des profils existants, sans nouveau lancement ni modification des puces. Les coûts propres sont une partition des échantillons RunFrame; aucun descendant n''est compté deux fois. Les coûts inclusifs des méthodes se recouvrent et ne doivent pas être additionnés. Les noms de paramètres ne servent jamais à attribuer le propriétaire.')
[void]$md.AppendLine('')
[void]$md.AppendLine('Les ms échantillonnées/image proviennent de la passe profilée, qui subit le coût du profileur. Les ms natives estimées projettent la part échantillonnée sur la moyenne des répétitions natives si disponible, sinon sur la passe native unique : estimation, pas chronométrage direct. Les périodes chaudes et les petits pourcentages sont soumis au bruit statistique. Les fenêtres et échauffements réels sont précisés par ROM; ils ne représentent pas tout le déroulement des jeux.')
foreach($r in $results) {
    [void]$md.AppendLine("`n## $($r.Game) ($($r.RomBytes) octets)")
    if($r.NativeAvailable){[void]$md.AppendLine(('Natif médiane/moyenne : {0:F3}/{1:F3} ms/image; profilé médiane : {2:F3} ms/image; {3} images. SHA256 module : `{4}`.' -f $r.NativeMedianMs,$r.NativeMeanMs,$r.ProfileMedianMs,$r.Frames,$r.CoreSha256))}
    else{[void]$md.AppendLine(('Profil seul : médiane {0:F3} ms/image; {1} images; aucun temps natif projeté. SHA256 module : `{2}`.' -f $r.ProfileMedianMs,$r.Frames,$r.CoreSha256))}
    [void]$md.AppendLine("Échauffement : $($r.WarmupFrames) images. Médiane native : première passe; moyenne : toutes les répétitions si disponibles. Politique JIT : $($r.TimingPolicy).")
    if($null -ne $r.Repeatability){[void]$md.AppendLine(('Répétitions : {0}; CV des moyennes : {1:F3} %; étendue : {2:F3}..{3:F3} ms/image.' -f $r.Repeatability.Repetitions,$r.Repeatability.CoefficientVariationPercent,$r.Repeatability.MinimumRunMeanMs,$r.Repeatability.MaximumRunMeanMs))}
    if($r.DirectPresentationMeanMs -gt 0){[void]$md.AppendLine(('Chronométrage direct dans la passe profilée : carte {0:F4}, présentation {1:F4}, vidange audio {2:F4} ms/image. À distinguer des poids attribués par échantillonnage.' -f $r.DirectBoardMeanMs,$r.DirectPresentationMeanMs,$r.DirectAudioDrainMeanMs))}
    [void]$md.AppendLine("`n| Composant | Coût propre % | ms échantillonnées/image | ms natives estimées/image |`n|---|---:|---:|---:|")
    foreach($c in $r.Components) {
        [void]$md.AppendLine(('| {0} | {1:F2} | {2:F2} | {3:F2} |' -f $c.Component,$c.OwnPercent,$c.SampledOwnMsPerFrame,$c.EstimatedNativeOwnMsPerFrame))
    }
    foreach($c in $r.Components | Where-Object {$_.Component -in @('CPU','VDP','DRAM MB8118','Logique','Câblage')}) {
        [void]$md.AppendLine("`n### $($c.Component) : méthodes dominantes`n`n| Méthode | Propre % | Inclusif % | ms propres/image profilée |`n|---|---:|---:|---:|")
        foreach($m in $c.Methods) {
            [void]$md.AppendLine(('| `{0}` | {1:F2} | {2:F2} | {3:F2} |' -f $m.Name,$m.OwnPercent,$m.InclusivePercent,$m.SampledOwnMsPerFrame))
        }
    }
    foreach($detail in @(@{Title='Accesseurs précis (coûts inclus dans ceux des composants)';Rows=$r.Accessors},
        @{Title='Conversion de présentation vidéo';Rows=$r.Presentation})) {
        [void]$md.AppendLine("`n### $($detail.Title)`n`n| Méthode | Propre % | Inclusif % | ms propres/image profilée |`n|---|---:|---:|---:|")
        foreach($method in $detail.Rows){[void]$md.AppendLine(('| `{0}` | {1:F3} | {2:F3} | {3:F3} |' -f $method.Name,$method.OwnPercent,$method.InclusivePercent,$method.SampledOwnMsPerFrame))}
    }
    [void]$md.AppendLine("`n### Volumes moyens par image`n`nDifférences entre les lignes cumulées : la première image est exclue pour ne pas compter le démarrage et l''échauffement.`n`n| Compteur | Par image |`n|---|---:|")
    foreach($v in $r.Volumes) { [void]$md.AppendLine(('| {0} | {1:F2} |' -f $v.Counter,$v.AveragePerFrame)) }
    [void]$md.AppendLine("`nSource : ``$($r.Source)``.")
}
$md.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'component-costs.md')
$results | ForEach-Object {
    Write-Output $_.Game
    $_.Components | Select-Object Component,OwnPercent,SampledOwnMsPerFrame,EstimatedNativeOwnMsPerFrame | Format-Table -AutoSize
}
