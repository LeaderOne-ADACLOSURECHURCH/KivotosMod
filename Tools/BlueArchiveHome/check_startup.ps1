$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $modRoot 'Content/Tablet/TabletStartupAnimation.cs'), (Join-Path $PSScriptRoot 'inspect_startup.cs'))
$checks = @(foreach ($fps in @(30, 60, 144)) { [TabletStartupProbe]::Validate($fps) | ConvertFrom-Json })
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'startup_validation.json'), (ConvertTo-Json -InputObject $checks -Depth 5))
$checks | ConvertTo-Json -Depth 5
