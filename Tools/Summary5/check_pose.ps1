$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletStudentAppearancePlayer.cs'))
$start = $source.IndexOf('    private Rectangle _originalLegFrame;')
$end = $source.IndexOf('    public override bool PreKill', $start)
$hooks = $source.Substring($start, $end - $start)
$prefix = @"
using System;
namespace Terraria.ID { public static class MountID { public const int WitchBroom=23; } }
public struct Rectangle { public int X,Y,Width,Height; }
public struct Vector2 { public float X,Y; }
public static class MathHelper { public const float PiOver2=(float)Math.PI/2; }
public class Asset { public bool IsLoaded=true; public object Value=new(); }
public class MountData { public Asset backTexture=new(),backTextureGlow=new(),backTextureExtra=new(),backTextureExtraGlow=new(),frontTexture=new(),frontTextureGlow=new(),frontTextureExtra=new(),frontTextureExtraGlow=new(); }
public class Mount { public bool Active; public int Type; public MountData _data=new(); }
public struct DrawData { public object texture; public Vector2 position; }
public class Player { public bool dead,ghost; public int direction=1; public float gravDir=1; public Mount mount=new(); public Vector2 velocity; public Rectangle legFrame=new(){Width=40,Height=56}; }
public struct PlayerDrawSet { public System.Collections.Generic.List<DrawData> DrawDataCache; public Player drawPlayer; public bool isSitting,isSleeping,headOnlyRender; public float rotation; }
public class BasePlayer { public Player Player=new(); public virtual void UpdateAutopause(){} public virtual void FrameEffects(){} public virtual void ModifyDrawInfo(ref PlayerDrawSet info){} public virtual void TransformDrawData(ref PlayerDrawSet info){} }
public class PosePlayer:BasePlayer { public bool Outfit=true; private bool UsesStudentOutfit()=>Outfit; private void ApplyStudentVisualSlots(Player p){}
"@
Add-Type -TypeDefinition ($prefix + $hooks + '}')
foreach ($direction in @(-1,1)) {
 $state=[PosePlayer]::new(); $state.Player.direction=$direction
 $state.Player.mount.Active=$true; $state.Player.mount.Type=23
 $frame=$state.Player.legFrame; $frame.Y=280; $state.Player.legFrame=$frame
 $info=[PlayerDrawSet]::new(); $info.DrawDataCache=[System.Collections.Generic.List[DrawData]]::new()
 $rider=[DrawData]::new(); $rider.texture=[object]::new(); $info.DrawDataCache.Add($rider)
 $mount=[DrawData]::new(); $mount.texture=$state.Player.mount._data.backTexture.Value; $info.DrawDataCache.Add($mount)
 $info.drawPlayer=$state.Player; $info.isSitting=$true
 $state.ModifyDrawInfo([ref]$info)
 if ($info.isSitting -or $state.Player.legFrame.Y -ne 0) { throw 'Broom still slices/uses sitting frame' }
 $state.TransformDrawData([ref]$info)
 if ($info.DrawDataCache[0].position.Y -ne -11 -or $info.DrawDataCache[1].position.Y -ne 0) { throw 'Rider must move up11 while broom stays fixed' }
 if ($state.Player.legFrame.Y -ne 280) { throw 'Leg animation not restored' }
 # The previous wrong mount must stay on its native path.
 $state.Player.mount.Type=45; $info.isSitting=$true
 $state.ModifyDrawInfo([ref]$info)
 if (!$info.isSitting -or $state.Player.legFrame.Y -ne 280) { throw 'Unrelated mount changed' }
 $state.Player.mount.Type=23; $state.Outfit=$false
 $state.ModifyDrawInfo([ref]$info)
 if (!$info.isSitting) { throw 'Ordinary character changed' }
 $state.Outfit=$true; $state.Player.mount.Active=$false; $info.isSleeping=$true
 $state.ModifyDrawInfo([ref]$info)
 if ([math]::Abs($info.rotation + $direction*[math]::PI/2) -gt .00001) { throw 'Sleep rotation changed' }
}
'PASS: production pose hooks: broom23 restores full standing legs in both directions, restores original frame, leaves mount45/ordinary character alone, preserves sleep rotation (engine stubbed).'
