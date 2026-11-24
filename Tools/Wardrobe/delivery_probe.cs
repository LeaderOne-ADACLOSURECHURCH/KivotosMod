using System;
using System.Collections.Generic;
using KivotosMod.Content.Tablet.Delivery;
namespace Microsoft.Xna.Framework {
 public struct Vector2 { public float X,Y;public Vector2(float x,float y){X=x;Y=y;}public static Vector2 Zero=>new();public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.X+b.X,a.Y+b.Y); }
 public static class MathHelper { public static float ToRadians(float degrees)=>degrees*(float)Math.PI/180; }
}
namespace Terraria.ID { public static class NetmodeID {public const int SinglePlayer=0,MultiplayerClient=1,Server=2;}public static class MessageID {public const int SyncProjectile=27,SyncItem=21;} }
namespace Terraria.ModLoader {
 public class ModSystem {public virtual void PostUpdatePlayers(){}public virtual void OnWorldUnload(){}public virtual void Unload(){}}
 public static class ModContent {public static int ItemType<T>()=>500;public static int ProjectileType<T>()=>600;}
}
namespace Terraria {
 using Microsoft.Xna.Framework;
 public static class WorldGen {public static bool gen=false;}
 public static class Extensions {public static Vector2 RotatedByRandom(this Vector2 value,double angle)=>value;}
 public static class Main {
  public static int myPlayer=3;public static int netMode=2;public const int maxPlayers=3,maxProjectiles=6,maxItems=16;
  public static float leftWorld=0,topWorld=0,rightWorld=10000,bottomWorld=10000;
  public static Random rand=new(42);public static Player[] player;public static Projectile[] projectile;public static Item[] item;
 }
 public class Player {public int whoAmI;public bool active=true,dead,ghost;public Vector2 Center=new(1000,2000);public object GetSource_Misc(string name)=>name;}
 public class Item {public static bool newItemDisabled=false;
  public int type,stack=1;public bool active=true;public object ModItem;public Vector2 velocity;
  public void SetDefaults(int id){type=id;ModItem=new DeliveryPackage();}
  public static int Drops;public static bool SuppressSpawn,EmptySpawn;
  public static int NewItem(object source,Vector2 position,Item package){if(SuppressSpawn)return 0;Main.item[0]=EmptySpawn?new Item{type=package.type,ModItem=new DeliveryPackage()}:package;Drops++;return 0;}
 }
 public class Projectile {
  public bool active;public int type,owner;public float[] ai=new float[3];public bool netUpdate;
  public static bool FailAllocation,DeactivateOnSpawn;public static int Attempts;
  public static List<(int Owner,int Recipient,int Order)> Spawns=new();
  public static int NewProjectile(object source,Vector2 position,Vector2 velocity,int type,int damage,float knockback,int owner,float ai0,float ai1,float ai2){
   Attempts++;if(FailAllocation)return Main.maxProjectiles;
   for(int i=0;i<Main.maxProjectiles;i++)if(!Main.projectile[i].active){
    Main.projectile[i]=new Projectile {active=!DeactivateOnSpawn,type=type,owner=owner,ai=owner==Main.myPlayer?new[]{ai0,ai1,ai2}:new float[3]};
    Spawns.Add((owner,(int)ai2,(int)ai0));return i;
   }
   return Main.maxProjectiles;
  }
 }
 public static class NetMessage {
  public static List<(int Type,int Index)> Calls=new();
  public static void SendData(int type,int to=-1,int from=-1,object text=null,int number=0){Calls.Add((type,number));}
 }
}
namespace KivotosMod.Content.Tablet.Delivery {
 public class DeliveryPackage {public List<Terraria.Item> Contents=new();public void SetContents(List<Terraria.Item> contents){Contents=new(contents);}}
 public class DeliveryDroneProjectile {}
}
public static class DeliveryQueueProbe {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Reset(){
  new TabletDeliveryOrderSystem().OnWorldUnload();Terraria.Main.netMode=2;
  Terraria.Main.player=new Terraria.Player[3];for(int i=0;i<3;i++)Terraria.Main.player[i]=new(){whoAmI=i};
  Terraria.Main.projectile=new Terraria.Projectile[6];for(int i=0;i<6;i++)Terraria.Main.projectile[i]=new();
  Terraria.Main.item=new Terraria.Item[16];Terraria.Projectile.Spawns.Clear();Terraria.Projectile.Attempts=0;
  Terraria.Projectile.FailAllocation=Terraria.Projectile.DeactivateOnSpawn=false;Terraria.Item.Drops=0;Terraria.NetMessage.Calls.Clear();
 }
 static void Advance(int ticks){var system=new TabletDeliveryOrderSystem();for(int i=0;i<ticks;i++)system.PostUpdatePlayers();}
 public static void Validate(){
  Reset();Terraria.Main.netMode=0;Terraria.Main.myPlayer=0;
  int single=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item()});Advance(31);
  Check(Terraria.Main.projectile[0].ai[0]==single&&Terraria.Main.projectile[0].ai[1]==500,"Singleplayer NewProjectile discarded order and package AI because owner != Main.myPlayer");
  Terraria.Main.myPlayer=3;

  Reset();int a=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item()});
  int b=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[2],new(){new Terraria.Item()});Advance(31);
  Check(Terraria.Projectile.Spawns.Count==2,"Two buyers did not receive independent drones");
  foreach(var spawn in Terraria.Projectile.Spawns)Check(spawn.Owner==Terraria.Main.maxPlayers,"Drone was assigned to a buyer client");
  Check(Terraria.Projectile.Spawns[0].Recipient==0&&Terraria.Projectile.Spawns[1].Recipient==2,"Recipient AI mismatch");
  Check(Terraria.NetMessage.Calls.FindAll(c=>c.Type==27).Count==2,"Initial drone sync missing");
  Terraria.Main.projectile[0].active=false;Advance(91);
  Check(Terraria.Projectile.Spawns.Count==3&&Terraria.Projectile.Spawns[2].Order==a,"Lost drone left paid order stuck");
  Check(TabletDeliveryOrderSystem.TryDropPackage(a,new()),"Package did not drop");
  Check(!TabletDeliveryOrderSystem.TryDropPackage(a,new())&&Terraria.Item.Drops==1,"Order delivered twice");
  Check(Terraria.NetMessage.Calls.Exists(c=>c.Type==21),"Package sync missing");
  Terraria.Main.player[2]=new(){whoAmI=2};Check(!TabletDeliveryOrderSystem.TryDropPackage(b,new()),"Reused player slot received someone else's order");
  Reset();Terraria.Projectile.FailAllocation=true;int retry=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item()});Advance(31);
  Terraria.Projectile.FailAllocation=false;Advance(91);Check(Terraria.Projectile.Spawns.Count==1&&Terraria.Projectile.Spawns[0].Order==retry,"Full projectile pool lost order");
  Reset();Terraria.Projectile.DeactivateOnSpawn=true;retry=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item()});Advance(31);
  Terraria.Projectile.DeactivateOnSpawn=false;Advance(91);Check(Terraria.Main.projectile[0].active,"OnSpawn cancellation left order in flight");
  Reset();int guarded=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item{type=42,stack=9}});
  Terraria.Item.SuppressSpawn=true;Terraria.Main.item[0]=new(){active=false};Check(!TabletDeliveryOrderSystem.TryDropPackage(guarded,new()),"Fake item index consumed order");Terraria.Item.SuppressSpawn=false;
  Terraria.Item.EmptySpawn=true;Check(!TabletDeliveryOrderSystem.TryDropPackage(guarded,new()),"Empty package consumed order");Terraria.Item.EmptySpawn=false;
  Check(TabletDeliveryOrderSystem.TryDropPackage(guarded,new()),"Failed spawn lost paid order");
  Reset();Terraria.Main.netMode=1;Check(TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new())==-1,"Client created a delivery order");Advance(100);Check(Terraria.Projectile.Spawns.Count==0,"Client spawned an authoritative drone");
 }
}

namespace KivotosMod {public class KivotosMod {public static KivotosMod Instance=new();public Log Logger=new();public class Log {public void Info(string s){}public void Warn(string s){}}}}
