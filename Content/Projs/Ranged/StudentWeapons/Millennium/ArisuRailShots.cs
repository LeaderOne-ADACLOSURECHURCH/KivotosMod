using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Projs.Ranged.StudentWeapons.Millennium;

public sealed class ArisuRailShot : ModProjectile
{
    private bool Powered => Projectile.ai[0] > 0;
    public override string Texture => ArisuRailVisuals.Art + "Shot";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 15;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 64;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 3;
        Projectile.timeLeft = 200;
        Projectile.light = 1f;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        ArisuRailVisuals.Play("NormalShot", Projectile.Center, .5f, .1f);
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity += Projectile.velocity.SafeNormalize(Vector2.Zero) * .9f;
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .2f, .6f, 1);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => ArisuRailVisuals.SweptHit(Projectile, projHitbox, targetHitbox);

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Burst();
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => Burst();

    private void Burst()
    {
        if (Projectile.owner != Main.myPlayer) return;
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
            Projectile.velocity.SafeNormalize(Vector2.Zero) * .5f, ProjectileType<ArisuRailNormalBlast>(),
            Projectile.damage / 2, 0, Projectile.owner, Main.rand.NextFloat(TwoPi), Main.rand.NextFloat(.6f, 1));
    }

    public override void OnKill(int timeLeft)
    {
        Burst();
        if (Main.dedServ) return;
        foreach (var old in Projectile.oldPos)
        {
            if (old == Vector2.Zero) break;
            var dust = Dust.NewDustPerfect(old + Projectile.Size * .5f,
                Powered ? DustID.CorruptSpray : DustID.PurificationPowder, Projectile.velocity * .3f);
            dust.noGravity = true;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
        return true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ArisuRailVisuals.DrawNormalTrail(Projectile, Powered);
        ArisuRailVisuals.Draw("Shot", Projectile.Center, Projectile.rotation, 1, 0, Color.White, .5f,
            ArisuRailVisuals.Facing(Projectile.velocity));
        return false;
    }
}

public sealed class ArisuRailNormalBlast : ModProjectile
{
    private const int Life = 24;
    public override string Texture => ArisuRailVisuals.Art + "NormalBlast";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 64;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Life;
        Projectile.light = 1;
    }

    public override void AI()
    {
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, .9f, .5f, 1);
        Projectile.light *= .9f;
        if (Projectile.localAI[0]++ == 0)
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .8f, Pitch = .2f }, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int age = Life - Projectile.timeLeft;
        float angle = age >= 4 ? Projectile.ai[0] : 0;
        float scale = age >= 4 ? Projectile.ai[1] : 1;
        using var layers = new ArisuRailVisuals.EffectLayers();
        layers.Use(BlendState.NonPremultiplied);
        ArisuRailVisuals.Draw("NormalBlast", Projectile.Center, angle, 12, age / 2, Color.White, scale, framePadding: 1);
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw("NormalBlast", Projectile.Center, angle, 12, age / 2, Color.White * .6f, scale,
            framePadding: 1);
        return false;
    }
}

