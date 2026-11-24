using System;
using System.Collections.Generic;
using KivotosMod.Cores.ScreenEffect;
using KivotosMod.Globals.Configs;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace KivotosMod.Content.Projs.Ranged.StudentWeapons.Millennium;

internal static class ArisuRailVisuals
{
    internal const string Art = "KivotosMod/Assets/Texture/Projs/Arisu/";
    private const string Audio = "KivotosMod/Assets/Sounds/Students/Arisu/";
    private static readonly Dictionary<string, Asset<Texture2D>> Textures = new();
    private static Asset<Effect> _normalTrail, _poweredTrail;

    internal static Texture2D Texture(string name)
    {
        if (!Textures.TryGetValue(name, out var asset))
            Textures[name] = asset = ModContent.Request<Texture2D>(Art + name, AssetRequestMode.ImmediateLoad);
        return asset.Value;
    }

    internal static void Unload()
    {
        Textures.Clear();
        _normalTrail = _poweredTrail = null;
    }

    internal static void Draw(string name, Vector2 position, float rotation, int frames, int frame, Color color,
        float scale = 1, SpriteEffects effects = SpriteEffects.None, Vector2? pivot = null, int framePadding = 0)
    {
        Texture2D texture = Texture(name);
        int stride = texture.Height / frames;
        int height = stride - 2 * framePadding;
        Rectangle source = new(0, Math.Clamp(frame, 0, frames - 1) * stride + framePadding, texture.Width, height);
        Main.spriteBatch.Draw(texture, position - Main.screenPosition, source, color, rotation,
            pivot ?? new Vector2(texture.Width * .5f, height * .5f), scale, effects, 0);
    }

    internal static SpriteEffects Facing(Vector2 aim) => aim.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

    // These sprites share a world batch with vanilla projectiles. Keep state changes local to their passes.
    internal sealed class EffectLayers : IDisposable
    {
        private BlendState _blend;

        internal void Use(BlendState blend)
        {
            if (_blend == blend) return;
            var batch = Main.spriteBatch;
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.Transform);
            _blend = blend;
        }

        public void Dispose()
        {
            if (_blend != null) RestoreWorldBatch();
        }
    }

    private static void RestoreWorldBatch()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
    }

    internal static void DrawGun(Projectile projectile, string phase, string glow, int frames, int frame, Color light)
    {
        var effects = Facing(projectile.velocity);
        Draw("Held" + phase, projectile.Center, projectile.rotation, frames, frame, light, 2, effects);
        using var layers = new EffectLayers();
        layers.Use(BlendState.Additive);
        Draw(glow, projectile.Center, projectile.rotation, frames, frame, Color.White, 2, effects,
            framePadding: phase == "Charge" ? 1 : 0);
    }

    internal static bool SweptHit(Projectile projectile, Rectangle hitbox, Rectangle target)
    {
        if (hitbox.Intersects(target)) return true;
        if (projectile.oldPosition == Vector2.Zero || projectile.oldPosition == projectile.position) return false;
        float contact = 0;
        Vector2 halfSize = projectile.Size * .5f;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(),
            projectile.oldPosition + halfSize, projectile.Center, projectile.width, ref contact);
    }

    internal static void DrawNormalTrail(Projectile projectile, bool powered)
    {
        int count = 0;
        while (count < projectile.oldPos.Length && projectile.oldPos[count] != Vector2.Zero) count++;
        if (count < 2) return;

        Vector2 from = projectile.oldPos[0] + projectile.Size * .5f - Main.screenPosition;
        Vector2 offset = projectile.oldPos[count - 1] - projectile.oldPos[0];
        if (offset.LengthSquared() < 1) return;
        float height = 80f * count / projectile.oldPos.Length;
        var trail = Texture("Beam");
        var effect = powered
            ? (_poweredTrail ??= ModContent.Request<Effect>("KivotosMod/Assets/Effects/SupernovaPoweredTrail", AssetRequestMode.ImmediateLoad))
            : (_normalTrail ??= ModContent.Request<Effect>("KivotosMod/Assets/Effects/SupernovaTrail", AssetRequestMode.ImmediateLoad));
        var batch = Main.spriteBatch;
        batch.End();
        batch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.TransformationMatrix);
        batch.Draw(trail, from, null, Color.White, offset.ToRotation(), new Vector2(0, trail.Height * .5f),
            new Vector2(offset.Length() / trail.Width, height / trail.Height), SpriteEffects.FlipVertically, 0);
        RestoreWorldBatch();
    }

    internal static SlotId Play(string name, Vector2 position, float volume = 1, float pitch = 0)
        => Main.dedServ ? SlotId.Invalid
            : SoundEngine.PlaySound(new SoundStyle(Audio + name) { Volume = volume, Pitch = pitch }, position);

    internal static SlotId PlayVoice(Player player, bool charge)
    {
        if (Main.dedServ) return SlotId.Invalid;
        var language = KivotosClientConfig.Instance?.SupernovaVoice ?? SupernovaVoiceLanguage.Japanese;
        string folder = language switch
        {
            SupernovaVoiceLanguage.Japanese => "JP",
            SupernovaVoiceLanguage.Chinese => "CN",
            SupernovaVoiceLanguage.Korean when charge => "KR",
            _ => null
        };
        if (folder == null) return SlotId.Invalid;
        string clip = charge ? $"ExSkill_Level_{Main.rand.Next(1,4)}" : "CommonSkill";
        var style = new SoundStyle(Audio + folder + "/" + clip)
            { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };
        return SoundEngine.PlaySound(style, player.Center, sound =>
        {
            if (!player.active || player.dead) return false;
            sound.Position = player.Center;
            return true;
        });
    }

    internal static void Shake(Vector2 position, float strength, int duration, float direction)
    {
        if (!Main.dedServ)
            ScreenShakeSystem.AddScreenShakes(position, strength, duration, direction, useDistanceFade: false);
    }
}

