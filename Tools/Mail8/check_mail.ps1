$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $PSScriptRoot 'mail_stubs.cs'),(Join-Path $PSScriptRoot 'mail_probe.cs'),(Join-Path $root 'Content/Tablet/Mail/TabletMailCatalog.cs'),(Join-Path $root 'Content/Tablet/Mail/TabletMailPlayer.cs'),(Join-Path $root 'Content/Tablet/Mail/TabletMailWorld.cs'))
[MailProbe]::Run()
