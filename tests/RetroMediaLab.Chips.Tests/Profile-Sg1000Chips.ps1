param([ValidateSet('Debug','Release','Both')][string]$Configuration='Both')
$ErrorActionPreference='Stop'
$project = Join-Path $PSScriptRoot 'RetroMediaLab.Chips.Tests.csproj'
$entry = Join-Path $PSScriptRoot 'Performance/Sg1000ChipEfficiency.Generated.cs'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
$oldOutput = $env:RML_CHIP_EFFICIENCY_OUTPUT
$oldTiered = $env:DOTNET_TieredCompilation
$createdEntry = $false
$exitCode = 0
try {
    if (Test-Path -LiteralPath $entry) { throw "Temporary benchmark entry already exists: $entry" }
    Set-Content -LiteralPath $entry -Value @'
using System.IO;
namespace RetroMediaLab.Chips.Tests.Performance;
public sealed class Sg1000ChipEfficiencyEntry {
    [Xunit.Fact]
    public void MeasureAll() => Sg1000ChipEfficiency.Run(System.Environment.GetEnvironmentVariable("RML_CHIP_EFFICIENCY_OUTPUT") ?? throw new System.InvalidOperationException("Use Profile-Sg1000Chips.ps1"));
}
'@
    $createdEntry = $true
    $env:DOTNET_TieredCompilation='0'
    $configs = if ($Configuration -eq 'Both') { @('Debug','Release') } else { @($Configuration) }
    foreach ($config in $configs) {
        $env:RML_CHIP_EFFICIENCY_OUTPUT=Join-Path $root "artifacts/TestsChips/Sega/SG-1000/Efficiency/$stamp/$config"
        & dotnet test $project -c $config -p:RmlChipEfficiencyOnly=true --filter 'FullyQualifiedName~Sg1000ChipEfficiencyEntry|FullyQualifiedName~CombinationalTruthTableTests' --logger 'console;verbosity=normal' --logger "trx;LogFileName=efficiency-$config.trx" --results-directory $env:RML_CHIP_EFFICIENCY_OUTPUT
        if ($LASTEXITCODE -ne 0) { $exitCode=$LASTEXITCODE }
        Write-Output "Results: $env:RML_CHIP_EFFICIENCY_OUTPUT"
    }
}
finally {
    if ($createdEntry -and (Test-Path -LiteralPath $entry)) { Remove-Item -LiteralPath $entry }
    $env:RML_CHIP_EFFICIENCY_OUTPUT=$oldOutput
    $env:DOTNET_TieredCompilation=$oldTiered
}
exit $exitCode
