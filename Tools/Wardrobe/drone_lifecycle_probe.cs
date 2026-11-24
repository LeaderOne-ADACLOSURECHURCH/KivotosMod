using System;
using System.Collections.Generic;
using KivotosMod.Content.Tablet.Delivery;
namespace Microsoft.Xna.Framework {
 public struct Vector2 { public float X,Y;public Vector2(float x,float y){X=x;Y=y;}public static Vector2 Zero=>new();public static Vector2 UnitY=>new(0,1);public float Length()=>(float)Math.Sqrt(X*X+Y*Y);public static float Distance(Vector2 a,Vector2 b)=>(a-b).Length();public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.X-b.X,a.Y-b.Y);public static Vector2 operator *(Vector2 a,float b)=>new(a.X*b,a.Y*b);public static Vector2 operator /(Vector2 a,float b)=>new(a.X/b,a.Y/b);public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.X+b.X,a.Y+b.Y); }
 public static class MathHelper { public const float PiOver4=(float)Math.PI/4;public static float ToRadians(float degrees)=>degrees*(float)Math.PI/180; }
}
namespace Terraria.ID { public static class ItemID {public const int None=0;}public static class NetmodeID {public const int SinglePlayer=0,MultiplayerClient=1,Server=2;}public static class MessageID {public const int SyncProjectile=27,SyncItem=21;} }
namespace Terraria.ModLoader {
 public class ModSystem {public virtual void PostUpdatePlayers(){}public virtual void OnWorldUnload(){}public virtual void Unload(){}}
 public static class ModContent {public static ReLogic.Content.Asset<T> Request<T>(string s,ReLogic.Content.AssetRequestMode mode) where T:new()=>new();public static int ItemType<T>()=>500;public static int ProjectileType<T>()=>600;}
}
namespace Terraria {
 using Microsoft.Xna.Framework;
 public static class WorldGen {public static bool gen=false;}
 public static class Extensions {public static Vector2 RotatedBy(this Vector2 v,float a)=>new(v.X*(float)Math.Cos(a)-v.Y*(float)Math.Sin(a),v.X*(float)Math.Sin(a)+v.Y*(float)Math.Cos(a));public static Vector2 SafeNormalize(this Vector2 v,Vector2 fallback)=>v.Length()>0?v/v.Length():fallback;public static Vector2 RotatedByRandom(this Vector2 value,double angle)=>value;}
 public static class Main {
  public static bool dedServ;public static ulong GameUpdateCount;public static Vector2 screenPosition;public static void EntitySpriteDraw(object a,Vector2 b,object c,object d,float e,Vector2 f,float g,object h,int i){}public static int myPlayer;public static int netMode=2;public const int maxPlayers=3,maxProjectiles=6,maxItems=16;
  public static float leftWorld=0,topWorld=0,rightWorld=10000,bottomWorld=10000;
  public static Random rand=new(42);public static Player[] player;public static Projectile[] projectile;public static Item[] item;
 }
 public class Player {public int whoAmI;public bool active=true,dead,ghost;public Vector2 Center=new(1000,2000);public object GetSource_Misc(string name)=>name;}
 public class Item {public static bool newItemDisabled=false;
  public int type,stack=1;public bool active=true;public object ModItem;public Vector2 velocity;
  public void SetDefaults(int id){type=id;ModItem=new DeliveryPackage();}
  public static int Drops;
  public static int NewItem(object source,Vector2 position,Item package){Main.item[0]=package;Drops++;return 0;}
 }
 public class Projectile {
  public bool active;public Vector2 Center,velocity;public float rotation,scale=1;public int width,height,aiStyle,penetrate,timeLeft;public bool friendly,hostile,tileCollide,ignoreWater,netImportant;public void Kill(){active=false;}public int type,owner;public float[] ai=new float[3];public bool netUpdate;
  public static DeliveryDroneProjectile LastDrone;public static bool FailAllocation,DeactivateOnSpawn;public static int Attempts;
  public static List<(int Owner,int Recipient,int Order)> Spawns=new();
  public static int NewProjectile(object source,Vector2 position,Vector2 velocity,int type,int damage,float knockback,int owner,float ai0,float ai1,float ai2){
   Attempts++;if(FailAllocation)return Main.maxProjectiles;
   for(int i=0;i<Main.maxProjectiles;i++)if(!Main.projectile[i].active){
    Main.projectile[i]=new Projectile {active=!DeactivateOnSpawn,type=type,owner=owner,ai=owner==Main.myPlayer?new[]{ai0,ai1,ai2}:new float[3]};
    LastDrone=new DeliveryDroneProjectile();LastDrone.Projectile=Main.projectile[i];LastDrone.SetDefaults();LastDrone.Projectile.Center=position;
    if(!Main.dedServ)LastDrone.Load();LastDrone.OnSpawn(null);
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

}
namespace KivotosMod {public class KivotosMod {public static KivotosMod Instance=new();public Log Logger=new();public class Log {public void Info(string s){}public void Warn(string s){}}}}

namespace Microsoft.Xna.Framework {public struct Rectangle {public int Width,Height;public Rectangle(int x,int y,int w,int h){Width=w;Height=h;}}public struct Color {}}
namespace Microsoft.Xna.Framework.Graphics {public enum SpriteEffects {None}public class Texture2D {public Microsoft.Xna.Framework.Rectangle Bounds=>new(0,0,26,24);}}
namespace ReLogic.Content {public enum AssetRequestMode {ImmediateLoad}public class Asset<T> where T:new(){public T Value=new();}}
namespace Terraria.DataStructures {public interface IEntitySource {}}
namespace Terraria.ModLoader {public class ModProjectile {public Terraria.Projectile Projectile=new(){active=true};public virtual string Texture=>"";public virtual void Load(){}public virtual void Unload(){}public virtual void SetDefaults(){}public virtual void OnSpawn(Terraria.DataStructures.IEntitySource source){}public virtual void AI(){}public virtual bool PreDraw(ref Microsoft.Xna.Framework.Color c)=>false;public virtual bool? CanDamage()=>false;public virtual void OnKill(int t){}public virtual void SendExtraAI(System.IO.BinaryWriter w){}public virtual void ReceiveExtraAI(System.IO.BinaryReader r){}}}

public static class DroneLifecycleProbe {
 public static void Validate(){foreach(int mode in new[]{0,2}){
  new KivotosMod.Content.Tablet.Delivery.TabletDeliveryOrderSystem().OnWorldUnload();
  Terraria.Main.netMode=mode;Terraria.Main.dedServ=mode==2;Terraria.Main.myPlayer=mode==0?0:3;Terraria.Item.Drops=0;
  Terraria.Main.player=new Terraria.Player[3];for(int i=0;i<3;i++)Terraria.Main.player[i]=new(){whoAmI=i};
  Terraria.Main.item=new Terraria.Item[16];
  int order=TabletDeliveryOrderSystem.Enqueue(Terraria.Main.player[0],new(){new Terraria.Item{type=42,stack=9}});
  Terraria.Main.projectile=new Terraria.Projectile[6];for(int i=0;i<6;i++)Terraria.Main.projectile[i]=new();
  var system=new TabletDeliveryOrderSystem();for(int i=0;i<31;i++)system.PostUpdatePlayers();
  var drone=Terraria.Projectile.LastDrone;
  if(drone==null||drone.Projectile.ai[0]!=order||drone.Projectile.ai[1]!=500)throw new Exception("Spawn lost order/package identity");
  bool held=false;
  for(int i=0;i<2000;i++){Terraria.Main.GameUpdateCount++;drone.AI();drone.Projectile.Center+=drone.Projectile.velocity;
   if(mode==0&&i==0){var c=new Microsoft.Xna.Framework.Color();drone.PreDraw(ref c);held=true;}
  }
  if(Terraria.Item.Drops!=1)throw new Exception("Drone AI failed delivery in mode "+mode);
  var package=(DeliveryPackage)Terraria.Main.item[0].ModItem;
  if(package.Contents.Count!=1||package.Contents[0].type!=42||package.Contents[0].stack!=9)throw new Exception("Contents lost");
  if(mode==0&&!held)throw new Exception("No held draw");
 }}
}
