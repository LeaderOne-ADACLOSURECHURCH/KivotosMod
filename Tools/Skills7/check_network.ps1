$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = [IO.File]::ReadAllText((Join-Path $root 'Content/Skills/WeaponShieldPlayer.cs'))
$start=$source.IndexOf('    internal static void HandleRequest(')
$end=$source.IndexOf('    public override void SyncPlayer(', $start)
$hooks=$source.Substring($start,$end-$start)
$stub=@"
using System;using System.IO;
namespace Terraria.ID { public static class NetmodeID { public const int Server=2,MultiplayerClient=1; } }
namespace Terraria {
 public static class Main { public static int netMode,maxPlayers=2; public static Player[] player={new(){whoAmI=0},new(){whoAmI=1}}; }
 public class Player { public bool active=true,dead,ghost; public int whoAmI; public KivotosMod.Content.Skills.WeaponShieldPlayer State=new(); public T GetModPlayer<T>() where T:class {State.Player=this;return State as T;} }
}
namespace KivotosMod {
 public class Packet:BinaryWriter { public static byte[] Last; public Packet():base(new MemoryStream()){} public void Send(int toWho=-1){Last=((MemoryStream)BaseStream).ToArray();} }
 public class KivotosMod { public static KivotosMod Instance=new(); public Packet GetPacket()=>new(); }
}
namespace KivotosMod.Content.Skills {using Terraria;using Terraria.ID;
 public class WeaponShieldPlayer {public Player Player; public bool Unlocked; public WeaponShieldState Shield=new();
"@
$probe=@"
}
public static class NetworkProbe {
 static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Request(byte action,int damage=0,byte direction=0,int sender=0){using var m=new MemoryStream();using var w=new BinaryWriter(m);w.Write(action);w.Write(damage);w.Write(direction);m.Position=0;WeaponShieldPlayer.HandleRequest(new BinaryReader(m),sender);}
 public static void Run(){
  Main.netMode=2;var owner=Main.player[0].GetModPlayer<WeaponShieldPlayer>();
  Request(0);Require(!owner.Shield.Active,"Locked weapon activated");
  owner.Unlocked=true;Request(0);Require(owner.Shield.Health==100,"Valid activation failed");
  Request(1,40,9);Require(owner.Shield.Health==60,"Server failed to absorb owner's hit");
  Request(0);Require(owner.Shield.Health==60,"Repeat activation refilled shield");
  Request(1,100,12);Require(owner.Shield.Health==60,"Invalid direction accepted");
  Request(1,100,0,5);Require(owner.Shield.Health==60,"Invalid sender accepted");
  byte[] snapshot=global::KivotosMod.Packet.Last;
  owner.Shield=new();Main.netMode=1;
  using(var m=new MemoryStream(snapshot)){var r=new BinaryReader(m);Require(r.ReadByte()==12,"Wrong state packet ID");WeaponShieldPlayer.ReceiveState(r);}
  Require(owner.Shield.Health==60 && owner.Shield.CooldownLeft==1200 && owner.Shield.HitDirection==9,"Snapshot roundtrip changed shield state");
  Request(1,100);Require(owner.Shield.Health==60,"Client processed server request handler");
  Main.netMode=2;Request(1,100);Require(!owner.Shield.Active && owner.Shield.BreakTicks==44,"Server break missing");
  Require(!Main.player[1].GetModPlayer<WeaponShieldPlayer>().Shield.Active,"Other player modified");
 }
}
}
"@
# Qualify test references without changing the production hooks.
$probe=$probe.Replace('Main.', 'Terraria.Main.')
Add-Type -TypeDefinition ($stub+$hooks+$probe+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $root 'Content/Skills/WeaponShieldState.cs')).Replace('using System;','').Replace('namespace KivotosMod.Content.Skills;','namespace KivotosMod.Content.Skills {')+'}')
[KivotosMod.Content.Skills.NetworkProbe]::Run()
'PASS: production shield packet handlers: server authority, locked weapon, repeat/refill guard, hit validation, sender identity, player isolation, snapshot byte roundtrip, break state (engine stubbed).'
