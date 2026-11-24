$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $root 'Content/Tablet/TabletCheckoutService.cs'), (Join-Path $PSScriptRoot 'checkout_probe.cs'))
[CheckoutProbe]::Validate()
'Checkout capacity: stack and payload limits reject before charge; boundary and stackable orders pass (stubbed engine).'
