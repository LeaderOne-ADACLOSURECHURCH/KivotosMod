$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source=[IO.File]::ReadAllText((Join-Path $root 'Content/Skills/WeaponShieldVisuals.cs'))
if(!$source.Contains('SamplerState.LinearClamp') -or !$source.Contains('ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One')){throw 'Premultiplied additive rendering pass missing'}
$renderer=$source.Substring($source.IndexOf('internal static class WeaponShieldRenderer'))
$prefix=@"
using System;using System.Collections.Generic;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using Terraria;
namespace Microsoft.Xna.Framework {
 public struct Vector2 {public float X,Y;public Vector2(float x,float y){X=x;Y=y;}public Vector2(float n){X=Y=n;}public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.X+b.X,a.Y+b.Y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.X-b.X,a.Y-b.Y);}
 public struct Rectangle {public int X,Y,Width,Height;public Rectangle(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}}
 public struct Color {public byte A;public static Color White=>new(){A=255};public static Color operator *(Color c,float n)=>new(){A=(byte)(c.A*n)};}
}
namespace Microsoft.Xna.Framework.Graphics {
 public enum SpriteEffects {None}
 public class Texture2D {public string Name;public int Width,Height;public Texture2D(string n,int w,int h){Name=n;Width=w;Height=h;}}
 public class SpriteBatch {public readonly List<(Texture2D Texture,Rectangle Source,Color Color)> Calls=new();public void Draw(Texture2D texture,Vector2 center,Rectangle source,Color color,float rotation,Vector2 origin,float scale,SpriteEffects effects,float depth){if(source.X<0||source.Y<0||source.X+source.Width>texture.Width||source.Y+source.Height>texture.Height)throw new Exception("Frame outside sheet");Calls.Add((texture,source,color));}}
}
namespace Terraria {public static class Main {public static Vector2 screenPosition;}public class Mount{public bool Active;public int Type;}public class Player {public Vector2 Center;public float gfxOffY,gravDir=1;public Mount mount=new();public KivotosMod.Content.Skills.WeaponShieldPlayer State=new();public T GetModPlayer<T>() where T:new()=>typeof(T)==typeof(KivotosMod.Content.Skills.WeaponShieldPlayer)?(T)(object)State:new();}}
namespace Terraria.ID {public static class MountID {public const int WitchBroom=23;}}
namespace KivotosMod.Content.Tablet {public class TabletStudentAppearancePlayer {public bool UsesStudentOutfit()=>true;}}
namespace KivotosMod.Content.Skills {
 public class WeaponShieldPlayer {public WeaponShieldState Shield=new();}
 public static class WeaponShieldVisuals {public static Texture2D Texture(string name)=>name=="Idle"?new(name,2400,2300):new(name,100,1500);public static Texture2D BreakTexture()=>new("Break",250,5500);}
"@
$probe=@"
public static class RenderProbe {
 public static void Run(){
 var player=new Player();var state=player.State.Shield;var batch=new SpriteBatch();state.Activate();
 WeaponShieldRenderer.Draw(batch,player);if(batch.Calls.Count!=1||batch.Calls[0].Texture.Name!="Spawn")throw new Exception("Spawn sequence");
 state.Age=31;batch.Calls.Clear();WeaponShieldRenderer.Draw(batch,player);if(batch.Calls[0].Color.A>77)throw new Exception("Idle fill too opaque");
 for(int direction=0;direction<12;direction++)for(int ticks=1;ticks<=30;ticks++){
 state.HitDirection=(byte)direction;state.HitTicks=ticks;batch.Calls.Clear();WeaponShieldRenderer.Draw(batch,player);
 if(batch.Calls.Count!=2||batch.Calls[0].Texture.Name!="Idle")throw new Exception("Hit replaced persistent layer");
 int clock=direction;bool reverse=clock>=3&&clock<=8;if(reverse)clock=(clock+6)%12;
 if(batch.Calls[1].Texture.Name!="Hit"+(clock==0?12:clock))throw new Exception("Wrong directional sheet");
 }
 state.Health=state.TimeLeft=0;for(int ticks=1;ticks<=44;ticks++){state.BreakTicks=ticks;batch.Calls.Clear();WeaponShieldRenderer.Draw(batch,player);if(batch.Calls.Count!=1||batch.Calls[0].Color.A>77)throw new Exception("Break too opaque");}
 Console.WriteLine("PASS: production shield draw method: spawn/idle,12 directions x30 ticks with persistent overlay,44 break ticks,all source bounds and 30% idle/break opacity; separate linear premultiplied additive pass present (graphics calls recorded).");
 }
}
}
"@
$temp=Join-Path $env:TEMP 'KivotosMail8RenderProbe.cs'
[IO.File]::WriteAllText($temp,$prefix+$renderer+$probe)
Add-Type -Path @($temp,(Join-Path $root 'Content/Skills/WeaponShieldState.cs'))
[KivotosMod.Content.Skills.RenderProbe]::Run()
