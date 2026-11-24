using System;
using Terraria;
using KivotosMod.Content.Tablet;
using KivotosMod.Globals.Classes;
public static class Summary4BoostProbe {
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Validate(){
  Main.netMode=0;var p=new Player();
  p.inventory[0]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};
  p.inventory[1]=new(){type=100,stack=1,ModItem=new StudentWeaponClass()};p.inventory[1].Data.Level=7;
  p.inventory[2]=new(){type=5,stack=6};p.inventory[3]=new(){type=3,stack=2};
  string result;
  Check(!TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Boost without tool");
  p.inventory[20]=new(){type=104,stack=1};
  Check(!TabletWeaponBoost.TryBoost(p,-1,100,1,out result),"Negative slot");
  Check(!TabletWeaponBoost.TryBoost(p,10,100,1,out result),"Non-hotbar slot");
  Check(!TabletWeaponBoost.TryBoost(p,0,999,1,out result),"Changed weapon type");
  Check(!TabletWeaponBoost.TryBoost(p,0,100,2,out result),"Stale weapon level");
  p.dead=true;Check(!TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Dead player");p.dead=false;
  p.active=false;Check(!TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Inactive player");p.active=true;
  Main.netMode=1;Check(!TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Direct client boost");Main.netMode=2;
  Check(TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Valid server boost");
  Check(p.inventory[0].Data.Level==2&&p.inventory[1].Data.Level==7,"More than one level or wrong copy");
  Check(!TabletWeaponBoost.TryBoost(p,0,100,1,out result),"Duplicate packet upgraded again");
  Check(p.inventory[0].Data.Level==2,"Duplicate packet changed level");
  for(int level=2;level<10;level++)Check(TabletWeaponBoost.TryBoost(p,0,100,level,out result),"Increment to "+(level+1));
  Check(p.inventory[0].Data.Level==10&&!TabletWeaponBoost.TryBoost(p,0,100,10,out result),"Max level cap");
  Check(p.inventory[2].stack==6&&p.inventory[3].stack==2&&p.inventory[20].stack==1&&p.Change.Count==0,"Developer boost consumed resources or tool");
  Main.netMode=0;
 }
}
