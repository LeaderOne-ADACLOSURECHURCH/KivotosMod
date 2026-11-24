using KivotosMod.Content.Tablet;
using KivotosMod.Globals.Classes;
using Terraria;
using System;
public static class Summary3ResetProbe {
 static void Check(bool b,string msg){if(!b)throw new Exception(msg);}
 public static void Validate(){
  Main.netMode=0;var p=new Player();p.inventory[0]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};p.inventory[0].Data.Level=10;
  p.inventory[1]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};p.inventory[1].Data.Level=8;
  string m;
  Check(!TabletWeaponReset.TryReset(p,0,100,10,out m),"Reset without tool");
  p.inventory[20]=new(){type=103,stack=1};
  Check(!TabletWeaponReset.TryReset(p,0,100,9,out m),"Stale reset level");
  Check(!TabletWeaponReset.TryReset(p,0,101,10,out m),"Wrong weapon reset");
  Check(!TabletWeaponReset.TryReset(p,10,100,10,out m),"Reset outside hotbar");
  Main.netMode=1;Check(!TabletWeaponReset.TryReset(p,0,100,10,out m),"Client cannot reset directly");Main.netMode=2;
  Check(TabletWeaponReset.TryReset(p,0,100,10,out m),"Server rejected valid developer reset");
  Check(p.inventory[0].Data.Level==1&&p.inventory[1].Data.Level==8,"Reset changed another weapon");
  Check(p.inventory[20].stack==1&&p.Change.Count==0,"Reset consumed tool or refunded currencies");
  Check(!TabletWeaponReset.TryReset(p,0,100,10,out m),"Replay reset");
  Check(!TabletWeaponReset.TryReset(p,0,100,1,out m),"Reset already level one");Main.netMode=0;
 }
}
