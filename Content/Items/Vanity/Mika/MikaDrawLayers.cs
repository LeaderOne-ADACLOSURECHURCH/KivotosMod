using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Mika;

internal static class MikaArtLayout
{
    internal const float HaloBobAmplitude = 1.25f;
    internal const float HaloBobPeriod = 2.4f;
    internal static float HaloOffset(double seconds)
        => HaloBobAmplitude * (float)System.Math.Sin(seconds * System.Math.PI * 2 / HaloBobPeriod);

    internal static Vector2 CanvasOrigin(int frameHeight, SpriteEffects effects)
    {
        // Reference canvas: the native 40x56 body starts at (13,18)
        // in 63x74, or (13,12) in 63x68. Convert its (20,28) anchor
        // to that canvas, rather than borrowing another character's padding.
        Vector2 origin = new(33, frameHeight - 28);
        if ((effects & SpriteEffects.FlipHorizontally) != 0) origin.X = 63 - origin.X;
        if ((effects & SpriteEffects.FlipVertically) != 0) origin.Y = frameHeight - origin.Y;
        return origin;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class MikaTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !StudentSpriteVariants.MikaBody2x2(info.drawPlayer) && !info.drawPlayer.dead && !info.drawPlayer.ghost && MikaVanitySet.BodyVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    protected override void Draw(ref PlayerDrawSet info)
    {
        Player p = info.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Mika/MikaTorso", AssetRequestMode.ImmediateLoad);
        Vector2 pos = new(
            (int)(info.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(info.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f));
        pos += p.bodyPosition + new Vector2(p.bodyFrame.Width / 2f, p.bodyFrame.Height / 2f);
        info.DrawDataCache.Add(new DrawData(_texture.Value, pos, p.bodyFrame, info.colorArmorBody,
            p.bodyRotation, info.bodyVect, 1f, info.playerEffect, 0f) { shader = info.cBody });
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class MikaWingLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _decoration, _flight, _decoration2x2;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && MikaVanitySet.FullSetVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    protected override void Draw(ref PlayerDrawSet info)
    {
        Player p = info.drawPlayer;
        var state = p.GetModPlayer<MikaPlayer>();
        if (StudentSpriteVariants.MikaBody2x2(info.drawPlayer) && !state.Flapping)
        {
            StudentSpriteDraw.Strip(ref info, ref _decoration2x2, "KivotosMod/Content/Items/Vanity/Mika/Mika2x2Wings");
            return;
        }
        _decoration ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Mika/MikaDecorWings", AssetRequestMode.ImmediateLoad);
        _flight ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Mika/MikaFlight", AssetRequestMode.ImmediateLoad);
        Texture2D tex = state.Flapping ? _flight.Value : _decoration.Value;
        int height = state.Flapping ? 68 : 74;
        int frame = state.Flapping ? state.FlightFrame : System.Math.Clamp(p.bodyFrame.Y / p.bodyFrame.Height, 0, 19);
        Vector2 pos = new(
            (int)(info.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(info.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f));
        pos += p.bodyPosition + new Vector2(p.bodyFrame.Width / 2f, p.bodyFrame.Height / 2f);
        info.DrawDataCache.Add(new DrawData(tex, pos, new Rectangle(0, frame * height, 63, height),
            info.colorArmorBody, p.bodyRotation, MikaArtLayout.CanvasOrigin(height, info.playerEffect), 1f, info.playerEffect, 0f)
            { shader = state.HasFlightWings ? info.cWings : info.cBody });
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class MikaBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture, _texture2x2;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && MikaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, ModContent.GetInstance<MikaWingLayer>());
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
    {
        if (StudentSpriteVariants.Mika2x2(info.drawPlayer))
        {
            StudentSpriteDraw.Strip(ref info, ref _texture2x2, "KivotosMod/Content/Items/Vanity/Mika/Mika2x2BackHair", head: true);
            return;
        }
        Player p = info.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Mika/MikaBackHair", AssetRequestMode.ImmediateLoad);
        Vector2 pos = info.helmetOffset + new Vector2(
            (int)(info.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(info.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f)) + p.headPosition + info.headVect;
        info.DrawDataCache.Add(new DrawData(_texture.Value, pos, p.bodyFrame, info.colorArmorHead,
            p.headRotation, info.headVect, 1f, info.playerEffect, 0f) { shader = info.cHead });
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class MikaHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture, _halo2x2;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && MikaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
    {
        if (StudentSpriteVariants.Mika2x2(info.drawPlayer))
        {
            StudentSpriteDraw.Halo(ref info, ref _halo2x2, "KivotosMod/Content/Items/Vanity/Mika/Mika2x2Halo", 4, 24);
            return;
        }
        Player p = info.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Mika/MikaHalo", AssetRequestMode.ImmediateLoad);
        Vector2 pos = info.helmetOffset + new Vector2(
            (int)(info.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(info.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f)) + p.headPosition + info.headVect;
        pos.Y += MikaArtLayout.HaloOffset(Main.GlobalTimeWrappedHourly) * p.gravDir;
        // Full-bright tint keeps the source halo luminous in darkness, while
        // respecting the fade on shadow/afterimage draw passes.
        Color glowColor = Color.White * (1f - info.shadow);
        info.DrawDataCache.Add(new DrawData(_texture.Value, pos, new Rectangle(0, 0, 63, 74),
            glowColor, p.headRotation, MikaArtLayout.CanvasOrigin(74, info.playerEffect), 1f, info.playerEffect, 0f) { shader = info.cHead });
    }
}
