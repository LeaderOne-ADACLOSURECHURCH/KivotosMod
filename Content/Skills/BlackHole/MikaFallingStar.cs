using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

public sealed class MikaFallingStar : ModProjectile
{
    public override string Texture => "KivotosMod/Assets/Skills/MikaDomain/T40";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 13;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 22;
        Projectile.timeLeft = 270;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile.rotation += .17f;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (var npc in Main.ActiveNPCs)
        {
            if (!MikaExDomain.CanStrike(npc) || !Projectile.Hitbox.Intersects(npc.Hitbox))
                continue;
            Projectile.Kill();
            break;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity) => Main.netMode != NetmodeID.MultiplayerClient;

    public override void OnKill(int timeLeft)
    {
        if (timeLeft <= 0) return;
        int owner = (int)Projectile.ai[0];
        if (Main.netMode != NetmodeID.MultiplayerClient && owner >= 0 && owner < Main.maxPlayers && Main.player[owner].active)
            MikaExDomain.StrikeArea(Main.player[owner], Projectile.Center, 5 * 16f, (int)Projectile.ai[1]);
        if (Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
        for (int i = 0; i < 24; i++)
        {
            var dust = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch,
                Main.rand.NextVector2CircularEdge(7, 7), 0, Color.Plum, 1.4f);
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = TextureAssets.Projectile[Type].Value;
        Vector2 origin = texture.Size() * .5f;
        for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
        {
            if (Projectile.oldPos[i] == Vector2.Zero) continue;
            float strength = 1 - (i + 1f) / (Projectile.oldPos.Length + 1);
            Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;
            Main.EntitySpriteDraw(texture, pos, null, new Color(190, 75, 220, 0) * strength,
                Projectile.rotation - .17f * i, origin, .18f * strength, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
            Color.White, Projectile.rotation, origin, .19f, SpriteEffects.None);
        return false;
    }
}
