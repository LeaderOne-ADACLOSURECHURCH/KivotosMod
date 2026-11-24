$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$production = Join-Path $modRoot 'Content/Items/Vanity/StudentSpriteDraw.cs'
$probe = Join-Path $PSScriptRoot 'loading_probe.cs'
# Run the complete production draw helper against a controlled async-placeholder
# asset loader, without loading Terraria/FNA or emitting the mod assembly.
Add-Type -Path @($production, $probe)
[PlayerTextureLoadingProbe]::Validate()
$report = [ordered]@{ passed = $true; productionDrawHelper = $true; engineDependenciesStubbed = $true
    originalOnePixelPlaceholderCrashReproduced = $true; firstDrawWaitsForRealTexture = $true
    pendingAsyncRequestCompleted = $true; loadedTextureCacheReused = $true
    standardAndSixtyPixelFrames = $true; haloUsesRealDimensions = $true }
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'loading_validation.json'), $json)
$json
