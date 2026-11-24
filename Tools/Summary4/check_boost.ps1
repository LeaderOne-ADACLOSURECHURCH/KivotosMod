$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$stubs = [IO.File]::ReadAllText((Join-Path $root 'Tools/Summary2/rules_probe.cs'))
$stubs = $stubs.Replace('public static int netMode;', 'public static int myPlayer;public static int netMode;')
$stubs = $stubs.Replace('public int whoAmI; public Item[]', 'public int whoAmI,selectedItem; public bool HasItem(int type)=>Array.Exists(inventory,i=>i.type==type&&i.stack>0); public Item[]')
$stubs = $stubs.Replace('public static int ItemType<T>()=>', 'public static int ItemType<T>()=>typeof(T).Name=="WeaponLevelBooster"?104:')
$stubs = $stubs.Replace('public static void SendUpgradeRequest', 'public static void SendBoostRequest(int s,int t,int l){} public static void SendUpgradeRequest')
$stubs += "`nnamespace KivotosMod.Content.Items.Tools { public class WeaponLevelBooster{} }"
$sourcePaths = @((Join-Path $root 'Content/Tablet/TabletWeaponUpgrade.cs'),(Join-Path $root 'Content/Tablet/TabletWallet.cs'),(Join-Path $root 'Content/Tablet/TabletUiPreferencesPlayer.cs'),(Join-Path $root 'Content/Tablet/TabletWeaponBoost.cs'),(Join-Path $PSScriptRoot 'boost_probe.cs'))
$probeDir = Join-Path $env:TEMP 'KivotosSummary4BoostProbe'
New-Item -ItemType Directory -Force -Path $probeDir | Out-Null
$stubPath=Join-Path $probeDir 'stubs.cs'
[IO.File]::WriteAllText($stubPath,$stubs)
Add-Type -Path ($sourcePaths + $stubPath)
[Summary2Probe]::Validate()
[Summary4BoostProbe]::Validate()
'PASS: production developer boost adds exactly one level, respects Lv.10 cap, validates tool/player/slot/type/old level, rejects direct client calls and duplicate requests, preserves other copies/materials/currency/tool (engine/network stubbed).'
