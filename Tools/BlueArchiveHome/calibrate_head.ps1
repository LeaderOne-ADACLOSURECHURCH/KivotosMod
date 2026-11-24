param([ValidateSet('arona', 'plana')][string]$Stem = 'arona')
$ErrorActionPreference = 'Stop'
$paths = @(Get-ChildItem ThirdParty/Spine38 -Filter *.cs -Recurse | Select-Object -ExpandProperty FullName)
$paths += @((Resolve-Path Content/Tablet/TabletSpine38Animation.cs).Path, (Resolve-Path Tools/BlueArchiveHome/inspect_arona38.cs).Path)
Add-Type -Path $paths -IgnoreWarnings
$folder = (Get-Culture).TextInfo.ToTitleCase($Stem) + '38'
$root = (Resolve-Path ('Assets/Tablet/Home/' + $folder)).Path
$poses = if ($Stem -eq 'plana') { @(@('neutral', 0, 0), @('xminus', -30, 0), @('xplus', 30, 0), @('yminus', 0, -30), @('yplus', 0, 30)) }
    else { @(@('neutral', 0, 0), @('xminus', -30, 0), @('xplus', 100, 0), @('yminus', 0, -60), @('yplus', 0, 15)) }
foreach ($pose in $poses) {
    $data = [Arona38Probe]::Pose($root, 0, $Stem, 0, 0, $false, [float]$pose[1], [float]$pose[2]) | ConvertFrom-Json
    $prefix = if ($Stem -eq 'arona') { 'head_' } else { $Stem + '_head_' }
    [IO.File]::WriteAllText((Join-Path (Resolve-Path Tools/BlueArchiveHome).Path ($prefix + $pose[0] + '_pose.json')), (ConvertTo-Json -InputObject @($data.meshes) -Depth 30 -Compress))
}