public sealed class ArisuRailFlash : ModProjectile
{
    private bool Ex => Projectile.ai[0] > 0;
    public override string Texture => ArisuRailVisuals.Art + "FlashNormal";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 30;
    }

    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0)
        {
            if (Ex) Projectile.timeLeft = 27;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * .5f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .2f, .6f, 1f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        string name = Ex ? "FlashEx" : "FlashNormal";
        int frames = Ex ? 9 : 15;
        int frame = Math.Min(frames - 1, (int)Projectile.localAI[0] / (Ex ? 3 : 2));
        float angle = Projectile.localAI[0] == 0 ? Projectile.velocity.ToRotation() : Projectile.rotation;
        var effects = ArisuRailVisuals.Facing(Projectile.velocity);
        using var layers = new ArisuRailVisuals.EffectLayers();
        layers.Use(BlendState.NonPremultiplied);
        ArisuRailVisuals.Draw(name, Projectile.Center, angle, frames, frame, Ex ? Color.White * .78f : lightColor, 2, effects);
        if (Ex)
        {
            layers.Use(BlendState.AlphaBlend);
            ArisuRailVisuals.Draw("FlashExCore", Projectile.Center, angle, frames, frame, lightColor * .3f, 2, effects);
        }
        layers.Use(BlendState.Additive);
        ArisuRailVisuals.Draw(name + "Glow", Projectile.Center, angle, frames, frame, Color.White * .67f, 2, effects);
        return false;
    }
}

