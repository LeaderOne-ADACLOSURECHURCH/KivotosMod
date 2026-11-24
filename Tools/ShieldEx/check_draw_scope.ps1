$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source=[IO.File]::ReadAllText((Join-Path $root 'Content/Skills/BlackHole/BlackHolePreviewSystem.cs'))
$start=$source.IndexOf('    private void DrawWithShimmer(')
$end=$source.IndexOf('    private void InsertIntoShimmerStars(', $start)
$method=$source.Substring($start,$end-$start)
$prefix=@'
using System;
public class GameTime {}
public class Main { public static bool gameMenu; public static float shimmerAlpha; }
public static class On_Main { public delegate void orig_DoDraw(Main self, GameTime time); }
public sealed class DrawScopeProbe {
 static bool Enabled;
 int prepared;
 bool preparationFails;
 void PrepareComposite() { prepared++; if(preparationFails) throw new Exception("prepare"); }
'@
$probe=@'
 public static void Run() {
  var probe=new DrawScopeProbe(); var main=new Main(); var time=new GameTime();
  foreach(float prior in new[]{0f,.37f,1f}) {
   foreach(bool enabled in new[]{false,true}) foreach(bool menu in new[]{false,true}) {
    Enabled=enabled;Main.gameMenu=menu;Main.shimmerAlpha=prior;int calls=0;probe.prepared=0;
    probe.DrawWithShimmer((m,t)=>{calls++; if(m!=main||t!=time)throw new Exception("Argument lost");
      float expected=enabled&&!menu?1f:prior;
      if(Main.shimmerAlpha!=expected)throw new Exception("Wrong render-only shimmer state");},main,time);
    if(calls!=1||Main.shimmerAlpha!=prior||probe.prepared!=(enabled&&!menu?1:0))throw new Exception("Render scope leak");
   }
   Enabled=true;Main.gameMenu=false;Main.shimmerAlpha=prior;
   try {probe.DrawWithShimmer((m,t)=>throw new Exception("draw"),main,time);throw new Exception("Exception swallowed");}
   catch(Exception e) when(e.Message=="draw") {}
   if(Main.shimmerAlpha!=prior)throw new Exception("Exception leaked forced shimmer");
   probe.preparationFails=true;
   try {probe.DrawWithShimmer((m,t)=>throw new Exception("orig must not run"),main,time);throw new Exception("Preparation failure swallowed");}
   catch(Exception e) when(e.Message=="prepare") {}
   probe.preparationFails=false;
   if(Main.shimmerAlpha!=prior)throw new Exception("Preparation changed shimmer");
  }
  Console.WriteLine("PASS: actual production draw wrapper: off/on/menu states, original draw once, argument preservation, prior shimmer 0/0.37/1 restored on success and draw/preparation exceptions (graphics stubbed).");
 }
}
'@
Add-Type -TypeDefinition ($prefix+$method+$probe)
[DrawScopeProbe]::Run()