// ReSoLSProj2: this is a moving, wall-piercing projectile, with a swept hitbox and smoke.
public sealed class ArisuRailBeam : ModProjectile
{
    private const int Life = 2100;
    private const int SmokePeriod = 18;
    private const int SmokeStart = 8;
    private const int TrailPeriod = 6;
    private int Age => Life - Projectile.timeLeft;
    public override string Texture => ArisuRailVisuals.Art + "ChargedShot";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 32;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 450;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 128;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Life;
        Projectile.ArmorPenetration = 99999;
        Projectile.hide = true;
    }

    public override void OnSpawn(IEntitySource source) => Projectile.rotation = Projectile.velocity.ToRotation();

    public override void AI()
    {
        var direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Projectile.rotation = direction.ToRotation();
        Projectile.velocity += direction * .04f;
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, 1, .35f, .8f);
            float distance = Projectile.Distance(Main.LocalPlayer.Center);
            if (distance < 3840)
                ArisuRailVisuals.Shake(Projectile.Center, 16 * (1 - distance / 3840), 2, Projectile.rotation);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => ArisuRailVisuals.SweptHit(Projectile, projHitbox, targetHitbox);

    internal static int DamageAfterHit(int damage, int floor) => Math.Max(damage / 2, floor);

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner != Main.myPlayer) return;
        Projectile.damage = DamageAfterHit(Projectile.damage, (int)Projectile.ai[0]);
        Projectile.netUpdate = true;
        ArisuRailBurst.Spawn(Projectile);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info) => ArisuRailBurst.Spawn(Projectile);
    public override void OnKill(int timeLeft) => ArisuRailBurst.Spawn(Projectile);

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindProjectiles.Add(index);

    public override bool PreDraw(ref Color lightColor)
    {
        var head = Projectile.Center;
        var aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        using var layers = new ArisuRailVisuals.EffectLayers();
        for (int age = Age % TrailPeriod + TrailPeriod; age < 30 && age < Age; age += TrailPeriod)
        {
            var old = Projectile.oldPos[age];
            if (old == Vector2.Zero) continue;
            var center = old + Projectile.Size * .5f + aim * (.5f * age);
            float scale = 2 * Math.Min(1.5f, 1 + (Age - age) / (float)TrailPeriod * .07f);
            layers.Use(BlendState.AlphaBlend);
            ArisuRailVisuals.Draw("TrailSmoke", center, Projectile.oldRot[age], 15, age / 2, Color.White, scale);
            ArisuRailVisuals.Draw("TrailSmokeGlow", center, Projectile.oldRot[age], 15, age / 2, lightColor, scale);
        }

        float size = 2 * Lerp(.2f, 1, Math.Min(1, Age / 15f));
        // The imported sheet trims 456 blank pixels from the right. Keep its original pivot.
        var pivot = new Vector2(280, 64);
        int frame = Age / 2 % 7;
        layers.Use(BlendState.NonPremultiplied);
        ArisuRailVisuals.Draw("ChargedShot", head, Projectile.rotation, 7, frame, Color.White, size,
            pivot: pivot, framePadding: 1);
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw("ChargedShot", head, Projectile.rotation, 7, frame,
            Color.White * .6f, size, pivot: pivot, framePadding: 1);

        if (Age >= SmokeStart)
        {
            layers.Use(BlendState.AlphaBlend);
            int age = (Age - SmokeStart) % SmokePeriod;
            for (; age < 28 && age <= Age - SmokeStart; age += SmokePeriod)
            {
                ArisuRailVisuals.Draw("BeamSmoke", head, Projectile.rotation, 14, age / 2, Color.White, 2);
                ArisuRailVisuals.Draw("BeamSmokeGlow", head, Projectile.rotation, 14, age / 2, lightColor, 2,
                    framePadding: 1);
            }
        }
        return false;
    }
}

public sealed class ArisuRailBurst : ModProjectile
{
    private const int Life = 28;
    public override string Texture => ArisuRailVisuals.Art + "Blast";

    internal static void Spawn(Projectile source)
    {
        if (source.owner != Main.myPlayer || source.damage <= 0) return;
        Projectile.NewProjectile(source.GetSource_FromThis(), source.Center, source.velocity.SafeNormalize(Vector2.Zero) * .5f,
            ProjectileType<ArisuRailBurst>(), source.damage, 0, source.owner, Main.rand.NextFloat(TwoPi),
            Main.rand.NextFloat(.8f, 1.1f));
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 128;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Life;
        Projectile.ArmorPenetration = 99999;
        Projectile.hide = true;
    }

    public override void AI()
    {
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, .9f, .5f, 1f);
        if (Projectile.localAI[0]++ == 0)
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 2.2f, Pitch = -.1f }, Projectile.Center);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        => behindNPCs.Add(index);

    public override bool PreDraw(ref Color lightColor)
    {
        int frame = Math.Clamp((Life - Projectile.timeLeft) / 2, 0, 13);
        int age = Life - Projectile.timeLeft + 1;
        float scale = 2 * Projectile.ai[1];
        var center = Projectile.Center;
        float angle = Projectile.ai[0];
        using var layers = new ArisuRailVisuals.EffectLayers();
        layers.Use(BlendState.AlphaBlend);
        ArisuRailVisuals.Draw("Blast", center, angle, 14, frame, Color.White, scale);
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw("Blast", center, angle, 14, frame, Color.White * .6f, scale);
        layers.Use(BlendState.NonPremultiplied);
        ArisuRailVisuals.Draw("BlastSmoke", center, angle, 14, frame, Color.White, scale);
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw("BlastSmoke", center, angle, 14, frame, Color.White * .5f, scale);
        ArisuRailVisuals.Draw("BlastColor", center, angle, 14, frame, Color.White, scale);
        layers.Use(BlendState.AlphaBlend);
        ArisuRailVisuals.Draw("BlastFlash", center, angle, 14, frame, Color.White, scale);
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw("BlastFlash", center, angle, 14, frame, Color.White * .6f, scale);
        if (age <= 5)
            ArisuRailVisuals.Draw("Shockwave", center, age * .02f, 1, 0, Color.White, .02f + .15f * age);
        return false;
    }
}
