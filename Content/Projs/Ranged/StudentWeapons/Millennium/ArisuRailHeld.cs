using System;
using KivotosMod.Content.Items.Weapons.Ranged.StudentWeapons.Millennium;
using KivotosMod.Globals.Methods;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Projs.Ranged.StudentWeapons.Millennium;

public sealed class ArisuRailHeld : ModProjectile
{
    internal const int ChargeTicks = 120;
    private const int ManaInterval = 10;
    private const int ManaStep = 10;
    internal const int ExMultiplier = 13;
    private ArisuWeapon _weapon;
    private bool _fired;
    private SlotId _voice = SlotId.Invalid;
    public override string Texture => ArisuRailVisuals.Art + "HeldCharge";
    public override bool ShouldUpdatePosition() => false;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = ChargeTicks;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        bool local = Projectile.owner == Main.myPlayer;
        if (!player.active || player.dead || player.ghost || player.HeldItem.ModItem is not ArisuWeapon weapon)
        {
            Projectile.Kill();
            return;
        }

        _weapon ??= weapon;
        if (local && (!ReferenceEquals(_weapon, weapon) || !Main.mouseRight || player.CCed ||
            player.noItems || player.mouseInterface || Main.blockInput || Main.drawingPlayerChat))
        {
            Projectile.Kill();
            if (ReferenceEquals(_weapon, weapon)) player.itemTime = player.itemAnimation = 0;
            return;
        }

        int tick = ++Projectile.frameCounter;
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true);
        if (local)
        {
            float angle = (Main.MouseWorld - player.Center).ToRotation();
            Vector2 aim = (tick == 1 ? angle : Utils.AngleLerp(Projectile.velocity.ToRotation(), angle, .12f))
                .ToRotationVector2();
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0001f) Projectile.netUpdate = true;
            Projectile.velocity = aim;
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        player.ChangeDir(Projectile.velocity.X < 0 ? -1 : 1);
        player.itemRotation = (Projectile.velocity * player.direction).ToRotation();
        player.ControlPlayerArm(Projectile.rotation);
        player.itemAnimation = Math.Max(2, player.itemAnimation);
        player.itemTime = Math.Max(2, player.itemTime);

        if (!Main.dedServ)
        {
            if (tick == 1) _voice = ArisuRailVisuals.PlayVoice(player, true);
            if (tick is >= 21 and <= 85 && tick % 4 == 0)
            {
                var muzzle = Projectile.Center + Projectile.velocity * 64;
                var offset = Main.rand.NextVector2CircularEdge(112, 112);
                Dust dust = Dust.NewDustPerfect(muzzle + offset, DustID.FireworksRGB,
                    -offset * .14f, 0, new Color(135, 205, 255), .85f);
                dust.noGravity = true;
            }
        }
        if (!local) return;
        if (tick == 1)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ProjectileType<ArisuRailShade>(), 0, 0, Projectile.owner, Projectile.identity);
        if (tick == 30) ArisuRailVisuals.Shake(Projectile.Center, 2, 20, PiOver2);
        if (tick == 54) ArisuRailVisuals.Shake(Projectile.Center, 3, 10, 0);
        if (tick >= 59 && tick % 2 != 0)
            ArisuRailVisuals.Shake(Projectile.Center, 2, 2, Main.rand.NextFloat(TwoPi));

        if (tick % ManaInterval == 0 && player.CheckMana(player.HeldItem, ManaStep, pay: true))
        {
            // ReSoLS pays 10 through CheckMana and then applies another 10.
            player.statMana = Math.Max(0, player.statMana - ManaStep);
            player.manaRegenDelay = (int)player.maxRegenDelay;
        }

        if (tick >= ChargeTicks && weapon.SpendEnergy()) Fire(player);
    }

    private void Fire(Player player)
    {
        _fired = true;
        var item = player.HeldItem;
        int damage = player.GetWeaponDamage(item) * ExMultiplier;
        int floor = (int)player.GetTotalDamage(item.DamageType).ApplyTo(item.damage) * 2;
        Vector2 aim = Projectile.velocity;
        Vector2 offsetAim = aim.RotatedBy(-.05 * player.direction);
        var source = player.GetSource_ItemUse(item);
        Projectile.NewProjectile(source, Projectile.Center - offsetAim * 12, aim * 30,
            ProjectileType<ArisuRailBeam>(), damage, item.knockBack, Projectile.owner, floor);
        Projectile.NewProjectile(source, Projectile.Center + offsetAim * 112, aim,
            ProjectileType<ArisuRailFlash>(), 0, 0, Projectile.owner, 1);
        Projectile.NewProjectile(source, Projectile.Center, aim, ProjectileType<ArisuRailRecoil>(),
            0, 0, Projectile.owner, 60, 1);
        Projectile.NewProjectile(source, Projectile.Center, aim, ProjectileType<ArisuRailStorage>(),
            0, 0, Projectile.owner, 3);
        player.itemTime = player.itemAnimation = 60;
        Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        if (!Main.dedServ && !_fired && Projectile.frameCounter < ChargeTicks &&
            SoundEngine.TryGetActiveSound(_voice, out var voice))
            voice.Stop();
    }

    public override void Unload() => ArisuRailVisuals.Unload();

    public override bool PreDraw(ref Color lightColor)
    {
        int frame = Math.Min(59, Projectile.frameCounter / 2);
        ArisuRailVisuals.DrawGun(Projectile, "Charge", "GlowCharge", 60, frame, lightColor);
        return false;
    }
}

public sealed class ArisuRailRecoil : ModProjectile
{
    private bool Ex => Projectile.ai[1] > 0;
    public override string Texture => ArisuRailVisuals.Art + "HeldNormal";
    public override bool ShouldUpdatePosition() => false;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 60;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.HeldItem.ModItem is not ArisuWeapon)
        {
            Projectile.Kill();
            return;
        }
        if (Projectile.frameCounter == 0) Projectile.timeLeft = Math.Max(1, (int)Projectile.ai[0]);
        Projectile.frameCounter++;
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true);

        float progress = Clamp((Projectile.frameCounter - 1) / Math.Max(1, Projectile.ai[0]), 0, 1);
        float time = Ex ? progress : Clamp(progress / .65f, 0, 1);
        float kick = Ex ? .05f : .03f;
        float strength = time < kick ? time / kick : (1 - time) / (1 - kick);
        if (!Ex && time >= kick) strength *= strength;
        Projectile.rotation = Projectile.velocity.ToRotation() - strength * (Ex ? .1f : .28f) * player.direction;
        Projectile.frame = Math.Min(29, (int)(progress * 30));

        if (Ex && Projectile.frameCounter == 9) ArisuRailVisuals.Play("ExShot", Projectile.Center, 1.3f, .1f);
        if (progress >= (Ex ? 14f : 16f) / 60 && Projectile.localAI[0] == 0)
        {
            Projectile.localAI[0] = 1;
            ArisuRailVisuals.Play("Recoil", Projectile.Center, Ex ? .2f : .5f);
        }
        if (Projectile.owner == Main.myPlayer && Projectile.frameCounter == (Ex ? 14 : 2))
            ArisuRailVisuals.Shake(Projectile.Center, Ex ? 7 : 3, 35, Projectile.rotation);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Projectile.frameCounter == 0) Projectile.rotation = Projectile.velocity.ToRotation();
        ArisuRailVisuals.DrawGun(Projectile, Ex ? "Ex" : "Normal",
            Ex ? "GlowEx" : Projectile.ai[2] > 0 ? "GlowNormalPowered" : "GlowNormal", 30, Projectile.frame, lightColor);
        return false;
    }
}
