$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$main = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletUiSystem.cs'))
$skin = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletUiSystem.UpgradeSkin.cs'))
$upgrade = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletUiSystem.Upgrades.cs'))
function Extract-Method([string]$source,[string]$name) {
 $m=[regex]::Match($source,'(?m)^    private static [^\r\n]*\b'+$name+'\(')
 if(!$m.Success){throw "Missing method $name"}
 $start=$m.Index;$brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){
  if($source[$i] -eq '{'){$depth++}
  if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 }
 throw "Unclosed method $name"
}
$methods=(Extract-Method $skin 'DrawNineSlice')+"`n"+(Extract-Method $skin 'DrawShopFavoriteCategory')+"`n"+(Extract-Method $main 'DrawNativeCentered')+"`n"+(Extract-Method $main 'DrawTexture')
$itemBounds=[regex]::Match($main,'Rectangle itemHoverRect = new\(card\.X[^;]+;').Value
if(!$main.Contains('DrawItemIcon(sb, product.ItemType, itemHoverRect, layout);')){throw 'Shop image does not use its bounded hit area'}
$star=[regex]::Match($upgrade,'(?m)^    private static Rectangle ProductFavoriteRect[^;]+;').Value
if(!$upgrade.Contains('射速：{3600f / Math.Max(1, item.useTime):0}')){throw 'RPM is not integer-formatted'}
if($upgrade.Contains('理论射速')){throw 'Old theoretical label remains'}
$wrapper=@"
using System;using System.Collections.Generic;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using ReLogic.Content;
namespace KivotosMod.Content.Tablet {public sealed partial class TabletUiSystem {
 private static Asset<Texture2D> _shopFavoriteNormal=new(new("normal",30,30)),_shopFavoriteHover=new(new("hover",30,30)),_shopFavoritePressed=new(new("selected",30,30)),_shopFavoriteSelectedHover=new(new("selected-hover",30,30));
 $methods
 $star
 private static Rectangle ShopPicture(Rectangle card) { $itemBounds return itemHoverRect; }
 public static string Validate(){
  var sb=new SpriteBatch();var texture=new Texture2D("title",244,36);int cases=0;
  foreach(float scale in new[]{.55f,.6f,.65f,.7f,.8f,.9f,1f,1.05f,1.1f,1.2f,1.3f,1.4f})
   foreach(float ox in new[]{0f,.1f,.5f,17.3f,400.75f})foreach(float oy in new[]{0f,.2f,.5f,29.7f})
    foreach(var target in new[]{new Rectangle(46,79,194,24),new Rectangle(40,74,206,336),new Rectangle(260,82,272,86)}){
     sb.Draws.Clear();var layout=new Layout(new Vector2(ox,oy),scale);DrawNineSlice(sb,texture,target,layout,Color.White);
     if(sb.Draws.Count!=9)throw new Exception("Nine-slice patch count");
     for(int y=0;y<3;y++)for(int x=0;x<3;x++){
      var p=sb.Draws[y*3+x].Rect;
      if(x<2&&p.Right!=sb.Draws[y*3+x+1].Rect.X)throw new Exception("Horizontal slice gap");
      if(y<2&&p.Bottom!=sb.Draws[(y+1)*3+x].Rect.Y)throw new Exception("Vertical slice gap");
     }
     var last=sb.Draws[8].Rect;var end=layout.ToScreen(new Vector2(target.Right,target.Bottom));
     if(last.Right!=(int)MathF.Round(end.X)||last.Bottom!=(int)MathF.Round(end.Y))throw new Exception("Slice outer bounds drift");cases++;
    }
  for(int selected=0;selected<2;selected++)for(int hover=0;hover<2;hover++){
   sb.Draws.Clear();DrawShopFavoriteCategory(sb,new Rectangle(274,136,24,24),selected==1,hover==1,new Layout(new Vector2(0,0),1));
   string expected=selected==1?(hover==1?"selected-hover":"selected"):(hover==1?"hover":"normal");
   if(sb.Draws.Count!=1||sb.Draws[0].Name!=expected)throw new Exception("Wrong favorite category state: "+expected);
   if(sb.Draws[0].Rect.Width!=30||sb.Draws[0].Rect.Height!=30)throw new Exception("Favorite category still shrunk to 24px");
  }
  foreach(int y in new[]{80,230,-42}){
   var card=new Rectangle(306,y,108,148);var picture=ShopPicture(card);var star=ProductFavoriteRect(card);
   // ProductCard.png's separator spans source rows 80..81, stretched from 130 to 148px.
   float separator=y+80f*148/130;
   if(picture.Y<y+32f*148/130||picture.Bottom>=separator||picture.X<card.X+4||picture.Right>card.Right-4)throw new Exception("Weapon image crosses card dividers");
   if(star.Bottom>=separator||star.Y<picture.Y)throw new Exception("Favorite star crosses lower divider");
  }
  return "PASS: "+cases+" actual nine-slice draw cases have shared pixel boundaries; all four category states render at native 30px; shop images/stars stay inside the separator lines; integer RPM label verified (graphics recorded, not GPU-rendered).";
 }
 private readonly struct Layout {public readonly Vector2 Origin;public readonly float Scale;public Layout(Vector2 origin,float scale){Origin=origin;Scale=scale;}public Vector2 ToScreen(Vector2 p)=>Origin+p*Scale;public Rectangle ToScreen(Rectangle r)=>new((int)MathF.Round(Origin.X+r.X*Scale),(int)MathF.Round(Origin.Y+r.Y*Scale),Math.Max(1,(int)MathF.Round(r.Width*Scale)),Math.Max(1,(int)MathF.Round(r.Height*Scale)));}
}}
"@
$probeDir=Join-Path $env:TEMP 'KivotosSummary4UiProbe'
New-Item -ItemType Directory -Force -Path $probeDir | Out-Null
$path=Join-Path $probeDir 'production_ui_probe.cs'
[IO.File]::WriteAllText($path,$wrapper)
Add-Type -Path @($path,(Join-Path $PSScriptRoot 'ui_stubs.cs'))
[KivotosMod.Content.Tablet.TabletUiSystem]::Validate()
