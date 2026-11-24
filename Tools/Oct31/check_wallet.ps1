$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path @((Join-Path $root 'Content/Tablet/TabletWeaponUpgrade.cs'), (Join-Path $root 'Content/Tablet/TabletWallet.cs'), (Join-Path $root 'Content/Tablet/TabletUiPreferencesPlayer.cs'), (Join-Path $root 'Tools/Summary2/rules_probe.cs'))
[Summary2Probe]::Validate()
$p = [Terraria.Player]::new()
$p.bank.item[0].type = [Terraria.ID.ItemID]::GoldCoin
$p.bank.item[0].stack = 3
if ([KivotosMod.Content.Tablet.TabletWallet]::GetTotalCredits($p) -ne 30000) { throw 'Piggy bank balance missing' }
if (![KivotosMod.Content.Tablet.TabletWallet]::TrySpend($p, 15000)) { throw 'Bank debit failed' }
$remaining = [KivotosMod.Content.Tablet.TabletWallet]::GetTotalCredits($p)
foreach ($item in $p.Change) { $remaining += [KivotosMod.Content.Tablet.TabletWallet]::GetUnitValue($item.type) * $item.stack }
if ($remaining -ne 15000) { throw 'Value conservation failed' }
if ([KivotosMod.Content.Tablet.TabletWallet]::TrySpend($p, 999999)) { throw 'Insufficient bank funds accepted' }
'PASS: piggy bank balance, debit, exact change, insufficient funds, existing upgrade and wallet regressions.'
