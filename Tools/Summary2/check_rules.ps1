$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $root 'Content/Tablet/TabletWeaponUpgrade.cs'), (Join-Path $root 'Content/Tablet/TabletWallet.cs'), (Join-Path $root 'Content/Tablet/TabletUiPreferencesPlayer.cs'), (Join-Path $PSScriptRoot 'rules_probe.cs'))
[Summary2Probe]::Validate()
