using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace KivotosMod.Globals.Classes;

public sealed class StudentAmmoProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private int _ammo;
    private bool _rocket, _bounced;

    internal static int RocketType(int item)
    {
        if (AmmoID.Sets.SpecificLauncherAmmoProjectileMatches.TryGetValue(ItemID.RocketLauncher, out var matches) &&
            matches.TryGetValue(item, out int projectile))
            return projectile;
        return ProjectileID.RocketI + ContentSamples.ItemsByType[item].shoot;
    }

    internal void SetAmmo(Projectile projectile, int type, int category)
    {
        if (type <= 0 || type >= ProjectileID.Count)
            return;
        _rocket = category == AmmoID.Rocket;
        _ammo = type;
        if (type == ProjectileID.MoonlordBullet)
        {
            projectile.penetrate = projectile.maxPenetrate = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = -1;
        }
        projectile.netUpdate = true;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
    {
        bitWriter.WriteBit(_ammo != 0);
        if (_ammo == 0) return;
        bitWriter.WriteBit(_rocket);
        writer.Write((ushort)_ammo);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        if (!bitReader.ReadBit()) { _ammo = 0; _rocket = false; return; }
        _rocket = bitReader.ReadBit();
        _ammo = reader.ReadUInt16();
        if (_ammo == ProjectileID.MoonlordBullet)
        {
            projectile.penetrate = projectile.maxPenetrate = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = -1;
        }
    }

    public override void AI(Projectile projectile)
    {
        if (_ammo != ProjectileID.ChlorophyteBullet || projectile.numUpdates != 0)
            return;
        var target = FindTarget(projectile, 400);
        if (target == null) return;
        float speed = projectile.velocity.Length();
        if (speed <= .001f) return;
        float current = projectile.velocity.ToRotation();
        float turn = MathHelper.WrapAngle((target.Center-projectile.Center).ToRotation()-current);
        projectile.velocity = (current+Math.Clamp(turn,-.18f,.18f)).ToRotationVector2()*speed;
    }

    private static NPC FindTarget(Projectile projectile, float range)
    {
        NPC target = null;
        float best = range * range;
        foreach (var npc in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(projectile.Center, npc.Center);
            if (distance >= best || !npc.CanBeChasedBy(projectile) ||
                !Collision.CanHitLine(projectile.Center, 1, 1, npc.position, npc.width, npc.height))
                continue;
            best = distance;
            target = npc;
        }
        return target;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        if (_ammo is not (ProjectileID.MeteorShot or ProjectileID.NanoBullet) || _bounced)
            return true;
        _bounced = true;
        projectile.velocity = new Vector2(
            projectile.velocity.X == oldVelocity.X ? oldVelocity.X : -oldVelocity.X,
            projectile.velocity.Y == oldVelocity.Y ? oldVelocity.Y : -oldVelocity.Y);
        if (_ammo == ProjectileID.NanoBullet)
        {
            var target = FindTarget(projectile, 50 * 16);
            if (target != null) projectile.velocity = projectile.DirectionTo(target.Center) * oldVelocity.Length();
            projectile.damage = Math.Max(1, (int)(projectile.damage * .66f));
        }
        projectile.netUpdate = true;
        return false;
    }

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (_ammo == ProjectileID.ChlorophyteBullet && projectile.maxPenetrate != 1)
        {
            _ammo = 0;
            projectile.netUpdate = true;
        }
        (int buff, int time) = _ammo switch
        {
            ProjectileID.CursedBullet => (BuffID.CursedInferno, 420),
            ProjectileID.IchorBullet => (BuffID.Ichor, 600),
            ProjectileID.VenomBullet => (BuffID.Venom, 600),
            ProjectileID.NanoBullet => (BuffID.Confused, 180),
            ProjectileID.GoldenBullet => (BuffID.Midas, 120),
            _ => (0, 0)
        };
        if (buff != 0) target.AddBuff(buff, time);
        if (_ammo == ProjectileID.MoonlordBullet)
            projectile.damage = Math.Max(1, (int)(projectile.damage * .96f));
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (_ammo == 0) return;
        if (_rocket)
        {
            RocketImpact(projectile);
            return;
        }
        if (projectile.owner != Main.myPlayer) return;
        var source = projectile.GetSource_Death();
        if (_ammo == ProjectileID.CrystalBullet)
        {
            for (int i = 0; i < 3; i++)
                Projectile.NewProjectile(source, projectile.Center, Main.rand.NextVector2CircularEdge(5, 5),
                    ProjectileID.CrystalShard, Math.Max(1, projectile.damage / 2), 0, projectile.owner);
        }
        else if (_ammo == ProjectileID.PartyBullet)
        {
            for (int i = 0; i < 10; i++)
                Dust.NewDust(projectile.Center, 4, 4, DustID.Confetti);
        }
        else if (_ammo == ProjectileID.ExplosiveBullet)
        {
            var burst = Projectile.NewProjectileDirect(source, projectile.Center, Vector2.Zero,
                ProjectileID.ExplosiveBullet, projectile.damage, projectile.knockBack, projectile.owner);
            burst.timeLeft = 1;
            burst.tileCollide = false;
        }
    }

    private void RocketImpact(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Point tile = projectile.Center.ToTileCoordinates();
            switch (_ammo)
            {
                case ProjectileID.WetRocket:
                    projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(tile, 3f, DelegateMethods.SpreadWater);
                    break;
                case ProjectileID.LavaRocket:
                    projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(tile, 3f, DelegateMethods.SpreadLava);
                    break;
                case ProjectileID.HoneyRocket:
                    projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(tile, 3f, DelegateMethods.SpreadHoney);
                    break;
                case ProjectileID.DryRocket:
                    projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(tile, 3.5f, DelegateMethods.SpreadDry);
                    break;
            }
        }
        if (projectile.owner != Main.myPlayer) return;
        int radius = _ammo switch
        {
            ProjectileID.RocketII => 3,
            ProjectileID.RocketIV => 5,
            ProjectileID.MiniNukeRocketII => 7,
            ProjectileID.ClusterRocketII => 3,
            _ => 0
        };
        if (radius > 0)
        {
            var center = projectile.Center;
            var tile = center.ToTileCoordinates();
            int left = Math.Max(0, tile.X - radius), right = Math.Min(Main.maxTilesX, tile.X + radius + 1);
            int top = Math.Max(0, tile.Y - radius), bottom = Math.Min(Main.maxTilesY, tile.Y + radius + 1);
            bool walls = projectile.ShouldWallExplode(center, radius, left, right, top, bottom);
            projectile.ExplodeTiles(center, radius, left, right, top, bottom, walls);
        }
        if (_ammo is ProjectileID.ClusterRocketI or ProjectileID.ClusterRocketII)
        {
            int fragments = _ammo == ProjectileID.ClusterRocketI ? ProjectileID.ClusterFragmentsI : ProjectileID.ClusterFragmentsII;
            for (int i = 0; i < 6; i++)
                Projectile.NewProjectile(projectile.GetSource_Death(), projectile.Center, Main.rand.NextVector2CircularEdge(6, 6),
                    fragments, Math.Max(1, projectile.damage / 3), 0, projectile.owner);
        }
    }
}
