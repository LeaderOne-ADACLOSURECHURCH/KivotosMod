using System;
using System.Collections.Generic;
using System.IO;
using KivotosMod.Content.Projs.Ranged.StudentWeapons.Trinity;
using KivotosMod.Content.Items.Tools;
using KivotosMod.Globals.Configs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

public sealed class MikaExDomain : ModSystem
{
    internal const float Radius = 500 * 16f;
    private const byte Snapshot = 0, Charge = 1, Spark = 2, Dps = 3;
    internal static MikaExState State { get; } = new();
    internal static int Owner { get; private set; } = -1;
    internal static int Serial { get; private set; }
    internal static int ResonanceSeed { get; private set; }
    internal static MikaVisualSettings Visual { get; private set; } = new();
    internal static bool Active => Owner >= 0 && State.Active;
    internal static bool Occupied => Owner >= 0;
    internal static int OpeningTick { get; private set; } = -1;
    internal static Vector2 OpeningCenter { get; private set; }
    private static bool _openingAborted;
    private static float _damageScale=1;
    private static int _fallCount;
    private static readonly Dictionary<(int Player, int Projectile, int Target), ulong> MikaHits = new();

    internal static bool InRange(Player player) => Active && Main.player[Owner].active &&
        player.active && !player.dead && !player.ghost &&
        Within(Main.player[Owner].Center, player.Center, Radius);
    internal static bool Within(Vector2 center, Vector2 point, float radius) =>
        Vector2.DistanceSquared(center, point) <= radius * radius;
    internal static bool VisualRange(Player player,Vector2 location) =>
        Active && Main.player[Owner].active && player.active && Within(Main.player[Owner].Center, location, Radius);

    public override void OnWorldLoad() => Reset();
    public override void OnWorldUnload() => Reset();
    private static void Reset()
    {
        Owner = -1;
        OpeningTick=-1;
        _openingAborted=false;
        Serial = ResonanceSeed = _fallCount = 0;
        State.Clear();
        MikaHits.Clear();
        MikaDomainStars.Reset();
    }

    internal static float DamageScale => KivotosClientConfig.Instance?.StudentWeaponDamageMultiplier ?? 1;
    internal static void SetDamageScale(float value)=>_damageScale=float.IsFinite(value)?Math.Clamp(value,.3f,20f):1;

    internal static void Press(Player player,MikaVisualSettings visual,float damageScale,bool debug=false)
    {
        if (Active)
        {
            if (Owner == player.whoAmI && State.Press())
            {
                SetDamageScale(damageScale);
                StartResonance();
            }
            return;
        }
        if(Occupied)return;
        var ex = player.GetModPlayer<MikaExPlayer>();
        if (!debug && (!ex.Equipped || ex.Charge != MikaExState.MaxCharge)) return;
        Owner = player.whoAmI;
        OpeningTick=0;
        OpeningCenter=player.Center;
        _openingAborted=false;
        SetDamageScale(damageScale);
        Visual = visual;
        Serial++;
        MikaDomainStars.Reset();
        ex.Charge=MikaExState.MaxCharge;
        Sync();
    }

    internal static void UpdateVisualSettings(MikaVisualSettings settings)
    {
        Visual = settings;
        Sync();
    }

    internal static void UseCrystal(Player player)
    {
        if (Active && Owner == player.whoAmI &&
            player.HeldItem.ModItem is MikaDomainCrystal && State.UseCrystal())
            StartResonance();
    }

    internal static void UseScatterSword(Player player)
    {
        if (!Active || Owner != player.whoAmI || State.ResonanceTime >= 0 ||
            player.HeldItem.ModItem is not MikaDomainScatterSword) return;
        if (MikaDomainStars.ScheduleSharedScatter(State.DomainTime+MikaExState.ScatterTick,restart:false))
            Sync();
    }

    private static void StartResonance()
    {
        ResonanceSeed = Main.rand.Next();
        _fallCount = MikaDomainStars.ActiveCount;
        MikaDomainStars.ScheduleSharedScatter(State.DomainTime + MikaExState.ScatterTick);
        Sync();
    }

