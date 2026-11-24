using System;using System.Collections.Generic;using System.IO;
namespace Terraria.ID {public static class NetmodeID {public const int MultiplayerClient=1;}}
namespace Terraria { public class Player {public bool active=true,dead;} public static class Main {public static int netMode;public static Player LocalPlayer=new();}public class Item {public int stack,maxStack;public static int Limit=1;public void SetDefaults(int id){maxStack=Limit;}public Item Clone()=>new(){maxStack=maxStack};}}
namespace Terraria.ModLoader.IO {public static class ItemIO {public static int Bytes=10;public static void Send(Terraria.Item i,BinaryWriter w,bool stack){w.Write(new byte[Bytes]);}}}
namespace KivotosMod.Content.Tablet {
 public class TabletCartLine {public int ItemType=1,Quantity;}
 public class TabletShopProduct {public int ItemType=1,PriceCredits=1,Stack=1;}
 public static class TabletShopCatalog {public static bool TryGetByItemType(int id,out TabletShopProduct product){product=new();return true;}}
 public static class TabletWallet {public static int Spends;public static bool TrySpend(Terraria.Player p,long amount){Spends++;return true;}}
 public static class TabletUiSystem {public static void HandleCheckoutResult(bool s,string m){}}
 public static class TabletNet {public static void SendCheckoutRequest(IReadOnlyList<TabletCartLine> lines){}}
}
namespace KivotosMod.Content.Tablet.Delivery {public static class TabletDeliveryOrderSystem {public static int Queued;public static void Enqueue(Terraria.Player p,List<Terraria.Item> items){Queued++;}}}
public static class CheckoutProbe {
 public static void Validate(){
  var p=new Terraria.Player();string message;
  bool Buy(int n)=>KivotosMod.Content.Tablet.TabletCheckoutService.TryCheckoutAndQueue(p,new[]{new KivotosMod.Content.Tablet.TabletCartLine{Quantity=n}},out message);
  if(Buy(129)||KivotosMod.Content.Tablet.TabletWallet.Spends!=0)throw new Exception("Oversized stack count charged");
  if(!Buy(128))throw new Exception("Boundary order rejected");
  Terraria.Item.Limit=9999;Terraria.ModLoader.IO.ItemIO.Bytes=32769;
  if(Buy(9999)||KivotosMod.Content.Tablet.TabletWallet.Spends!=1)throw new Exception("Oversized payload charged");
  Terraria.ModLoader.IO.ItemIO.Bytes=10;
  if(!Buy(9999)||KivotosMod.Content.Tablet.Delivery.TabletDeliveryOrderSystem.Queued!=2)throw new Exception("Stackable order failed");
 }
}
