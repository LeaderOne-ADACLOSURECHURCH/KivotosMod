$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$stubs = [IO.File]::ReadAllText((Join-Path $root 'Tools/Summary2/rules_probe.cs'))
$stubs = $stubs.Replace('public static int netMode;', 'public static int myPlayer;public static int netMode;')
$stubs = $stubs.Replace('public int whoAmI; public Item[]', 'public int whoAmI,selectedItem; public bool HasItem(int type)=>Array.Exists(inventory,i=>i.type==type&&i.stack>0); public Item[]')
$stubs = $stubs.Replace('public static int ItemType<T>()=>', 'public static int ItemType<T>()=>typeof(T).Name=="WeaponLevelResetter"?103:')
$stubs = $stubs.Replace('t==100?new ModItem():null', 't==100||t==200?new ModItem{FullName="KivotosMod/"+t}:null')
$stubs = $stubs.Replace('public static void SendUpgradeRequest', 'public static void SendResetRequest(int s,int t,int l){} public static void SendUpgradeRequest')
$stubs += "`nnamespace KivotosMod.Content.Items.Tools { public class WeaponLevelResetter{} }"
$ui = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletUiSystem.Upgrades.cs'))
$mainUi = [IO.File]::ReadAllText((Join-Path $root 'Content/Tablet/TabletUiSystem.cs'))
function Extract-Method([string]$source, [string]$name) {
    $match = [regex]::Match($source, '(?m)^    private static [^\r\n]*\b' + $name + '\(')
    if (!$match.Success) {throw "Missing method $name"}
    $start=$match.Index; $brace=$source.IndexOf('{',$start); $arrow=$source.IndexOf('=>',$start)
    if ($arrow -ge 0 -and $arrow -lt $brace) {return $source.Substring($start,$source.IndexOf(';',$arrow)-$start+1)}
    $depth=0
    for($i=$brace;$i -lt $source.Length;$i++){
        if($source[$i] -eq '{'){$depth++}
        if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
    }
    throw "Unclosed method $name"
}
$methods = (Extract-Method $ui 'VisibleProducts') + "`n" + (Extract-Method $ui 'WeaponSlots') + "`n" + (Extract-Method $ui 'ProductFavoriteRect') + "`n" + (Extract-Method $mainUi 'GetShopCardRect')
$wrapper = @"
using System; using System.Collections.Generic; using System.Linq; using Microsoft.Xna.Framework; using Terraria; using KivotosMod.Globals.Classes;
namespace KivotosMod.Content.Tablet { public sealed partial class TabletUiSystem {
 private static TabletShopCategory _selectedCategory;
 private static TabletUiPreferencesPlayer Preferences=new();
 private static float _shopScrollVisual;
 $methods
 public static void ValidateOrdering() {
  var a=new TabletShopProduct{ItemType=1,InternalName="A"};var b=new TabletShopProduct{ItemType=2,InternalName="B"};
  TabletShopCatalog.Items.Add(a);TabletShopCatalog.Items.Add(b);Preferences.ToggleProduct(b);
  if(VisibleProducts()[0]!=a)throw new Exception("Shop favorites changed catalogue order");
  _selectedCategory=TabletShopCategory.Favorites;
  if(VisibleProducts().Count!=1||VisibleProducts()[0]!=b)throw new Exception("Favorite category filtering");
  Main.LocalPlayer=new();Main.LocalPlayer.inventory[2]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};
  Main.LocalPlayer.inventory[5]=new(){type=200,stack=1,ModItem=new StudentWeaponClass()};Main.LocalPlayer.inventory[6]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};
  Preferences.ToggleWeapon(200);var slots=WeaponSlots();
  if(slots.Count!=3||slots[0]!=5||slots[1]!=2||slots[2]!=6)throw new Exception("Weapon favorites must pin without merging copies");
  var tag=new Terraria.ModLoader.IO.TagCompound();Preferences.SaveData(tag);var p=new TabletUiPreferencesPlayer();p.LoadData(tag);
  if(!p.IsFavoriteWeapon(200))throw new Exception("Weapon favorites persistence");p.ToggleWeapon(200);if(p.IsFavoriteWeapon(200))throw new Exception("Unfavorite weapon");
  if(GetShopCardRect(2).Bottom!=378||GetShopCardRect(0).Height!=148)throw new Exception("Shop cards leave the lower gap");
  _shopScrollVisual=73.5f;var card=GetShopCardRect(2);var star=ProductFavoriteRect(card);
  if(star.Y!=card.Y+74||star.Right>card.Right||star.Bottom>card.Bottom)throw new Exception("Animated favorite hit target misaligned");
 }
}}
namespace KivotosMod.Content.Tablet {public enum TabletShopCategory{All,Favorites}public static class TabletShopCatalog{public static List<TabletShopProduct> Items=new();public static IReadOnlyList<TabletShopProduct> Products=>Items;public static IReadOnlyList<TabletShopProduct> GetProducts(TabletShopCategory c)=>Items;}}
namespace Microsoft.Xna.Framework {public struct Rectangle {public int X,Y,Width,Height;public int Right=>X+Width;public int Bottom=>Y+Height;public Rectangle(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}}}
"@
$sources=@($stubs,$wrapper,[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'reset_probe.cs')))
foreach($p in @('Content/Tablet/TabletWeaponUpgrade.cs','Content/Tablet/TabletWallet.cs','Content/Tablet/TabletUiPreferencesPlayer.cs','Content/Tablet/TabletWeaponReset.cs')){$sources += [IO.File]::ReadAllText((Join-Path $root $p))}
$probeDir = Join-Path $env:TEMP 'KivotosSummary3Probes'
New-Item -ItemType Directory -Force -Path $probeDir | Out-Null
$sourcePaths = @()
for($i=0;$i -lt $sources.Count;$i++){
    $sourcePath=Join-Path $probeDir ("source"+$i+".cs")
    [IO.File]::WriteAllText($sourcePath,$sources[$i])
    $sourcePaths += $sourcePath
}
Add-Type -Path $sourcePaths
[Summary2Probe]::Validate()
[KivotosMod.Content.Tablet.TabletUiSystem]::ValidateOrdering()
[Summary3ResetProbe]::Validate()
'PASS: actual shop/weapon ordering and animated card geometry; persistent weapon favorites; developer reset authorization, replay, slot isolation and no refunds (engine stubbed).'