    private static void Stop()
    {
        State.Clear();
        Owner = -1;
        OpeningTick=-1;
        MikaDomainStars.Reset();
        Sync();
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 120 == 0 && MikaHits.Count != 0)
        {
            var stale = new List<(int, int, int)>();
            foreach (var hit in MikaHits)
                if (Main.GameUpdateCount - hit.Value > 200) stale.Add(hit.Key);
            foreach (var key in stale) MikaHits.Remove(key);
        }
        if (!Occupied) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if(OpeningTick>=0)OpeningTick=Math.Min(OpeningTick+1,MikaExState.OpeningEnd);
            if(Active)State.DomainTime++;
            return;
        }
        Player host = Main.player[Owner];
        if(OpeningTick>=0)
        {
            OpeningTick++;
            if(OpeningTick<=MikaExState.OpeningDuration && (!host.active || host.dead || host.ghost))
                _openingAborted=true;
            if(OpeningTick==MikaExState.OpeningDuration && !_openingAborted)
            {
                State.Charge=MikaExState.MaxCharge;
                State.Press();
                foreach(Player member in Main.ActivePlayers)
                    member.GetModPlayer<MikaExPlayer>().Charge=0;
                Sync();
                return;
            }
            if(OpeningTick>=MikaExState.OpeningEnd)
            {
                if(!Active){Stop();return;}
                OpeningTick=-1;
                Sync();
            }
        }
        if (!Active) return;
        if (!host.active || host.dead || host.ghost)
        {
            Stop();
            return;
        }
        int oldStart = State.ResonanceStart;
        State.Advance();
        if (!State.Active)
        {
            Stop();
            return;
        }
        bool started = State.ResonanceStart != oldStart && State.ResonanceStart >= 0;
        if (started)
            StartResonance();
        int tick = State.ResonanceTime;
        if (tick >= MikaExState.ImpactTick && tick <= MikaExState.LastPulseTick)
        {
            ClearHostileProjectiles(host.Center);
            int damage = State.DamageAt(tick);
            if (damage > 0) StrikeArea(host, host.Center, Radius, damage, vary: true);
        }
        if (tick == MikaExState.StarfallTick) SpawnStarfall(host);
        bool starsChanged = MikaDomainStars.AdvanceShared(State.DomainTime);
        if (!started && (starsChanged || State.DomainTime % 120 == 0 || oldStart != State.ResonanceStart))
            Sync();
    }

    internal static void RecordHit(Player player, NPC target, bool mika)
    {
        if (!Active)
        {
            if(Occupied && player.whoAmI==Owner)return;
            if (!mika) return;
            var ex = player.GetModPlayer<MikaExPlayer>();
            int value = Math.Min(MikaExState.MaxCharge, ex.Charge + Main.rand.Next(1, 3));
            if (value == ex.Charge) return;
            ex.Charge = value;
            if (Main.netMode == NetmodeID.Server)
            {
                var packet = Packet(Charge);
                packet.Write((byte)value);
                packet.Send(player.whoAmI);
            }
            return;
        }
        if (!InRange(player) || !Within(Main.player[Owner].Center, target.Center, Radius) ||
            (State.ResonanceTime >= MikaExState.ScatterTick && State.ResonanceTime < MikaExState.ScatterTick + 60))
            return;
        bool host = player.whoAmI == Owner;
        if (!host && !Main.rand.NextBool()) return;
        int count = host ? (mika ? Main.rand.Next(4, 8) : Main.rand.Next(1, 4)) : Main.rand.Next(1, 3);
        count = MikaDomainStars.QueueSharedHit(State.DomainTime, count);
        if (count <= 0) return;
        int seed = Main.rand.Next();
        if (Main.netMode == NetmodeID.SinglePlayer)
            MikaDomainStars.AddSparks(target.Center, count, seed, 0);
        else
        {
            var packet = Packet(Spark);
            packet.Write(Serial);
            packet.Write(State.DomainTime);
            packet.Write(target.Center.X);
            packet.Write(target.Center.Y);
            packet.Write((byte)count);
            packet.Write(seed);
            packet.Send();
        }
    }

    internal static bool ValidateMikaHit(Player player, NPC target, int identity)
    {
        var key = (player.whoAmI, identity, target.whoAmI);
        ulong now = Main.GameUpdateCount;
        if (MikaHits.TryGetValue(key, out ulong last) && now - last < 10)
            return false;
        var nearby = target.Hitbox;
        nearby.Inflate(256, 256);
        foreach (var projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != player.whoAmI || projectile.identity != identity ||
                projectile.ModProjectile is not MikaWeaponBullet || projectile.ai[2] < 10 ||
                !nearby.Contains(projectile.Center.ToPoint()))
                continue;
            MikaHits[key] = now;
            return true;
        }
        return false;
    }

    private static void ClearHostileProjectiles(Vector2 center)
    {
        foreach (var projectile in Main.ActiveProjectiles)
            if (projectile.hostile && Within(center, projectile.Center, Radius))
                projectile.Kill();
    }

    internal static void StrikeArea(Player player, Vector2 center, float radius, int damage, bool vary = false)
    {
        Span<bool> struck = stackalloc bool[Main.maxNPCs];
        struck.Clear();
        int total = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!CanStrike(npc) || !Within(center, npc.Center, radius))
                continue;
            int healthOwner = npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;
            if (struck[healthOwner]) continue;
            struck[healthOwner] = true;
            float scale=Main.netMode==NetmodeID.SinglePlayer?DamageScale:_damageScale;
            if(!float.IsFinite(scale))scale=1;
            int amount = vary ? (int)MathF.Round(damage*Math.Clamp(scale,.3f,20f)) + Main.rand.Next(-200,201) : damage;
            bool crit=vary && Main.rand.Next(100)<5;
            if(crit)amount=(int)MathF.Round(amount*1.5f);
            var hit = new NPC.HitInfo
            {
                Damage = amount,
                SourceDamage = amount,
                Crit = crit,
                DamageType = DamageClass.Ranged,
                HitDirection = npc.Center.X >= center.X ? 1 : -1
            };
            npc.PlayerInteraction(player.whoAmI);
            total += npc.StrikeNPC(hit, noPlayerInteraction: true);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(npc, hit);
        }
        if (total <= 0 || !player.active) return;
        if (Main.netMode == NetmodeID.SinglePlayer) player.addDPS(total);
        else if (Main.netMode == NetmodeID.Server)
        {
            var packet = Packet(Dps);
            packet.Write(total);
            packet.Send(player.whoAmI);
        }
    }

    internal static bool CanStrike(NPC npc) => npc.life > 0 && !npc.townNPC &&
        !NPCID.Sets.ActsLikeTownNPC[npc.type] && !npc.dontTakeDamage &&
        (!npc.immortal || npc.type == NPCID.TargetDummy);

    private static void SpawnStarfall(Player host)
    {
        var source = host.GetSource_Misc("MikaEXStarfall");
        for (int i = 0; i < _fallCount; i++)
        {
            Vector2 start = host.Center + new Vector2(Main.rand.NextFloat(-1250, 550), Main.rand.NextFloat(-1050, -650));
            start=Vector2.Clamp(start,new Vector2(160),new Vector2((Main.maxTilesX-10)*16,(Main.maxTilesY-10)*16));
            int type = ModContent.ProjectileType<MikaFallingStar>();
            Projectile.NewProjectile(source, start, new Vector2(9, 14) * Main.rand.NextFloat(.9f, 1.25f),
                type, 0, 0, Main.myPlayer, host.whoAmI, Main.rand.Next(280, 351));
        }
    }

    internal static void Sync(int toWho = -1)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Packet(Snapshot);
        packet.Write((short)Owner);
        packet.Write(Serial);
        if (Occupied)
        {
            packet.Write(Active);
            packet.Write((short)OpeningTick);
            packet.Write(OpeningCenter.X);
            packet.Write(OpeningCenter.Y);
            packet.Write(State.DomainTime);
            packet.Write(State.ResonanceStart);
            packet.Write((byte)State.Reserve);
            packet.Write(State.Final);
            packet.Write(ResonanceSeed);
            Visual.Write(packet);
            MikaDomainStars.WriteShared(packet);
        }
        packet.Send(toWho);
    }

    private static ModPacket Packet(byte kind)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write(MikaExPlayer.StateMessage);
        packet.Write(kind);
        return packet;
    }

    internal static void ReceiveState(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        var me = Main.LocalPlayer.GetModPlayer<MikaExPlayer>();
        switch (reader.ReadByte())
        {
            case Snapshot:
                int owner = reader.ReadInt16();
                int serial = reader.ReadInt32();
                if (owner < 0 || serial != Serial) MikaDomainStars.Reset();
                Owner = owner;
                Serial = serial;
                if (!Occupied) { State.Clear(); OpeningTick=-1; return; }
                State.Active=reader.ReadBoolean();
                OpeningTick=reader.ReadInt16();
                OpeningCenter=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                if(Active)me.Charge=0;
                else if(Owner==Main.myPlayer && !Main.LocalPlayer.dead)me.Charge=MikaExState.MaxCharge;
                State.DomainTime = reader.ReadInt32();
                State.ResonanceStart = reader.ReadInt32();
                State.Reserve = reader.ReadByte();
                State.Final = reader.ReadBoolean();
                ResonanceSeed = reader.ReadInt32();
                Visual = MikaVisualSettings.Read(reader);
                MikaDomainStars.ReadShared(reader);
                break;
            case Charge:
                me.Charge = reader.ReadByte();
                break;
            case Spark:
                int id = reader.ReadInt32(), time = reader.ReadInt32();
                var location = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                int count = reader.ReadByte(), seed = reader.ReadInt32();
                if (Active && Serial == id && InRange(Main.LocalPlayer))
                    MikaDomainStars.AddSparks(location, count, seed, Math.Max(0, State.DomainTime - time));
                break;
            case Dps:
                Main.LocalPlayer.addDPS(reader.ReadInt32());
                break;
        }
    }
}
