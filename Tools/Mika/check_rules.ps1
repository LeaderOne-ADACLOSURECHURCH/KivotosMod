$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$studentPath = Join-Path $modRoot 'Content/Items/Vanity/Mika'
# Execute production identity, flight-skin animation and voice hooks. Only
# engine dependencies are stubbed: this is not an in-game flight/audio test.
$playerSource = [IO.File]::ReadAllText((Join-Path $studentPath 'MikaPlayer.cs'))
$playerSource = $playerSource.Substring(0, $playerSource.IndexOf('    public override void HideDrawLayers')) + "`n}"
$identitySource = [IO.File]::ReadAllText((Join-Path $studentPath 'MikaSet.cs'))
$identitySource = $identitySource.Substring(0, $identitySource.IndexOf('[AutoloadEquip'))
$drawSource = [IO.File]::ReadAllText((Join-Path $studentPath 'MikaDrawLayers.cs'))
$layoutClass = [regex]::Match($drawSource, '(?ms)^internal static class MikaArtLayout\r?\n\{.*?^}').Value
if (-not $layoutClass) { throw 'Mika canvas layout source not found' }
$layoutSource = "using Microsoft.Xna.Framework; using Microsoft.Xna.Framework.Graphics; namespace KivotosMod.Content.Items.Vanity.Mika;`n" + $layoutClass
$stubSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'rules_stubs.cs'))
$probeSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'rules_probe.cs'))
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ('kivotos-mika-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempDir) | Out-Null
$paths = @()
try {
    $sources = @($playerSource, $identitySource, $layoutSource, $stubSource, $probeSource)
    for ($i = 0; $i -lt $sources.Count; $i++) {
        $path = Join-Path $tempDir ($i.ToString() + '.cs')
        [IO.File]::WriteAllText($path, $sources[$i])
        $paths += $path
    }
    Add-Type -Path $paths
    [MikaRulesProbe]::Validate()
}
finally {
    foreach ($path in $paths) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    [IO.Directory]::Delete($tempDir)
}
$report = [ordered]@{ passed = $true; productionHooks = $true; engineDependenciesStubbed = $true
    mikaSkinOnlyAndLiveSwitch = $true; physicalVanityAndFormationSelection = $true; usesExistingWingStats = $true
    fiveFlightFrames = $true; idleFallingMountedAndUnequippedDecorative = $true
    exhaustedWingTimeStopsFlapping = $true
    referenceCanvasOffsets = $true; staticAndFlightWingsAligned = $true; horizontalAndVerticalFlipPivots = $true
    flightFrameTicks = 4; haloBobAmplitude = 1.25; haloBobPeriodSeconds = 2.4
    voiceCooldownTicks = 45; voiceReplacesOldWithoutOverlap = $true; vanillaHurtAlwaysMutedIncludingCooldown = $true
    heavyHitBoundaryAndRandomPools = $true; noRemoteOrServerVoice = $true }
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'rules_validation.json'), $json)
$json