public sealed class ArisuRailStorage : ModProjectile
{
    private int Mark => (int)Projectile.ai[0] + 2;
    public override string Texture => ArisuRailVisuals.Art + "Storage0";
    public override bool ShouldUpdatePosition() => false;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 110;
    }

    public override void AI()
    {
        var player = Main.player[Projectile.owner];
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        if (Projectile.localAI[0]++ == 0 && Mark == 5) Projectile.timeLeft = 60;
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true);
        if (!Main.dedServ && Mark < 5)
        {
            if (Projectile.localAI[0] == 3)
            {
                ArisuRailVisuals.Play("StoreChime", Projectile.Center, .4f);
                if (Main.rand.NextBool(5)) ArisuRailVisuals.PlayVoice(player, false);
            }
            if (Projectile.localAI[0] == 51) ArisuRailVisuals.Play("Store", Projectile.Center, .6f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int age = Math.Max(0, (int)Projectile.localAI[0] - 1);
        int facing = Projectile.velocity.X < 0 ? -1 : 1;
        var effects = ArisuRailVisuals.Facing(Projectile.velocity);
        float rotation = facing < 0 ? Pi : 0;
        int markFrames = Mark == 5 ? 30 : 54;
        using var layers = new ArisuRailVisuals.EffectLayers();
        if (age < markFrames * 2)
        {
            var position = Projectile.Center - new Vector2(facing * 96, 96);
            layers.Use(BlendState.NonPremultiplied);
            ArisuRailVisuals.Draw("StorageMark" + Mark, position, rotation, markFrames, age / 2, Color.White, 2, effects,
                framePadding: 1);
            layers.Use(BlendState.Additive);
            ArisuRailVisuals.Draw("StorageMark" + Mark, position, rotation, markFrames, age / 2,
                Color.White * .3f, 2, effects, framePadding: 1);
        }
        if (Mark == 5) return false;

        if (age < 78)
        {
            int frame = age / 2;
            int sheet = frame / 10;
            string texture = "Storage" + sheet;
            var position = Projectile.Center - new Vector2(0, 198);
            int frames = sheet == 3 ? 9 : 10;
            int padding = sheet == 0 ? 1 : 0;
            layers.Use(BlendState.NonPremultiplied);
            ArisuRailVisuals.Draw(texture, position, rotation, frames, frame % 10, Color.White * .5f, 2, effects,
                framePadding: padding);
            layers.Use(BlendState.Additive);
            ArisuRailVisuals.Draw(texture, position, rotation, frames, frame % 10, Color.White * .6f, 2, effects,
                framePadding: padding);
        }
        if (age is >= 60 and < 100)
        {
            int frame = (age - 60) / 2;
            string texture = frame < 10 ? "StorageFinish" : "StorageFinishEnd";
            var position = Projectile.Center - new Vector2(0, 287);
            layers.Use(BlendState.NonPremultiplied);
            ArisuRailVisuals.Draw(texture, position, rotation, 10, frame % 10, Color.White * .8f, 2, effects);
            layers.Use(BlendState.Additive);
            ArisuRailVisuals.Draw(texture, position, rotation, 10, frame % 10, Color.White * .5f, 2, effects);
        }
        layers.Use(BlendState.Additive);
        DrawOrbs(age);
        return false;
    }

    private void DrawOrbs(int age)
    {
        var feet = Main.player[Projectile.owner].MountedCenter + new Vector2(0, 21);
        for (int trail = 15; trail >= 0; trail--)
        {
            int tick = age - trail - 2;
            if (tick < 0) continue;
            float travel = Math.Min(1, tick / 90f);
            float fade = Clamp((110 - age) / 20f, 0, 1) * (1 - trail / 16f);
            float offset = 80 * Lerp(1, 1.5f, Math.Min(1, travel * 2)) * MathF.Cos(travel * TwoPi * 2);
            var color = new Color(130, 220, 255) * (fade * (trail == 0 ? 1 : .65f));
            ArisuRailVisuals.Draw("StorageOrb", feet - new Vector2(offset, 0), 0, 1, 0, color, 2 * fade);
            ArisuRailVisuals.Draw("StorageOrb", feet + new Vector2(offset, 0), 0, 1, 0, color, 2 * fade);
        }
    }
}

public sealed class ArisuRailShade : ModProjectile
{
    private int _parentIndex = -1;
    private int _elapsed;
    private int _fade;
    private float _opacity;
    private SlotId _sound = SlotId.Invalid;
    public override string Texture => ArisuRailVisuals.Art + "Shade";
    public override bool ShouldUpdatePosition() => false;
    public override void SetStaticDefaults() => Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.hide = true;
        Projectile.timeLeft = 140;
    }

    private bool IsCharge(Projectile candidate)
        => candidate.active && candidate.owner == Projectile.owner && candidate.identity == (int)Projectile.ai[0]
            && candidate.type == ProjectileType<ArisuRailHeld>();

    public override void AI()
    {
        var player = Main.player[Projectile.owner];
        Projectile.Center = player.MountedCenter;
        if (Projectile.owner != Main.myPlayer) return;
        if (_elapsed++ == 0)
        {
            foreach (var projectile in Main.ActiveProjectiles)
                if (IsCharge(projectile)) { _parentIndex = projectile.whoAmI; break; }
            _sound = ArisuRailVisuals.Play("Charge", Projectile.Center, .8f);
        }
        if (_fade == 0 && _parentIndex >= 0 && IsCharge(Main.projectile[_parentIndex]))
        {
            _opacity = Math.Min(1, _elapsed / 90f);
            return;
        }
        if (SoundEngine.TryGetActiveSound(_sound, out var sound))
            sound.Volume = Math.Max(0, sound.Volume - .05f);
        if (++_fade >= 15) Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        if (!Main.dedServ && SoundEngine.TryGetActiveSound(_sound, out var sound)) sound.Stop();
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        => behindNPCs.Add(index);

    public override bool PreDraw(ref Color lightColor)
    {
        if (Projectile.owner == Main.myPlayer)
        {
            Texture2D shade = ArisuRailVisuals.Texture("Shade");
            int alpha = (int)(140 * _opacity * (1 - _fade / 15f));
            Main.spriteBatch.Draw(shade, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(0, 0, 0, alpha));
        }
        return false;
    }
}
