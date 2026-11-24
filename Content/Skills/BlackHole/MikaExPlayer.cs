using System.IO;
using KivotosMod.Content.Items.Tools;
using KivotosMod.Content.Items.Weapons.Ranged.StudentWeapons.Trinity;
using KivotosMod.Content.Projs.Ranged.StudentWeapons.Trinity;
using KivotosMod.Content.Tablet;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

public sealed class MikaExPlayer : ModPlayer
{
    internal const byte ActionMessage = 15;
    internal const byte StateMessage = 16;
    private const byte Activate = 0, Hit = 1, Crystal = 2, VisualSettings = 3, Scatter = 4, Grenade = 5, DamageSetting = 6;
    private static ModKeybind _key;
    private float _sentDamageScale;
    internal int Charge;
    internal bool Equipped => Player.HeldItem.ModItem is MikaWeapon &&
        Player.HeldItem.GetGlobalItem<TabletWeaponLevel>().Level == 10;

    public override void Load()
    {
        if (!Main.dedServ) _key = KeybindLoader.RegisterKeybind(Mod, "MikaEX", "X");
    }
    public override void Unload() => _key = null;
    public override void OnEnterWorld() => Charge = 0;
    public override void PlayerDisconnect() => Charge = 0;
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (newPlayer && Player.whoAmI == toWho) MikaExDomain.Sync(toWho);
    }
    public override void UpdateDead() => Charge = 0;

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (_key?.JustPressed != true || Player.dead || Player.ghost || Player.mouseInterface ||
            Main.blockInput || Main.drawingPlayerChat || Main.editSign || Main.editChest)
            return;
        if (!(MikaExDomain.Active ? MikaExDomain.Owner == Player.whoAmI && MikaExDomain.State.CanCastManually &&
            MikaExDomain.State.Reserve > 0 : !MikaExDomain.Occupied && Equipped && Charge == MikaExState.MaxCharge))
            return;
        SendOpeningAction(Activate);
    }

    internal void UseOpeningGrenade()=>SendOpeningAction(Grenade);

    private void SendOpeningAction(byte action)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = NewAction(action);
            MikaDomainTuning.Committed.Write(packet);
            _sentDamageScale=MikaExDomain.DamageScale;
            packet.Write(_sentDamageScale);
            packet.Send();
        }
        else MikaExDomain.Press(Player,MikaDomainTuning.Committed,MikaExDomain.DamageScale,action==Grenade);
    }

    public override void PostUpdate()
    {
        if(Main.netMode!=NetmodeID.MultiplayerClient || !MikaExDomain.Occupied ||
            MikaExDomain.Owner!=Player.whoAmI || Player.whoAmI!=Main.myPlayer)return;
        float value=MikaExDomain.DamageScale;
        if(value.Equals(_sentDamageScale))return;
        _sentDamageScale=value;
        var packet=NewAction(DamageSetting);
        packet.Write(value);
        packet.Send();
    }

    internal void SendVisualSettings()
    {
        if (!MikaExDomain.Active || MikaExDomain.Owner != Player.whoAmI) return;
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        var packet = NewAction(VisualSettings);
        MikaDomainTuning.Committed.Write(packet);
        packet.Send();
    }

    internal void UseCrystal()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) NewAction(Crystal).Send();
        else if (Main.netMode == NetmodeID.SinglePlayer) MikaExDomain.UseCrystal(Player);
    }

    internal void UseScatterSword()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) NewAction(Scatter).Send();
        else if (Main.netMode == NetmodeID.SinglePlayer) MikaExDomain.UseScatterSword(Player);
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone > 0) ReportHit(target, null);
    }

    public override void OnHitNPCWithProj(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone > 0 && projectile.ModProjectile is not MikaFallingStar) ReportHit(target, projectile);
    }

    private void ReportHit(NPC target, Projectile projectile)
    {
        if (Player.whoAmI != Main.myPlayer || target.friendly || target.townNPC)
            return;
        bool mika = projectile?.ModProjectile is MikaWeaponBullet && projectile.ai[2] >= 10;
        if (MikaExDomain.Active)
        {
            if (!MikaExDomain.InRange(Player)) return;
        }
        else
        {
            if (mika) MikaDomainStars.Hit(projectile.Center);
            if (!mika || Charge >= MikaExState.MaxCharge) return;
        }

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            MikaExDomain.RecordHit(Player, target, mika);
            return;
        }

        var packet = NewAction(Hit);
        packet.Write((short)target.whoAmI);
        packet.Write(mika ? projectile.identity : -1);
        packet.Send();
    }

    internal static void ReceiveAction(BinaryReader reader, int sender)
    {
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers)
            return;
        var player = Main.player[sender];
        if (!player.active || player.dead || player.ghost) return;
        switch (reader.ReadByte())
        {
            case Activate:
                MikaExDomain.Press(player,MikaVisualSettings.Read(reader),reader.ReadSingle());
                break;
            case Grenade:
                if(player.HeldItem.ModItem is MikaOpeningGrenade)
                    MikaExDomain.Press(player,MikaVisualSettings.Read(reader),reader.ReadSingle(),debug:true);
                break;
            case DamageSetting:
                if(MikaExDomain.Owner==sender)MikaExDomain.SetDamageScale(reader.ReadSingle());
                break;
            case VisualSettings:
                if (MikaExDomain.Active && MikaExDomain.Owner == sender)
                    MikaExDomain.UpdateVisualSettings(MikaVisualSettings.Read(reader));
                break;
            case Crystal:
                if (player.HeldItem.ModItem is MikaDomainCrystal) MikaExDomain.UseCrystal(player);
                break;
            case Scatter:
                MikaExDomain.UseScatterSword(player);
                break;
            case Hit:
                int targetId = reader.ReadInt16(), identity = reader.ReadInt32();
                if (targetId < 0 || targetId >= Main.maxNPCs) return;
                NPC target = Main.npc[targetId];
                if (target.friendly || target.townNPC) return;
                bool mika = identity >= 0 && MikaExDomain.ValidateMikaHit(player, target, identity);
                if (identity >= 0 && !mika) return;
                MikaExDomain.RecordHit(player, target, mika);
                break;
        }
    }

    private ModPacket NewAction(byte action)
    {
        var packet = Mod.GetPacket();
        packet.Write(ActionMessage);
        packet.Write(action);
        return packet;
    }

    public override void PostUpdateEquips()
    {
        if (!MikaExDomain.InRange(Player)) return;
        if (Player.wingsLogic == 0)
            Player.wingsLogic = ContentSamples.ItemsByType[ItemID.AngelWings].wingSlot;
        Player.wingTimeMax = System.Math.Max(Player.wingTimeMax, 100);
        Player.wingTime = Player.wingTimeMax;
        bool host = MikaExDomain.Owner == Player.whoAmI;
        Player.endurance += Main.masterMode ? (host ? .50f : .25f) : (host ? .30f : .15f);
    }
}
