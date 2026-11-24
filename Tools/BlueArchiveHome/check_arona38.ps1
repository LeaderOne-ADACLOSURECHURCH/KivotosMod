$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$probeRuntimePaths = @(Get-ChildItem -LiteralPath (Join-Path $modRoot 'ThirdParty/Spine38') -Filter '*.cs' -Recurse | Select-Object -ExpandProperty FullName)
$probeRuntimePaths += @(Get-ChildItem -LiteralPath (Join-Path $modRoot 'ThirdParty/Spine42') -Filter '*.cs' -Recurse | Select-Object -ExpandProperty FullName)
$probeRuntimePaths += Join-Path $modRoot 'Content/Tablet/TabletSpine38Animation.cs'
$probeRuntimePaths += Join-Path $PSScriptRoot 'inspect_arona38.cs'
# Compile both runtimes together with the headless probe, not KivotosMod/tML.
$uiSource = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletUiSystem.cs'))
$deltaMethod = [regex]::Match($uiSource, '(?ms)^    internal static float UiAnimationDelta\(.*?^    }').Value.Replace('internal static', 'public static')
if (-not $deltaMethod) { throw 'UI animation clock method not found' }
$clockProbePath = Join-Path ([IO.Path]::GetTempPath()) ('kivotos-ui-clock-' + [Guid]::NewGuid().ToString('N') + '.cs')
try {
    [IO.File]::WriteAllText($clockProbePath, ('using System; using System.Diagnostics; public static class UiClockProbe { ' + $deltaMethod + ' }'))
    $probeRuntimePaths += $clockProbePath
    Add-Type -Path $probeRuntimePaths -IgnoreWarnings
}
finally { if (Test-Path -LiteralPath $clockProbePath) { Remove-Item -LiteralPath $clockProbePath } }
foreach ($stem in @('arona', 'sora', 'plana')) {
    $folder = (Get-Culture).TextInfo.ToTitleCase($stem) + '38'
    $probeRoot = Join-Path $modRoot ('Assets/Tablet/Home/' + $folder)
    $result = [Arona38Probe]::Validate($probeRoot, $stem)
    $delta = [Func[long,long,float]]{ param($previous, $current) [UiClockProbe]::UiAnimationDelta($previous, $current) }
    $clockChecks = @(foreach ($fps in @(60, 144)) { [Arona38Probe]::ValidateUiClock($probeRoot, $delta, $fps, $stem) | ConvertFrom-Json })
    $report = $result | ConvertFrom-Json
    $report | Add-Member -NotePropertyName 'uiClockChecks' -NotePropertyValue $clockChecks
    $report | Add-Member -NotePropertyName 'tracking' -NotePropertyValue ([Arona38Probe]::ValidateTracking($probeRoot, $stem) | ConvertFrom-Json)
    if ($stem -ne 'sora') { $report | Add-Member -NotePropertyName 'headTracking' -NotePropertyValue ([Arona38Probe]::ValidateHeadTracking($probeRoot, $stem) | ConvertFrom-Json) }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($stem + '38_validation.json')), ($report | ConvertTo-Json -Depth 6))
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($stem + '38_inspection.json')), [Arona38Probe]::Inspect($probeRoot, $stem))
    foreach ($pose in @(@('idle', 0), @('closed', 0.2))) {
        $poseData = [Arona38Probe]::Pose($probeRoot, [float]$pose[1], $stem, 0, 0, $false) | ConvertFrom-Json
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot "$($stem)38_$($pose[0])_pose.json"), (ConvertTo-Json -InputObject @($poseData.meshes) -Depth 30 -Compress))
    }
    foreach ($pose in @(@('left', -10000), @('right', 10000))) {
        $poseData = [Arona38Probe]::Pose($probeRoot, 0, $stem, [float]$pose[1], 550, $true) | ConvertFrom-Json
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot "$($stem)38_$($pose[0])_pose.json"), (ConvertTo-Json -InputObject @($poseData.meshes) -Depth 30 -Compress))
    }
    foreach ($pose in @(@('closed_left_up', -10000, 10000), @('closed_right_down', 10000, -10000))) {
        $poseData = [Arona38Probe]::Pose($probeRoot, 0.2, $stem, [float]$pose[1], [float]$pose[2], $true) | ConvertFrom-Json
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot "$($stem)38_$($pose[0])_pose.json"), (ConvertTo-Json -InputObject @($poseData.meshes) -Depth 30 -Compress))
    }
    if ($stem -ne 'sora') {
        foreach ($pose in @(@('up', 10000), @('down', -10000))) {
            $data = [Arona38Probe]::Pose($probeRoot, 0, $stem, -8.45, [float]$pose[1], $true, 0, 0) | ConvertFrom-Json
            [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($stem + "38_" + $pose[0] + "_pose.json")), (ConvertTo-Json -InputObject @($data.meshes) -Depth 30 -Compress))
        }
    }
    $report | ConvertTo-Json -Depth 6
}
