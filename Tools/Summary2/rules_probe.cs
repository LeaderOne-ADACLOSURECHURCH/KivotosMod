using System; using System.IO; using System.Collections.Generic;
namespace Terraria.ID {
 public static class NetmodeID { public const int SinglePlayer=0, MultiplayerClient=1,Server=2; }
 public static class MessageID { public const int SyncEquipment=5; }
 public static class ItemID { public const int CopperCoin=1,SilverCoin=2,GoldCoin=3,PlatinumCoin=4,Lens=5,IronBar=6,TissueSample=7,ShadowScale=8,Bone=9,HellstoneBar=10,HallowedBar=11,SoulofMight=12,SoulofSight=13,SoulofFright=14,ChlorophyteBar=15,BeetleHusk=16,FragmentSolar=17,FragmentVortex=18,FragmentNebula=19,FragmentStardust=20,LunarBar=21; }
}
namespace Terraria {
 public struct StatModifier { public float Value; public static StatModifier operator *(StatModifier s,float n)=>new(){Value=s.Value*n}; }
 public static class WorldGen { public static bool crimson; }
 public static class NPC { public static bool downedBoss1,downedBoss2,downedBoss3,downedMechBoss1,downedMechBoss2,downedMechBoss3,downedPlantBoss,downedGolemBoss,downedAncientCultist,downedMoonlord; }
 public static class Main { public static int netMode; public static bool hardMode;public static Player LocalPlayer=new();public static void NewText(string s){} }
 public static class NetMessage { public static void SendData(int msg,int a,int b,object c,int d,int i){} }
 public class Item {
  public int type,stack; public string Name="test"; public Terraria.ModLoader.ModItem ModItem; public KivotosMod.Content.Tablet.TabletWeaponLevel Data=new();
  public T GetGlobalItem<T>() where T:class=>Data as T;
  public void TurnToAir(){type=0;stack=0;ModItem=null;}
 }
 public class Chest { public Item[] item=new Item[40]; public Chest(){for(int i=0;i<item.Length;i++)item[i]=new();} }
 public class Player {
  public Chest bank=new();
  public bool active=true,dead; public int whoAmI; public Item[] inventory=new Item[59]; public List<Item> Change=new();
  public Player(){for(int i=0;i<inventory.Length;i++)inventory[i]=new();}
  public object GetSource_Misc(string s)=>null;
  public void QuickSpawnItem(object s,int type,int stack){Change.Add(new Item{type=type,stack=stack});}
 }
}
namespace Terraria.ModLoader {
 public class ModItem { public string FullName="KivotosMod/Test"; }
 public static class ItemLoader { public static ModItem GetItem(int t)=>t==100?new ModItem():null; }
 public class ModContent { public static int ItemType<T>()=>typeof(T).Name=="UnionCredit"?101:102; }
 public class TooltipLine { public TooltipLine(object mod,string name,string text){} }
 public class GlobalItem {
  public object Mod; public virtual bool InstancePerEntity=>false;public virtual bool AppliesToEntity(Terraria.Item i,bool late)=>true;
  public virtual void SaveData(Terraria.Item i,Terraria.ModLoader.IO.TagCompound t){}public virtual void LoadData(Terraria.Item i,Terraria.ModLoader.IO.TagCompound t){}
  public virtual void NetSend(Terraria.Item i,BinaryWriter w){} public virtual void NetReceive(Terraria.Item i,BinaryReader r){}
  public virtual void ModifyWeaponDamage(Terraria.Item i,Terraria.Player p,ref Terraria.StatModifier d){}public virtual void ModifyWeaponCrit(Terraria.Item i,Terraria.Player p,ref float c){}
  public virtual void ModifyTooltips(Terraria.Item i,List<TooltipLine> l){}
 }
 public class ModPlayer { public virtual void SaveData(Terraria.ModLoader.IO.TagCompound t){} public virtual void LoadData(Terraria.ModLoader.IO.TagCompound t){} }
}
namespace Terraria.ModLoader.IO {public class TagCompound:Dictionary<string,object> {
 public int GetInt(string k)=>ContainsKey(k)?(int)this[k]:0; public bool GetBool(string k)=>ContainsKey(k)&&(bool)this[k];public float GetFloat(string k)=>ContainsKey(k)?(float)this[k]:0;
 public IList<T> GetList<T>(string k)=>ContainsKey(k)?(IList<T>)this[k]:new List<T>();
}}
namespace Microsoft.Xna.Framework {public struct Vector2 {public float X,Y;public Vector2(float x,float y){X=x;Y=y;}}}
namespace KivotosMod.Globals.Classes {public class StudentWeaponClass:Terraria.ModLoader.ModItem {
 public float[] DamageUpgradeSteps=new float[11];public KivotosMod.Content.Tablet.WeaponUpgradeMaterial[] UpgradeMaterials(int l)=>KivotosMod.Content.Tablet.TabletWeaponUpgrade.CommonMaterials(l);
}}
namespace KivotosMod.Globals.Configs {public class KivotosClientConfig {public static KivotosClientConfig Instance=new();public float StudentWeaponDamageMultiplier=1;}}
namespace KivotosMod.Content.Tablet.Currency { public class UnionCredit{}public class Pyroxene{} }
namespace KivotosMod.Content.Tablet {
 public enum TabletStudentId {Yuuka=0,Mika=11}
 public class TabletShopProduct {public int ItemType;public string InternalName="Test";}
 public static class TabletNet {public static void SendUpgradeRequest(int s,int t,int l){} }
}
public static class Summary2Probe {
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static Terraria.Player PlayerWithWeapon(){var p=new Terraria.Player();p.inventory[0]=new Terraria.Item{type=100,stack=1,ModItem=new KivotosMod.Globals.Classes.StudentWeaponClass()};return p;}
 static long Value(Terraria.Player p){long n=KivotosMod.Content.Tablet.TabletWallet.GetTotalCredits(p);foreach(var i in p.Change)n+=KivotosMod.Content.Tablet.TabletWallet.GetUnitValue(i.type)*i.stack;return n;}
 public static string Validate(){
  var p=PlayerWithWeapon();var item=p.inventory[0];
  Check(Math.Abs(KivotosMod.Content.Tablet.TabletWeaponUpgrade.DamageBonus(item,10)-1.2f)<.00001,"Damage must sum against base, not compound");
  Check(KivotosMod.Content.Tablet.TabletWeaponUpgrade.CritBonus(10)==13,"Crit increments");
  ((KivotosMod.Globals.Classes.StudentWeaponClass)item.ModItem).DamageUpgradeSteps[2]=.05f;
  Check(Math.Abs(KivotosMod.Content.Tablet.TabletWeaponUpgrade.DamageBonus(item,3)-.25f)<.00001,"Weapon-specific increment");
  Check(KivotosMod.Content.Tablet.TabletWeaponUpgrade.CommonMaterials(6).Length==4,"Three mech soul recipe");
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.BossUnlocked(6),"Mechs locked");
  Terraria.NPC.downedMechBoss1=Terraria.NPC.downedMechBoss2=true;
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.BossUnlocked(6),"Two mechs insufficient");
  Terraria.NPC.downedMechBoss3=true;Check(KivotosMod.Content.Tablet.TabletWeaponUpgrade.BossUnlocked(6),"All mechs unlock");
  p.inventory[1]=new(){type=3,stack=10};p.inventory[2]=new(){type=5,stack=6};p.inventory[3]=new(){type=6,stack=10};
  string msg;Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,100,1,out msg),"Boss gate");Check(Value(p)==100000,"Rejected upgrade charged");
  Terraria.NPC.downedBoss1=true;
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,100,2,out msg),"Stale level");
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,999,1,out msg),"Wrong type");
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,58,100,1,out msg),"Invalid slot");
  Terraria.Main.netMode=1;Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,100,1,out msg),"Client authority");Terraria.Main.netMode=0;
  Check(KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,100,1,out msg),"Valid upgrade");
  Check(item.Data.Level==2&&Value(p)==90000&&p.inventory[2].stack==0&&p.inventory[3].stack==0,"Upgrade atomic debit");
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.TryUpgrade(p,0,100,1,out msg),"Replay upgrade");Check(Value(p)==90000,"Replay charged");
  var tag=new Terraria.ModLoader.IO.TagCompound();item.Data.SaveData(item,tag);var loaded=new KivotosMod.Content.Tablet.TabletWeaponLevel();loaded.LoadData(item,tag);Check(loaded.Level==2,"Level persistence");
  using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))item.Data.NetSend(item,w);m.Position=0;loaded.NetReceive(item,new BinaryReader(m));Check(loaded.Level==2,"Level network round trip");}
  p.inventory[2]=new(){type=5,stack=0};item.Data.Level=1;
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.CanUpgrade(p,item),"Missing materials");
  item.Data.Level=10;
  var damage=new Terraria.StatModifier{Value=1};KivotosMod.Globals.Configs.KivotosClientConfig.Instance.StudentWeaponDamageMultiplier=20;
  item.Data.ModifyWeaponDamage(item,p,ref damage);Check(Math.Abs(damage.Value-45)<.0001,"Scaled base plus cumulative upgrade");
  float crit=4;item.Data.ModifyWeaponCrit(item,p,ref crit);Check(crit==17,"Actual crit hook");
  KivotosMod.Globals.Configs.KivotosClientConfig.Instance.StudentWeaponDamageMultiplier=1;
  loaded.LoadData(item,new Terraria.ModLoader.IO.TagCompound());Check(loaded.Level==1,"Old saves start at level one");
  using(var invalid=new MemoryStream(new byte[]{255}))loaded.NetReceive(item,new BinaryReader(invalid));Check(loaded.Level==10,"Network level bounds");
  Check(!KivotosMod.Content.Tablet.TabletWeaponUpgrade.CanUpgrade(p,item),"Max level");
  for(int trial=1;trial<=150;trial++){
   var w=new Terraria.Player();w.inventory[0]=new(){type=101,stack=trial*57};w.inventory[1]=new(){type=102,stack=trial%7};w.inventory[2]=new(){type=4,stack=1};
   long before=Value(w);long cost=trial*371;Check(KivotosMod.Content.Tablet.TabletWallet.TrySpend(w,cost),"Wallet spend");Check(Value(w)==before-cost,"Currency conservation");
   Check(w.inventory[0].stack==0&&w.inventory[1].stack==0,"Remaining custom currency not converted");foreach(var coin in w.Change)Check(coin.type>=1&&coin.type<=4,"Custom change");
  }
  var poor=new Terraria.Player();poor.inventory[0]=new(){type=101,stack=50};Check(!KivotosMod.Content.Tablet.TabletWallet.TrySpend(poor,51)&&Value(poor)==50,"Insufficient funds mutated inventory");
  var pref=new KivotosMod.Content.Tablet.TabletUiPreferencesPlayer();pref.ToggleStudent(KivotosMod.Content.Tablet.TabletStudentId.Mika);pref.ToggleProduct(new(){ItemType=100});pref.SaveData(tag);
  var restored=new KivotosMod.Content.Tablet.TabletUiPreferencesPlayer();restored.LoadData(tag);Check(restored.FavoriteStudents.Contains(11)&&restored.IsFavorite(new(){ItemType=100}),"Favorites persistence");restored.ToggleStudent(KivotosMod.Content.Tablet.TabletStudentId.Mika);Check(restored.FavoriteStudents.Count==0,"Unfavorite");
  return "PASS: production upgrade rules, authority and replay guards, level save/net round trip, 150 currency-conservation cases, rejected transactions, favorite persistence (engine stubbed).";
 }
}
