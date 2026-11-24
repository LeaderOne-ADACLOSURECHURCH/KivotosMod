$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $modRoot 'Content/Tablet/Delivery/TabletDeliveryOrderSystem.cs'), (Join-Path $PSScriptRoot 'delivery_probe.cs'))
[DeliveryQueueProbe]::Validate()
$report = [ordered]@{ passed = $true; productionOrderSystem = $true; engineAndNetworkStubbed = $true
    singlePlayerOwnerGatedAiRegression = $true; independentBuyerDrones = $true; serverOwnerAndRecipientAi = $true; immediateSpawnAndPackageSync = $true
    directDeactivationRecoversPaidOrder = $true; allocationAndOnSpawnFailureRetry = $true
    atMostOncePackage = $true; reusedPlayerSlotRejected = $true; clientCannotCreateOrdersOrDrones = $true }
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'delivery_validation.json'), $json)
$json
