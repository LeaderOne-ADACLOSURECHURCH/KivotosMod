using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.ModLoader;
using KivotosMod.Globals.Classes;
using KivotosMod.Content.Tablet;

namespace KivotosMod.Content.Skills;

public sealed class WeaponShieldPlayer : ModPlayer
{
    internal static ModKeybind ShieldKey;
    public readonly WeaponShieldState Shield = new();
    internal bool Unlocked => Player.HeldItem.ModItem is StudentWeaponClass
        && Player.HeldItem.GetGlobalItem<TabletWeaponLevel>().Level >= 5;
    public override void Load()
    {
        if (!Main.dedServ) ShieldKey = KeybindLoader.RegisterKeybind(Mod, "WeaponShield", "Mouse3");
    }
    public override void Unload() => ShieldKey = null;
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!Unlocked || Player.dead || Player.ghost || Player.mouseInterface || Main.gameMenu
            || Main.blockInput || Main.drawingPlayerChat || Main.editSign || Main.editChest) return;
        if (ShieldKey?.JustPressed != true) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) SendRequest(0, 0, 0);
        else Shield.Activate();
    }
    public override void PostUpdate() => Shield.Tick();
    public override void UpdateDead() { Shield.Stop(); Shield.Tick(); }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.whoAmI == Main.myPlayer && Shield.Active)
            modifiers.ModifyHurtInfo += AbsorbHit;
    }
    private void AbsorbHit(ref Player.HurtInfo info)
    {
        if (!Shield.Active || info.Cancelled || info.Damage <= 0) return;
        Vector2 toward = new(-info.HitDirection, 0);
        if (info.DamageSource.TryGetCausingEntity(out Entity entity) && entity != null)
            toward = entity.Center - Player.Center;
        // Clock sectors: zero is 12 o'clock, increasing clockwise.
        byte direction = (byte)(((int)MathF.Round((MathF.Atan2(toward.Y, toward.X) + MathHelper.PiOver2) / MathHelper.TwoPi * 12) + 12) % 12);
        int incoming = info.Damage;
        int remaining = Shield.Absorb(incoming, direction);
        if (remaining == 0)
        {
            info.Cancelled = true;
            // A cancelled Hurt does not grant native hurt immunity; prevent contact damage every tick.
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 20);
        }
        else info.Damage = remaining;
        if (Main.netMode == NetmodeID.MultiplayerClient) SendRequest(1, incoming, direction);
    }
    private static void SendRequest(byte action, int damage, byte direction)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)11); packet.Write(action); packet.Write(damage); packet.Write(direction); packet.Send();
    }
    internal static void HandleRequest(BinaryReader reader, int sender)
    {
        byte action = reader.ReadByte(); int damage = reader.ReadInt32(); byte direction = reader.ReadByte();
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers) return;
        var player = Main.player[sender];
        if (!player.active || player.dead || player.ghost) return;
        var state = player.GetModPlayer<WeaponShieldPlayer>();
        if (action == 0) { if (state.Unlocked) state.Shield.Activate(); }
        else if (action == 1 && damage > 0 && direction < 12) state.Shield.Absorb(damage, direction);
        else return;
        state.SendState(-1);
    }
    private void SendState(int toWho)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)12); packet.Write((byte)Player.whoAmI);
        packet.Write(Shield.Health); packet.Write(Shield.TimeLeft); packet.Write(Shield.CooldownLeft);
        packet.Write(Shield.Age); packet.Write(Shield.HitTicks); packet.Write(Shield.BreakTicks); packet.Write(Shield.HitDirection);
        packet.Send(toWho);
    }
    internal static void ReceiveState(BinaryReader reader)
    {
        int index = reader.ReadByte();
        if (Main.netMode != NetmodeID.MultiplayerClient || index >= Main.maxPlayers) return;
        var shield = Main.player[index].GetModPlayer<WeaponShieldPlayer>().Shield;
        shield.Health = Math.Clamp(reader.ReadInt32(), 0, WeaponShieldState.Capacity);
        shield.TimeLeft = Math.Clamp(reader.ReadInt32(), 0, WeaponShieldState.Duration);
        shield.CooldownLeft = Math.Clamp(reader.ReadInt32(), 0, WeaponShieldState.Cooldown);
        shield.Age = Math.Max(0, reader.ReadInt32());
        shield.HitTicks = Math.Clamp(reader.ReadInt32(), 0, 30);
        shield.BreakTicks = Math.Clamp(reader.ReadInt32(), 0, 44);
        shield.HitDirection = (byte)(reader.ReadByte() % 12);
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode == NetmodeID.Server) SendState(toWho);
    }
}
