[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string[]]$Report,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
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
# A pipe-separated list also supports powershell.exe/pwsh -File argument passing.
$paths=@($Report | ForEach-Object { $_ -split '\|' })
$results=@(foreach($path in $paths) {
    $source=Get-Item -LiteralPath $path
    $r=Get-Content -Raw -LiteralPath $source.FullName | ConvertFrom-Json
    if($r.Status -ne 'Completed') { throw "Incomplete profile: $path" }
    $native=@($r.Passes | Where-Object Kind -eq 'Measure')[0]
    $profile=@($r.Passes | Where-Object Kind -eq 'Profile')[0]
    if(!$native -or !$profile) { throw "Both native and sampled passes are required: $path" }
    if($native.CoreSha256 -ne $profile.CoreSha256) { throw "Different core binaries: $path" }
    $costPath=Join-Path $source.DirectoryName 'Profile/cpu-costs.json'
    $cost=Get-Content -Raw -LiteralPath $costPath | ConvertFrom-Json
    $frames=@($profile.Frames)
    if($frames.Count -ne $r.FrameCount -or $cost.ScopedSampleMs -le 0) { throw "Invalid sample/frame counts: $path" }
    $components=@($cost.Methods | Group-Object { Component $_.Name } | ForEach-Object {
        $own=[double](($_.Group | Measure-Object OwnMs -Sum).Sum)
        [pscustomobject]@{
            Component=$_.Name; OwnSampleMs=$own
            OwnPercent=100*$own/$cost.ScopedSampleMs
            SampledOwnMsPerFrame=$own/$frames.Count
            EstimatedNativeOwnMsPerFrame=$own/$cost.ScopedSampleMs*$native.Statistics.MeanMs
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
        'CpuBusResolutions','DirectCauseVisits','GateCauseVisits','GateSkippedCpuWords','MemoryResponseRecordsOmitted')) {
        if($frames.Count -gt 1) {
            [pscustomobject]@{Counter=$counter;AveragePerFrame=($frames[-1].$counter-$frames[0].$counter)/($frames.Count-1)}
        }
    })
    [pscustomobject]@{
        Game=[IO.Path]::GetFileNameWithoutExtension($r.Rom);Source=$source.FullName;RomBytes=$r.Bytes
        CoreSha256=$native.CoreSha256;Frames=$frames.Count;WarmupFrames=$r.WarmupFrames
        NativeMedianMs=$native.Statistics.MedianMs;NativeMeanMs=$native.Statistics.MeanMs
        ProfileMedianMs=$profile.Statistics.MedianMs;ScopedSampleMs=$cost.ScopedSampleMs
        BlockedSampleMs=$cost.BlockedSampleMs;Components=$components;Volumes=$volumes
    }
})
[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
$results | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'component-costs.json')
$md=[Text.StringBuilder]::new()
[void]$md.AppendLine('# Coûts des composants sur les images complètes SG-1000')
[void]$md.AppendLine('')
[void]$md.AppendLine('Réanalyse des profils existants, sans nouveau lancement ni modification des puces. Les coûts propres sont une partition des échantillons RunFrame; aucun descendant n''est compté deux fois. Les coûts inclusifs des méthodes se recouvrent et ne doivent pas être additionnés. Les noms de paramètres ne servent jamais à attribuer le propriétaire.')
[void]$md.AppendLine('')
[void]$md.AppendLine('Les ms échantillonnées/image proviennent de la passe profilée, qui subit le coût du profileur. Les ms natives estimées sont une projection de la part échantillonnée sur la moyenne murale de la passe sans profileur : estimation, pas chronométrage direct de chaque puce. Les périodes chaudes et les petits pourcentages sont soumis au bruit statistique. Les profils couvrent 12 images après 2 images d''échauffement; ils ne représentent pas tout le déroulement des jeux.')
foreach($r in $results) {
    [void]$md.AppendLine("`n## $($r.Game) ($($r.RomBytes) octets)")
    [void]$md.AppendLine(('Natif médiane/moyenne : {0:F3}/{1:F3} ms/image; profilé médiane : {2:F3} ms/image; {3} images. SHA256 module : `{4}`.' -f $r.NativeMedianMs,$r.NativeMeanMs,$r.ProfileMedianMs,$r.Frames,$r.CoreSha256))
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
    [void]$md.AppendLine("`n### Volumes moyens par image`n`nDifférences entre les lignes cumulées : la première image est exclue pour ne pas compter le démarrage et l''échauffement.`n`n| Compteur | Par image |`n|---|---:|")
    foreach($v in $r.Volumes) { [void]$md.AppendLine(('| {0} | {1:F2} |' -f $v.Counter,$v.AveragePerFrame)) }
    [void]$md.AppendLine("`nSource : ``$($r.Source)``.")
}
$md.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'component-costs.md')
$results | ForEach-Object {
    Write-Output $_.Game
    $_.Components | Select-Object Component,OwnPercent,SampledOwnMsPerFrame,EstimatedNativeOwnMsPerFrame | Format-Table -AutoSize
}
