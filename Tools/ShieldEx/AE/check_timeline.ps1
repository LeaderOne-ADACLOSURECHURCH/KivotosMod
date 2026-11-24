$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$timeline=Join-Path $root 'Content/Skills/BlackHole/BlackHoleTimeline.cs'
$probe=@'
using System;
using System.IO;
using System.Text.Json;
namespace KivotosMod.Content.Skills.BlackHole {
public static class TimelineProbe {
 public static void Run(string path) {
  using var doc=JsonDocument.Parse(File.ReadAllText(path));
  var comp=doc.RootElement.GetProperty("comp");
  if(comp.GetProperty("width").GetInt32()!=BlackHoleTimeline.Width||comp.GetProperty("height").GetInt32()!=BlackHoleTimeline.Height)throw new Exception("Canvas differs from AEP");
  var layers=doc.RootElement.GetProperty("layers");
  for(int i=0;i<3;i++) {
   var layer=layers[i]; JsonElement position=default;
   foreach(var prop in layer.GetProperty("properties").EnumerateArray())
    if(prop.GetProperty("match_name").GetString()=="ADBE Position")position=prop;
   var keys=position.GetProperty("keyframes");
   double first=keys[0].GetProperty("frame_time").GetDouble(),last=keys[1].GetProperty("frame_time").GetDouble();
   double start=keys[0].GetProperty("value")[0].GetDouble(),end=keys[1].GetProperty("value")[0].GetDouble();
   for(int f=0;f<900;f++) {
    bool visible=BlackHoleTimeline.TryGetSpectrumX(i,f,out float x);
    bool expected=f>=Math.Round(layer.GetProperty("in_point").GetDouble()*30);
    double want=start+(end-start)*Math.Clamp((f-first)/(last-first),0,1);
    if(visible!=expected||Math.Abs(x-want)>.001)throw new Exception($"Layer {i} frame {f}: {x} != {want}");
   }
   BlackHoleTimeline.TryGetSpectrumX(i,899,out float a);BlackHoleTimeline.TryGetSpectrumX(i,int.MaxValue,out float b);
   if(a!=b)throw new Exception("Timeline did not hold final frame");
  }
  Console.WriteLine("PASS: actual production timeline matches extracted AEP canvas, in-points and linear position keys for all 2700 layer/frame combinations; final frame held.");
 }
}}
'@
$temp=Join-Path $env:TEMP 'KivotosAeTimelineProbe.cs'
[IO.File]::WriteAllText($temp,$probe)
Add-Type -Path @($timeline,$temp)
[KivotosMod.Content.Skills.BlackHole.TimelineProbe]::Run((Join-Path $PSScriptRoot 'project_inspection.json'))
