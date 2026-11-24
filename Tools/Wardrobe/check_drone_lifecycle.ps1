$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $root 'Content/Tablet/Delivery/TabletDeliveryOrderSystem.cs'),(Join-Path $root 'Content/Tablet/Delivery/DeliveryDroneProjectile.cs'),(Join-Path $PSScriptRoot 'drone_lifecycle_probe.cs'))
[DroneLifecycleProbe]::Validate()
'Production drone AI + order queue: approach, brake, drop and depart delivered exactly one populated package in singleplayer and headless-server simulation.'
