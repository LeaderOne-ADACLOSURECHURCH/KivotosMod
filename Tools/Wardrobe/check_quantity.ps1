$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $modRoot 'Content/Tablet/TabletUiSystem.Quantity.cs'), (Join-Path $PSScriptRoot 'quantity_probe.cs')) -IgnoreWarnings
[KivotosMod.Content.Tablet.TabletUiSystem]::ValidateQuantity()
$report = [ordered]@{ passed = $true; productionInputMethods = $true; engineInputStubbed = $true
    typedAndPastedDigits = $true; quantityBounds = '1..9999'; enterCommits = $true; escapeCancels = $true
    pageChangeAndCloseReleaseKeyboard = $true }
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'quantity_validation.json'), $json)
$json
