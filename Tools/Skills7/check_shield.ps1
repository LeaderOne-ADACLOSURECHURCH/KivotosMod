$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $root 'Content/Skills/WeaponShieldState.cs')
$s=[KivotosMod.Content.Skills.WeaponShieldState]::new()
if (!$s.Activate() -or $s.Health -ne 100 -or $s.TimeLeft -ne 600 -or $s.CooldownLeft -ne 1200) { throw 'Activation values' }
if ($s.Activate()) { throw 'Shield stacked' }
if ($s.Absorb(40,0) -ne 0 -or $s.Health -ne 60 -or $s.HitTicks -ne 30) { throw 'Partial capacity absorption' }
if ($s.Absorb(90,9) -ne 30 -or $s.Active -or $s.BreakTicks -ne 44) { throw 'Overflow damage / break' }
if ($s.Absorb(50,1) -ne 50) { throw 'Broken shield still absorbs' }
for ($i=0;$i -lt 1199;$i++) {$s.Tick()}
if ($s.Activate()) { throw 'Cooldown ended early' }
$s.Tick()
if (!$s.Activate()) { throw 'Cooldown did not end' }
for ($i=0;$i -lt 599;$i++) {$s.Tick()}
if (!$s.Active) { throw 'Duration ended early' }
$s.Tick()
if ($s.Active -or $s.BreakTicks -ne 44) { throw 'Expiry did not break' }
for ($i=0;$i -lt 600;$i++) {$s.Tick()}
if (!$s.Activate()) { throw 'Cooldown did not continue after expiry' }
$s.Stop();$s.Tick()
if ($s.Active -or $s.CooldownLeft -ne 1199) { throw 'Death cleared cooldown' }
for ($damage=1;$damage -le 250;$damage++) {
 $s=[KivotosMod.Content.Skills.WeaponShieldState]::new();[void]$s.Activate()
 $left=$s.Absorb($damage,11)
 if ($left -ne [math]::Max(0,$damage-100) -or $s.Health -ne [math]::Max(0,100-$damage)) { throw 'Damage conservation' }
}
'PASS: actual shield state: activation, no stacking, capacity, overflow, break, 600tick duration,1200tick shared cooldown,death keeps cooldown,250 damage-conservation cases.'
